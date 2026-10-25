// One cell of an integer grid (column X, row Z), used by GridRoute.
namespace HomeCheat
{
    public struct GridCell
    {
        public readonly int X, Z;

        public GridCell(int x, int z) { X = x; Z = z; }

        public override string ToString() { return X + "," + Z; }
    }
}
