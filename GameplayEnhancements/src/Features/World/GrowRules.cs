// Would the game let it grow here? The checks of Plant.UpdateHealth (Valheim 1.0.20, il-reader): the biome under the cell is
// one of Plant.m_biome; cultivated ground when m_needCultivatedGround. A sapling is its own Plant; a crop (Pickable_*) is grown
// by the seed Plant whose m_grownPrefabs holds it. Maksym 2026-10-25: never plant what the game would not let grow there.
// (Roof, grow space and the heat / cold shield rule are not checked: they change after planting.)
using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    internal static class GrowRules
    {
        static readonly Dictionary<string, Plant> seedOf = new Dictionary<string, Plant>();

        // the Plant that decides where it grows: the prefab's own, or the seed that grows this crop; null = no rule known
        public static Plant PlantOf(GameObject prefab)
        {
            if (prefab == null) return null;
            Plant own = prefab.GetComponent<Plant>();
            if (own != null) return own;
            Plant seed;
            if (seedOf.TryGetValue(prefab.name, out seed)) return seed;
            seed = null;
            foreach (GameObject g in Prefabs.All())
            {
                Plant pl = g.GetComponent<Plant>();
                if (pl == null || pl.m_grownPrefabs == null) continue;
                foreach (GameObject grown in pl.m_grownPrefabs)
                    if (grown != null && grown.name == prefab.name) { seed = pl; break; }
                if (seed != null) break;
            }
            seedOf[prefab.name] = seed;
            return seed;
        }

        // true when it may grow at pos; why = "wrong biome (Mountain; grows in Meadows, BlackForest)" or "not cultivated"
        public static bool Allowed(Plant pl, Vector3 pos, out string why)
        {
            why = "";
            if (pl == null) return true;
            Heightmap hm = Heightmap.FindHeightmap(pos);
            if (hm != null)
            {
                Heightmap.Biome b = hm.GetBiome(pos);
                if ((b & pl.m_biome) == 0) { why = "wrong biome (" + b + "; grows in " + pl.m_biome + ")"; return false; }
                if (pl.m_needCultivatedGround && !hm.IsCultivated(pos)) { why = "not cultivated"; return false; }
            }
            return true;
        }
    }
}
