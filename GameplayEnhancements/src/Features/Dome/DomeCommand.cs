// op=dome: a protected zone made of vanilla wards (guard_stone), placed as if Maksym built them with the hammer, so every
// player on the server sees and gets the same: the standard ward look and the vanilla ward rules, with no mod on their side.
// Build order steps 2-3 of the dome requirements: two points -> one ward at the centre, or a group of wards covering a
// larger zone; the group acts as one (on/off and the permitted list are copied to every ward by the host).
// Not here yet: the server monster guard (step 4), the owner panel and hover text (step 5), extra effects (step 6).
// All logic is host only: the wards are shared world data.
using System;
using System.Collections.Generic;
using System.Text;
using HomeCheat.Kit;
using Splatform;
using UnityEngine;

namespace HomeCheat.Features.DomeCommands
{
    public sealed class DomeCommand : CommandBase
    {
        const float MaxRadius = 150f;      // zone radius cap (m)
        const int MaxWards = 50;           // wards per dome cap
        const float BaseRadius = 0.6f;     // half the ward's base: where its edges are probed for slope and buildings
        const float MaxBaseDrop = 0.5f;    // largest height difference under the base edges before the spot is refused
        const float Sink = 0.05f;          // how far the ward sits below its lowest base edge, so no part floats
        const float BuildingMargin = 1.5f; // pieces are measured to their pivot (Kit.Constructions): this much extra
        const float SyncSeconds = 2f;      // how often the host copies a changed ward's state to its group
        const float RescanSeconds = 30f;   // how often the host looks for dome wards again (built or removed elsewhere)
        const int MaxListed = 8;           // spots named in an answer

        internal static DomeCommand Current;

        Dictionary<string, List<ZDO>> groups = new Dictionary<string, List<ZDO>>();
        bool syncRunning;
        float nextRescan;
        bool nextMarkIsEnd;

        public DomeCommand()
        {
            SubAction("step", "step=mark [point=start|end] (stand on a point: first the start, then the end; uses the coord points)"
                + " | step=build from=coords|x,z [to=x,z] [cost=0] [partial=0] [preview=0] (as without step)"
                + " | step=list (every dome: centre, radius, wards, on/off, permitted)"
                + " | step=remove [confirm=0] (the dome at the crosshair with all its wards; without confirm=1 only says what it would remove)",
                delegate (Args a) { return Step(a); });
        }

        public override string Name { get { return "dome"; } }

        protected override string Usage
        {
            get
            {
                return "dome from=coords | from=x,z&to=x,z [cost=0 (1 = take the ward materials from your inventory)] [partial=0 (1 = skip spots that"
                    + " cannot take a ward, leaving gaps)] [preview=0 (1 = only say where the wards would go)] - a protected zone of vanilla wards between two"
                    + " points on opposite edges (centre = midpoint, radius = half the distance, cap " + Fmt.F(MaxRadius, 0) + " m): one ward while the zone"
                    + " fits in one ward's radius, else a group of up to " + MaxWards + " wards without gaps; you are their creator, as with the hammer;"
                    + " refuses water, buildings, slopes over " + Fmt.F(MaxBaseDrop, 1) + " m under the base and other players' ward areas;"
                    + " a group acts as one (on/off and the permitted list copied to every ward by the host); host only";
            }
        }

        protected override string Execute(Args a)
        {
            if (!a.Has("from")) throw new CommandException("from required: from=coords (the coord start and end points) or from=x,z&to=x,z; or step=mark|list|remove");
            return Build(a);
        }

        string Step(Args a)
        {
            string step = a.Str("step", "").ToLowerInvariant();
            if (step == "mark") return Mark(a);
            if (step == "build") return Build(a);
            if (step == "list") return List();
            if (step == "remove") return Remove(a);
            throw new CommandException("step=" + step + " is unknown: mark, build, list or remove");
        }

        // ---- mark -----------------------------------------------------------------------------------

        string Mark(Args a)
        {
            Host.RequireWorld();
            Player p = Players.Local;
            string point = a.Str("point", nextMarkIsEnd ? Coords.End : Coords.Start).ToLowerInvariant();
            if (point != Coords.Start && point != Coords.End) throw new CommandException("point=" + point + ": start or end");
            Vector3 pos = p.transform.position;
            Coords.Set(point, pos);
            nextMarkIsEnd = point == Coords.Start;
            string line = "dome mark " + point + " = " + Fmt.Vec(pos) + " (your feet)";
            Vector3 s, e;
            if (point == Coords.Start || !Coords.Get(Coords.Start, out s) || !Coords.Get(Coords.End, out e))
                return line + "; next: op=dome&step=mark (the end point, on the opposite edge)";
            float cx, cz, r;
            DomeLayout.Zone(s.x, s.z, e.x, e.z, out cx, out cz, out r);
            string wards = "";
            PrivateArea area = WardArea(false);
            if (area != null) wards = " -> " + DomeLayout.Wards(cx, cz, r, area.m_radius).Count + " ward(s) of radius " + Fmt.F(area.m_radius, 1) + " m";
            return line + "; zone centre " + Fmt.F(cx, 1) + "," + Fmt.F(cz, 1) + ", radius " + Fmt.F(r, 1) + " m" + wards
                + "; build: op=dome&step=build&from=coords";
        }

        // ---- build ----------------------------------------------------------------------------------

        sealed class Spot
        {
            public DomePoint Lattice;
            public Vector3 Pos;
            public bool Ok;
            public string Why;
        }

        string Build(Args a)
        {
            Host.RequireServer();
            Player p = Players.Local;
            long me = p.GetPlayerID();

            Vector3 s, e;
            string from = a.Str("from", "coords").ToLowerInvariant();
            if (from == "coords")
            {
                s = Coords.Require(Coords.Start);
                e = Coords.Require(Coords.End);
            }
            else
            {
                s = ParseXZ("from", from);
                e = ParseXZ("to", a.Required("to", "to=x,z (the point on the opposite edge)"));
            }
            float cx, cz, radius;
            DomeLayout.Zone(s.x, s.z, e.x, e.z, out cx, out cz, out radius);
            if (radius < 1f) throw new CommandException("the two points are " + Fmt.F(radius * 2f, 1) + " m apart: mark two points on opposite edges of the zone");
            Caps.Check("radius", radius, MaxRadius);

            GameObject prefab = Prefabs.Require(DomeWards.WardPrefab, "(the vanilla ward)");
            PrivateArea wardArea = WardArea(true);
            float wr = wardArea.m_radius;
            List<DomePoint> lattice = DomeLayout.Wards(cx, cz, radius, wr);
            Caps.Check("wards", lattice.Count, MaxWards);
            float slack = lattice.Count == 1 ? Mathf.Max(0f, wr - radius) : DomeLayout.Nudge(wr);

            bool cost = a.Flag("cost", false);
            bool partial = a.Flag("partial", false);
            bool preview = a.Flag("preview", false);

            List<ZDO> allWards = DomeWards.AllWards();
            List<Spot> spots = new List<Spot>();
            int bad = 0;
            foreach (DomePoint lp in lattice)
            {
                Spot sp = FindSpot(lp, slack, me, wr, allWards);
                if (!sp.Ok) bad++;
                spots.Add(sp);
            }
            int good = spots.Count - bad;
            string zone = "centre " + Fmt.F(cx, 1) + "," + Fmt.F(cz, 1) + ", radius " + Fmt.F(radius, 1) + " m, ward radius " + Fmt.F(wr, 1) + " m";
            if (bad > 0 && (!partial || good == 0))
                throw new CommandException(bad + " of " + spots.Count + " ward spot(s) cannot take a ward, nothing placed (" + zone + ")"
                    + (good > 0 ? "; partial=1 places the other " + good + " and leaves gaps" : "") + ": " + Refusals(spots));

            Piece piece = prefab.GetComponent<Piece>();
            string paid = "";
            if (cost && !preview) paid = Pay(p, piece, good);

            if (preview)
                return "preview: " + good + " ward(s) would be placed" + (bad > 0 ? ", " + bad + " spot(s) skipped (gaps)" : "") + " (" + zone + ")"
                    + Lines(spots, true) + (bad > 0 ? "\n  skipped: " + Refusals(spots) : "");

            string id = "d" + DateTime.UtcNow.Ticks.ToString("x");
            if (id.Length > 9) id = "d" + id.Substring(id.Length - 8);
            Quaternion rot = Quaternion.Euler(0f, p.transform.eulerAngles.y, 0f);
            string profileName = Game.instance.GetPlayerProfile().GetName();
            List<ZDO> placed = new List<ZDO>();
            foreach (Spot sp in spots)
            {
                if (!sp.Ok) continue;
                ZDO z = Place(prefab, sp.Pos, rot, me, profileName);
                if (z == null) continue;
                z.Set(DomeWards.KeyDome, id);
                z.Set(DomeWards.KeyCx, cx);
                z.Set(DomeWards.KeyCz, cz);
                z.Set(DomeWards.KeyRadius, radius);
                z.Set(DomeWards.KeyCount, good);
                placed.Add(z);
            }
            if (placed.Count == 0) throw new CommandException("no ward could be created (the guard_stone prefab has no network view?)");

            // one state for the group from the start: the first ward's (a new ward is on when the prefab says so)
            WardState first = DomeWards.Read(placed[0]);
            string sig = first.Signature();
            foreach (ZDO z in placed)
            {
                if (DomeWards.Read(z).Signature() != sig) DomeWards.Write(z, first);
                z.Set(DomeWards.KeySig, sig);
            }
            groups[id] = placed;
            EnsureSync();

            return "dome " + id + ": placed " + placed.Count + " ward(s) (" + zone + ")" + (bad > 0 ? ", " + bad + " spot(s) skipped (gaps)" : "")
                + ", state " + (first.Enabled ? "on" : "off") + paid
                + (placed.Count > 1 ? "; one group: on/off and the permitted list are copied to every ward" : "")
                + Lines(spots, false) + (bad > 0 ? "\n  skipped: " + Refusals(spots) : "");
        }

        static Vector3 ParseXZ(string key, string text)
        {
            string[] parts = text.Split(',');
            float x, z;
            if (parts.Length != 2
                || !float.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x)
                || !float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out z))
                throw new CommandException(key + "=" + text + ": expected x,z (two numbers, a point as decimal mark)");
            return new Vector3(x, 0f, z);
        }

        // the vanilla ward's PrivateArea on its prefab (its radius is the asset's value, read live)
        static PrivateArea WardArea(bool require)
        {
            GameObject prefab = require ? Prefabs.Require(DomeWards.WardPrefab, "(the vanilla ward)") : Prefabs.Get(DomeWards.WardPrefab);
            PrivateArea area = prefab == null ? null : prefab.GetComponent<PrivateArea>();
            if (area == null && require) throw new CommandException(DomeWards.WardPrefab + " has no PrivateArea (game changed?)");
            return area;
        }

        // the lattice point itself, then up to 'slack' metres around it in 8 directions
        Spot FindSpot(DomePoint lp, float slack, long me, float wr, List<ZDO> allWards)
        {
            Spot sp = new Spot();
            sp.Lattice = lp;
            Vector3 pos;
            string why;
            if (CheckSpot(lp.X, lp.Z, me, wr, allWards, out pos, out why)) { sp.Ok = true; sp.Pos = pos; return sp; }
            sp.Why = why;
            float[] rings = { slack * 0.5f, slack };
            foreach (float ring in rings)
            {
                if (ring < 0.25f) continue;
                for (int i = 0; i < 8; i++)
                {
                    float ang = i * Mathf.PI * 0.25f;
                    string other;
                    if (CheckSpot(lp.X + Mathf.Cos(ang) * ring, lp.Z + Mathf.Sin(ang) * ring, me, wr, allWards, out pos, out other))
                    {
                        sp.Ok = true; sp.Pos = pos; sp.Why = null;
                        return sp;
                    }
                }
            }
            return sp;
        }

        bool CheckSpot(float x, float z, long me, float wr, List<ZDO> allWards, out Vector3 pos, out string why)
        {
            pos = new Vector3(x, 0f, z);
            why = null;
            if (Heightmap.FindHeightmap(pos) == null) { why = "terrain not loaded here (stand nearer the zone)"; return false; }
            float water = ZoneSystem.instance.m_waterLevel;
            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i <= 8; i++)
            {
                Vector3 probe = pos;
                if (i > 0)
                {
                    float ang = (i - 1) * Mathf.PI * 0.25f;
                    probe.x += Mathf.Cos(ang) * BaseRadius;
                    probe.z += Mathf.Sin(ang) * BaseRadius;
                }
                float h = Ground.Height(probe);
                if (h < water) { why = "on water (ground " + Fmt.F(water - h, 1) + " m below the water line)"; return false; }
                if (h < min) min = h;
                if (h > max) max = h;
            }
            if (max - min > MaxBaseDrop) { why = "slope too steep: the base edges differ by " + Fmt.F(max - min, 2) + " m, " + Fmt.F(max - min - MaxBaseDrop, 2) + " m over the limit " + Fmt.F(MaxBaseDrop, 1) + " m"; return false; }
            pos.y = min - Sink;

            Piece nearest;
            if (Constructions.Near(pos, BaseRadius + BuildingMargin, out nearest) > 0) { why = "on a building: " + Constructions.Describe(nearest, pos); return false; }
            if (Ground.IsBlocked(new Vector3(pos.x, max, pos.z), BaseRadius)) { why = "blocked by a tree, rock or object"; return false; }

            foreach (ZDO w in allWards)
            {
                long creator = DomeWards.Creator(w);
                if (creator == me || creator == 0L) continue;
                float d = DomeWards.DistanceXZ(w.GetPosition(), pos);
                if (d < wr * 2f) { why = "overlaps another player's ward area (ward at " + Fmt.F(w.GetPosition().x, 1) + "," + Fmt.F(w.GetPosition().z, 1) + ", " + Fmt.F(d, 1) + " m)"; return false; }
            }
            return true;
        }

        // creates the ward the way Player.PlacePiece does: creator, creator name, OnPlaced, place effect
        static ZDO Place(GameObject prefab, Vector3 pos, Quaternion rot, long me, string profileName)
        {
            GameObject go = (GameObject)UnityEngine.Object.Instantiate(prefab, pos, rot);
            ZNetView nv = go.GetComponent<ZNetView>();
            if (nv == null || !nv.IsValid()) { UnityEngine.Object.Destroy(go); return null; }
            Piece piece = go.GetComponent<Piece>();
            if (piece != null) piece.SetCreator(me, PlatformManager.DistributionPlatform.LocalUser.PlatformUserID);
            PrivateArea area = go.GetComponent<PrivateArea>();
            if (area != null) area.Setup(profileName);
            WearNTear wnt = go.GetComponent<WearNTear>();
            if (wnt != null) wnt.OnPlaced();
            if (piece != null && piece.m_placeEffect != null) piece.m_placeEffect.Create(pos, rot, go.transform, 1f, -1);
            return nv.GetZDO();
        }

        // takes count x the ward's recipe from the inventory, after checking all of it is there
        static string Pay(Player p, Piece piece, int count)
        {
            if (piece == null || piece.m_resources == null || piece.m_resources.Length == 0) return ", cost: none (the ward has no recipe)";
            Inventory inv = p.GetInventory();
            List<string> parts = new List<string>();
            foreach (Piece.Requirement r in piece.m_resources)
            {
                if (r.m_resItem == null || r.m_amount <= 0) continue;
                string item = r.m_resItem.m_itemData.m_shared.m_name;
                int need = r.m_amount * count, have = inv.CountItems(item, -1, true);
                if (have < need) throw new CommandException("cost=1: " + count + " ward(s) need " + need + " " + Prefabs.BaseName(r.m_resItem.gameObject) + ", you have " + have + "; nothing placed");
            }
            foreach (Piece.Requirement r in piece.m_resources)
            {
                if (r.m_resItem == null || r.m_amount <= 0) continue;
                inv.RemoveItem(r.m_resItem.m_itemData.m_shared.m_name, r.m_amount * count, -1, true);
                parts.Add(r.m_amount * count + " " + Prefabs.BaseName(r.m_resItem.gameObject));
            }
            return ", cost: " + string.Join(", ", parts.ToArray());
        }

        static string Lines(List<Spot> spots, bool preview)
        {
            StringBuilder sb = new StringBuilder();
            int n = 0;
            foreach (Spot sp in spots)
            {
                if (!sp.Ok) continue;
                if (++n > MaxListed) { sb.Append("\n  ... and ").Append(CountOk(spots) - MaxListed).Append(" more"); break; }
                float moved = DomeWards.DistanceXZ(sp.Pos, new Vector3(sp.Lattice.X, 0f, sp.Lattice.Z));
                sb.Append("\n  ").Append(preview ? "would place at " : "ward at ").Append(Fmt.Vec(sp.Pos));
                if (moved > 0.01f) sb.Append(" (moved ").Append(Fmt.F(moved, 1)).Append(" m from a blocked spot)");
            }
            return sb.ToString();
        }

        static int CountOk(List<Spot> spots)
        {
            int n = 0;
            foreach (Spot sp in spots) if (sp.Ok) n++;
            return n;
        }

        static string Refusals(List<Spot> spots)
        {
            List<string> parts = new List<string>();
            foreach (Spot sp in spots)
            {
                if (sp.Ok) continue;
                if (parts.Count == MaxListed) { parts.Add("..."); break; }
                parts.Add(Fmt.F(sp.Lattice.X, 1) + "," + Fmt.F(sp.Lattice.Z, 1) + " " + sp.Why);
            }
            return string.Join("; ", parts.ToArray());
        }

        // ---- list and remove ------------------------------------------------------------------------

        string List()
        {
            string world = Host.RequireServer();
            Rescan();
            if (groups.Count == 0) return "no domes in world " + world + ": op=dome&step=mark twice, then op=dome&step=build&from=coords";
            Player p = Player.m_localPlayer;
            StringBuilder sb = new StringBuilder();
            sb.Append(groups.Count).Append(" dome(s) in world ").Append(world);
            foreach (KeyValuePair<string, List<ZDO>> kv in groups)
            {
                List<ZDO> wards = kv.Value;
                ZDO z0 = wards[0];
                Vector3 c = DomeWards.Centre(z0);
                int on = 0;
                Dictionary<long, string> permitted = new Dictionary<long, string>();
                foreach (ZDO z in wards)
                {
                    WardState st = DomeWards.Read(z);
                    if (st.Enabled) on++;
                    foreach (KeyValuePair<long, string> pp in st.Permitted) permitted[pp.Key] = pp.Value;
                }
                sb.Append("\n  ").Append(kv.Key).Append(": centre ").Append(Fmt.F(c.x, 1)).Append(",").Append(Fmt.F(c.z, 1))
                    .Append(", radius ").Append(Fmt.F(z0.GetFloat(DomeWards.KeyRadius, 0f), 1)).Append(" m, wards ").Append(wards.Count)
                    .Append("/").Append(z0.GetInt(DomeWards.KeyCount, wards.Count)).Append(", on ").Append(on).Append("/").Append(wards.Count)
                    .Append(", permitted ").Append(permitted.Count);
                if (p != null) sb.Append(", ").Append(Fmt.F(DomeWards.DistanceXZ(p.transform.position, c), 0)).Append(" m from you");
            }
            return sb.ToString();
        }

        string Remove(Args a)
        {
            Host.RequireServer();
            Player p = Players.Local;
            Rescan();
            string id = null;
            RaycastHit hit;
            if (Aim.SolidHit(60f, out hit))
            {
                ZNetView nv = hit.collider.GetComponentInParent<ZNetView>();
                if (nv != null && nv.IsValid()) id = nv.GetZDO().GetString(DomeWards.KeyDome, "");
                if (id != null && id.Length == 0) id = null;
            }
            bool aimed;
            Vector3 at = Aim.Point(p, out aimed);
            if (id == null)
            {
                float best = float.MaxValue;
                foreach (KeyValuePair<string, List<ZDO>> kv in groups)
                {
                    ZDO z0 = kv.Value[0];
                    float d = DomeWards.DistanceXZ(at, DomeWards.Centre(z0));
                    if (d <= z0.GetFloat(DomeWards.KeyRadius, 0f) && d < best) { best = d; id = kv.Key; }
                }
            }
            if (id == null || !groups.ContainsKey(id)) throw new CommandException("no dome at the crosshair (" + Fmt.Vec(at) + "): aim at a dome ward or into a zone; op=dome&step=list");
            List<ZDO> wards = groups[id];
            if (!a.Flag("confirm", false))
                return "would remove dome " + id + " with " + wards.Count + " ward(s): add confirm=1";
            int n = 0;
            foreach (ZDO z in wards)
            {
                if (!DomeWards.Exists(z)) continue;
                DomeWards.Remove(z);
                n++;
            }
            groups.Remove(id);
            return "removed dome " + id + ": " + n + " ward(s)";
        }

        // ---- group sync (host) ----------------------------------------------------------------------

        void Rescan()
        {
            groups = DomeWards.Groups(DomeWards.AllWards());
            nextRescan = Time.time + RescanSeconds;
        }

        // starts the host's sync loop once per world; Player.OnSpawned (DomePatches) and every dome command call it
        internal void EnsureSync()
        {
            if (syncRunning || !Host.IsServer || ZDOMan.instance == null) return;
            syncRunning = true;
            nextRescan = 0f;
            Later(SyncSeconds, Tick);
        }

        void Tick()
        {
            if (!Host.IsServer || ZDOMan.instance == null) { syncRunning = false; return; }
            try
            {
                if (Time.time >= nextRescan) Rescan();
                foreach (KeyValuePair<string, List<ZDO>> kv in groups) SyncGroup(kv.Key, kv.Value);
            }
            catch (Exception e) { HomeApi.LogError("dome: sync: " + e); }
            Later(SyncSeconds, Tick);
        }

        // a ward whose state differs from the state last copied to the group was changed by a player (on/off by its
        // creator, a player added or removed while it was off): its state goes to every ward of the group.
        // Two changed in the same tick: the one with the newer ZDO data wins.
        void SyncGroup(string id, List<ZDO> wards)
        {
            ZDO source = null;
            foreach (ZDO z in wards)
            {
                if (!DomeWards.Exists(z)) { nextRescan = 0f; continue; }
                if (DomeWards.Read(z).Signature() == z.GetString(DomeWards.KeySig, "")) continue;
                if (source == null || z.DataRevision > source.DataRevision) source = z;
            }
            if (source == null) return;
            WardState st = DomeWards.Read(source);
            string sig = st.Signature();
            int copied = 0;
            foreach (ZDO z in wards)
            {
                if (!DomeWards.Exists(z)) continue;
                if (DomeWards.Read(z).Signature() != sig) { DomeWards.Write(z, st); copied++; }
                if (z.GetString(DomeWards.KeySig, "") != sig) { DomeWards.Claim(z); z.Set(DomeWards.KeySig, sig); }
            }
            Vector3 at = source.GetPosition();
            HomeApi.Log("dome " + id + ": ward at " + Fmt.F(at.x, 1) + "," + Fmt.F(at.z, 1) + " changed to " + (st.Enabled ? "on" : "off")
                + ", " + st.Permitted.Count + " permitted; copied to " + copied + " other ward(s)");
        }

        // ---- lifecycle ------------------------------------------------------------------------------

        protected override void OnLoad()
        {
            Current = this;
            DomePatches.Apply(this);
            if (Player.m_localPlayer != null) EnsureSync();
        }

        protected override void OnUnload() { Current = null; }

        protected override void OnWorldExit()
        {
            syncRunning = false;
            groups.Clear();
            nextMarkIsEnd = false;
        }

        internal PatchSet PatchesForDome { get { return Patches; } }
    }
}
