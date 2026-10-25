// A point on the ground plane (world X and Z), from GridMath.RectCells. No Unity reference: the caller adds the height.
namespace HomeCheat
{
    public struct GridPoint
    {
        public readonly float X, Z;

        public GridPoint(float x, float z) { X = x; Z = z; }

        public override string ToString() { return X.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + Z.ToString(System.Globalization.CultureInfo.InvariantCulture); }
    }
}
