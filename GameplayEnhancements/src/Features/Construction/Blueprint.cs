// One .blueprint file: its header and pieces, positions relative to the point it was captured at (Unity-free).
using System.Collections.Generic;

namespace HomeCheat.Features.Construction
{
    internal sealed class Blueprint
    {
        public string Name;
        public string Creator;
        public string Description;
        public string Category;
        public string File;
        public int SnapPoints;
        public int BadLines;
        public List<BlueprintPiece> Pieces = new List<BlueprintPiece>();
    }
}
