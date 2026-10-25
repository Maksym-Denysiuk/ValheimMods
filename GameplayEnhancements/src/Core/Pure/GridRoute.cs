// Shortest route on an integer grid: A* with 8 neighbours (straight cost 10, diagonal 14, octile heuristic).
// A diagonal step is taken only when both straight cells beside it are free, so a route never squeezes between two blocked
// corners. Start and end are always passable (they are the points the player chose). Grids over MaxCells are refused before
// any search. No Unity reference: also compiled into HomeCheat.SelfTest.exe. Users: path (from=coords&avoid=1).
using System;
using System.Collections.Generic;

namespace HomeCheat
{
    public static class GridRoute
    {
        public const int MaxCells = 40000;

        static readonly int[] DX = { 1, -1, 0, 0, 1, 1, -1, -1 };
        static readonly int[] DZ = { 0, 0, 1, -1, 1, -1, 1, -1 };

        // blocked(x, z) is asked at most once per cell; path is null unless the status is Found
        public static GridRouteStatus Find(int width, int height, Func<int, int, bool> blocked, GridCell start, GridCell end, out List<GridCell> path)
        {
            path = null;
            if (width <= 0 || height <= 0) return GridRouteStatus.Invalid;
            if ((long)width * height > MaxCells) return GridRouteStatus.OverCap;
            if (!Inside(start, width, height) || !Inside(end, width, height)) return GridRouteStatus.Invalid;

            int n = width * height;
            int s = start.Z * width + start.X, e = end.Z * width + end.X;
            sbyte[] state = new sbyte[n];          // 0 unknown, 1 free, 2 blocked
            int[] g = new int[n];
            int[] from = new int[n];
            bool[] closed = new bool[n];
            for (int i = 0; i < n; i++) { g[i] = int.MaxValue; from[i] = -1; }
            state[s] = 1; state[e] = 1;
            g[s] = 0;
            MinHeap open = new MinHeap();
            open.Push(H(start.X, start.Z, end), s);
            while (open.Count > 0)
            {
                int cur = open.Pop();
                if (closed[cur]) continue;
                if (cur == e) { path = Walk(from, e, width); return GridRouteStatus.Found; }
                closed[cur] = true;
                int cx = cur % width, cz = cur / width;
                for (int d = 0; d < 8; d++)
                {
                    int nx = cx + DX[d], nz = cz + DZ[d];
                    if (nx < 0 || nz < 0 || nx >= width || nz >= height) continue;
                    int ni = nz * width + nx;
                    if (closed[ni] || !Free(ni, nx, nz, state, blocked)) continue;
                    bool diagonal = d >= 4;
                    if (diagonal && (!Free(cz * width + nx, nx, cz, state, blocked) || !Free(nz * width + cx, cx, nz, state, blocked))) continue;
                    int cost = g[cur] + (diagonal ? 14 : 10);
                    if (cost >= g[ni]) continue;
                    g[ni] = cost;
                    from[ni] = cur;
                    open.Push(cost + H(nx, nz, end), ni);
                }
            }
            return GridRouteStatus.NoRoute;
        }

        // the cells of a path where its direction changes, plus the first and the last: the polyline to stamp along
        public static List<GridCell> Corners(List<GridCell> path)
        {
            List<GridCell> c = new List<GridCell>();
            if (path == null || path.Count == 0) return c;
            c.Add(path[0]);
            for (int i = 1; i < path.Count - 1; i++)
            {
                int ax = path[i].X - path[i - 1].X, az = path[i].Z - path[i - 1].Z;
                int bx = path[i + 1].X - path[i].X, bz = path[i + 1].Z - path[i].Z;
                if (ax != bx || az != bz) c.Add(path[i]);
            }
            if (path.Count > 1) c.Add(path[path.Count - 1]);
            return c;
        }

        static bool Inside(GridCell c, int w, int h) { return c.X >= 0 && c.Z >= 0 && c.X < w && c.Z < h; }

        static bool Free(int i, int x, int z, sbyte[] state, Func<int, int, bool> blocked)
        {
            if (state[i] == 0) state[i] = (sbyte)(blocked != null && blocked(x, z) ? 2 : 1);
            return state[i] == 1;
        }

        static int H(int x, int z, GridCell end)
        {
            int dx = Math.Abs(x - end.X), dz = Math.Abs(z - end.Z);
            return 10 * Math.Max(dx, dz) + 4 * Math.Min(dx, dz);
        }

        static List<GridCell> Walk(int[] from, int e, int width)
        {
            List<GridCell> p = new List<GridCell>();
            for (int i = e; i >= 0; i = from[i]) p.Add(new GridCell(i % width, i / width));
            p.Reverse();
            return p;
        }

        // binary heap of (priority, cell index); stale entries are skipped by the closed check above
        sealed class MinHeap
        {
            readonly List<long> items = new List<long>();

            public int Count { get { return items.Count; } }

            public void Push(int priority, int index)
            {
                items.Add(((long)priority << 32) | (uint)index);
                int i = items.Count - 1;
                while (i > 0)
                {
                    int up = (i - 1) / 2;
                    if (items[up] <= items[i]) break;
                    long t = items[up]; items[up] = items[i]; items[i] = t;
                    i = up;
                }
            }

            public int Pop()
            {
                long top = items[0];
                int last = items.Count - 1;
                items[0] = items[last];
                items.RemoveAt(last);
                int i = 0;
                while (true)
                {
                    int l = 2 * i + 1, r = l + 1, m = i;
                    if (l < items.Count && items[l] < items[m]) m = l;
                    if (r < items.Count && items[r] < items[m]) m = r;
                    if (m == i) break;
                    long t = items[m]; items[m] = items[i]; items[i] = t;
                    i = m;
                }
                return (int)(top & 0xffffffffL);
            }
        }
    }
}
