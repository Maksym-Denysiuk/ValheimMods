// One #Pieces line of a .blueprint file: prefab;category;x;y;z;qx;qy;qz;qw;"extra";sx;sy;sz (PlanBuild), or the same with
// the saved data in base64 as a 14th field (InfinityHammer). Unity-free.
namespace HomeCheat.Features.Construction
{
    internal sealed class BlueprintPiece
    {
        public string Prefab;
        public string Category;
        public string Extra;
        public bool HasSavedData;   // InfinityHammer: the object's saved data in base64 after the scale (not applied)
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
