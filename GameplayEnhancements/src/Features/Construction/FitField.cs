// ground=fit as one smooth surface (Maksym 2026-10-25: per-column stamps left spikes and hanging floors). Unity-free.
// Every piece lays its collider box over the 1 m cells it covers, with its bottom height. A cell's "own" height is the lowest
// bottom over it. A cell whose own height hangs FitMin..FitMax above the base is a target: the ground rises to it there and
// falls away from it at Slope metres per metre, so neighbouring targets join into one surface and lone markers get a mound,
// not a spike. No cell rises above its own height (nothing is buried), so a floor at the base keeps the ground at the base.
using System;
using System.Collections.Generic;

namespace HomeCheat.Features.Construction
{
    internal static class FitField
    {
        public const float Slope = 0.7f;     // metres down per metre away from a target
        public const float Clearance = 0.02f;   // the ground stays this far under a piece's bottom
        public const float MinRaise = 0.1f;  // cells raised less than this above the base are not stamped

        // one box: x0..x1, z0..z1 (world metres) and the bottom height
        public struct Box
        {
            public float X0, X1, Z0, Z1, Bottom;
            public Box(float x0, float x1, float z0, float z1, float bottom) { X0 = x0; X1 = x1; Z0 = z0; Z1 = z1; Bottom = bottom; }
        }

        // the surface: one entry per raised cell, x and z of the cell centre and the height; lowest first
        public static List<float[]> Build(List<Box> boxes, float baseY, float fitMin, float fitMax, int max)
        {
            Dictionary<long, float> own = new Dictionary<long, float>();
            foreach (Box b in boxes)
            {
                int ix0 = (int)Math.Floor(b.X0), ix1 = (int)Math.Floor(b.X1), iz0 = (int)Math.Floor(b.Z0), iz1 = (int)Math.Floor(b.Z1);
                for (int ix = ix0; ix <= ix1; ix++)
                    for (int iz = iz0; iz <= iz1; iz++)
                    {
                        long k = Key(ix, iz);
                        float cur;
                        if (!own.TryGetValue(k, out cur) || b.Bottom < cur) own[k] = b.Bottom;
                    }
            }
            Dictionary<long, float> surface = new Dictionary<long, float>();
            foreach (KeyValuePair<long, float> t in own)
            {
                float rise = t.Value - baseY;
                if (rise < fitMin || rise > fitMax) continue;
                int tx = (int)(t.Key >> 32), tz = (int)(t.Key & 0xffffffffL);
                int reach = (int)Math.Ceiling(rise / Slope);
                for (int dx = -reach; dx <= reach; dx++)
                    for (int dz = -reach; dz <= reach; dz++)
                    {
                        float h = t.Value - Slope * (float)Math.Sqrt(dx * dx + dz * dz);
                        if (h - baseY < MinRaise) continue;
                        long k = Key(tx + dx, tz + dz);
                        float cur;
                        if (!surface.TryGetValue(k, out cur) || h > cur) surface[k] = h;
                    }
            }
            List<float[]> cells = new List<float[]>();
            foreach (KeyValuePair<long, float> s in surface)
            {
                float h = s.Value, cap;
                if (own.TryGetValue(s.Key, out cap)) h = Math.Min(h, cap - Clearance);
                if (h - baseY < MinRaise) continue;
                int x = (int)(s.Key >> 32), z = (int)(s.Key & 0xffffffffL);
                cells.Add(new float[] { x + 0.5f, h, z + 0.5f });
            }
            cells.Sort(delegate (float[] l, float[] r) { return l[1].CompareTo(r[1]); });
            if (cells.Count > max) cells.RemoveRange(max, cells.Count - max);
            return cells;
        }

        static long Key(int x, int z) { return ((long)x << 32) | (uint)z; }
    }
}
