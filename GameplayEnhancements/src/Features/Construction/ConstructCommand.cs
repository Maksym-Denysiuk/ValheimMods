using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using HomeCheat.Kit;
using Splatform;
using UnityEngine;

namespace HomeCheat.Features.Construction
{
    // construct: builds a saved blueprint (BepInEx/config/homecheat/blueprints/) as if the player had built it with the hammer.
    // Milestone 1 (Maksym 2026-10-25): list, place at the crosshair, status, undo. The preview pillars, the flattening, the
    // material swap and the panel follow (card docs/Backlog/construct.md).
    // Placement: the centre of the footprint at the crosshair (or at your feet), turned to your facing snapped to 22.5°, the
    // lowest piece pivot on the ground there (card A5, no flattening yet). Pieces go bottom-up in batches, so what carries a
    // piece stands before it; every piece gets the creator and OnPlaced as in Player.PlacePiece and the ZDO mark hc_construct.
    public sealed class ConstructCommand : CommandBase
    {
        const int MaxPieces = 4000;
        const float MaxSide = 64f;          // metres, the larger side of the footprint
        const int MaxList = 50;
        const int BatchSize = 100;          // pieces per step
        const float BatchGap = 0.1f;        // seconds between steps
        const float CheckAfter = 5f;        // seconds after the last step: how many still stand
        const float BuildingMargin = 1f;    // keep-away from player-built pieces, metres past the build's half diagonal
        const float UndoRadius = 100f;      // a build without id= is looked for within this distance
        public const string KeyBuild = "hc_construct";   // ZDO mark: the build id <name>-<yyyyMMddHHmmss>

        string lastId = "", lastName = "";
        int lastPlaced;

        sealed class Job
        {
            public GameObject Prefab;
            public BlueprintPiece Piece;
            public Vector3 Pos;
            public Quaternion Rot;
        }

        sealed class Tally
        {
            public int Placed, Scaled, Signs, ExtraIgnored, Invalid;
        }

        public override string Name { get { return "construct"; } }
        protected override string Usage
        {
            get
            {
                return "construct step=list | step=place&name=<construction> [at=aim|self] [yaw=<world degrees, default your facing snapped to 22.5>]"
                    + " [overlap=0 (1 = build even with player-built pieces inside the footprint)] | step=status | step=undo [id=<build id, default the last>] [confirm=1]"
                    + " - builds a blueprint from BepInEx/config/homecheat/blueprints/ (a file = one construction, a subfolder = one construction of all its files);"
                    + " host only; caps " + MaxPieces + " pieces, footprint " + (int)MaxSide + " m a side, list " + MaxList;
            }
        }

        static string Dir { get { return Path.Combine(Path.Combine(Paths.ConfigPath, "homecheat"), "blueprints"); } }

        protected override void OnWorldExit()
        {
            lastId = ""; lastName = ""; lastPlaced = 0;
        }

        protected override string Execute(Args a)
        {
            string step = a.Str("step", "").ToLowerInvariant();
            if (step == "list") return List();
            if (step == "place") return Place(a);
            if (step == "status") return Status();
            if (step == "undo") return Undo(a);
            throw new CommandException("step= list, place, status or undo (op=help&cmd=construct)");
        }

        string List()
        {
            List<BlueprintEntry> all = BlueprintLibrary.Scan(Dir);
            if (all.Count == 0) return "0 constructions: put .blueprint files (or a folder of them) into BepInEx/config/homecheat/blueprints/";
            bool canCheck = ZNetScene.instance != null;
            StringBuilder sb = new StringBuilder();
            sb.Append(all.Count + " construction(s)" + (all.Count > MaxList ? ", first " + MaxList + " shown" : "")
                + (canCheck ? "" : "; load a world to check the prefabs"));
            for (int i = 0; i < all.Count && i < MaxList; i++)
            {
                BlueprintEntry e = all[i];
                float x0, x1, y0, y1, z0, z1;
                e.Bounds(out x0, out x1, out y0, out y1, out z0, out z1);
                sb.Append("\n").Append(e.Name).Append(" [").Append(e.Source).Append("]: ")
                  .Append(e.Parts.Count).Append(e.Parts.Count == 1 ? " part, " : " parts, ").Append(e.PieceCount).Append(" pieces, ")
                  .Append(Fmt.F(x1 - x0, 1)).Append(" x ").Append(Fmt.F(z1 - z0, 1)).Append(" x ").Append(Fmt.F(y1 - y0, 1)).Append(" m");
                if (e.Parts.Count == 1 && !string.IsNullOrEmpty(e.Parts[0].Creator)) sb.Append(", by ").Append(e.Parts[0].Creator);
                if (e.BadLines > 0) sb.Append(", ").Append(e.BadLines).Append(" bad line(s)");
                if (canCheck)
                {
                    List<string> missing = Missing(e);
                    if (missing.Count > 0)
                        sb.Append(", missing prefabs ").Append(missing.Count).Append(": ")
                          .Append(string.Join(" ", missing.GetRange(0, Math.Min(8, missing.Count)).ToArray())).Append(missing.Count > 8 ? " ..." : "");
                }
            }
            return sb.ToString();
        }

        // prefab names of the construction that this game does not know (modded pieces), each once
        static List<string> Missing(BlueprintEntry e)
        {
            List<string> missing = new List<string>();
            foreach (Blueprint b in e.Parts)
                foreach (BlueprintPiece pc in b.Pieces)
                    if (!missing.Contains(pc.Prefab) && Prefabs.Get(pc.Prefab) == null) missing.Add(pc.Prefab);
            return missing;
        }

        string Place(Args a)
        {
            Host.RequireServer();
            Player p = Players.Local;
            string name = a.Required("name", "op=construct&step=list shows the names");
            List<BlueprintEntry> all = BlueprintLibrary.Scan(Dir);
            BlueprintEntry e = BlueprintLibrary.Find(all, name);
            if (e == null)
            {
                List<string> names = new List<string>();
                foreach (BlueprintEntry x in all) names.Add(x.Name);
                throw new CommandException("no construction '" + name + "' (have: " + (names.Count == 0 ? "none" : string.Join(", ", names.ToArray())) + ")");
            }
            int count = Caps.Check("pieces", e.PieceCount, MaxPieces);
            if (count == 0) throw new CommandException(e.Name + " has no pieces");
            float x0, x1, y0, y1, z0, z1;
            e.Bounds(out x0, out x1, out y0, out y1, out z0, out z1);
            Caps.Check("footprint", Mathf.Max(x1 - x0, z1 - z0), MaxSide);

            string at = a.Str("at", "aim").ToLowerInvariant();
            bool aimed = true;
            Vector3 anchor;
            if (at == "self") anchor = p.transform.position;
            else if (at == "aim") anchor = Aim.Point(p, out aimed);
            else throw new CommandException("at must be aim or self");
            // a crosshair hit stands on the surface it hit (a rock, a floor); otherwise on the terrain (Maksym 2026-10-25)
            bool onSurface = at == "aim" && aimed;
            float groundY = onSurface ? anchor.y : Ground.Height(anchor);

            float yaw;
            if (a.Has("yaw")) yaw = a.Float("yaw", 0f);
            else
            {
                Vector3 f = p.transform.forward;
                yaw = Mathf.Round(Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg / 22.5f) * 22.5f;
            }
            yaw = Mathf.Repeat(yaw, 360f);
            Quaternion turn = Quaternion.Euler(0f, yaw, 0f);

            // the footprint centre goes to the anchor, the lowest pivot to the ground
            Vector3 centre = new Vector3((x0 + x1) * 0.5f, y0, (z0 + z1) * 0.5f);
            Vector3 origin = new Vector3(anchor.x, groundY, anchor.z);

            if (!a.Flag("overlap", false))
            {
                Vector3 mid = origin + Vector3.up * ((y1 - y0) * 0.5f);
                float reach = 0.5f * Mathf.Sqrt((x1 - x0) * (x1 - x0) + (z1 - z0) * (z1 - z0) + (y1 - y0) * (y1 - y0)) + BuildingMargin;
                Piece nearest;
                int n = Constructions.Near(mid, reach, out nearest);
                if (n > 0)
                    throw new CommandException(n + " player-built piece(s) inside the footprint, nearest " + Constructions.Describe(nearest, mid)
                        + ": aim elsewhere or add overlap=1");
            }

            List<Job> jobs = new List<Job>();
            List<string> missing = new List<string>();
            int skipped = 0;
            foreach (Blueprint b in e.Parts)
                foreach (BlueprintPiece pc in b.Pieces)
                {
                    GameObject prefab = Prefabs.Get(pc.Prefab);
                    if (prefab == null) { skipped++; if (!missing.Contains(pc.Prefab)) missing.Add(pc.Prefab); continue; }
                    Job j = new Job();
                    j.Prefab = prefab;
                    j.Piece = pc;
                    j.Pos = origin + turn * (new Vector3(pc.X, pc.Y, pc.Z) - centre);
                    j.Rot = turn * new Quaternion(pc.Qx, pc.Qy, pc.Qz, pc.Qw);
                    jobs.Add(j);
                }
            if (jobs.Count == 0) throw new CommandException("none of the " + count + " pieces is a prefab this game knows (" + string.Join(" ", missing.ToArray()) + ")");
            jobs.Sort(delegate (Job l, Job r) { return l.Pos.y.CompareTo(r.Pos.y); });

            string id = e.Name + "-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            long me = p.GetPlayerID();
            Tally t = new Tally();
            int batches = (jobs.Count + BatchSize - 1) / BatchSize;
            for (int bi = 0; bi < batches; bi++)
            {
                int from = bi * BatchSize, to = Mathf.Min(jobs.Count, from + BatchSize);
                Action stepAct = delegate { for (int i = from; i < to; i++) PlaceOne(jobs[i], id, me, t); };
                if (bi == 0) stepAct(); else Later(bi * BatchGap, stepAct);
            }
            string buildName = e.Name;
            int total = jobs.Count;
            Later((batches - 1) * BatchGap + CheckAfter, delegate
            {
                int standing = Loaded(id).Count;
                string msg = "construct: " + buildName + " " + standing + " of " + t.Placed + " standing";
                HomeApi.Log(msg + " (id " + id + ", " + total + " planned)");
                Notify.Center(msg);
            });
            lastId = id; lastName = e.Name; lastPlaced = total;

            return (batches == 1 ? "placed " + t.Placed : "placing " + total) + " of " + e.Name + " (" + count + " in the blueprint"
                + (skipped > 0 ? ", " + skipped + " skipped: unknown prefabs " + string.Join(" ", missing.ToArray()) : "") + ")"
                + " at " + Fmt.Vec(origin) + (at == "aim" ? (aimed ? " (crosshair surface, terrain there " + Fmt.F(Ground.Height(anchor), 1) + ")" : " (no crosshair hit, 8 m ahead, on the terrain)") : " (your position, on the terrain)")
                + ", yaw " + Fmt.F(yaw, 1) + ", id " + id
                + (batches > 1 ? ", " + batches + " batches over " + Fmt.F((batches - 1) * BatchGap, 1) + " s" : "")
                + (t.Scaled > 0 ? ", " + t.Scaled + " scaled" : "") + (t.Signs > 0 ? ", " + t.Signs + " sign text(s)" : "")
                + (t.ExtraIgnored > 0 ? ", extra data of " + t.ExtraIgnored + " piece(s) ignored" : "")
                + "; standing count in " + Fmt.F(CheckAfter, 0) + " s (op=construct&step=status)";
        }

        // one piece the way Player.PlacePiece makes it: creator, OnPlaced; no place effect per piece
        static void PlaceOne(Job j, string id, long me, Tally t)
        {
            GameObject go = (GameObject)UnityEngine.Object.Instantiate(j.Prefab, j.Pos, j.Rot);
            ZNetView nv = go.GetComponent<ZNetView>();
            if (nv == null || !nv.IsValid()) { UnityEngine.Object.Destroy(go); t.Invalid++; return; }
            nv.GetZDO().Set(KeyBuild, id);
            BlueprintPiece pc = j.Piece;
            if (Mathf.Abs(pc.Sx - 1f) > 0.001f || Mathf.Abs(pc.Sy - 1f) > 0.001f || Mathf.Abs(pc.Sz - 1f) > 0.001f)
            {
                Vector3 s = new Vector3(pc.Sx, pc.Sy, pc.Sz);
                go.transform.localScale = s;
                nv.GetZDO().Set(Trees.ScaleKey, s);
                t.Scaled++;
            }
            Piece piece = go.GetComponent<Piece>();
            if (piece != null) piece.SetCreator(me, PlatformManager.DistributionPlatform.LocalUser.PlatformUserID);
            WearNTear wnt = go.GetComponent<WearNTear>();
            if (wnt != null) wnt.OnPlaced();
            if (!string.IsNullOrEmpty(pc.Extra))
            {
                Sign sign = go.GetComponent<Sign>();
                if (sign != null) { sign.SetText(pc.Extra); t.Signs++; }
                else t.ExtraIgnored++;
            }
            t.Placed++;
        }

        // loaded objects with the build mark: of one build, or of every build when id is empty
        static List<ZNetView> Loaded(string id)
        {
            List<ZNetView> found = new List<ZNetView>();
            foreach (ZNetView nv in UnityEngine.Object.FindObjectsOfType<ZNetView>())
            {
                if (nv == null || !nv.IsValid()) continue;
                string v = nv.GetZDO().GetString(KeyBuild, "");
                if (v.Length == 0) continue;
                if (id.Length == 0 || v == id) found.Add(nv);
            }
            return found;
        }

        // build id -> loaded pieces
        static Dictionary<string, List<ZNetView>> Groups()
        {
            Dictionary<string, List<ZNetView>> g = new Dictionary<string, List<ZNetView>>();
            foreach (ZNetView nv in Loaded(""))
            {
                string v = nv.GetZDO().GetString(KeyBuild, "");
                List<ZNetView> l;
                if (!g.TryGetValue(v, out l)) { l = new List<ZNetView>(); g[v] = l; }
                l.Add(nv);
            }
            return g;
        }

        string Status()
        {
            Host.RequireWorld();
            Dictionary<string, List<ZNetView>> g = Groups();
            StringBuilder sb = new StringBuilder();
            if (lastId.Length > 0)
            {
                List<ZNetView> mine;
                int standing = g.TryGetValue(lastId, out mine) ? mine.Count : 0;
                sb.Append("last build ").Append(lastName).Append(" (id ").Append(lastId).Append("): ").Append(standing).Append(" of ").Append(lastPlaced).Append(" standing");
            }
            else sb.Append("no build in this session");
            sb.Append("; ").Append(g.Count).Append(" build(s) loaded around you");
            foreach (KeyValuePair<string, List<ZNetView>> kv in g)
                sb.Append("\n").Append(kv.Key).Append(": ").Append(kv.Value.Count).Append(" pieces near ").Append(Fmt.Vec(kv.Value[0].transform.position));
            return sb.ToString();
        }

        string Undo(Args a)
        {
            Host.RequireServer();
            Player p = Players.Local;
            Dictionary<string, List<ZNetView>> g = Groups();
            string id = a.Str("id", lastId);
            if (id.Length == 0)
            {
                // no id and no build in this session: the nearest loaded build within UndoRadius
                float best = UndoRadius;
                foreach (KeyValuePair<string, List<ZNetView>> kv in g)
                    foreach (ZNetView nv in kv.Value)
                    {
                        float d = Vector3.Distance(p.transform.position, nv.transform.position);
                        if (d < best) { best = d; id = kv.Key; }
                    }
                if (id.Length == 0) throw new CommandException("no build within " + (int)UndoRadius + " m (op=construct&step=status lists the loaded ones)");
            }
            List<ZNetView> pieces;
            if (!g.TryGetValue(id, out pieces) || pieces.Count == 0)
                throw new CommandException("no loaded pieces of build " + id + ": go near it (op=construct&step=status)");
            if (!a.Flag("confirm", false))
                return "would remove " + pieces.Count + " piece(s) of build " + id + " near " + Fmt.Vec(pieces[0].transform.position) + ": add confirm=1 (the ground stays as it is)";
            int removed = 0;
            foreach (ZNetView nv in pieces)
            {
                if (nv == null || !nv.IsValid()) continue;
                nv.ClaimOwnership();
                ZNetScene.instance.Destroy(nv.gameObject);
                removed++;
            }
            if (id == lastId) { lastId = ""; lastName = ""; lastPlaced = 0; }
            return "removed " + removed + " piece(s) of build " + id + " (loaded pieces only; the ground stays as it is)";
        }
    }
}
