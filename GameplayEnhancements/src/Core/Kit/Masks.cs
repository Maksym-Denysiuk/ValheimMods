// Physics layer masks, computed once.
//   Solid            buildings, trees, bushes, rocks, plants (no terrain, no loose items)
//   SolidAndTerrain  the same plus terrain: what the crosshair can hit
using UnityEngine;

namespace HomeCheat.Kit
{
    public static class Masks
    {
        static int solid = -1, solidAndTerrain = -1;

        public static int Solid
        {
            get
            {
                if (solid == -1) solid = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "vehicle");
                return solid;
            }
        }

        public static int SolidAndTerrain
        {
            get
            {
                if (solidAndTerrain == -1) solidAndTerrain = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle");
                return solidAndTerrain;
            }
        }
    }
}
