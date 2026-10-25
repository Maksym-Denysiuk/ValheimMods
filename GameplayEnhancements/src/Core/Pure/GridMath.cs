// Offsets of one cell in a rows x columns block: 'along' runs away from the player (row r), 'across' to the right.
// 'centre' is the column (0-based, may be fractional) that sits on the anchor line: (cols - 1) / 2 centres the block,
// an integer aim column puts that column on the crosshair. No Unity reference: also compiled into HomeCheat.SelfTest.exe.
// RectCells: cell centres inside the rectangle spanned by two corner points (edges along world X and Z).
using System;
using System.Collections.Generic;

namespace HomeCheat
{
    public static class GridMath
    {
        public static float CentreColumn(int cols) { return (cols - 1) / 2f; }

        public static void Offset(int row, int col, float centre, float spacing, out float along, out float across)
        {
            along = row * spacing;
            across = (col - centre) * spacing;
        }

        // columns (along X) and rows (along Z) of a 'spacing' grid that fits between the corners a and b, both edges included
        public static void RectSize(float ax, float az, float bx, float bz, float spacing, out int cols, out int rows)
        {
            if (spacing <= 0f) throw new CommandException("spacing must be > 0");
            cols = (int)Math.Floor(Math.Abs(bx - ax) / spacing + 0.0001) + 1;
            rows = (int)Math.Floor(Math.Abs(bz - az) / spacing + 0.0001) + 1;
        }

        // Cell centres of that grid, row by row from the low-Z edge, low X first; the part of the width (and depth) that is
        // less than one spacing is split evenly between both edges, so the grid sits centred in the rectangle.
        // At most 'max' cells are returned: compare with cols * rows from RectSize to report the cap.
        public static List<GridPoint> RectCells(float ax, float az, float bx, float bz, float spacing, int max)
        {
            int cols, rows;
            RectSize(ax, az, bx, bz, spacing, out cols, out rows);
            float minX = Math.Min(ax, bx), minZ = Math.Min(az, bz);
            float x0 = minX + (Math.Abs(bx - ax) - (cols - 1) * spacing) / 2f;
            float z0 = minZ + (Math.Abs(bz - az) - (rows - 1) * spacing) / 2f;
            List<GridPoint> cells = new List<GridPoint>();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    if (cells.Count >= max) return cells;
                    cells.Add(new GridPoint(x0 + c * spacing, z0 + r * spacing));
                }
            return cells;
        }
    }
}
