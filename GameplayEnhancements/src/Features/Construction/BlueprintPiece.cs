// One #Pieces line of a .blueprint file: prefab;category;x;y;z;qx;qy;qz;qw;"extra";sx;sy;sz (Unity-free).
namespace HomeCheat.Features.Construction
{
    internal sealed class BlueprintPiece
    {
        public string Prefab;
        public string Category;
        public string Extra;
        public float X;
        public float Y;
        public float Z;
        public float Qx;
        public float Qy;
        public float Qz;
        public float Qw;
        public float Sx;
        public float Sy;
        public float Sz;
    }
}
