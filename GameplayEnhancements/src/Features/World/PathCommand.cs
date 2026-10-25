using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // path: a dirt or stone strip, 'length' m along the direction from the player to the start point and 'width' m wide,
    // starting at the crosshair (or 'dist' m ahead), centred on its axis.
    // from=coords runs it from the stored point start to end instead (op=coord); avoid=1 routes it around trees, rocks and
    // player-built pieces on a 1 m grid (GridRoute, A*). Both stamp along a polyline and cut it at the stamp cap.
    // Since 2026-10-25 every stamp is a saved paint TerrainOp with the stamp's radius (TerrainOps), so the strip keeps its width
    // after the area reloads; the hoe stamps it used before came back at the prefab's paint radius (2 m: a 2 m path grew to 4 m).
    public sealed class PathCommand : CommandBase
    {
        const int MaxStamps = 400;
        const float Margin = 10f;          // grid reaches this far beyond the two points (room for a detour)
        const float CellClear = 0.6f;      // probe radius per grid cell (Ground.IsBlocked)
        const float PieceClear = 1f;       // cells within this distance of a player-built piece's pivot are blocked
        const float EdgeOut = 0.8f;        // report=1 looks this far outside the strip's edge for paint that should not be there
        const float MaskHalfCell = 0.5f;   // the paint mask is a 1 m grid: a paint radius of rr alone leaves the strip thinner than its width (seen 2026-10-25)

        // one point on the path's axis with its sideways direction, for report=1
        sealed class AxisPoint
        {
            public Vector3 At, Right;
        }
        sealed class Last
        {
            public List<AxisPoint> Axis = new List<AxisPoint>();
            public float Half;
            public string What;
            public GameObject Tool;
        }
        static Last last;   // the last path of this session, for report=1

        public PathCommand()
        {
            SubFlag("report", "report=1 (read-only: is the last path of this session still painted with its paint type at its width - samples on the axis and at 0.6 of the half"
                + " width inside, and " + Fmt.F(EdgeOut, 1) + " m outside each edge, where the strip should end)", Report);
        }

        public override string Name { get { return "path"; } }
        protected override string Usage
        {
            get
            {
                return "path [type=dirt|stone] [length=5] [width=2] [tool=<terrain prefab>] [dist=<m ahead instead of the crosshair>]"
                    + " [from=aim|coords (coords = stored start to end, length ignored)] [avoid=0 (1 = route around obstacles on a 1 m grid, max "
                    + GridRoute.MaxCells + " cells)] - max " + MaxStamps + " stamps | report=1 (read-only)";
            }
        }

        protected override string Execute(Args a)
        {
            string type = a.Str("type", "dirt").ToLowerInvariant(), tool = a.Str("tool", "");
            string from = a.Str("from", "aim").ToLowerInvariant();
            float length = a.Float("length", 5f), width = a.Float("width", 2f);
            bool avoid = a.Flag("avoid", false);
            Host.RequireWorld();
            Player p = Players.Local;
            if (tool.Length == 0) tool = type == "stone" ? "paved_road" : "path";
            if (type != "dirt" && type != "stone") throw new CommandException("type must be dirt or stone");
            if (from != "aim" && from != "coords") throw new CommandException("from must be aim or coords");
            if (length <= 0f || width <= 0f) throw new CommandException("length and width must be > 0");
            GameObject go = Prefabs.Get(tool);
            if (go == null) throw new CommandException("no terrain prefab '" + tool + "'");
            float rr = Mathf.Clamp(width / 2f, 0.5f, 4f);
            if (from == "aim" && !avoid) return Straight(p, a, go, type, tool, length, width, rr);

            Vector3 s, e;
            string src;
            if (from == "coords")
            {
                if (!Coords.Get(Coords.Start, out s) || !Coords.Get(Coords.End, out e))
                    throw new CommandException("set coord start and end first (op=coord&set=start, op=coord&set=end)");
                src = "coords";
            }
            else
            {
                Anchor an = Anchor.FromArgs(p, a);
                s = an.Origin; e = an.Origin + an.Fwd * length;
                src = an.Aimed ? "crosshair" : "ahead";
            }
            s.y = 0f; e.y = 0f;
            float direct = Vector3.Distance(s, e);
            if (direct < 0.5f) throw new CommandException("start and end are " + Fmt.F(direct, 1) + " m apart: nothing to pave");

            List<Vector3> line = new List<Vector3>();
            string route = "";
            if (avoid)
            {
                int blocked;
                line = Route(s, e, out blocked);
                route = ", around obstacles: " + (line.Count - 2) + " corner(s), " + blocked + " blocked cell(s)";
            }
            else { line.Add(s); line.Add(e); }

            float total;
            int needed;
            int made = Stamp(go, line, width, rr, out total, out needed);
            string cut = needed > made ? "; cut at " + MaxStamps + " stamps (needed " + needed + "): shorten the path or split it" : "";
            return "path " + type + " from " + src + " " + Fmt.Vec(s) + " to " + Fmt.Vec(e) + ": " + Fmt.F(total, 1) + " m long (direct "
                + Fmt.F(direct, 1) + " m) x " + Fmt.F(width, 1) + " m" + route + ", " + made + " stamps of r=" + Fmt.F(rr, 1) + " (tool " + tool + ")"
                + (from == "coords" && a.Has("length") ? "; length= ignored with from=coords" : "") + cut + "; check with op=path&report=1";
        }

        // today's path: evenly spaced stamps along a straight line from the crosshair, refused over the cap
        string Straight(Player p, Args a, GameObject go, string type, string tool, float length, float width, float rr)
        {
            Anchor an = Anchor.FromArgs(p, a);
            int across = Mathf.Max(1, Mathf.CeilToInt(width / (2f * rr)));
            int along = Mathf.Max(1, Mathf.CeilToInt(length / rr));
            if (across * along > MaxStamps) throw new CommandException("too many stamps (" + (across * along) + " > " + MaxStamps + "); shorten the path");
            int made = 0;
            Painter pt = new Painter(go, rr, width, type + " " + Fmt.F(length, 1) + " x " + Fmt.F(width, 1) + " m from " + Fmt.Vec(an.Origin));
            for (int i = 0; i < along; i++)
            {
                float d = along == 1 ? length / 2f : rr + i * ((length - 2f * rr) / Mathf.Max(1, along - 1));
                if (length < 2f * rr) d = length / 2f;
                pt.Axis(an.Origin + an.Fwd * d, an.Right);
                for (int k = 0; k < across; k++)
                {
                    float lat = across == 1 ? 0f : -(width / 2f - rr) + k * ((width - 2f * rr) / (across - 1));
                    Vector3 pos = an.Origin + an.Fwd * d + an.Right * lat;
                    pos.y = Ground.Height(pos);
                    pt.Stamp(pos);
                    made++;
                }
            }
            pt.Done();
            return "path " + type + " " + length.ToString("F1") + " x " + width.ToString("F1") + " m from " + Fmt.Vec(an.Origin)
                + (an.Aimed ? " (crosshair)" : "") + ": " + made + " stamps of r=" + rr.ToString("F1") + " (tool " + tool + "); check with op=path&report=1";
        }

        // the corners of a route from s to e on a 1 m grid over their bounding box plus Margin; s and e are kept exact
        static List<Vector3> Route(Vector3 s, Vector3 e, out int blockedCount)
        {
            float minX = Mathf.Min(s.x, e.x) - Margin, minZ = Mathf.Min(s.z, e.z) - Margin;
            int w = Mathf.CeilToInt(Mathf.Abs(e.x - s.x) + 2f * Margin) + 1;
            int h = Mathf.CeilToInt(Mathf.Abs(e.z - s.z) + 2f * Margin) + 1;
            if ((long)w * h > GridRoute.MaxCells)
                throw new CommandException("start and end are too far apart for avoid=1: grid " + w + " x " + h + " = " + ((long)w * h)
                    + " cells > " + GridRoute.MaxCells + " (keep them within about 180 m, or leave out avoid=1)");
            Vector3 centre = (s + e) / 2f;
            float half = Mathf.Sqrt(w * w + h * h) / 2f + PieceClear;
            List<Vector3> pieces = new List<Vector3>();
            foreach (Piece pc in Constructions.All(new Vector3(centre.x, Ground.Height(centre), centre.z), half)) pieces.Add(pc.transform.position);
            int count = 0;
            System.Func<int, int, bool> blocked = delegate (int x, int z)
            {
                Vector3 pos = new Vector3(minX + x + 0.5f, 0f, minZ + z + 0.5f);
                pos.y = Ground.Height(pos);
                bool b = Ground.IsBlocked(pos, CellClear);
                for (int i = 0; !b && i < pieces.Count; i++)
                {
                    Vector3 d = pieces[i] - pos; d.y = 0f;
                    if (d.sqrMagnitude < PieceClear * PieceClear) b = true;
                }
                if (b) count++;
                return b;
            };
            GridCell gs = new GridCell(Mathf.Clamp((int)(s.x - minX), 0, w - 1), Mathf.Clamp((int)(s.z - minZ), 0, h - 1));
            GridCell ge = new GridCell(Mathf.Clamp((int)(e.x - minX), 0, w - 1), Mathf.Clamp((int)(e.z - minZ), 0, h - 1));
            List<GridCell> cells;
            GridRouteStatus st = GridRoute.Find(w, h, blocked, gs, ge, out cells);
            blockedCount = count;
            if (st == GridRouteStatus.NoRoute)
                throw new CommandException("no route from start to end around obstacles (grid " + w + " x " + h + ", " + count + " blocked cells): nothing stamped");
            if (st != GridRouteStatus.Found) throw new CommandException("route search failed (" + st + "): nothing stamped");
            List<GridCell> corners = GridRoute.Corners(cells);
            List<Vector3> line = new List<Vector3>();
            line.Add(s);
            for (int i = 1; i < corners.Count - 1; i++) line.Add(new Vector3(minX + corners[i].X + 0.5f, 0f, minZ + corners[i].Z + 0.5f));
            line.Add(e);
            return line;
        }

        // stamps every 'rr' metres along the polyline (y ignored), 'across' stamps wide, until MaxStamps; returns stamps made
        static int Stamp(GameObject go, List<Vector3> line, float width, float rr, out float total, out int needed)
        {
            Painter pt = new Painter(go, rr, width, "from " + Fmt.Vec(line[0]) + " to " + Fmt.Vec(line[line.Count - 1]));
            int across = Mathf.Max(1, Mathf.CeilToInt(width / (2f * rr)));
            total = 0f;
            for (int i = 1; i < line.Count; i++) total += Vector3.Distance(line[i - 1], line[i]);
            int along = Mathf.Max(1, Mathf.CeilToInt(total / rr)) + 1;
            needed = along * across;
            int made = 0, seg = 1;
            float segStart = 0f;
            for (int i = 0; i < along && made < MaxStamps; i++)
            {
                float d = Mathf.Min(total, i * (total / (along - 1)));
                while (seg < line.Count - 1 && d > segStart + Vector3.Distance(line[seg - 1], line[seg]))
                {
                    segStart += Vector3.Distance(line[seg - 1], line[seg]);
                    seg++;
                }
                Vector3 a0 = line[seg - 1], a1 = line[seg];
                Vector3 fwd = a1 - a0;
                float segLen = fwd.magnitude;
                fwd = segLen > 0.001f ? fwd / segLen : Vector3.forward;
                Vector3 right = Vector3.Cross(Vector3.up, fwd);
                Vector3 axis = a0 + fwd * Mathf.Clamp(d - segStart, 0f, segLen);
                pt.Axis(axis, right);
                for (int k = 0; k < across && made < MaxStamps; k++)
                {
                    float lat = across == 1 ? 0f : -(width / 2f - rr) + k * ((width - 2f * rr) / (across - 1));
                    Vector3 pos = axis + right * lat;
                    pos.y = Ground.Height(pos);
                    pt.Stamp(pos);
                    made++;
                }
            }
            pt.Done();
            return made;
        }

        // the stamps of one path as saved terrain operations: the tool's paint and smoothing at radius rr per stamp, and when
        // the tool levels (tool=raise), its level circle as one operation per vertex at the stamp's own height + level offset
        // (the first stamp to reach a vertex sets it; overlapping level operations would stack)
        sealed class Painter
        {
            readonly string template;
            readonly TerrainModifier tm;
            readonly Dictionary<long, Vector3> verts = new Dictionary<long, Vector3>();
            readonly Last path = new Last();

            public Painter(GameObject go, float rr, float width, string what)
            {
                tm = go.GetComponent<TerrainModifier>();
                if (tm == null) throw new CommandException(go.name + " has no TerrainModifier");
                TerrainOps.Prepare();
                template = TerrainOps.PaintTemplate(go, rr + MaskHalfCell, true);
                path.Half = width / 2f;
                path.What = what;
                path.Tool = go;
            }

            public void Axis(Vector3 at, Vector3 right)
            {
                AxisPoint p = new AxisPoint();
                p.At = at;
                p.Right = right;
                path.Axis.Add(p);
            }

            public void Stamp(Vector3 pos)
            {
                if (tm.m_level)
                    foreach (Vector2 v in TerrainOps.Disc(pos, tm.m_levelRadius))
                    {
                        long key = ((long)(int)v.x << 32) ^ (uint)(int)v.y;
                        if (!verts.ContainsKey(key)) verts[key] = new Vector3(v.x, pos.y + tm.m_levelOffset, v.y);
                    }
                if (template != null) TerrainOps.Paint(template, pos);
            }

            public void Done()
            {
                foreach (Vector3 v in verts.Values) TerrainOps.Vertex(v.x, v.z, v.y);
                last = path;
            }
        }

        string Report(Args a)
        {
            Host.RequireWorld();
            if (last == null || last.Axis.Count == 0 || last.Tool == null) throw new CommandException("no path made in this session");
            string type = last.Tool.GetComponent<TerrainModifier>().m_paintType.ToString();
            int inside = 0, inPainted = 0, outside = 0, outPainted = 0;
            foreach (AxisPoint p in last.Axis)
            {
                Vector3[] ins = { p.At, p.At + p.Right * (last.Half * 0.6f), p.At - p.Right * (last.Half * 0.6f) };
                foreach (Vector3 q in ins) { inside++; if (TerrainOps.PaintedLike(q, last.Tool)) inPainted++; }
                Vector3[] outs = { p.At + p.Right * (last.Half + EdgeOut), p.At - p.Right * (last.Half + EdgeOut) };
                foreach (Vector3 q in outs) { outside++; if (TerrainOps.PaintedLike(q, last.Tool)) outPainted++; }
            }
            return "path " + last.What + ": " + inPainted + " of " + inside + " points inside the strip painted " + type + " (" + Fmt.F(100f * inPainted / inside, 1) + " %), "
                + outPainted + " of " + outside + " points " + Fmt.F(EdgeOut, 1) + " m outside its edges painted " + type + " (" + Fmt.F(100f * outPainted / outside, 1)
                + " %; the same paint there from an older path or a bend counts too)";
        }
    }
}
