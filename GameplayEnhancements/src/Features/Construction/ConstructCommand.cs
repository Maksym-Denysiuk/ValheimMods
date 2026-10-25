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
        const int StampBatch = 50;
        const float Settle = 1f;            // seconds after the last stamp before pieces go down (terrain rebuild)
        const float GapTolerance = 0.15f;   // a bottom piece this far above the surface below it gets a pole column
        const float ColumnSpacing = 1f;     // one column per metre
        const int MaxPoles = 600;
        const int MaxPolesPerColumn = 10;
        const string Pole = "wood_pole_log_4";
        public const string KeyBuild = "hc_construct";          // ZDO mark: the build id <name>-<yyyyMMddHHmmss>
        public const string KeyOrigin = "hc_construct_o";       // the copy's origin (Vector3), for continue=1
        public const string KeyYaw = "hc_construct_yaw";
        public const string KeyName = "hc_construct_name";
        public const string KeyPillar = "hc_construct_pillar";  // ZDO mark of a preview pole: the plan id
        const string PillarTorch = "piece_groundtorch", PillarTorchFallback = "piece_groundtorch_wood";
        const int PillarStack = 5;          // poles a preview column (5 x 4 m)
        const float PillarClear = 0.3f;     // seconds between taking the preview poles down and building
        const float FloorBin = 0.25f;       // metres: pivot levels for the ground floor
        const float FloorShare = 0.05f;     // the ground floor holds at least this share of the pieces
        const int FloorMin = 10;            // and at least this many
        const float FitMin = 0.3f, FitMax = 4f;   // ground=fit: columns hanging this far above the base get terrain under them
        const int VertexBatch = 150;        // level operations per step
        const int MaxVertices = 6000;       // about 77 x 77 m
        const float LegacyWait = 1f;        // seconds after removing old hoe stamps: the terrain rebuilds without them
        const float LegacyMargin = 4f;      // old hoe stamps this far around the levelled box are removed too (their smoothing reaches in)
        const float TerrainLimit = 8f;      // the game keeps terrain within this many metres of the world's own ground
        const string PlinthFloor = "stone_floor_2x2", PlinthWall = "stone_wall_2x1";
        const float PlinthTile = 2f;        // the stone foundation's tile: one 2 x 2 m floor
        const int MaxPlinth = 1500, MaxPlinthWalls = 12;
        const float ClearMargin = 1f, ClearBelow = 3f;   // clear the space: around the pieces' box, and under the base
        const float GroundMiss = 0.05f;     // a ground step with more vertices than this share off target stops the build
        const float ItemMargin = 2f;        // undo items=1: dropped items this far around the build's box
        const float SettleCheck = 30f;     // seconds after the last step: the support report once the game has computed it

        GroundJob lastGround;
        string lastBroken = "";
        string lastId = "", lastName = "";
        int lastPlaced;

        sealed class Job
        {
            public GameObject Prefab;
            public BlueprintPiece Piece;    // null for a support pole
            public Vector3 Pos;
            public Quaternion Rot;
            public Vector3 Origin;          // of its copy
            public ZNetView Placed;         // set by PlaceOne; null again once the game breaks the piece
            public bool Tried;
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
                    + " m up (pieces that need ground: stone, other non-wood, loose objects); flatten = only the levelling; pillars = poles under bottom pieces, terrain unchanged)]"
                    + " [base=floor|lowest (floor: the ground floor on the base, foundations below it sunk)]"
                    + " (5 columns of " + PillarStack + " poles at the corners and centre; the plan is kept per world in construct.state)"
                    + " | step=rotate [by=90] | step=start&confirm=1 [overlap=0 (1 = build even with player-built pieces inside the footprint)] | step=remove (poles down, nothing built)"
                    + " | step=level&name=<construction> [at=] [yaw=] [ground=flatten|fit] (only the ground, no pieces: task 0, then build with ground=none; the plan is kept) | step=flatness (read-only)"
                    + " | step=place&name=<construction> (direct, no preview; same keys)"
                    + " | step=place&continue=1 [count=1 (max " + MaxCopies + ")] [name=<default the aimed build's>] (aim at the top of a build: the next copy stacks on it, no ground change)"
                    + " | step=support [id=<default the last>] (read-only) | step=ground [id=] (read-only: terrain against the lowest piece of every 1 m cell) | step=status | step=undo [id=<build id, default the last>] [items=0 (1 = also the items dropped inside the build's box + 2 m)] [confirm=1]"
                    + " - builds a blueprint from BepInEx/config/homecheat/blueprints/ (a file = one construction, a subfolder = one construction of all its files);"
                    + " host only; caps " + MaxPieces + " pieces, footprint " + (int)MaxSide + " m a side, " + MaxVertices + " levelled vertices, " + MaxPoles + " poles, list " + MaxList;
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
            if (step == "ground") return GroundReport(a);
            if (step == "level") return Level(a);
            if (step == "flatness") return Flatness(lastGround);
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
                  .Append(e.Parts.Count == 1 ? e.PieceCount + " pieces, " : e.Parts.Count + " versions (" + BlueprintLibrary.Versions(e) + "; the largest is built unless name=" + e.Name + "/<version>), ")
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
            string note;
            BlueprintEntry e = BlueprintLibrary.Resolve(all, name, out note);
            if (e != null) { if (note.Length > 0) HomeApi.Log("construct: " + name + ": " + note); return e; }
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
            return Build(MakePlan(a, p, e), e, p, a.Flag("overlap", false), false);
        }

        // restore: a build from step=start puts its plan and preview poles back when the ground gate stops it
        string Build(Plan pl, BlueprintEntry e, Player p, bool overlap, bool restore)
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

            // the ground first (flatten: flat at the base; fit: also raised under hanging pieces), then the pieces
            float delay = 0f;
            GroundJob g = null;
            if (ground == "flatten" || ground == "fit") { g = PrepareGround(jobs, baseY, ground == "fit"); delay = g.Ready; }

            string id = e.Name + "-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            long me = p.GetPlayerID();
            Tally t = new Tally();
            string buildName = e.Name;
            // poles only on request (lessons 2026-10-25: wood stands on its own posts; stone on a wooden pole breaks anyway)
            bool poles = ground == "pillars";
            List<string> notes = new List<string>();
            GroundJob ready = g;
            Action build = delegate
            {
                // measure before building (lessons 2026-10-25: the levelling failed silently three times): a ground step that
                // missed more than GroundMiss of its vertices stops here with its report, and nothing is placed
                if (ready != null)
                {
                    float miss;
                    List<Vector3> lows = new List<Vector3>();
                    string flat = Flatness(ready, out miss, lows);
                    HomeApi.Log("construct: " + buildName + " " + flat);
                    if (miss > GroundMiss)
                    {
                        string msg = "construct: " + buildName + " not built, the ground is not ready (" + flat + "); see LogOutput.log for terrain errors";
                        if (restore) { SavePlan(pl); PlacePillars(pl, e); msg += "; the plan and its preview poles are back"; }
                        HomeApi.LogError(msg);
                        Notify.Center(msg);
                        return;
                    }
                    if (lows.Count > 0) jobs.AddRange(Plinth(lows, yaw, origin, notes));
                }
                if (poles) AddPoles(jobs, baseY, t, notes);
                string cleared = ClearSpace(jobs, baseY);
                HomeApi.Log("construct: " + buildName + " " + cleared);
                notes.Add(cleared);
                Run(jobs, id, yaw, buildName, me, t, notes);
            };
            if (delay <= 0f) build(); else Later(delay, build);
            lastId = id; lastName = e.Name; lastPlaced = jobs.Count;

            return (delay <= 0f && jobs.Count <= BatchSize ? "placed " + t.Placed : "placing " + jobs.Count) + " of " + e.Name + " (" + count + " in the blueprint"
                + (skipped > 0 ? ", " + skipped + " skipped: unknown prefabs " + string.Join(" ", missing.ToArray()) : "") + ")"
                + " at " + Fmt.Vec(origin) + " on the " + baseText + " (base " + Fmt.F(baseY, 2) + ", terrain " + Fmt.F(Ground.Height(anchor), 2) + ")"
                + ", " + floorText
                + ", ground " + ground + (g != null ? " (" + (g.Legacy > 0 ? g.Legacy + " old hoe stamps removed, " : "") + g.Targets.Count + " vertices levelled, " + g.Raised + " raised, pieces after " + Fmt.F(delay, 1) + " s)" : "")
                + (t.Poles > 0 ? ", " + t.Poles + " support pole(s)" : "")
                + ", yaw " + Fmt.F(yaw, 1) + ", id " + id
                + "; support on screen in " + Fmt.F(delay + CheckAfter, 0) + " s and settled in " + Fmt.F(delay + SettleCheck, 0) + " s (op=construct&step=support)";
        }

        // step=level (Maksym 2026-10-25, "task 0 - make a flat ground first"): only the ground of a construction, no pieces; the
        // plan is kept with ground=none, so step=start builds on this ground without levelling again
        string Level(Args a)
        {
            Host.RequireServer();
            Player p = Players.Local;
            BlueprintEntry e = Require(a.Required("name", "op=construct&step=list shows the names"));
            Plan pl = MakePlan(a, p, e);
            if (!a.Has("ground")) pl.Ground = "flatten";
            if (pl.Ground != "flatten" && pl.Ground != "fit") throw new CommandException("step=level takes ground=flatten or ground=fit");
            List<string> missing = new List<string>();
            int skipped;
            Vector3 origin;
            string floorText;
            List<Job> jobs = Layout(pl, e, missing, out skipped, out origin, out floorText);
            if (jobs.Count == 0) throw new CommandException("none of the pieces is a prefab this game knows");
            GroundJob g = PrepareGround(jobs, pl.BaseY, pl.Ground == "fit");
            string mode = pl.Ground;
            Later(g.Ready + Settle, delegate
            {
                string msg = "construct: level " + mode + " done; " + Flatness(g);
                HomeApi.Log(msg);
                Notify.Center(msg);
            });
            Plan old = LoadPlan(pl.World);
            if (old != null) RemovePillars(old.PillarId);
            pl.Ground = "none";
            pl.PillarId = "";
            SavePlan(pl);
            return "levelling for " + e.Name + " (" + mode + "): base " + Fmt.F(pl.BaseY, 2) + " on the " + pl.BaseText + ", " + (g.Legacy > 0 ? g.Legacy + " old hoe stamps removed, " : "")
                + g.Targets.Count + " vertices, " + g.Raised + " raised; done in " + Fmt.F(g.Ready, 1) + " s, flatness report " + Fmt.F(Settle, 0) + " s later (or step=flatness)."
                + " The plan is kept with ground=none: step=start&confirm=1 builds on this ground";
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
            // a ground torch on top of every column, seen from afar at night (Maksym 2026-10-25); marked like the poles
            GameObject torch = Prefabs.Get(PillarTorch);
            if (torch == null) torch = Prefabs.Get(PillarTorchFallback);
            float tlo = 0f, thi = 0f;
            if (torch != null && !PieceGhost.Measure(torch, Quaternion.identity, out tlo, out thi)) tlo = 0f;
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
                if (torch != null)
                {
                    float columnTop = surface - lo + (PillarStack - 1) * len + hi;
                    GameObject tg = (GameObject)UnityEngine.Object.Instantiate(torch, new Vector3(pt.x, columnTop - tlo, pt.z), Quaternion.identity);
                    ZNetView tn = tg.GetComponent<ZNetView>();
                    if (tn == null || !tn.IsValid()) UnityEngine.Object.Destroy(tg);
                    else tn.GetZDO().Set(KeyPillar, pl.PillarId);
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
                    + Fmt.F(FitMin, 1) + "-" + Fmt.F(FitMax, 0) + " m up under stone and other ground-needing pieces)" : pl.Ground == "flatten" ? " (the footprint + " + (int)FlattenMargin + " m levelled flat to the base)" : pl.Ground == "pillars" ? " (poles under gaps, terrain unchanged)" : " (unchanged)");
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
            return "preview " + Describe(pl, e) + "; " + n + " poles in 5 columns at the corners and the centre, a ground torch on each"
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
                try { HomeApi.Log("construct: start " + Build(pl, e, p, overlap, true)); }
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
        // the target height of every terrain vertex under the build (FitField): every piece's collider box over the 1 m cells it
        // covers (bushes, stones and other non-pieces as a 0.5 m square at their pivot); fit=false = flat at the base
        static List<Vector3> GroundTargets(List<Job> jobs, float baseY, bool fit, float ax, float az, float bx, float bz, out int raised)
        {
            List<FitField.Box> boxes = new List<FitField.Box>();
            foreach (Job j in jobs)
            {
                if (j.Piece == null) continue;
                Vector3 lo, hi;
                bool needs = NeedsGround(j.Prefab);
                if (PieceGhost.Box(j.Prefab, j.Rot, out lo, out hi))
                    boxes.Add(new FitField.Box(j.Pos.x + lo.x, j.Pos.x + hi.x, j.Pos.z + lo.z, j.Pos.z + hi.z, j.Pos.y + lo.y, needs));
                else
                    boxes.Add(new FitField.Box(j.Pos.x - 0.25f, j.Pos.x + 0.25f, j.Pos.z - 0.25f, j.Pos.z + 0.25f, j.Pos.y, needs));
            }
            List<Vector3> verts = new List<Vector3>();
            foreach (float[] v in FitField.Vertices(boxes, baseY, FitMin, FitMax, fit, ax, az, bx, bz, MaxVertices, out raised)) verts.Add(new Vector3(v[0], v[1], v[2]));
            return verts;
        }

        // where the terrain cannot reach its target (more than TerrainLimit from the world's own ground), a stone foundation
        // (Maksym 2026-10-25: "raise ground level, place stone walls and floor if not enough"): the shortfall in 2 x 2 m tiles
        // turned with the build, per tile one stone floor with its top at the highest target in the tile and stone walls stacked
        // under it down to the real ground, so stone stands on stone. The jobs carry the build's mark: undo takes them too
        List<Job> Plinth(List<Vector3> lows, float yaw, Vector3 origin, List<string> notes)
        {
            List<Job> add = new List<Job>();
            if (lows.Count == 0) return add;
            GameObject floor = Prefabs.Get(PlinthFloor), wall = Prefabs.Get(PlinthWall);
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f), inv = Quaternion.Inverse(rot);
            Vector3 flo, fhi, wlo, whi;
            if (floor == null || wall == null || !PieceGhost.Box(floor, rot, out flo, out fhi) || !PieceGhost.Box(wall, rot, out wlo, out whi) || whi.y - wlo.y < 0.2f)
            {
                notes.Add("no stone foundation: " + PlinthFloor + " or " + PlinthWall + " missing or not measurable");
                return add;
            }
            float wallH = whi.y - wlo.y, floorT = fhi.y - flo.y;
            Dictionary<long, float> top = new Dictionary<long, float>(), ground = new Dictionary<long, float>();
            foreach (Vector3 v in lows)
            {
                Vector3 l = inv * new Vector3(v.x, 0f, v.z);
                long k = ((long)Mathf.FloorToInt(l.x / PlinthTile) << 32) | (uint)Mathf.FloorToInt(l.z / PlinthTile);
                float cur, g = Ground.Height(v);
                if (!top.TryGetValue(k, out cur) || v.y > cur) top[k] = v.y;
                if (!ground.TryGetValue(k, out cur) || g < cur) ground[k] = g;
            }
            int floors = 0, walls = 0;
            foreach (KeyValuePair<long, float> kv in top)
            {
                if (add.Count >= MaxPlinth) { notes.Add("stone foundation capped at " + MaxPlinth + " pieces"); break; }
                int ix = (int)(kv.Key >> 32), iz = (int)(kv.Key & 0xffffffffL);
                Vector3 c = rot * new Vector3((ix + 0.5f) * PlinthTile, 0f, (iz + 0.5f) * PlinthTile);
                add.Add(PlinthJob(floor, new Vector3(c.x, kv.Value - fhi.y, c.z), rot, origin));
                floors++;
                float wallTop = kv.Value - floorT, bottom = ground[kv.Key];
                for (int n = 0; n < MaxPlinthWalls && wallTop > bottom; n++)
                {
                    add.Add(PlinthJob(wall, new Vector3(c.x, wallTop - whi.y, c.z), rot, origin));
                    walls++;
                    wallTop -= wallH;
                }
            }
            notes.Add("stone foundation where the ground cannot reach: " + floors + " floor(s), " + walls + " wall(s)");
            return add;
        }

        static Job PlinthJob(GameObject prefab, Vector3 pos, Quaternion rot, Vector3 origin)
        {
            Job j = new Job();
            j.Prefab = prefab;
            j.Pos = pos;
            j.Rot = rot;
            j.Origin = origin;
            return j;
        }

        // clear the space (Maksym 2026-10-25: "before placing the real construction, clear the space from existing objects"):
        // inside the box of the pieces (+ ClearMargin around, from ClearBelow under the base to ClearMargin over the top) the
        // world's own objects go: trees, logs, stumps and rocks, bushes, pickables, saplings, items on the ground. Never players,
        // creatures, player-built pieces (those refuse the build before this) or this command's own marked objects
        static string ClearSpace(List<Job> jobs, float baseY)
        {
            float ax = float.MaxValue, az = float.MaxValue, bx = float.MinValue, bz = float.MinValue, top = float.MinValue;
            foreach (Job j in jobs) { ax = Mathf.Min(ax, j.Pos.x); az = Mathf.Min(az, j.Pos.z); bx = Mathf.Max(bx, j.Pos.x); bz = Mathf.Max(bz, j.Pos.z); top = Mathf.Max(top, j.Pos.y); }
            Bounds box = new Bounds();
            box.SetMinMax(new Vector3(ax - ClearMargin, baseY - ClearBelow, az - ClearMargin), new Vector3(bx + ClearMargin, top + ClearMargin, bz + ClearMargin));
            Dictionary<string, int> kinds = new Dictionary<string, int>();
            int n = 0;
            foreach (ZNetView nv in UnityEngine.Object.FindObjectsOfType<ZNetView>())
            {
                if (nv == null || !nv.IsValid() || !box.Contains(nv.transform.position)) continue;
                GameObject go = nv.gameObject;
                if (go.GetComponent<Character>() != null) continue;
                ZDO z = nv.GetZDO();
                if (z.GetString(KeyBuild, "").Length > 0 || z.GetString(KeyPillar, "").Length > 0) continue;
                Piece pc = go.GetComponent<Piece>();
                if (pc != null && pc.IsPlacedByPlayer()) continue;
                string kind = go.GetComponent<TreeBase>() != null ? "trees" : go.GetComponent<TreeLog>() != null ? "logs"
                    : go.GetComponent<MineRock5>() != null || go.GetComponent<MineRock>() != null ? "rocks" : go.GetComponent<Destructible>() != null ? "stumps, rocks and bushes"
                    : go.GetComponent<Pickable>() != null ? "pickables" : go.GetComponent<Plant>() != null ? "saplings" : go.GetComponent<ItemDrop>() != null ? "items" : "";
                if (kind.Length == 0) continue;
                nv.ClaimOwnership();
                ZNetScene.instance.Destroy(go);
                int c;
                kinds.TryGetValue(kind, out c);
                kinds[kind] = c + 1;
                n++;
            }
            if (n == 0) return "space clear: nothing to remove";
            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, int> kv in kinds) parts.Add(kv.Value + " " + kv.Key);
            return "space cleared: " + n + " object(s) removed (" + string.Join(", ", parts.ToArray()) + ")";
        }

        // a piece that must rest on the ground: no WearNTear (bushes, pickable stones), or a material other than wood (stone,
        // marble, iron ...): on flat ground only these broke, wood stood on its posts (gumdrop endgame, 2026-10-25)
        static bool NeedsGround(GameObject prefab)
        {
            WearNTear w = prefab.GetComponent<WearNTear>();
            if (w == null) return true;
            string m = w.m_materialType.ToString();
            return m != "Wood" && m != "HardWood";
        }

        // the ground step of a build: the old hoe stamps in the box removed, a moment for the terrain to rebuild, then one level
        // operation per vertex (TerrainLevel.Vertex). Ready = seconds until the ground is done
        sealed class GroundJob
        {
            public int Legacy, Raised;
            public float Ready, BaseY;
            public List<Vector3> Targets;
        }

        GroundJob PrepareGround(List<Job> jobs, float baseY, bool fit)
        {
            float ax = float.MaxValue, az = float.MaxValue, bx = float.MinValue, bz = float.MinValue;
            foreach (Job j in jobs) { ax = Mathf.Min(ax, j.Pos.x); az = Mathf.Min(az, j.Pos.z); bx = Mathf.Max(bx, j.Pos.x); bz = Mathf.Max(bz, j.Pos.z); }
            ax -= FlattenMargin; az -= FlattenMargin; bx += FlattenMargin; bz += FlattenMargin;
            Caps.Check("levelled side", Mathf.Max(bx - ax, bz - az), MaxSide + 2f * FlattenMargin);
            GroundJob g = new GroundJob();
            g.BaseY = baseY;
            g.Legacy = TerrainLevel.RemoveLegacy(ax - LegacyMargin, az - LegacyMargin, bx + LegacyMargin, bz + LegacyMargin);
            g.Targets = GroundTargets(jobs, baseY, fit, ax, az, bx, bz, out g.Raised);
            List<Vector3> todo = g.Targets;
            int nb = (todo.Count + VertexBatch - 1) / VertexBatch;
            float first = g.Legacy > 0 ? LegacyWait : PillarClear;
            for (int b = 0; b < nb; b++)
            {
                int from = b * VertexBatch, to = Mathf.Min(todo.Count, from + VertexBatch);
                Later(first + b * BatchGap, delegate { for (int i = from; i < to; i++) TerrainLevel.Vertex(todo[i].x, todo[i].z, todo[i].y); });
            }
            g.Ready = first + Mathf.Max(0, nb - 1) * BatchGap + Settle;
            lastGround = g;
            return g;
        }

        // how close the terrain came to the targets: within 5 cm, the largest miss, and where
        static string Flatness(GroundJob g)
        {
            float miss;
            return Flatness(g, out miss, null);
        }

        // miss = the share of vertices more than 5 cm off their target (0..1)
        // beyondLows (when given): the vertices left too low by the game's terrain limit, collected for the stone foundation and
        // left out of miss
        static string Flatness(GroundJob g, out float miss, List<Vector3> beyondLows)
        {
            miss = 0f;
            if (g == null || g.Targets == null || g.Targets.Count == 0) return "no levelled ground in this session";
            int ok = 0, high = 0, low = 0, beyond = 0, covered = 0;
            float worst = 0f;
            Vector3 at = Vector3.zero;
            foreach (Vector3 v in g.Targets)
            {
                float d = Ground.Height(v) - v.y;
                if (Mathf.Abs(d) <= 0.05f) ok++; else if (d > 0f) high++; else low++;
                // the game keeps terrain within 8 m of the world's own ground (TerrainComp.LevelTerrain clamps the change at +-8)
                if (Mathf.Abs(d) > 0.05f && WorldGenerator.instance != null && Mathf.Abs(v.y - WorldGenerator.instance.GetHeight(v.x, v.z)) > TerrainLimit - 0.1f)
                {
                    beyond++;
                    if (d < 0f && beyondLows != null) { beyondLows.Add(v); covered++; }
                }
                if (Mathf.Abs(d) > Mathf.Abs(worst)) { worst = d; at = v; }
            }
            miss = 1f - (float)(ok + covered) / g.Targets.Count;
            return "ground: " + ok + " of " + g.Targets.Count + " vertices within 5 cm of their target (" + g.Raised + " raised above the base " + Fmt.F(g.BaseY, 2) + "), "
                + high + " too high, " + low + " too low" + (beyond > 0 ? " (" + beyond + " of them more than " + Fmt.F(TerrainLimit, 0) + " m from the world's own ground, the game's limit)" : "") + (Mathf.Abs(worst) > 0.05f ? ", worst " + Fmt.F(worst, 2) + " m at " + Fmt.F(at.x, 0) + "," + Fmt.F(at.z, 0) : "");
        }

        // after the settle: every piece that was placed and is gone, by prefab and by reason. The reason is read where the piece
        // stood: a solid object right under its bottom (it rested on other pieces), else the gap to the terrain
        static string Broken(List<Job> jobs)
        {
            Dictionary<string, int> byPrefab = new Dictionary<string, int>();
            int onPieces = 0, noGround = 0, onGround = 0, total = 0;
            float worstGap = 0f;
            string worstAt = "";
            foreach (Job j in jobs)
            {
                if (!j.Tried || !(j.Placed == null)) continue;
                total++;
                string pn = j.Prefab.name;
                int c;
                byPrefab.TryGetValue(pn, out c);
                byPrefab[pn] = c + 1;
                float bottom = Bottom(j);
                float gap = bottom - Ground.Height(j.Pos);
                RaycastHit h;
                if (Physics.Raycast(new Vector3(j.Pos.x, bottom + 0.05f, j.Pos.z), Vector3.down, out h, 0.4f, Masks.Solid, QueryTriggerInteraction.Ignore)) onPieces++;
                else if (gap > FitMin) { noGround++; if (gap > worstGap) { worstGap = gap; worstAt = pn + " at " + Fmt.Vec(j.Pos); } }
                else onGround++;
            }
            if (total == 0) return "nothing broke";
            List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>(byPrefab);
            list.Sort(delegate (KeyValuePair<string, int> l, KeyValuePair<string, int> r) { return r.Value.CompareTo(l.Value); });
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < list.Count && i < 10; i++) sb.Append(i == 0 ? "" : ", ").Append(list[i].Key).Append(" ").Append(list[i].Value);
            return total + " broke: " + noGround + " with no ground under them (gap over " + Fmt.F(FitMin, 1) + " m" + (worstGap > 0f ? ", worst " + Fmt.F(worstGap, 2) + " m, " + worstAt : "") + "), "
                + onPieces + " resting on other pieces (those gave way), " + onGround + " on the ground; by prefab: " + sb;
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
                lastBroken = Broken(jobs);
                string msg = "construct: " + name + " settled: " + SupportLine(id, t.Placed);
                HomeApi.Log(msg + "; " + lastBroken + " (id " + id + ")");
                Notify.Center(msg + "; " + lastBroken);
            });
        }

        // one piece the way Player.PlacePiece makes it: creator, OnPlaced; no place effect per piece
        static void PlaceOne(Job j, string id, float yaw, string name, long me, Tally t)
        {
            j.Tried = true;
            GameObject go = (GameObject)UnityEngine.Object.Instantiate(j.Prefab, j.Pos, j.Rot);
            ZNetView nv = go.GetComponent<ZNetView>();
            if (nv == null || !nv.IsValid()) { UnityEngine.Object.Destroy(go); t.Invalid++; return; }
            j.Placed = nv;
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
                // the text of a sign or the tag of a portal (both TextReceiver)
                TextReceiver text = go.GetComponent<TextReceiver>();
                if (text != null) { text.SetText(pc.Extra); t.Signs++; }
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
            return "build " + id + ": " + line + (sb.Length > 0 ? "; weak and critical by prefab: " + sb : "") + (id == lastId && lastBroken.Length > 0 ? "; " + lastBroken : "")
                + " (loaded pieces; grounded = rests on the ground or a rock, then full / good / weak / critical as the hammer colours them)";
        }

        // step=ground (read-only): the terrain under a build against the lowest piece over every 1 m cell, from the pieces
        // as they stand (their colliders): buried (the ground above a piece bottom), right, hanging 0.3-3 m, or higher
        string GroundReport(Args a)
        {
            Host.RequireWorld();
            string id = a.Str("id", lastId);
            if (id.Length == 0) throw new CommandException("no build in this session: add id= (op=construct&step=status lists the loaded ones)");
            List<ZNetView> pieces = Loaded(id);
            if (pieces.Count == 0) throw new CommandException("no loaded pieces of build " + id);
            Dictionary<long, float> own = new Dictionary<long, float>();
            Dictionary<long, string> who = new Dictionary<long, string>();
            foreach (ZNetView nv in pieces)
            {
                Bounds bb = new Bounds();
                bool any = false;
                foreach (Collider c in nv.GetComponentsInChildren<Collider>())
                {
                    if (c == null || c.isTrigger || !c.enabled) continue;
                    if (!any) { bb = c.bounds; any = true; } else bb.Encapsulate(c.bounds);
                }
                Vector3 pos = nv.transform.position;
                float x0 = any ? bb.min.x : pos.x, x1 = any ? bb.max.x : pos.x, z0 = any ? bb.min.z : pos.z, z1 = any ? bb.max.z : pos.z, bottom = any ? bb.min.y : pos.y;
                if (x1 - x0 > 8f || z1 - z0 > 8f) continue;   // a long roof or wall: not a ground marker
                for (int ix = Mathf.FloorToInt(x0); ix <= Mathf.FloorToInt(x1); ix++)
                    for (int iz = Mathf.FloorToInt(z0); iz <= Mathf.FloorToInt(z1); iz++)
                    {
                        long k = ((long)ix << 32) | (uint)iz;
                        float cur;
                        if (!own.TryGetValue(k, out cur) || bottom < cur) { own[k] = bottom; who[k] = Prefabs.BaseName(nv.gameObject); }
                    }
            }
            int buried = 0, deep = 0, right = 0, hanging = 0, high = 0;
            float worstHang = 0f;
            List<KeyValuePair<float, string>> hangs = new List<KeyValuePair<float, string>>();
            foreach (KeyValuePair<long, float> kv in own)
            {
                float x = (int)(kv.Key >> 32) + 0.5f, z = (int)(kv.Key & 0xffffffffL) + 0.5f;
                float t = Ground.Height(new Vector3(x, 0f, z));
                float d = kv.Value - t;
                if (d < -0.5f) deep++;
                else if (d < -0.1f) buried++;
                else if (d < FitMin) right++;
                else if (d <= FitMax)
                {
                    hanging++;
                    if (d > worstHang) worstHang = d;
                    hangs.Add(new KeyValuePair<float, string>(d, who[kv.Key] + " " + Fmt.F(d, 2) + " m up at " + Fmt.F(x, 1) + "," + Fmt.F(z, 1)));
                }
                else high++;
            }
            hangs.Sort(delegate (KeyValuePair<float, string> l, KeyValuePair<float, string> r) { return r.Key.CompareTo(l.Key); });
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hangs.Count && i < 6; i++) sb.Append(i == 0 ? "" : "; ").Append(hangs[i].Value);
            return "ground under " + id + ": " + own.Count + " cells of 1 m; " + right + " right (bottom 0-" + Fmt.F(FitMin, 1) + " m above the ground), "
                + hanging + " hanging " + Fmt.F(FitMin, 1) + "-" + Fmt.F(FitMax, 0) + " m" + (hanging > 0 ? " (worst " + Fmt.F(worstHang, 2) + " m)" : "") + ", "
                + buried + " slightly buried (0.1-0.5 m), " + deep + " deep in the ground (foundations), " + high + " higher than " + Fmt.F(FitMax, 0) + " m (overhangs)"
                + (sb.Length > 0 ? "; worst hanging: " + sb : "");
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
            bool items = a.Flag("items", false);
            Bounds box = new Bounds(pieces[0].transform.position, Vector3.zero);
            foreach (ZNetView nv in pieces) if (nv != null) box.Encapsulate(nv.transform.position);
            box.Expand(new Vector3(ItemMargin * 2f, ItemMargin * 2f + 8f, ItemMargin * 2f));   // broken pieces drop and roll: a margin, and below
            List<ItemDrop> drops = items ? Drops(box) : new List<ItemDrop>();
            if (!a.Flag("confirm", false))
                return "would remove " + pieces.Count + " piece(s) of build " + id + " near " + Fmt.Vec(pieces[0].transform.position)
                    + (items ? " and " + drops.Count + " dropped item(s) inside its box + " + Fmt.F(ItemMargin, 0) + " m" : "") + ": add confirm=1 (the ground stays as it is)";
            int removed = 0;
            foreach (ZNetView nv in pieces)
            {
                if (nv == null || !nv.IsValid()) continue;
                nv.ClaimOwnership();
                ZNetScene.instance.Destroy(nv.gameObject);
                removed++;
            }
            int cleared = 0;
            foreach (ItemDrop d in drops)
            {
                if (d == null) continue;
                ZNetView dn = d.GetComponent<ZNetView>();
                if (dn == null || !dn.IsValid()) continue;
                dn.ClaimOwnership();
                ZNetScene.instance.Destroy(d.gameObject);
                cleared++;
            }
            if (id == lastId) { lastId = ""; lastName = ""; lastPlaced = 0; }
            return "removed " + removed + " piece(s) of build " + id + (items ? " and " + cleared + " dropped item(s) inside its box" : "") + " (loaded pieces only; the ground stays as it is)";
        }

        // items lying on the ground inside a box (the debris of broken pieces; lessons 2026-10-25: the world-wide earthquake
        // clearitems=1 also took the player's own items)
        static List<ItemDrop> Drops(Bounds box)
        {
            List<ItemDrop> found = new List<ItemDrop>();
            foreach (ItemDrop d in UnityEngine.Object.FindObjectsOfType<ItemDrop>())
                if (d != null && box.Contains(d.transform.position)) found.Add(d);
            return found;
        }
    }
}
