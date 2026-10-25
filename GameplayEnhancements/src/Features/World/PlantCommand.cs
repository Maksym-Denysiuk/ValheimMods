using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // plant: saplings in a rows x perrow block. Row 1 is placed so that column 'aimcol' (0-based) stands on the crosshair point;
    // further rows go away from the player. Spacing defaults to twice the sapling's own m_growRadius, the free space a Plant
    // needs to be allowed to grow.
    // area=coords (W3-world-plant, 2026-10-25): the cells of the rectangle with op=coord start and end as opposite corners, edges
    // along world X and Z (GridMath.RectCells); rows/perrow/aimcol ignored. area=aim = the crosshair block (the default).
    // avoid=1: a cell with a player-built piece within clear x scale + PieceMargin is skipped (Constructions.Near).
    public sealed class PlantCommand : CommandBase
    {
        const int MaxSaplings = 2000;   // planted per cast (was 400 and refused a larger Start-end as a whole, test 2026-10-25)
        const int MaxCells = 20000;     // grid cells examined with area=coords; above that the area is refused
        const int BatchSize = 100;      // saplings spawned per step, BatchGap s apart (no frame freeze, no network flood)
        const float BatchGap = 0.1f;
        const float MaxScale = 20f;
        const float PieceMargin = 2f;   // Constructions measures to a piece's pivot: a wall's end reaches past it

        public override string Name { get { return "plant"; } }
        protected override string Usage
        {
            get
            {
                return "plant prefab=<Sapling> [rows=1] [perrow=1] [spacing=<2 x grow radius x scale>] [aimcol=<middle>] [scale=1 (cap " + MaxScale
                    + ")] [clear=<auto>] [area=aim|self|coords (self = from you: row 1 one spacing ahead, rows run towards the crosshair; coords = the rectangle of op=coord start and end, edges along world X/Z; rows, perrow, aimcol ignored)]"
                    + " [avoid=0 (1 = skip cells within clear x scale + " + PieceMargin + " m of a player-built piece)] - saplings at the crosshair; cap " + MaxSaplings + " saplings per cast (more: cast again), " + MaxCells + " cells with area=coords";
            }
        }

        protected override string Execute(Args a)
        {
            Host.RequireWorld();
            string prefab = a.Required("prefab", "e.g. prefab=Birch_Sapling (op=list&filter=sapling)");
            string area = a.Str("area", "aim").ToLowerInvariant();
            if (area != "aim" && area != "coords" && area != "self") throw new CommandException("area must be aim, self or coords (or left out)");
            int rows = a.Int("rows", 1), cols = a.Int("perrow", a.Int("cols", 1));
            int aimcol = a.Int("aimcol", (cols - 1) / 2);
            float spacing = a.Float("spacing", 0f), scale = a.Float("scale", 1f), clear = a.Float("clear", 0f);
            bool avoid = a.Flag("avoid", false);
            if (scale <= 0f) throw new CommandException("scale must be > 0");
            Caps.Check("scale", scale, MaxScale);
            Player p = Players.Local;
            GameObject go = Prefabs.Require(prefab, "(try op=list&filter=sapling)");
            Plant pl = go.GetComponent<Plant>();
            float need = pl != null ? pl.m_growRadius : 1f;
            if (spacing <= 0f) spacing = need * 2f * Mathf.Max(1f, scale);
            if (clear <= 0f) clear = Mathf.Max(0.5f, need * 0.5f);   // free radius around each trunk
            float keep = clear * Mathf.Max(1f, scale) + PieceMargin;

            List<Vector3> cells = new List<Vector3>();
            string where, tail = "";
            if (area == "coords")
            {
                Vector3 s = Coords.Require(Coords.Start), e = Coords.Require(Coords.End);
                int rc, rr;
                GridMath.RectSize(s.x, s.z, e.x, e.z, spacing, out rc, out rr);
                long count = (long)rc * rr;
                if (count > MaxCells) throw new CommandException("cells=" + count + " is over the cap " + MaxCells + " (" + rc + " x " + rr + " at "
                    + Fmt.F(spacing, 1) + " m): move the points closer or use a larger spacing=");
                foreach (GridPoint pt in GridMath.RectCells(s.x, s.z, e.x, e.z, spacing, MaxCells))
                {
                    Vector3 pos = new Vector3(pt.X, 0f, pt.Z);
                    pos.y = Ground.Height(pos);
                    cells.Add(pos);
                }
                bool ignored = a.Has("rows") || a.Has("perrow") || a.Has("cols") || a.Has("aimcol");
                where = "in the rectangle start " + Fmt.Vec(s) + " - end " + Fmt.Vec(e) + " (" + rc + " x " + rr + " cells)"
                    + (ignored ? "; rows/perrow/aimcol ignored with area=coords" : "");
            }
            else
            {
                if (rows * cols > MaxSaplings) throw new CommandException("max " + MaxSaplings + " saplings");
                Anchor an = Anchor.AtAim(p);
                if (area == "self")
                {
                    // self (Maksym 2026-10-25): the block starts at you, row 1 one spacing ahead, and runs towards the crosshair
                    an.Origin = p.transform.position + an.Fwd * spacing;
                    an.Origin.y = Ground.Height(an.Origin);
                    if (!a.Has("aimcol")) aimcol = (cols - 1) / 2;
                }
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        cells.Add(an.Cell(r, c, aimcol, spacing));
                where = "(" + rows + " rows x " + cols + ")";
                tail = area == "self"
                    ? ", from you towards " + (an.Aimed ? "the crosshair" : "where you look") + ", row 1 at " + Fmt.Vec(an.Origin)
                    : ", tree " + (aimcol + 1) + " of row 1 at " + Fmt.Vec(an.Origin) + (an.Aimed ? " (crosshair)" : " (no crosshair hit, 8 m ahead)");
            }

            int made = 0, blocked = 0, nearPieces = 0, noGrow = 0, capped = 0;
            List<Vector3> todo = new List<Vector3>();
            string noGrowWhy = "";
            Piece firstPiece = null;
            Vector3 firstCell = Vector3.zero;
            foreach (Vector3 pos in cells)
            {
                if (avoid)
                {
                    Piece nearest;
                    if (Constructions.Near(pos, keep, out nearest) > 0)
                    {
                        if (firstPiece == null) { firstPiece = nearest; firstCell = pos; }
                        nearPieces++;
                        continue;
                    }
                }
                if (Ground.IsBlocked(pos, clear)) { blocked++; continue; }
                string why;
                if (!GrowRules.Allowed(pl, pos, out why)) { noGrow++; if (noGrowWhy.Length == 0) noGrowWhy = why; continue; }
                if (made >= MaxSaplings) { capped++; continue; }
                todo.Add(pos);
                made++;
            }
            string avoided = avoid ? ", " + nearPieces + " skipped near buildings (keep " + Fmt.F(keep, 1) + " m"
                + (firstPiece != null ? ", e.g. " + Constructions.Describe(firstPiece, firstCell) : "") + ")" : "";
            string batches = Spawn(go, todo, scale);
            return "planted " + made + batches + " " + prefab + " x" + scale.ToString("F2") + " " + where + ", " + blocked
                + " blocked by objects (clear radius " + clear.ToString("F2") + ")"
                + (capped > 0 ? ", " + capped + " over the cap of " + MaxSaplings + " per cast: cast again to plant the rest" : "")
                + (noGrow > 0 ? ", " + noGrow + " not planted: the game would not let them grow there (" + noGrowWhy + ")" : "") + avoided + ", spacing " + spacing.ToString("F2")
                + " m (growRadius " + need.ToString("F2") + ")" + tail;
        }
        // the saplings BatchSize at a time, BatchGap s apart; " in 7 batches over 0.6 s" when more than one
        string Spawn(GameObject go, List<Vector3> todo, float scale)
        {
            int n = (todo.Count + BatchSize - 1) / BatchSize;
            for (int b = 0; b < n; b++)
            {
                int from = b * BatchSize, to = Mathf.Min(todo.Count, from + BatchSize);
                System.Action step = delegate
                {
                    for (int i = from; i < to; i++)
                    {
                        GameObject inst = (GameObject)UnityEngine.Object.Instantiate(go, todo[i], Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
                        Trees.SetScale(inst, scale);   // the grown tree copies this scale when the sapling grows
                    }
                };
                if (b == 0) step(); else Later(b * BatchGap, step);
            }
            return n > 1 ? " in " + n + " batches over " + Fmt.F((n - 1) * BatchGap, 1) + " s" : "";
        }
    }
}
