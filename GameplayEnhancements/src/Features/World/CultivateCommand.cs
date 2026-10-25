using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // cultivate: cultivated soil under a block, one 'cultivate' piece per cell (what the Cultivator tool places)
    // area=coords (W2-world-field, 2026-10-25): the cells of the rectangle with op=coord start and end as opposite corners, edges
    // along world X and Z (GridMath.RectCells, centred, both edges included); rows/perrow/dist are ignored there.
    public sealed class CultivateCommand : CommandBase
    {
        const int MaxPieces = 2500;

        public override string Name { get { return "cultivate"; } }
        protected override string Usage { get { return "cultivate [rows=1] [perrow=1] [spacing=1.5] [dist=<m from you to row 1, default 0 = your own cell>] [area=coords (the rectangle of op=coord start and end, edges along world X/Z; rows, perrow, dist ignored)] - cultivated soil from your cell along your look direction, for op=field; cap " + MaxPieces + " cells"; } }

        protected override string Execute(Args a)
        {
            string area = a.Str("area", "").ToLowerInvariant();
            if (area.Length > 0 && area != "coords") throw new CommandException("area must be coords (or left out)");
            if (area == "coords") return Rectangle(a);
            int rows = a.Int("rows", 1), cols = a.Int("perrow", a.Int("cols", 1));
            float spacing = a.Float("spacing", 1.5f);
            Anchor an = FieldAnchor.AheadOfPlayer(Players.Local, a, spacing);
            GameObject go = Prefabs.Get("cultivate");
            if (go == null) throw new CommandException("no 'cultivate' prefab");
            if (rows * cols > MaxPieces) throw new CommandException("max " + MaxPieces + " pieces");
            float centre = GridMath.CentreColumn(cols);
            int made = 0;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    UnityEngine.Object.Instantiate(go, an.Cell(r, c, centre, spacing), Quaternion.identity);
                    made++;
                }
            return "cultivated " + made + " cells, row 1 at " + Fmt.Vec(an.Origin) + " (" + Vector3.Distance(Players.Local.transform.position, an.Origin).ToString("F1") + " m from you)";
        }

        // area=coords: refused as a whole over the cap (a cut rectangle would leave a strip the player did not ask for)
        string Rectangle(Args a)
        {
            Host.RequireWorld();
            float spacing = a.Float("spacing", 1.5f);
            Vector3 s = Coords.Require(Coords.Start), e = Coords.Require(Coords.End);
            GameObject go = Prefabs.Require("cultivate", "the Cultivator's soil piece");
            int cols, rows;
            GridMath.RectSize(s.x, s.z, e.x, e.z, spacing, out cols, out rows);
            long cells = (long)cols * rows;
            if (cells > MaxPieces) throw new CommandException("cells=" + cells + " is over the cap " + MaxPieces + " (" + cols + " x " + rows + " at " + Fmt.F(spacing, 1) + " m): move the points closer or use a larger spacing=");
            List<GridPoint> pts = GridMath.RectCells(s.x, s.z, e.x, e.z, spacing, MaxPieces);
            foreach (GridPoint pt in pts)
            {
                Vector3 pos = new Vector3(pt.X, 0f, pt.Z);
                pos.y = Ground.Height(pos);
                UnityEngine.Object.Instantiate(go, pos, Quaternion.identity);
            }
            bool ignored = a.Has("rows") || a.Has("perrow") || a.Has("cols") || a.Has("dist");
            return "cultivated " + pts.Count + " cells (" + cols + " x " + rows + ", spacing " + Fmt.F(spacing, 1) + " m, cap " + MaxPieces + ") in the rectangle start "
                + Fmt.Vec(s) + " - end " + Fmt.Vec(e) + (ignored ? "; rows/perrow/dist ignored with area=coords" : "");
        }
    }
}
