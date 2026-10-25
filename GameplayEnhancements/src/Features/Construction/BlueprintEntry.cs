// One construction of the blueprint folder: a single .blueprint file, or a subfolder whose files are its versions (one is
// built: BlueprintLibrary.Resolve). Unity-free.
using System.Collections.Generic;

namespace HomeCheat.Features.Construction
{
    internal sealed class BlueprintEntry
    {
        public string Name;     // the #Name of a single file, the folder name of a group
        public string Source;   // file or folder name inside blueprints/
        public List<Blueprint> Parts = new List<Blueprint>();

        public int PieceCount
        {
            get { int n = 0; foreach (Blueprint b in Parts) n += b.Pieces.Count; return n; }
        }

        public int BadLines
        {
            get { int n = 0; foreach (Blueprint b in Parts) n += b.BadLines; return n; }
        }

        // the box of every piece pivot of every part, in blueprint coordinates
        public void Bounds(out float minX, out float maxX, out float minY, out float maxY, out float minZ, out float maxZ)
        {
            minX = maxX = minY = maxY = minZ = maxZ = 0f;
            bool any = false;
            foreach (Blueprint b in Parts)
            {
                if (b.Pieces.Count == 0) continue;
                float a0, a1, b0, b1, c0, c1;
                BlueprintParse.Bounds(b, out a0, out a1, out b0, out b1, out c0, out c1);
                if (!any) { minX = a0; maxX = a1; minY = b0; maxY = b1; minZ = c0; maxZ = c1; any = true; continue; }
                if (a0 < minX) minX = a0; if (a1 > maxX) maxX = a1;
                if (b0 < minY) minY = b0; if (b1 > maxY) maxY = b1;
                if (c0 < minZ) minZ = c0; if (c1 > maxZ) maxZ = c1;
            }
        }
    }
}
