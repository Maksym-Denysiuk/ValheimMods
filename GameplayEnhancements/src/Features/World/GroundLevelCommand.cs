using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // groundlevel: level the ground around the player to the player's own ground height + delta metres.
    // delta 0 = flatten at the current level; +1 / -1 = one metre up / down (raise like the Hoe's raise, lower like a pickaxe).
    // avoid=1 keeps away from player-built pieces: it refuses when one stands inside the area the stamp changes
    // (level radius, or the smooth radius around it, whichever is larger), or with shrink=1 levels a smaller circle instead.
    public sealed class GroundLevelCommand : CommandBase
    {
        const float MaxRadius = 12f;
        const float MinShrunkRadius = 1f;
        const float MaxDelta = 8f;   // the game keeps terrain within ±8 m of the original ground (TerrainComp)
        const float AreaStamp = 3f, AreaStep = 4f;   // area=coords: circles of 3 m every 4 m cover the rectangle without gaps
        const int MaxStamps = 1600;                  // about 160 x 160 m
        const int BatchSize = 50;                    // stamps per step, 0.1 s apart (terrain rebuilds are heavy)

        public override string Name { get { return "groundlevel"; } }
        protected override string Usage
        {
            get
            {
                return "groundlevel [delta=0 (m up or down from where you stand; the game clamps at ±8 m from the original ground)] [radius=4 (max 12)] [tool=raise (terrain prefab)]"
                    + " [avoid=0 (1 = refuse near player-built pieces)] [shrink=0 (with avoid=1: level a smaller circle instead, min 1 m)]"
                    + " [area=self|coords (coords = the rectangle of op=coord start and end, levelled to your height + delta; radius and shrink ignored;"
                    + " with avoid=1 the parts near buildings are left as they are; cap " + MaxStamps + " stamps)]";
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
            if (area == "coords") return Rectangle(p, go, tm, delta, avoid);
            Vector3 c = p.transform.position;
            float smooth = Refl.GetFloat(tm, "m_smoothRadius", 0f);
            string shrunk = "";
            if (avoid)
            {
                // TerrainStamp smooths out to max(prefab smooth radius, radius + 1); a piece's distance is to its pivot
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
            // the piece levels to its own y + m_levelOffset, so place it at target - offset
            float off = Refl.GetFloat(tm, "m_levelOffset", 0f);
            TerrainStamp.At(go, new Vector3(c.x, target - off, c.z), radius, 0f);
            string clamp = Mathf.Abs(delta) > MaxDelta ? "; the game clamps terrain at ±8 m from the original ground, so delta " + Fmt.F(delta, 1) + " stops there" : "";
            return "ground levelled around " + Fmt.Vec(c) + " r=" + Fmt.F(radius, 1) + " m: " + Fmt.F(baseY, 2) + " -> " + Fmt.F(target, 2)
                + " (delta " + Fmt.F(delta, 1) + ", tool " + tool + ", levelOffset " + Fmt.F(off, 2) + ")" + shrunk + clamp;
        }
            // area=coords (Maksym 2026-10-25, the Flatten spell): the start-end rectangle levelled to your ground height + delta with 3 m
        // stamps every 4 m, in batches; with avoid=1 a stamp whose smoothing would reach a player-built piece is skipped
        string Rectangle(Player p, GameObject go, TerrainModifier tm, float delta, bool avoid)
        {
            Vector3 s = Coords.Require(Coords.Start), e = Coords.Require(Coords.End);
            int cols, rows;
            GridMath.RectSize(s.x, s.z, e.x, e.z, AreaStep, out cols, out rows);
            long n = (long)cols * rows;
            if (n > MaxStamps) throw new CommandException("stamps=" + n + " is over the cap " + MaxStamps + " (" + cols + " x " + rows + " every " + Fmt.F(AreaStep, 0) + " m): move the points closer");
            float baseY = Ground.Height(p.transform.position);
            float target = baseY + delta;
            float off = Refl.GetFloat(tm, "m_levelOffset", 0f);
            float reach = Mathf.Max(Refl.GetFloat(tm, "m_smoothRadius", 0f), AreaStamp + 1f);
            List<Vector3> todo = new List<Vector3>();
            int skipped = 0;
            foreach (GridPoint pt in GridMath.RectCells(s.x, s.z, e.x, e.z, AreaStep, MaxStamps))
            {
                Vector3 at = new Vector3(pt.X, target - off, pt.Z);
                Piece nearest;
                if (avoid && Constructions.Near(new Vector3(pt.X, Ground.Height(new Vector3(pt.X, 0f, pt.Z)), pt.Z), reach, out nearest) > 0) { skipped++; continue; }
                todo.Add(at);
            }
            int batches = (todo.Count + BatchSize - 1) / BatchSize;
            for (int b = 0; b < batches; b++)
            {
                int from = b * BatchSize, to = Mathf.Min(todo.Count, from + BatchSize);
                System.Action step = delegate { for (int i = from; i < to; i++) TerrainStamp.At(go, todo[i], AreaStamp, 0f); };
                if (b == 0) step(); else Later(b * 0.1f, step);
            }
            string clamp = Mathf.Abs(delta) > MaxDelta ? "; the game clamps terrain at ±8 m from the original ground, so delta " + Fmt.F(delta, 1) + " stops there" : "";
            return "ground levelled in the rectangle start " + Fmt.Vec(s) + " - end " + Fmt.Vec(e) + " (" + cols + " x " + rows + " stamps of " + Fmt.F(AreaStamp, 0) + " m"
                + (batches > 1 ? ", " + batches + " batches over " + Fmt.F((batches - 1) * 0.1f, 1) + " s" : "") + "): " + Fmt.F(baseY, 2) + " -> " + Fmt.F(target, 2)
                + " (delta " + Fmt.F(delta, 1) + ")" + (skipped > 0 ? ", " + skipped + " stamp(s) skipped near buildings" : "") + clamp;
        }
    }
}
