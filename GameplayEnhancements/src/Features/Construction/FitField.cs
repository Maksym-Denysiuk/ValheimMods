// The ground under a construction as one target height per terrain vertex (Maksym 2026-10-25: per-column stamps left spikes
// and hanging floors; overlapping level squares stacked their changes). Unity-free.
// Every piece lays its collider box over the 1 m cells it covers, with its bottom height; a cell's "own" height is the lowest
// bottom over it. A cell whose lowest piece needs ground (stone and other non-wood materials, bushes, loose objects:
// on flat ground only those broke, 2026-10-25), does not rest on non-wood pieces that have their own foot (Carried),
// and hangs FitMin..FitMax above the base is a target; wooden pieces stand on
// their posts and only cap the ground: the ground rises to it there and
// falls away at Slope metres per metre, so neighbouring targets join into one surface and a lone marker gets a mound, not a
// spike; elsewhere the ground is the base. A vertex next to a target always reaches it (a wooden piece beside it may touch
// the ground: that only supports it); on the slopes a vertex never rises above the bottom of a piece resting on one of its
// four cells (foundations below the base may sink), so the ground does not push through the house floors. The terrain has one vertex per metre, at whole
// world coordinates, so one level operation per vertex sets each exactly once: nothing stacks.
using System;
using System.Collections.Generic;

namespace HomeCheat.Features.Construction
{
    internal static class FitField
    {
        public const float Slope = 0.7f;        // metres down per metre away from a target
        public const float Clearance = 0.02f;   // the ground stays this far under a piece's bottom
        public const float Rest = 0.1f;         // a piece rests on another when its bottom is at most this far over the other's top
        public const float Overlap = 0.1f;      // ... and the two overlap by more than this on both ground axes

        // one box: x0..x1, z0..z1 (world metres), the bottom and top heights; Carries = a building piece of a non-wood
        // material (stone, marble, iron ...), which can carry the non-wood piece resting on it
        public struct Box
        {
            public float X0, X1, Z0, Z1, Bottom, Top;
            public bool NeedsGround, Carries;
            public Box(float x0, float x1, float z0, float z1, float bottom, float top, bool needsGround, bool carries)
            {
                X0 = x0; X1 = x1; Z0 = z0; Z1 = z1; Bottom = bottom; Top = top; NeedsGround = needsGround; Carries = carries;
            }
        }

        // which boxes rest on a non-wood structure that has its own foot (Maksym 2026-10-25, portal hub: a stone plaza 4 m up
        // on stone walls got the whole lower level filled with earth). A foot is a Carries box with no Carries box under it:
        // it keeps its ground (the fit raises the terrain to it as before). A Carries box resting on the top of a foot, or of
        // a box carried by one, is carried and needs no ground of its own. Only straight down: stone passes no support
        // sideways (WearNTear.GetMaterialProperties, 1.0.20: stone horizontal loss 1). Boxes in the build's own frame, so
        // pieces turned with the build stay axis-aligned
        public static bool[] Carried(List<Box> boxes)
        {
            int n = boxes.Count;
            bool[] carried = new bool[n], reached = new bool[n];
            Queue<int> todo = new Queue<int>();
            for (int i = 0; i < n; i++)
            {
                if (!boxes[i].Carries) continue;
                bool foot = true;
                for (int k = 0; k < n && foot; k++) if (k != i && boxes[k].Carries && Rests(boxes[i], boxes[k])) foot = false;
                if (foot) { reached[i] = true; todo.Enqueue(i); }
            }
            while (todo.Count > 0)
            {
                Box lo = boxes[todo.Dequeue()];
                for (int j = 0; j < n; j++)
                {
                    if (reached[j] || !boxes[j].Carries || !Rests(boxes[j], lo)) continue;
                    reached[j] = carried[j] = true;
                    todo.Enqueue(j);
                }
            }
            return carried;
        }

        // up rests on lo: up's bottom is above lo's bottom and at most Rest over lo's top, and they overlap on both ground axes
        static bool Rests(Box up, Box lo)
        {
            if (up.Bottom <= lo.Bottom || up.Bottom > lo.Top + Rest) return false;
            return Math.Min(up.X1, lo.X1) - Math.Max(up.X0, lo.X0) > Overlap && Math.Min(up.Z1, lo.Z1) - Math.Max(up.Z0, lo.Z0) > Overlap;
        }

        // the target height of every vertex in the rectangle ax..bx, az..bz: x, height, z per vertex (at most max); raised
        // = how many are above the base. carried (Carried, same order as boxes, or null): those boxes need no ground
        public static List<float[]> Vertices(List<Box> boxes, bool[] carried, float baseY, float fitMin, float fitMax, bool fit,
            float ax, float az, float bx, float bz, int max, out int raised)
        {
            Dictionary<long, float> own = new Dictionary<long, float>();
            Dictionary<long, bool> needs = new Dictionary<long, bool>();   // the lowest piece of the cell needs ground
            for (int i = 0; i < boxes.Count; i++)
            {
                Box b = boxes[i];
                bool nd = b.NeedsGround && (carried == null || !carried[i]);
                int ix0 = (int)Math.Floor(b.X0), ix1 = (int)Math.Floor(b.X1), iz0 = (int)Math.Floor(b.Z0), iz1 = (int)Math.Floor(b.Z1);
                for (int ix = ix0; ix <= ix1; ix++)
                    for (int iz = iz0; iz <= iz1; iz++)
                    {
                        long k = Key(ix, iz);
                        float cur;
                        if (!own.TryGetValue(k, out cur) || b.Bottom < cur) { own[k] = b.Bottom; needs[k] = nd; }
                    }
            }
            // cell surface: the slopes around every target (cells not in it stay at the base)
            Dictionary<long, float> surface = new Dictionary<long, float>();
            if (fit)
                foreach (KeyValuePair<long, float> t in own)
                {
                    float rise = t.Value - baseY;
                    if (rise < fitMin || rise > fitMax || !needs[t.Key]) continue;
                    int tx = (int)(t.Key >> 32), tz = (int)(t.Key & 0xffffffffL);
                    int reach = (int)Math.Ceiling(rise / Slope);
                    for (int dx = -reach; dx <= reach; dx++)
                        for (int dz = -reach; dz <= reach; dz++)
                        {
                            float h = t.Value - Slope * (float)Math.Sqrt(dx * dx + dz * dz);
                            if (h <= baseY) continue;
                            long k = Key(tx + dx, tz + dz);
                            float cur;
                            if (!surface.TryGetValue(k, out cur) || h > cur) surface[k] = h;
                        }
                }
            List<float[]> verts = new List<float[]>();
            raised = 0;
            int vx0 = (int)Math.Ceiling(ax), vx1 = (int)Math.Floor(bx), vz0 = (int)Math.Ceiling(az), vz1 = (int)Math.Floor(bz);
            for (int vx = vx0; vx <= vx1; vx++)
                for (int vz = vz0; vz <= vz1; vz++)
                {
                    float h = baseY, cap = float.MaxValue, target = baseY;
                    for (int cx = vx - 1; cx <= vx; cx++)
                        for (int cz = vz - 1; cz <= vz; cz++)
                        {
                            long k = Key(cx, cz);
                            float s, o;
                            if (surface.TryGetValue(k, out s) && s > h) h = s;
                            if (own.TryGetValue(k, out o) && o >= baseY - Clearance && o - Clearance < cap) cap = o - Clearance;
                            bool nd;
                            if (fit && own.TryGetValue(k, out o) && needs.TryGetValue(k, out nd) && nd && o - baseY >= fitMin && o - baseY <= fitMax && o - Clearance > target) target = o - Clearance;
                        }
                    if (h > cap) h = Math.Max(cap, baseY);
                    if (target > h) h = target;
                    if (h > baseY + 0.01f) raised++;
                    verts.Add(new float[] { vx, h, vz });
                    if (verts.Count >= max) return verts;
                }
            return verts;
        }

        static long Key(int x, int z) { return ((long)x << 32) | (uint)z; }
    }
}
