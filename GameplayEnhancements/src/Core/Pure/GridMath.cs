// Offsets of one cell in a rows x columns block: 'along' runs away from the player (row r), 'across' to the right.
// 'centre' is the column (0-based, may be fractional) that sits on the anchor line: (cols - 1) / 2 centres the block,
// an integer aim column puts that column on the crosshair. No Unity reference: also compiled into HomeCheat.SelfTest.exe.
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
    }
}
