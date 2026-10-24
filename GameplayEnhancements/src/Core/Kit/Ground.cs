// Ground height and what stands on a spot.
using UnityEngine;

namespace HomeCheat.Kit
{
    public static class Ground
    {
        public static float Height(Vector3 pos) { return ZoneSystem.instance.GetGroundHeight(pos); }

        // true when something solid already stands at the spot: buildings, trees, bushes, rocks, other plants
        // (terrain and loose items are not counted). The probe sphere sits just above the ground.
        public static bool IsBlocked(Vector3 pos, float radius)
        {
            Collider[] hits = Physics.OverlapSphere(pos + Vector3.up * (radius + 0.1f), radius, Masks.Solid, QueryTriggerInteraction.Ignore);
            return hits.Length > 0;
        }

        public static bool IsCultivated(Vector3 pos)
        {
            Heightmap hm = Heightmap.FindHeightmap(pos);
            return hm != null && hm.IsCultivated(pos);
        }
    }
}
