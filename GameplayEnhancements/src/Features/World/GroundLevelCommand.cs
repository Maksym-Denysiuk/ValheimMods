using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // groundlevel: level the ground around the player to the player's own ground height + delta metres.
    // delta 0 = flatten at the current level; +1 / -1 = one metre up / down (raise like the Hoe's raise, lower like a pickaxe).
    // avoid=1 keeps away from player-built pieces: it refuses when one stands inside the area the level changes
    // (radius + 1 m, or the tool's smooth radius when larger), or with shrink=1 levels a smaller circle instead.
    // Since 2026-10-25 every vertex is set by its own saved TerrainOp (TerrainOps), so the level stays after the area reloads;
    // the hoe stamps it used before came back at the prefab radius (1.5 m) and a levelled area fell apart.
    public sealed class GroundLevelCommand : CommandBase
    {
        const float MaxRadius = 12f;
        const float MinShrunkRadius = 1f;
        const float MaxDelta = 8f;   // the game keeps terrain within ±8 m of the original ground (TerrainComp)
        const float AreaStamp = 3f, AreaStep = 4f;   // area=coords with avoid=1: the old 3 m circles every 4 m decide which parts stay
        const int MaxStamps = 1600;                  // about 160 x 160 m
        const int VertexBatch = 150;                 // level operations per step, 0.1 s apart (terrain rebuilds are heavy)
        const float BatchGap = 0.1f;
        const float LegacyWait = 1f;                 // seconds after removing old hoe stamps: the terrain rebuilds without them
        const float Settle = 1f;                     // seconds after the last operation before the flatness check
        const float LegacyKeep = 1f;                 // avoid=1: an old stamp reaching this close to a piece's pivot stays

        sealed class Job
        {
            public List<Vector2> Verts;
            public float Target;
            public string Where;
        }
        static Job last;   // the last levelled area of this session, for report=1

        public GroundLevelCommand()
        {
            SubFlag("report", "report=1 [area=self|coords] [radius=4] (read-only: how level the ground is. Without area= the last levelled area of this session"
                + " against its target; with area= the circle around you or the start-end rectangle against the median height)", Report);
        }

        public override string Name { get { return "groundlevel"; } }
        protected override string Usage
        {
            get
            {
                return "groundlevel [delta=0 (m up or down from where you stand; the game clamps at ±8 m from the original ground)] [radius=4 (max 12)] [tool=raise (terrain prefab: its paint goes on the levelled ground)]"
                    + " [avoid=0 (1 = refuse near player-built pieces)] [shrink=0 (with avoid=1: level a smaller circle instead, min 1 m)]"
                    + " [area=self|coords (coords = the rectangle of op=coord start and end, levelled to your height + delta; radius and shrink ignored;"
                    + " with avoid=1 the parts near buildings are left as they are; cap " + MaxStamps + " stamps of " + Fmt.F(AreaStep, 0) + " m, about 160 x 160 m)]";
            }
        }

        protected override string Execute(Args a)
        {
            float delta = a.Float("delta", 0f), radius = a.Float("radius", 4f);
            string tool = a.Str("tool", "raise");
            bool avoid = a.Flag("avoid", false), shrink = a.Flag("shrink", false);
            Host.RequireWorld();
            Player p = Players.Local;
            if (radius <= 0f || radius > MaxRadius) throw new CommandException("radius must be 0..12 m");
            GameObject go = Prefabs.Require(tool, "(try op=list&filter=raise)");
            TerrainModifier tm = go.GetComponent<TerrainModifier>();
            if (tm == null) throw new CommandException("'" + tool + "' has no TerrainModifier (use op=inspect&prefab=" + tool + ")");
            string area = a.Str("area", "self").ToLowerInvariant();
            if (area != "self" && area != "coords") throw new CommandException("area must be self or coords (or left out)");
            if (area == "coords") return Rectangle(p, go, delta, avoid);
            Vector3 c = p.transform.position;
            float smooth = tm.m_smooth ? tm.m_smoothRadius : 0f;
            string shrunk = "";
            if (avoid)
            {
                // the level changes the vertices within the radius and the slopes 1 m beyond; a piece's distance is to its pivot
                float reach = Mathf.Max(smooth, radius + 1f);
                Piece nearest;
                int n = Constructions.Near(c, reach, out nearest);
                if (n > 0)
                {
                    float d = Vector3.Distance(c, nearest.transform.position);
                    float r = d - 1.1f;
                    if (!shrink)
                        throw new CommandException("piece " + Constructions.Describe(nearest, c) + " inside the radius (" + n + " piece(s) within "
                            + Fmt.F(reach, 1) + " m): move away, lower radius= or add shrink=1");
                    if (r < MinShrunkRadius || smooth >= d)
                        throw new CommandException("piece " + Constructions.Describe(nearest, c) + " is too close to level even a " + Fmt.F(MinShrunkRadius, 0)
                            + " m circle: move away");
                    shrunk = ", radius shrunk " + Fmt.F(radius, 1) + " -> " + Fmt.F(r, 1) + " m (" + Constructions.Describe(nearest, c) + ")";
                    radius = r;
                }
            }
            float baseY = Ground.Height(c);
            float target = baseY + delta;
            List<Vector2> verts = TerrainOps.Disc(c, radius);
            List<Vector3> paint = new List<Vector3>();
            paint.Add(new Vector3(c.x, target, c.z));
            int kept;
            int legacy = TerrainOps.RemoveLegacy(c.x - radius, c.z - radius, c.x + radius, c.z + radius, avoid, LegacyKeep, out kept);
            string where = "the circle at " + Fmt.F(c.x, 0) + "," + Fmt.F(c.z, 0) + " r=" + Fmt.F(radius, 1) + " m";
            int took;
            float ready = Run(verts, target, paint, go, legacy, where, out took);
            return "ground levelled around " + Fmt.Vec(c) + " r=" + Fmt.F(radius, 1) + " m: " + Fmt.F(baseY, 2) + " -> " + Fmt.F(target, 2)
                + " (delta " + Fmt.F(delta, 1) + ", tool " + tool + ", " + verts.Count + " vertices)" + shrunk + Took(took) + Legacy(legacy, kept) + Clamp(delta)
                + "; flatness check in the log in " + Fmt.F(ready, 1) + " s (or op=groundlevel&report=1)";
        }

        // area=coords (Maksym 2026-10-25, the Flatten spell): every vertex of the start-end rectangle levelled to your ground
        // height + delta. With avoid=1 the parts near buildings stay as they are, by the old rule: a 3 m circle every 4 m is
        // left out when a player-built piece stands within 4 m of its centre, and only vertices inside a kept circle are levelled
        string Rectangle(Player p, GameObject go, float delta, bool avoid)
        {
            Vector3 s = Coords.Require(Coords.Start), e = Coords.Require(Coords.End);
            int cols, rows;
            GridMath.RectSize(s.x, s.z, e.x, e.z, AreaStep, out cols, out rows);
            long n = (long)cols * rows;
            if (n > MaxStamps) throw new CommandException("stamps=" + n + " is over the cap " + MaxStamps + " (" + cols + " x " + rows + " every " + Fmt.F(AreaStep, 0) + " m): move the points closer");
            float baseY = Ground.Height(p.transform.position);
            float target = baseY + delta;
            TerrainModifier tm = go.GetComponent<TerrainModifier>();
            float reach = Mathf.Max(tm.m_smooth ? tm.m_smoothRadius : 0f, AreaStamp + 1f);
            List<Vector3> centres = new List<Vector3>();
            int skipped = 0;
            foreach (GridPoint pt in GridMath.RectCells(s.x, s.z, e.x, e.z, AreaStep, MaxStamps))
            {
                Piece nearest;
                if (avoid && Constructions.Near(new Vector3(pt.X, Ground.Height(new Vector3(pt.X, 0f, pt.Z)), pt.Z), reach, out nearest) > 0) { skipped++; continue; }
                centres.Add(new Vector3(pt.X, target, pt.Z));
            }
            List<Vector2> verts = TerrainOps.Box(s.x, s.z, e.x, e.z);
            int left = 0;
            if (skipped > 0)
            {
                List<Vector2> keep = new List<Vector2>();
                foreach (Vector2 v in verts)
                {
                    bool covered = false;
                    for (int i = 0; !covered && i < centres.Count; i++)
                    {
                        float dx = v.x - centres[i].x, dz = v.y - centres[i].z;
                        covered = dx * dx + dz * dz <= AreaStamp * AreaStamp;
                    }
                    if (covered) keep.Add(v); else left++;
                }
                verts = keep;
            }
            int kept;
            int legacy = TerrainOps.RemoveLegacy(Mathf.Min(s.x, e.x), Mathf.Min(s.z, e.z), Mathf.Max(s.x, e.x), Mathf.Max(s.z, e.z), avoid, LegacyKeep, out kept);
            string where = "the start-end rectangle " + Fmt.F(s.x, 0) + "," + Fmt.F(s.z, 0) + " - " + Fmt.F(e.x, 0) + "," + Fmt.F(e.z, 0);
            int took;
            float ready = Run(verts, target, centres, go, legacy, where, out took);
            int batches = (verts.Count + VertexBatch - 1) / VertexBatch;
            return "ground levelled in the rectangle start " + Fmt.Vec(s) + " - end " + Fmt.Vec(e) + " (" + verts.Count + " vertices"
                + (batches > 1 ? ", " + batches + " batches over " + Fmt.F((batches - 1) * BatchGap, 1) + " s" : "") + "): " + Fmt.F(baseY, 2) + " -> " + Fmt.F(target, 2)
                + " (delta " + Fmt.F(delta, 1) + ")" + (skipped > 0 ? ", " + skipped + " of " + n + " parts skipped near buildings (" + left + " vertices left as they are)" : "")
                + Took(took) + Legacy(legacy, kept) + Clamp(delta) + "; flatness check in the log in " + Fmt.F(ready, 1) + " s (or op=groundlevel&report=1)";
        }

        // the level operations in batches (after a wait when old stamps were removed), then the tool's paint at the given
        // points, then the flatness line in the log. Every batch first takes the terrain zones another peer owns (took = the
        // zones taken now). Returns the seconds until that check
        float Run(List<Vector2> verts, float target, List<Vector3> paint, GameObject tool, int legacy, string where, out int took)
        {
            TerrainOps.Prepare();
            string pt = TerrainOps.PaintTemplate(tool, 0f, false);
            TerrainModifier tm = tool.GetComponent<TerrainModifier>();
            float reach = Mathf.Max(1f, Mathf.Max(tm.m_paintRadius, tm.m_smooth ? tm.m_smoothRadius : 0f));
            float x0, z0, x1, z1;
            bool any = TerrainOps.Bounds(verts, paint, out x0, out z0, out x1, out z1);
            took = any ? TerrainOps.Claim(x0, z0, x1, z1, reach) : 0;
            if (took > 0) HomeApi.Log("groundlevel: took over " + took + " terrain zone(s) owned by another peer, so the level operations land there");
            float first = legacy > 0 ? LegacyWait : 0f;
            int nb = (verts.Count + VertexBatch - 1) / VertexBatch;
            for (int b = 0; b < nb; b++)
            {
                int from = b * VertexBatch, to = Mathf.Min(verts.Count, from + VertexBatch);
                System.Action step = delegate { TerrainOps.Claim(x0, z0, x1, z1, reach); for (int i = from; i < to; i++) TerrainOps.Vertex(verts[i].x, verts[i].y, target); };
                if (first + b * BatchGap <= 0f) step(); else Later(first + b * BatchGap, step);
            }
            float end = first + Mathf.Max(0, nb - 1) * BatchGap;
            if (pt != null) Later(end + BatchGap, delegate { TerrainOps.Claim(x0, z0, x1, z1, reach); foreach (Vector3 q in paint) TerrainOps.Paint(pt, q); });
            Job j = new Job();
            j.Verts = verts;
            j.Target = target;
            j.Where = where;
            last = j;
            Later(end + Settle, delegate { HomeApi.Log("groundlevel: " + where + ": " + TerrainOps.Flatness(j.Verts, j.Target)
                + (any ? "; " + TerrainOps.OwnersLine(x0, z0, x1, z1, 1f) : "")); });
            return end + Settle;
        }

        string Report(Args a)
        {
            Host.RequireWorld();
            Player p = Players.Local;
            if (!a.Has("area"))
            {
                if (last == null) throw new CommandException("nothing levelled in this session: give area=self or area=coords to measure against the median height");
                float x0, z0, x1, z1;
                return "flatness of " + last.Where + ": " + TerrainOps.Flatness(last.Verts, last.Target)
                    + (TerrainOps.Bounds(last.Verts, null, out x0, out z0, out x1, out z1) ? "; " + TerrainOps.OwnersLine(x0, z0, x1, z1, 1f) : "");
            }
            string area = a.Str("area", "self").ToLowerInvariant();
            if (area == "coords")
            {
                Vector3 s = Coords.Require(Coords.Start), e = Coords.Require(Coords.End);
                int cols, rows;
                GridMath.RectSize(s.x, s.z, e.x, e.z, AreaStep, out cols, out rows);
                Caps.Check("stamps", cols * rows, MaxStamps);
                return "flatness of the start-end rectangle: " + TerrainOps.Flatness(TerrainOps.Box(s.x, s.z, e.x, e.z), float.NaN)
                    + "; " + TerrainOps.OwnersLine(s.x, s.z, e.x, e.z, 1f);
            }
            if (area != "self") throw new CommandException("area must be self or coords (or left out)");
            float radius = a.Float("radius", 4f);
            if (radius <= 0f || radius > MaxRadius) throw new CommandException("radius must be 0..12 m");
            Vector3 c = p.transform.position;
            return "flatness of the circle at " + Fmt.F(c.x, 0) + "," + Fmt.F(c.z, 0) + " r=" + Fmt.F(radius, 1) + " m: " + TerrainOps.Flatness(TerrainOps.Disc(c, radius), float.NaN)
                + "; " + TerrainOps.OwnersLine(c.x - radius, c.z - radius, c.x + radius, c.z + radius, 1f);
        }

        static string Took(int took)
        {
            return took > 0 ? ", took over " + took + " terrain zone(s) owned by another peer" : "";
        }

        static string Legacy(int removed, int kept)
        {
            return (removed > 0 ? ", " + removed + " old hoe stamp(s) removed" : "") + (kept > 0 ? ", " + kept + " old hoe stamp(s) kept near buildings (they may override part of the level)" : "");
        }

        static string Clamp(float delta)
        {
            return Mathf.Abs(delta) > MaxDelta ? "; the game clamps terrain at ±8 m from the original ground, so delta " + Fmt.F(delta, 1) + " stops there" : "";
        }
    }
}
