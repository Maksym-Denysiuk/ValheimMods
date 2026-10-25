// The smallest legal distance between two crops, from the game's own data (Maksym 2026-10-25: "always the minimum").
// The game lets a sapling grow only when no collider of another healthy Plant lies within its m_growRadius (Plant.HaveGrowSpace),
// so two saplings need centre distance = growRadius + the neighbour's collider radius. The sapling of a crop is the prefab whose
// Plant.m_grownPrefabs holds it (sapling_carrot -> Pickable_Carrot): carrot 0.5 + 0.18 = 0.68 m, magecap 0.8 + 0.18 = 0.98 m.
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    internal static class CropSpacing
    {
        // 0 when no sapling grows into this crop (then the caller keeps its own default)
        public static float Minimum(GameObject crop, out string source)
        {
            source = "no sapling grows into " + crop.name;
            foreach (GameObject g in Prefabs.All())
            {
                Plant pl = g.GetComponent<Plant>();
                if (pl == null || pl.m_grownPrefabs == null) continue;
                foreach (GameObject grown in pl.m_grownPrefabs)
                {
                    if (grown == null || grown.name != crop.name) continue;
                    float colliderR = ColliderRadius(g);
                    source = g.name + " growRadius " + pl.m_growRadius.ToString("F2") + " + collider " + colliderR.ToString("F2");
                    return pl.m_growRadius + colliderR;
                }
            }
            return 0f;
        }

        static float ColliderRadius(GameObject g)
        {
            CapsuleCollider cc = g.GetComponentInChildren<CapsuleCollider>(true);
            if (cc != null) return cc.radius * Mathf.Max(Mathf.Abs(cc.transform.lossyScale.x), Mathf.Abs(cc.transform.lossyScale.z));
            SphereCollider sc = g.GetComponentInChildren<SphereCollider>(true);
            if (sc != null) return sc.radius * Mathf.Max(Mathf.Abs(sc.transform.lossyScale.x), Mathf.Abs(sc.transform.lossyScale.z));
            return 0f;
        }
    }
}
