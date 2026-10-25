// Cultivated soil that hangs together (field, harvest). Two tools:
//   Connected  the flood fill of field's own grid (moved out of FieldCommand, behaviour unchanged): cells of a rows x cols
//              soil grid joined to the start cells through soil, all 8 neighbours.
//   Sample     the patch under a point, sampled on a 0.5 m world X/Z grid over Ground.IsCultivated and grown from the cell
//              under the point through cultivated cells (8 neighbours), at most MaxSamples cells examined.
// A feature helper (not in the core) so it reloads; users: field (default, area=patch), harvest.
using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    internal sealed class FieldPatch
    {
        public const float Step = 0.5f;
        public const int MaxSamples = 40000;

        readonly HashSet<long> cells = new HashSet<long>();
        Vector3 origin;
        int minX, maxX, minZ, maxZ;

        public bool StartSoil { get; private set; }  // the cell under the point is cultivated
        public bool OverCap { get; private set; }    // MaxSamples reached: the patch may go on beyond what was examined
        public int Examined { get; private set; }
        public int Count { get { return cells.Count; } }

        // flood fill over a soil grid from row startRow, columns colFrom..colTo; reached[r, c] = joined to a start cell
        public static bool[,] Connected(bool[,] soil, int startRow, int colFrom, int colTo, out bool startSoil)
        {
            int rows = soil.GetLength(0), cols = soil.GetLength(1);
            bool[,] reached = new bool[rows, cols];
            Queue<int> queue = new Queue<int>();
            for (int c = colFrom; c <= colTo; c++)
                if (soil[startRow, c] && !reached[startRow, c]) { reached[startRow, c] = true; queue.Enqueue(startRow * cols + c); }
            startSoil = queue.Count > 0;
            while (queue.Count > 0)
            {
                int cell = queue.Dequeue(), r0 = cell / cols, c0 = cell % cols;
                for (int dr = -1; dr <= 1; dr++)
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        int r = r0 + dr, c = c0 + dc;
                        if (r < 0 || r >= rows || c < 0 || c >= cols || reached[r, c] || !soil[r, c]) continue;
                        reached[r, c] = true;
                        queue.Enqueue(r * cols + c);
                    }
            }
            return reached;
        }

        // the cultivated patch the point stands on; with the point's own cell not cultivated the patch is empty (StartSoil false)
        public static FieldPatch Sample(Vector3 at)
        {
            FieldPatch fp = new FieldPatch();
            fp.origin = new Vector3(at.x, 0f, at.z);
            HashSet<long> seen = new HashSet<long>();
            Queue<long> queue = new Queue<long>();
            seen.Add(Key(0, 0));
            fp.Examined = 1;
            if (!Ground.IsCultivated(fp.CellPos(0, 0))) return fp;
            fp.StartSoil = true;
            fp.Add(0, 0);
            queue.Enqueue(Key(0, 0));
            while (queue.Count > 0)
            {
                long k = queue.Dequeue();
                int x0 = (int)(k >> 32), z0 = (int)(k & 0xffffffffL);
                for (int dx = -1; dx <= 1; dx++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int x = x0 + dx, z = z0 + dz;
                        long nk = Key(x, z);
                        if (seen.Contains(nk)) continue;
                        if (fp.Examined >= MaxSamples) { fp.OverCap = true; return fp; }
                        seen.Add(nk);
                        fp.Examined++;
                        if (!Ground.IsCultivated(fp.CellPos(x, z))) continue;
                        fp.Add(x, z);
                        queue.Enqueue(nk);
                    }
            }
            return fp;
        }

        // the point lies in a sampled cell of the patch
        public bool Contains(Vector3 pos)
        {
            int x = Mathf.RoundToInt((pos.x - origin.x) / Step), z = Mathf.RoundToInt((pos.z - origin.z) / Step);
            return cells.Contains(Key(x, z));
        }

        // the patch's bounding box on the ground (corners of the outer sample cells)
        public void Bounds(out float ax, out float az, out float bx, out float bz)
        {
            ax = origin.x + (minX - 0.5f) * Step; bx = origin.x + (maxX + 0.5f) * Step;
            az = origin.z + (minZ - 0.5f) * Step; bz = origin.z + (maxZ + 0.5f) * Step;
        }

        // about the patch's area in m² (sample cells x 0.25)
        public float Area { get { return cells.Count * Step * Step; } }

        void Add(int x, int z)
        {
            if (cells.Count == 0) { minX = maxX = x; minZ = maxZ = z; }
            else
            {
                if (x < minX) minX = x; if (x > maxX) maxX = x;
                if (z < minZ) minZ = z; if (z > maxZ) maxZ = z;
            }
            cells.Add(Key(x, z));
        }

        Vector3 CellPos(int x, int z)
        {
            Vector3 pos = new Vector3(origin.x + x * Step, 0f, origin.z + z * Step);
            pos.y = Ground.Height(pos);
            return pos;
        }

        static long Key(int x, int z) { return ((long)x << 32) | (uint)z; }
    }
}
