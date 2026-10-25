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
    // Milestone 1 (Maksym 2026-10-25): list, place, continue, support, status, undo. The preview pillars, the material swap and
    // the panel follow (card docs/Backlog/construct.md).
    // Placement: the centre of the footprint at the crosshair (or at your feet), turned to your facing snapped to 22.5°, the
    // lowest collider bottom on the aimed surface (on the terrain without a hit). The ground step depends on that base
    // (Maksym 2026-10-25: stacking stairs must not raise the ground): on terrain the footprint is levelled to the base first and
    // gaps left under the bottom pieces get core-wood poles; on a piece or a rock nothing is changed, the support comes from it.
    // continue=1 stacks the next copy exactly on the build under the crosshair, by the blueprint's own repeat (BlueprintRepeat).
    // Pieces go bottom-up in batches; every piece gets the creator and OnPlaced as in Player.PlacePiece and the ZDO mark
    // hc_construct with its build's origin, turn and name, so continue and undo find it again.
    public sealed class ConstructCommand : CommandBase
    {
        const int MaxPieces = 4000;
        const float MaxSide = 64f;          // metres, the larger side of the footprint
        const int MaxList = 50;
        const int MaxCopies = 20;           // continue=1&count=
        const int BatchSize = 100;          // pieces per step
        const float BatchGap = 0.1f;        // seconds between steps
        const float CheckAfter = 5f;        // seconds after the last step: how many stand, how well supported
        const float BuildingMargin = 1f;    // keep-away from player-built pieces, metres past the build's half diagonal
        const float UndoRadius = 100f;      // a build without id= is looked for within this distance
        const float LowestBand = 1f;        // pieces with a pivot this close to the lowest one are measured for the bottom
        const float BaseBand = 0.3f;        // a piece whose bottom is this close to the base rests on the ground
        const float FlattenMargin = 1f;     // metres levelled around the footprint
        const float AreaStamp = 3f, AreaStep = 4f;   // groundlevel area=coords: 3 m stamps every 4 m cover a rectangle
        const int MaxStamps = 1600;
        const int StampBatch = 50;
        const float Settle = 1f;            // seconds after the last stamp before pieces go down (terrain rebuild)
        const float GapTolerance = 0.15f;   // a bottom piece this far above the surface below it gets a pole column
        const float ColumnSpacing = 1f;     // one column per metre
        const int MaxPoles = 600;
        const int MaxPolesPerColumn = 10;
        const string Pole = "wood_pole_log_4";
        const string FlattenTool = "raise";
        public const string KeyBuild = "hc_construct";          // ZDO mark: the build id <name>-<yyyyMMddHHmmss>
        public const string KeyOrigin = "hc_construct_o";       // the copy's origin (Vector3), for continue=1
        public const string KeyYaw = "hc_construct_yaw";
        public const string KeyName = "hc_construct_name";
        public const string KeyPillar = "hc_construct_pillar";  // ZDO mark of a preview pole: the plan id
        const int PillarStack = 5;          // poles a preview column (5 x 4 m)
        const float PillarClear = 0.3f;     // seconds between taking the preview poles down and building
        const float FloorBin = 0.25f;       // metres: pivot levels for the ground floor
        const float FloorShare = 0.05f;     // the ground floor holds at least this share of the pieces
        const int FloorMin = 10;            // and at least this many
        const float FitMin = 0.3f, FitMax = 3f;   // ground=fit: columns hanging this far above the base get terrain under them
        const float FitStamp = 0.8f;        // level radius of a fit stamp (one 1 m column)
        const int MaxFitStamps = 2500;
        const float SettleCheck = 30f;     // seconds after the last step: the support report once the game has computed it

        string lastId = "", lastName = "";
        int lastPlaced;

        sealed class Job
        {
            public GameObject Prefab;
            public BlueprintPiece Piece;    // null for a support pole
            public Vector3 Pos;
            public Quaternion Rot;
            public Vector3 Origin;          // of its copy
        }

        sealed class Tally
        {
            public int Placed, Scaled, Signs, ExtraIgnored, Invalid, Poles;
        }

        public override string Name { get { return "construct"; } }
        protected override string Usage
        {
            get
            {
                return "construct step=list | step=preview&name=<construction> [at=aim|self|x,z|x,y,z] [yaw=<world degrees, default your facing snapped to 22.5>]"
                    + " [ground=auto|fit|flatten|pillars|none (auto: on terrain = fit; on a piece or rock = none. fit = flatten the footprint + " + (int)FlattenMargin
                    + " m to the base, then raise the terrain under every 1 m column whose lowest piece hangs " + Fmt.F(FitMin, 1) + "-" + Fmt.F(FitMax, 0)
                    + " m up; flatten = only the levelling; both pole the gaps left; pillars = poles only)]"
                    + " [base=floor|lowest (floor: the ground floor on the base, foundations below it sunk)]"
                    + " (5 columns of " + PillarStack + " poles at the corners and centre; the plan is kept per world in construct.state)"
                    + " | step=rotate [by=90] | step=start&confirm=1 [overlap=0 (1 = build even with player-built pieces inside the footprint)] | step=remove (poles down, nothing built)"
                    + " | step=place&name=<construction> (direct, no preview; same keys)"
                    + " | step=place&continue=1 [count=1 (max " + MaxCopies + ")] [name=<default the aimed build's>] (aim at the top of a build: the next copy stacks on it, no ground change)"
                    + " | step=support [id=<default the last>] (read-only) | step=status | step=undo [id=<build id, default the last>] [confirm=1]"
                    + " - builds a blueprint from BepInEx/config/homecheat/blueprints/ (a file = one construction, a subfolder = one construction of all its files);"
                    + " host only; caps " + MaxPieces + " pieces, footprint " + (int)MaxSide + " m a side, " + MaxStamps + " flatten stamps, " + MaxPoles + " poles, list " + MaxList;
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
            if (step == "preview") return Preview(a);
            if (step == "rotate") return Rotate(a);
            if (step == "start") return Start(a);
            if (step == "remove") return RemovePlan();
            if (step == "place") return a.Flag("continue", false) ? Continue(a) : Place(a);
            if (step == "support") return Support(a);
            if (step == "status") return Status();
            if (step == "undo") return Undo(a);
            throw new CommandException("step= list, preview, rotate, start, remove, place, support, status or undo (op=help&cmd=construct)");
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
                float dx, dy, dz;
                int m;
                if (BlueprintRepeat.Find(e, out dx, out dy, out dz, out m)) sb.Append(", stacks (continue=1, +").Append(Fmt.F(dy, 2)).Append(" m a copy)");
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

        static BlueprintEntry Require(string name)
        {
            List<BlueprintEntry> all = BlueprintLibrary.Scan(Dir);
            BlueprintEntry e = BlueprintLibrary.Find(all, name);
            if (e != null) return e;
            List<string> names = new List<string>();
            foreach (BlueprintEntry x in all) names.Add(x.Name);
            throw new CommandException("no construction '" + name + "' (have: " + (names.Count == 0 ? "none" : string.Join(", ", names.ToArray())) + ")");
        }

        static Vector3 Centre(BlueprintEntry e)
        {
            float x0, x1, y0, y1, z0, z1;
            e.Bounds(out x0, out x1, out y0, out y1, out z0, out z1);
            return new Vector3((x0 + x1) * 0.5f, y0, (z0 + z1) * 0.5f);
        }

        // the pieces of one copy: world position = origin + turn * (blueprint position - centre)
        static int AddJobs(List<Job> jobs, BlueprintEntry e, Vector3 origin, Quaternion turn, HashSet<BlueprintPiece> skip, List<string> missing)
        {
            Vector3 centre = Centre(e);
            int skipped = 0;
            foreach (Blueprint b in e.Parts)
                foreach (BlueprintPiece pc in b.Pieces)
                {
                    if (skip != null && skip.Contains(pc)) continue;
                    GameObject prefab = Prefabs.Get(pc.Prefab);
                    if (prefab == null) { skipped++; if (!missing.Contains(pc.Prefab)) missing.Add(pc.Prefab); continue; }
                    Job j = new Job();
                    j.Prefab = prefab;
                    j.Piece = pc;
                    j.Pos = origin + turn * (new Vector3(pc.X, pc.Y, pc.Z) - centre);
                    j.Rot = turn * new Quaternion(pc.Qx, pc.Qy, pc.Qz, pc.Qw);
                    j.Origin = origin;
                    jobs.Add(j);
                }
            return skipped;
        }

        // collider bottom of a job (pivot when it cannot be measured)
        static float Bottom(Job j)
        {
            float lo, hi;
            return PieceGhost.Measure(j.Prefab, j.Rot, out lo, out hi) ? j.Pos.y + lo : j.Pos.y;
        }

        static bool AimHit(out RaycastHit hit)
        {
            hit = new RaycastHit();
            Camera cam = Camera.main;
            return cam != null && Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, 60f, Masks.SolidAndTerrain, QueryTriggerInteraction.Ignore);
        }

        static bool IsTerrain(Collider c)
        {
            return c != null && (c.GetComponent<Heightmap>() != null || LayerMask.LayerToName(c.gameObject.layer) == "terrain");
        }

        // where and how a construction goes: from the crosshair at preview (or a direct place), kept until start or remove
        sealed class Plan
        {
            public string World, Name, Ground, BaseRule, BaseText, PillarId;
            public Vector3 Anchor;
            public float BaseY, Yaw;
        }

        static Plan MakePlan(Args a, Player p, BlueprintEntry e)
        {
            string at = a.Str("at", "aim").ToLowerInvariant();
            // at=x,z or at=x,y,z: a given point (to build again exactly where a build stood), treated as terrain
            Vector3 given = Vector3.zero;
            bool point = at.IndexOf(',') >= 0;
            if (point)
            {
                string[] f = at.Split(',');
                float gx = 0f, gy = float.NaN, gz = 0f;
                System.Globalization.CultureInfo inv = System.Globalization.CultureInfo.InvariantCulture;
                System.Globalization.NumberStyles ns = System.Globalization.NumberStyles.Float;
                bool ok = f.Length == 2 ? float.TryParse(f[0], ns, inv, out gx) & float.TryParse(f[1], ns, inv, out gz)
                    : f.Length == 3 && float.TryParse(f[0], ns, inv, out gx) & float.TryParse(f[1], ns, inv, out gy) & float.TryParse(f[2], ns, inv, out gz);
                if (!ok) throw new CommandException("at must be aim, self, x,z or x,y,z (world metres)");
                given = new Vector3(gx, float.IsNaN(gy) ? Ground.Height(new Vector3(gx, 0f, gz)) : gy, gz);
            }
            else if (at != "aim" && at != "self") throw new CommandException("at must be aim, self, x,z or x,y,z (world metres)");
            string ground = a.Str("ground", "auto").ToLowerInvariant();
            if (ground != "auto" && ground != "fit" && ground != "flatten" && ground != "pillars" && ground != "none") throw new CommandException("ground must be auto, fit, flatten, pillars or none");
            string baseRule = a.Str("base", "floor").ToLowerInvariant();
            if (baseRule != "floor" && baseRule != "lowest") throw new CommandException("base must be floor or lowest");
            RaycastHit hit = new RaycastHit();
            bool aimed = at == "aim" && AimHit(out hit);
            Vector3 anchor = point ? given : at == "self" ? p.transform.position : (aimed ? hit.point : p.transform.position + p.transform.forward * 8f);
            bool onTerrain = !aimed || IsTerrain(hit.collider);
            Plan pl = new Plan();
            pl.World = Host.RequireWorld();
            pl.Name = e.Name;
            pl.Anchor = anchor;
            pl.BaseY = aimed || point ? anchor.y : Ground.Height(anchor);
            pl.BaseText = point ? "given point " + Fmt.Vec(anchor) : aimed ? (onTerrain ? "terrain at the crosshair" : "crosshair on " + Prefabs.BaseName(hit.collider.transform.root.gameObject))
                : (at == "self" ? "terrain at your feet" : "terrain 8 m ahead (no crosshair hit)");
            pl.Ground = ground == "auto" ? (onTerrain ? "fit" : "none") : ground;
            pl.BaseRule = baseRule;
            float yaw;
            if (a.Has("yaw")) yaw = a.Float("yaw", 0f);
            else
            {
                Vector3 f = p.transform.forward;
                yaw = Mathf.Round(Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg / 22.5f) * 22.5f;
            }
            pl.Yaw = Mathf.Repeat(yaw, 360f);
            pl.PillarId = "";
            return pl;
        }

        // the pieces of a plan in the world, the ground floor's collider bottom on the base (Maksym 2026-10-25: gumdrop stood
        // 2.7 m in the air on its own foundation posts). Ground floor = the lowest FloorBin level of pivots holding at least
        // FloorShare of the pieces (and FloorMin); the pieces below it are foundations and sink into the ground. Without such a
        // level (the stairs: about 4 pieces a level), or with base=lowest, the lowest piece goes on the base.
        static List<Job> Layout(Plan pl, BlueprintEntry e, List<string> missing, out int skipped, out Vector3 origin, out string floorText)
        {
            Quaternion turn = Quaternion.Euler(0f, pl.Yaw, 0f);
            List<Job> jobs = new List<Job>();
            skipped = AddJobs(jobs, e, new Vector3(pl.Anchor.x, pl.BaseY, pl.Anchor.z), turn, null, missing);
            origin = new Vector3(pl.Anchor.x, pl.BaseY, pl.Anchor.z);
            floorText = "";
            if (jobs.Count == 0) return jobs;
            float lowPivot = float.MaxValue;
            foreach (Job j in jobs) if (j.Pos.y < lowPivot) lowPivot = j.Pos.y;
            float floorFrom = lowPivot, floorTo = lowPivot + LowestBand;
            int floorCount = 0;
            if (pl.BaseRule == "floor")
            {
                Dictionary<int, int> bins = new Dictionary<int, int>();
                foreach (Job j in jobs)
                {
                    int bin = Mathf.FloorToInt((j.Pos.y - lowPivot) / FloorBin);
                    int c;
                    bins.TryGetValue(bin, out c);
                    bins[bin] = c + 1;
                }
                int need = Mathf.Max(FloorMin, Mathf.CeilToInt(jobs.Count * FloorShare)), found = -1;
                foreach (KeyValuePair<int, int> kv in bins)
                    if (kv.Value >= need && (found < 0 || kv.Key < found)) found = kv.Key;
                if (found > 0) { floorFrom = lowPivot + found * FloorBin; floorTo = floorFrom + FloorBin; floorCount = bins[found]; }
            }
            float lowest = float.MaxValue;
            int sunk = 0;
            foreach (Job j in jobs)
            {
                if (j.Pos.y < floorFrom) { sunk++; continue; }
                if (j.Pos.y < floorTo) lowest = Mathf.Min(lowest, Bottom(j));
            }
            float lift = pl.BaseY - lowest;
            origin = new Vector3(pl.Anchor.x, pl.BaseY + lift, pl.Anchor.z);
            foreach (Job j in jobs) { j.Pos.y += lift; j.Origin = origin; }
            floorText = floorCount > 0
                ? "ground floor " + Fmt.F(floorFrom - lowPivot, 2) + " m above the lowest piece (" + floorCount + " pieces) on the base, " + sunk + " foundation piece(s) below it sunk into the ground"
                : "lowest piece on the base";
            return jobs;
        }

        string Place(Args a)
        {
            Host.RequireServer();
            Player p = Players.Local;
            BlueprintEntry e = Require(a.Required("name", "op=construct&step=list shows the names"));
            return Build(MakePlan(a, p, e), e, p, a.Flag("overlap", false));
        }

        string Build(Plan pl, BlueprintEntry e, Player p, bool overlap)
        {
            int count = Caps.Check("pieces", e.PieceCount, MaxPieces);
            if (count == 0) throw new CommandException(e.Name + " has no pieces");
            float x0, x1, y0, y1, z0, z1;
            e.Bounds(out x0, out x1, out y0, out y1, out z0, out z1);
            Caps.Check("footprint", Mathf.Max(x1 - x0, z1 - z0), MaxSide);
            Vector3 anchor = pl.Anchor;
            float baseY = pl.BaseY, yaw = pl.Yaw;
            string ground = pl.Ground, baseText = pl.BaseText;

            List<string> missing = new List<string>();
            int skipped;
            Vector3 origin;
            string floorText;
            List<Job> jobs = Layout(pl, e, missing, out skipped, out origin, out floorText);
            if (jobs.Count == 0) throw new CommandException("none of the " + count + " pieces is a prefab this game knows (" + string.Join(" ", missing.ToArray()) + ")");

            if (!overlap)
            {
                Vector3 mid = new Vector3(anchor.x, baseY + (y1 - y0) * 0.5f, anchor.z);
                float reach = 0.5f * Mathf.Sqrt((x1 - x0) * (x1 - x0) + (z1 - z0) * (z1 - z0) + (y1 - y0) * (y1 - y0)) + BuildingMargin;
                Piece nearest;
                int n = Constructions.Near(mid, reach, out nearest);
                if (n > 0)
                    throw new CommandException(n + " player-built piece(s) inside the footprint, nearest " + Constructions.Describe(nearest, mid)
                        + ": aim elsewhere, stack with continue=1, or add overlap=1");
            }

            // flatten: the box of every piece pivot + margin levelled to the base, before anything is placed
            float delay = 0f;
            int stamps = 0, fitStamps = 0;
            if (ground == "flatten" || ground == "fit")
            {
                float ax = float.MaxValue, az = float.MaxValue, bx = float.MinValue, bz = float.MinValue;
                foreach (Job j in jobs) { ax = Mathf.Min(ax, j.Pos.x); az = Mathf.Min(az, j.Pos.z); bx = Mathf.Max(bx, j.Pos.x); bz = Mathf.Max(bz, j.Pos.z); }
                ax -= FlattenMargin; az -= FlattenMargin; bx += FlattenMargin; bz += FlattenMargin;
                int cols, rows;
                GridMath.RectSize(ax, az, bx, bz, AreaStep, out cols, out rows);
                Caps.Check("stamps", cols * rows, MaxStamps);
                GameObject tool = Prefabs.Require(FlattenTool, "(the hoe's raise piece)");
                TerrainModifier tm = tool.GetComponent<TerrainModifier>();
                if (tm == null) throw new CommandException(FlattenTool + " has no TerrainModifier");
                float off = Refl.GetFloat(tm, "m_levelOffset", 0f);
                List<Vector3> todo = new List<Vector3>();
                foreach (GridPoint pt in GridMath.RectCells(ax, az, bx, bz, AreaStep, MaxStamps)) todo.Add(new Vector3(pt.X, baseY - off, pt.Z));
                stamps = todo.Count;
                int nb = (todo.Count + StampBatch - 1) / StampBatch;
                for (int b = 0; b < nb; b++)
                {
                    int from = b * StampBatch, to = Mathf.Min(todo.Count, from + StampBatch);
                    Action stamp = delegate { for (int i = from; i < to; i++) TerrainStamp.At(tool, todo[i], AreaStamp, 0f); };
                    if (b == 0) stamp(); else Later(b * BatchGap, stamp);
                }
                delay = (nb - 1) * BatchGap + Settle;

                // fit (Maksym 2026-10-25, "build based on these rules"): the lowest piece of every 1 m column marks where the
                // original ground was; a column whose lowest piece hangs FitMin..FitMax above the base gets the terrain raised
                // up to it (garden stones, bushes, fences, a deck on a slope). Higher columns are roof overhangs: left alone.
                if (ground == "fit")
                {
                    List<Vector3> fit = FitStamps(jobs, baseY, off);
                    fitStamps = fit.Count;
                    int nf = (fit.Count + StampBatch - 1) / StampBatch;
                    float start = delay;
                    for (int b = 0; b < nf; b++)
                    {
                        int from = b * StampBatch, to = Mathf.Min(fit.Count, from + StampBatch);
                        Later(start + b * BatchGap, delegate { for (int i = from; i < to; i++) TerrainStamp.At(tool, fit[i], FitStamp, 0f); });
                    }
                    if (nf > 0) delay = start + (nf - 1) * BatchGap + Settle;
                }
            }

            string id = e.Name + "-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            long me = p.GetPlayerID();
            Tally t = new Tally();
            string buildName = e.Name;
            bool poles = ground == "flatten" || ground == "fit" || ground == "pillars";
            List<string> notes = new List<string>();
            Action build = delegate
            {
                if (poles) AddPoles(jobs, baseY, t, notes);
                Run(jobs, id, yaw, buildName, me, t, notes);
            };
            if (delay <= 0f) build(); else Later(delay, build);
            lastId = id; lastName = e.Name; lastPlaced = jobs.Count;

            return (delay <= 0f && jobs.Count <= BatchSize ? "placed " + t.Placed : "placing " + jobs.Count) + " of " + e.Name + " (" + count + " in the blueprint"
                + (skipped > 0 ? ", " + skipped + " skipped: unknown prefabs " + string.Join(" ", missing.ToArray()) : "") + ")"
                + " at " + Fmt.Vec(origin) + " on the " + baseText + " (base " + Fmt.F(baseY, 2) + ", terrain " + Fmt.F(Ground.Height(anchor), 2) + ")"
                + ", " + floorText
                + ", ground " + ground + (stamps > 0 ? " (" + stamps + " level stamps" + (ground == "fit" ? ", " + fitStamps + " columns raised to their lowest piece" : "")
                    + ", pieces after " + Fmt.F(delay, 1) + " s)" : "")
                + (t.Poles > 0 ? ", " + t.Poles + " support pole(s)" : "")
                + ", yaw " + Fmt.F(yaw, 1) + ", id " + id
                + "; support on screen in " + Fmt.F(delay + CheckAfter, 0) + " s and settled in " + Fmt.F(delay + SettleCheck, 0) + " s (op=construct&step=support)";
        }

        // ---- preview -> start | remove (the agreed flow, Maksym 2026-10-25): the area is shown first, built only on confirm ----

        static string StateFile { get { return Path.Combine(Path.Combine(Paths.ConfigPath, "homecheat"), "construct.state"); } }

        // one plan per world, one line per world: world, name, anchor x y z, base, yaw, ground, base rule, pillar id, base text
        static Dictionary<string, string[]> ReadState()
        {
            Dictionary<string, string[]> all = new Dictionary<string, string[]>();
            if (!File.Exists(StateFile)) return all;
            foreach (string line in File.ReadAllLines(StateFile))
            {
                string[] f = line.Split('\t');
                if (f.Length >= 11) all[f[0]] = f;
            }
            return all;
        }

        static void WriteState(Dictionary<string, string[]> all)
        {
            List<string> lines = new List<string>();
            foreach (KeyValuePair<string, string[]> kv in all) lines.Add(string.Join("\t", kv.Value));
            string tmp = StateFile + ".tmp";
            File.WriteAllLines(tmp, lines.ToArray());
            if (File.Exists(StateFile)) File.Delete(StateFile);
            File.Move(tmp, StateFile);
        }

        static void SavePlan(Plan pl)
        {
            Dictionary<string, string[]> all = ReadState();
            all[pl.World] = new string[] { pl.World, pl.Name, Fmt.F(pl.Anchor.x, 3), Fmt.F(pl.Anchor.y, 3), Fmt.F(pl.Anchor.z, 3), Fmt.F(pl.BaseY, 3),
                Fmt.F(pl.Yaw, 1), pl.Ground, pl.BaseRule, pl.PillarId, pl.BaseText.Replace("\t", " ") };
            WriteState(all);
        }

        static void ClearPlan(string world)
        {
            Dictionary<string, string[]> all = ReadState();
            if (all.Remove(world)) WriteState(all);
        }

        static Plan LoadPlan(string world)
        {
            string[] f;
            if (!ReadState().TryGetValue(world, out f)) return null;
            System.Globalization.CultureInfo inv = System.Globalization.CultureInfo.InvariantCulture;
            Plan pl = new Plan();
            pl.World = f[0]; pl.Name = f[1];
            pl.Anchor = new Vector3(float.Parse(f[2], inv), float.Parse(f[3], inv), float.Parse(f[4], inv));
            pl.BaseY = float.Parse(f[5], inv); pl.Yaw = float.Parse(f[6], inv);
            pl.Ground = f[7]; pl.BaseRule = f[8]; pl.PillarId = f[9]; pl.BaseText = f[10];
            return pl;
        }

        static Plan RequirePlan()
        {
            Plan pl = LoadPlan(Host.RequireWorld());
            if (pl == null) throw new CommandException("no construction planned in this world: op=construct&step=preview&name=<construction> first");
            return pl;
        }

        // the 4 corners of the turned footprint and its centre, at the plan's anchor
        static List<Vector3> PillarPoints(Plan pl, BlueprintEntry e)
        {
            float x0, x1, y0, y1, z0, z1;
            e.Bounds(out x0, out x1, out y0, out y1, out z0, out z1);
            Vector3 c = Centre(e);
            Quaternion turn = Quaternion.Euler(0f, pl.Yaw, 0f);
            List<Vector3> pts = new List<Vector3>();
            float[,] xz = new float[,] { { x0, z0 }, { x1, z0 }, { x1, z1 }, { x0, z1 } };
            for (int i = 0; i < 4; i++)
                pts.Add(pl.Anchor + turn * new Vector3(xz[i, 0] - c.x, 0f, xz[i, 1] - c.z));
            pts.Add(pl.Anchor);
            return pts;
        }

        // PillarStack poles on the surface under each point; no creator, so they never count as player-built (keep-away)
        static int PlacePillars(Plan pl, BlueprintEntry e)
        {
            GameObject pole = Prefabs.Require(Pole, "(the core-wood pole)");
            float lo, hi;
            if (!PieceGhost.Measure(pole, Quaternion.identity, out lo, out hi) || hi - lo < 0.5f) throw new CommandException(Pole + " could not be measured");
            float len = hi - lo;
            int n = 0;
            foreach (Vector3 pt in PillarPoints(pl, e))
            {
                float surface = Ground.Height(pt);
                RaycastHit h;
                if (Physics.Raycast(new Vector3(pt.x, Mathf.Max(surface, pl.BaseY) + 50f, pt.z), Vector3.down, out h, 200f, Masks.SolidAndTerrain, QueryTriggerInteraction.Ignore))
                    surface = h.point.y;
                for (int k = 0; k < PillarStack; k++)
                {
                    GameObject go = (GameObject)UnityEngine.Object.Instantiate(pole, new Vector3(pt.x, surface - lo + k * len, pt.z), Quaternion.identity);
                    ZNetView nv = go.GetComponent<ZNetView>();
                    if (nv == null || !nv.IsValid()) { UnityEngine.Object.Destroy(go); continue; }
                    nv.GetZDO().Set(KeyPillar, pl.PillarId);
                    n++;
                }
            }
            return n;
        }

        static int RemovePillars(string pillarId)
        {
            int n = 0;
            foreach (ZNetView nv in UnityEngine.Object.FindObjectsOfType<ZNetView>())
            {
                if (nv == null || !nv.IsValid()) continue;
                string v = nv.GetZDO().GetString(KeyPillar, "");
                if (v.Length == 0 || (pillarId.Length > 0 && v != pillarId)) continue;
                nv.ClaimOwnership();
                ZNetScene.instance.Destroy(nv.gameObject);
                n++;
            }
            return n;
        }

        // what start will do, for the preview answer and for start without confirm=1
        static string Describe(Plan pl, BlueprintEntry e)
        {
            float x0, x1, y0, y1, z0, z1;
            e.Bounds(out x0, out x1, out y0, out y1, out z0, out z1);
            List<string> missing = new List<string>();
            int skipped;
            Vector3 origin;
            string floorText;
            Layout(pl, e, missing, out skipped, out origin, out floorText);
            return pl.Name + ": " + e.PieceCount + " pieces" + (skipped > 0 ? " (" + skipped + " unknown, skipped)" : "") + ", " + Fmt.F(x1 - x0, 1) + " x " + Fmt.F(z1 - z0, 1)
                + " x " + Fmt.F(y1 - y0, 1) + " m, yaw " + Fmt.F(pl.Yaw, 1) + ", base " + Fmt.F(pl.BaseY, 2) + " on the " + pl.BaseText + ", " + floorText
                + ", ground " + pl.Ground + (pl.Ground == "fit" ? " (the footprint + " + (int)FlattenMargin + " m levelled to the base, then the terrain raised under every column whose lowest piece hangs "
                    + Fmt.F(FitMin, 1) + "-" + Fmt.F(FitMax, 0) + " m up, poles under gaps left)" : pl.Ground == "flatten" ?" (the footprint + " + (int)FlattenMargin + " m levelled to the base, poles under gaps left)" : pl.Ground == "pillars" ? " (poles under gaps, terrain unchanged)" : " (unchanged)");
        }

        string Preview(Args a)
        {
            Host.RequireServer();
            Player p = Players.Local;
            BlueprintEntry e = Require(a.Required("name", "op=construct&step=list shows the names"));
            Caps.Check("pieces", e.PieceCount, MaxPieces);
            Plan old = LoadPlan(Host.RequireWorld());
            int removed = old != null ? RemovePillars(old.PillarId) : 0;
            Plan pl = MakePlan(a, p, e);
            pl.PillarId = "plan-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            int n = PlacePillars(pl, e);
            SavePlan(pl);
            return "preview " + Describe(pl, e) + "; " + n + " poles in 5 columns at the corners and the centre"
                + (removed > 0 ? " (the previous plan" + (old.Name != pl.Name ? " of " + old.Name : "") + " and its " + removed + " poles replaced)" : "")
                + ". Next: step=start&confirm=1 builds it, step=rotate&by=90 turns it, step=preview again moves it, step=remove takes the poles down";
        }

        string Rotate(Args a)
        {
            Host.RequireServer();
            Plan pl = RequirePlan();
            BlueprintEntry e = Require(pl.Name);
            float by = a.Float("by", 90f);
            RemovePillars(pl.PillarId);
            pl.Yaw = Mathf.Repeat(pl.Yaw + by, 360f);
            int n = PlacePillars(pl, e);
            SavePlan(pl);
            return "rotated by " + Fmt.F(by, 1) + ": " + Describe(pl, e) + "; " + n + " poles moved";
        }

        string Start(Args a)
        {
            Host.RequireServer();
            Player p = Players.Local;
            Plan pl = RequirePlan();
            BlueprintEntry e = Require(pl.Name);
            bool overlap = a.Flag("overlap", false);
            if (!a.Flag("confirm", false)) return "would build " + Describe(pl, e) + ": add confirm=1";
            int removed = RemovePillars(pl.PillarId);
            ClearPlan(pl.World);
            // the poles go at the end of this frame: build a moment later so the pole check and the keep-away do not see them
            Later(PillarClear, delegate
            {
                try { HomeApi.Log("construct: start " + Build(pl, e, p, overlap)); }
                catch (CommandException ex)
                {
                    SavePlan(pl);
                    PlacePillars(pl, e);
                    Notify.Center("construct: not built: " + ex.Message);
                    HomeApi.Log("construct: start refused: " + ex.Message + " (plan and poles restored)");
                }
            });
            return "building " + pl.Name + ": " + removed + " preview poles removed, construction starts in " + Fmt.F(PillarClear, 1) + " s (the result shows on screen; op=construct&step=support)";
        }

        string RemovePlan()
        {
            Host.RequireServer();
            Plan pl = RequirePlan();
            int n = RemovePillars(pl.PillarId);
            ClearPlan(pl.World);
            return "removed the plan of " + pl.Name + " and its " + n + " preview poles; nothing was built";
        }

        // continue=1: the next copy (or count= copies) on the build under the crosshair, shifted by the blueprint's repeat
        string Continue(Args a)
        {
            Host.RequireServer();
            Player p = Players.Local;
            RaycastHit hit;
            if (!Aim.SolidHit(60f, out hit)) throw new CommandException("aim at the top of the build to continue (nothing solid under the crosshair)");
            ZNetView below = hit.collider.GetComponentInParent<ZNetView>();
            if (below == null || !below.IsValid() || below.GetZDO().GetString(KeyBuild, "").Length == 0)
                throw new CommandException("aim at a piece of a construct build (" + Prefabs.BaseName(hit.collider.transform.root.gameObject) + " is not one)");
            ZDO z = below.GetZDO();
            string fromId = z.GetString(KeyBuild, "");
            Vector3 o0 = z.GetVec3(KeyOrigin, Vector3.zero);
            float yaw = z.GetFloat(KeyYaw, 0f);
            string name = a.Str("name", z.GetString(KeyName, ""));
            if (name.Length == 0 || o0 == Vector3.zero) throw new CommandException("build " + fromId + " was placed before continue=1 existed: it has no origin to stack on; undo it and place it again");
            BlueprintEntry e = Require(name);
            float dx, dy, dz;
            int matched;
            if (!BlueprintRepeat.Find(e, out dx, out dy, out dz, out matched))
                throw new CommandException(e.Name + " does not stack: its top layer does not repeat its bottom layer");
            int copies = Caps.Check("count", a.Int("count", 1), MaxCopies);
            if (copies < 1) throw new CommandException("count must be 1.." + MaxCopies);
            HashSet<BlueprintPiece> shared = BlueprintRepeat.Shared(e, dx, dy, dz);
            Caps.Check("pieces", (e.PieceCount - shared.Count) * copies, MaxPieces);

            Quaternion turn = Quaternion.Euler(0f, yaw, 0f);
            Vector3 shift = turn * new Vector3(dx, dy, dz);
            List<Job> jobs = new List<Job>();
            List<string> missing = new List<string>();
            int skipped = 0;
            for (int c = 1; c <= copies; c++) skipped += AddJobs(jobs, e, o0 + shift * c, turn, shared, missing);

            // the copy above must be free: a construct piece of the same prefab already there means this is not the top
            Dictionary<string, List<Vector3>> existing = new Dictionary<string, List<Vector3>>();
            foreach (ZNetView nv in Loaded(""))
            {
                string pn = Prefabs.BaseName(nv.gameObject);
                List<Vector3> l;
                if (!existing.TryGetValue(pn, out l)) { l = new List<Vector3>(); existing[pn] = l; }
                l.Add(nv.transform.position);
            }
            foreach (Job j in jobs)
            {
                List<Vector3> l;
                if (!existing.TryGetValue(j.Prefab.name, out l)) continue;
                foreach (Vector3 v in l)
                    if ((v - j.Pos).sqrMagnitude < 0.01f)
                        throw new CommandException("a copy already stands above " + fromId + " (" + j.Prefab.name + " at " + Fmt.Vec(v) + "): aim at the top copy");
            }

            string id = e.Name + "-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            Tally t = new Tally();
            Run(jobs, id, yaw, e.Name, p.GetPlayerID(), t, new List<string>());
            lastId = id; lastName = e.Name; lastPlaced = jobs.Count;
            return (jobs.Count <= BatchSize ? "placed " + t.Placed : "placing " + jobs.Count) + " of " + e.Name + " in " + copies + " cop" + (copies == 1 ? "y" : "ies")
                + " on " + fromId + " (shift " + Fmt.F(dx, 2) + "," + Fmt.F(dy, 2) + "," + Fmt.F(dz, 2) + " a copy, " + shared.Count + " shared landing piece(s) not placed twice"
                + (skipped > 0 ? ", " + skipped + " skipped: unknown prefabs " + string.Join(" ", missing.ToArray()) : "") + ")"
                + ", top piece now at " + Fmt.F(Top(jobs), 1) + ", ground unchanged, id " + id
                + "; standing and support in " + Fmt.F(CheckAfter, 0) + " s (op=construct&step=support)";
        }

        // one raise stamp per 1 m column whose lowest piece (collider bottom; pivot for bushes, stones and other non-pieces)
        // hangs FitMin..FitMax above the base, levelled to that bottom; lower columns first, so a higher stamp's smoothing wins
        // the fit surface (FitField): every piece's collider box over the 1 m cells it covers (bushes, stones and other non-pieces
        // as a 0.5 m square at their pivot), then one raise stamp per raised cell, lowest first
        static List<Vector3> FitStamps(List<Job> jobs, float baseY, float levelOffset)
        {
            List<FitField.Box> boxes = new List<FitField.Box>();
            foreach (Job j in jobs)
            {
                if (j.Piece == null) continue;
                Vector3 lo, hi;
                if (PieceGhost.Box(j.Prefab, j.Rot, out lo, out hi))
                    boxes.Add(new FitField.Box(j.Pos.x + lo.x, j.Pos.x + hi.x, j.Pos.z + lo.z, j.Pos.z + hi.z, j.Pos.y + lo.y));
                else
                    boxes.Add(new FitField.Box(j.Pos.x - 0.25f, j.Pos.x + 0.25f, j.Pos.z - 0.25f, j.Pos.z + 0.25f, j.Pos.y));
            }
            List<Vector3> stamps = new List<Vector3>();
            foreach (float[] c in FitField.Build(boxes, baseY, FitMin, FitMax, MaxFitStamps)) stamps.Add(new Vector3(c[0], c[1] - levelOffset, c[2]));
            return stamps;
        }

        static float Top(List<Job> jobs)
        {
            float top = float.MinValue;
            foreach (Job j in jobs) if (j.Pos.y > top) top = j.Pos.y;
            return top;
        }

        // core-wood poles under every bottom piece that does not reach the surface below it (after the flattening settled)
        static void AddPoles(List<Job> jobs, float baseY, Tally t, List<string> notes)
        {
            GameObject pole = Prefabs.Get(Pole);
            float lo, hi;
            if (pole == null || !PieceGhost.Measure(pole, Quaternion.identity, out lo, out hi) || hi - lo < 0.5f) { notes.Add("no poles: " + Pole + " could not be measured"); return; }
            float len = hi - lo;
            List<Vector3> columns = new List<Vector3>();
            List<Job> add = new List<Job>();
            int noSurface = 0, capped = 0;
            foreach (Job j in jobs)
            {
                if (j.Piece == null) continue;
                float bottom = Bottom(j);
                if (bottom > baseY + BaseBand || bottom < baseY - BaseBand) continue;   // above the base, or a sunk foundation
                // terrain at or above the bottom: the piece already sits on or in the ground (a raised fit column); a ray from
                // inside the terrain would find no surface
                if (Ground.Height(j.Pos) >= bottom - GapTolerance) continue;
                RaycastHit h;
                if (!Physics.Raycast(new Vector3(j.Pos.x, bottom + 0.3f, j.Pos.z), Vector3.down, out h, 64f, Masks.SolidAndTerrain, QueryTriggerInteraction.Ignore)) { noSurface++; continue; }
                if (bottom - h.point.y <= GapTolerance) continue;
                bool near = false;
                foreach (Vector3 c in columns) if (new Vector2(c.x - j.Pos.x, c.z - j.Pos.z).sqrMagnitude < ColumnSpacing * ColumnSpacing) { near = true; break; }
                if (near) continue;
                columns.Add(j.Pos);
                for (int k = 0; k < MaxPolesPerColumn; k++)
                {
                    if (t.Poles + add.Count >= MaxPoles) { capped++; break; }
                    float poleBottom = bottom - (k + 1) * len;
                    Job pj = new Job();
                    pj.Prefab = pole;
                    pj.Pos = new Vector3(j.Pos.x, poleBottom - lo, j.Pos.z);
                    pj.Rot = Quaternion.identity;
                    pj.Origin = j.Origin;
                    add.Add(pj);
                    if (poleBottom <= h.point.y) break;
                }
            }
            jobs.AddRange(add);
            t.Poles = add.Count;
            if (noSurface > 0) notes.Add(noSurface + " bottom piece(s) with no surface within 64 m below");
            if (capped > 0) notes.Add("poles capped at " + MaxPoles);
        }

        // places the jobs bottom-up in batches, then reports standing and support
        void Run(List<Job> jobs, string id, float yaw, string name, long me, Tally t, List<string> notes)
        {
            jobs.Sort(delegate (Job l, Job r) { return l.Pos.y.CompareTo(r.Pos.y); });
            int batches = (jobs.Count + BatchSize - 1) / BatchSize;
            for (int bi = 0; bi < batches; bi++)
            {
                int from = bi * BatchSize, to = Mathf.Min(jobs.Count, from + BatchSize);
                Action stepAct = delegate { for (int i = from; i < to; i++) PlaceOne(jobs[i], id, yaw, name, me, t); };
                if (bi == 0) stepAct(); else Later(bi * BatchGap, stepAct);
            }
            // the game computes support over several seconds (a fresh piece reads as fully supported until then), so the
            // report at CheckAfter is marked as settling and the one at SettleCheck is the real one
            float last = (batches - 1) * BatchGap;
            Later(last + CheckAfter, delegate
            {
                string msg = "construct: " + name + " " + SupportLine(id, t.Placed) + (t.Poles > 0 ? ", " + t.Poles + " poles" : "")
                    + (notes.Count > 0 ? "; " + string.Join("; ", notes.ToArray()) : "") + "; support still settling, final in " + Fmt.F(SettleCheck - CheckAfter, 0) + " s";
                HomeApi.Log(msg + " (id " + id + ")");
                Notify.Center(msg);
            });
            Later(last + SettleCheck, delegate
            {
                string msg = "construct: " + name + " settled: " + SupportLine(id, t.Placed);
                HomeApi.Log(msg + " (id " + id + ")");
                Notify.Center(msg);
            });
        }

        // one piece the way Player.PlacePiece makes it: creator, OnPlaced; no place effect per piece
        static void PlaceOne(Job j, string id, float yaw, string name, long me, Tally t)
        {
            GameObject go = (GameObject)UnityEngine.Object.Instantiate(j.Prefab, j.Pos, j.Rot);
            ZNetView nv = go.GetComponent<ZNetView>();
            if (nv == null || !nv.IsValid()) { UnityEngine.Object.Destroy(go); t.Invalid++; return; }
            ZDO z = nv.GetZDO();
            z.Set(KeyBuild, id);
            z.Set(KeyOrigin, j.Origin);
            z.Set(KeyYaw, yaw);
            z.Set(KeyName, name);
            BlueprintPiece pc = j.Piece;
            if (pc != null && (Mathf.Abs(pc.Sx - 1f) > 0.001f || Mathf.Abs(pc.Sy - 1f) > 0.001f || Mathf.Abs(pc.Sz - 1f) > 0.001f))
            {
                Vector3 s = new Vector3(pc.Sx, pc.Sy, pc.Sz);
                go.transform.localScale = s;
                z.Set(Trees.ScaleKey, s);
                t.Scaled++;
            }
            Piece piece = go.GetComponent<Piece>();
            if (piece != null) piece.SetCreator(me, PlatformManager.DistributionPlatform.LocalUser.PlatformUserID);
            WearNTear wnt = go.GetComponent<WearNTear>();
            if (wnt != null) wnt.OnPlaced();
            if (pc != null && !string.IsNullOrEmpty(pc.Extra))
            {
                Sign sign = go.GetComponent<Sign>();
                if (sign != null) { sign.SetText(pc.Extra); t.Signs++; }
                else t.ExtraIgnored++;
            }
            t.Placed++;
        }

        // the hammer's support colour, 0..1 (private in 1.0.20: GetSupport against the material's min and max support)
        static readonly System.Reflection.MethodInfo SupportValue = typeof(WearNTear).GetMethod("GetSupportColorValue",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, Type.EmptyTypes, null);

        // "<standing> of <placed> standing; support: n full, n good, n weak, n critical (weakest <prefab> 12%)"
        static string SupportLine(string id, int placed)
        {
            return SupportLine(id, placed, null);
        }

        // breakdown (when given) collects the weak and critical pieces by prefab
        static string SupportLine(string id, int placed, Dictionary<string, int> breakdown)
        {
            List<ZNetView> pieces = Loaded(id);
            int grounded = 0, full = 0, good = 0, weak = 0, critical = 0;
            float worst = 2f;
            string worstName = "";
            foreach (ZNetView nv in pieces)
            {
                WearNTear w = nv.GetComponent<WearNTear>();
                if (w == null || SupportValue == null) continue;
                float v;
                try { v = (float)SupportValue.Invoke(w, null); }
                catch (Exception) { continue; }
                // -1 = at the material's maximum (the hammer's blue): the piece rests on the ground or a rock (IL 1.0.20)
                if (v < 0f) { grounded++; continue; }
                if (v >= 0.99f) full++; else if (v >= 0.5f) good++; else if (v >= 0.25f) weak++; else critical++;
                if (v < 0.5f && breakdown != null)
                {
                    string pn = Prefabs.BaseName(nv.gameObject);
                    int c;
                    breakdown.TryGetValue(pn, out c);
                    breakdown[pn] = c + 1;
                }
                if (v < worst) { worst = v; worstName = Prefabs.BaseName(nv.gameObject); }
            }
            return pieces.Count + " of " + (placed > 0 ? placed : pieces.Count) + " standing; support " + grounded + " grounded, " + full + " full, " + good + " good, " + weak + " weak, " + critical + " critical"
                + (worstName.Length > 0 ? " (weakest " + worstName + " " + Mathf.RoundToInt(worst * 100f) + "%)" : "");
        }

        string Support(Args a)
        {
            Host.RequireWorld();
            string id = a.Str("id", lastId);
            if (id.Length == 0) throw new CommandException("no build in this session: add id= (op=construct&step=status lists the loaded ones)");
            Dictionary<string, int> low = new Dictionary<string, int>();
            string line = SupportLine(id, 0, low);
            List<KeyValuePair<string, int>> byCount = new List<KeyValuePair<string, int>>(low);
            byCount.Sort(delegate (KeyValuePair<string, int> l, KeyValuePair<string, int> r) { return r.Value.CompareTo(l.Value); });
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < byCount.Count && i < 12; i++) sb.Append(i == 0 ? "" : ", ").Append(byCount[i].Key).Append(" ").Append(byCount[i].Value);
            return "build " + id + ": " + line + (sb.Length > 0 ? "; weak and critical by prefab: " + sb : "")
                + " (loaded pieces; grounded = rests on the ground or a rock, then full / good / weak / critical as the hammer colours them)";
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
            Plan pl = LoadPlan(Host.RequireWorld());
            BlueprintEntry pe = pl != null ? BlueprintLibrary.Find(BlueprintLibrary.Scan(Dir), pl.Name) : null;
            if (pl != null)
                sb.Append("; planned (preview poles standing): ").Append(pe != null ? Describe(pl, pe) : pl.Name + " (its blueprint file is gone: step=remove)")
                  .Append(" - step=start&confirm=1 to build, step=remove to drop it");
            else sb.Append("; nothing planned");
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
