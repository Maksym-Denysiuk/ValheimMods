// Terrain levelling that stays as it was made (Maksym 2026-10-25: "missing some level making activity").
// The hoe pieces (raise, mud_road, digg_v2) carry the old TerrainModifier: it stays in the world as an object that the
// terrain re-applies from its own fields, and after the area reloads it comes back from the prefab with the prefab's radius
// (1.5 m for raise), so a 3 m stamp shrinks and leaves unlevelled ground between stamps (IL 1.0.20: TerrainModifier.Awake
// adds itself to s_instances; no radius in its ZDO). A TerrainOp is applied once into the zone's TerrainComp, which saves the
// heights, and then destroys itself (TerrainOp.Awake -> TerrainComp.ApplyOperation -> Object.Destroy).
// The operation travels as its prefab's name hash: the terrain owner reads the settings back from the TerrainOp registered
// under that hash in ObjectDB.m_terrainOpsByHash (Settings.Serialize / Deserialize; an unregistered one is cancelled with "Failed to
// deserialize TerrainOp settings"). No vanilla prefab levels one vertex, so one template is registered here; the level
// height comes from each operation's own position. This PC must own the zone (it hosts the world).
using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.Construction
{
    internal static class TerrainLevel
    {
        const string PrefabName = "hc_construct_level";
        static GameObject template;

        // the template (inactive, across worlds) registered in this world's ZNetScene
        static void Ensure()
        {
            if (ZNetScene.instance == null) throw new CommandException("no world loaded");
            if (template == null)
            {
                template = new GameObject(PrefabName);
                template.SetActive(false);
                Object.DontDestroyOnLoad(template);
                TerrainOp op = template.AddComponent<TerrainOp>();
                TerrainOp.Settings s = new TerrainOp.Settings();
                s.m_level = true;
                s.m_levelOffset = 0f;
                // a circle, not a square: TerrainComp.LevelTerrain rounds the radius up to whole vertices and checks the
                // distance only for a circle, so a 0.3 m square would still cover 3 x 3 vertices (IL 1.0.20)
                s.m_levelRadius = 0.3f;
                s.m_square = false;
                s.m_raise = false;
                s.m_smooth = false;
                s.m_paintCleared = false;
                op.m_settings = s;
                op.m_onPlacedEffect = new EffectList();
            }
            // the owner looks the settings up in ObjectDB.m_terrainOpsByHash (Settings.Deserialize -> ObjectDB.TryGetTerrainOp)
            if (ObjectDB.instance == null) throw new CommandException("no ObjectDB: load a world first");
            System.Collections.IDictionary ops = Refl.Get(ObjectDB.instance, "m_terrainOpsByHash") as System.Collections.IDictionary;
            if (ops == null) throw new CommandException("ObjectDB.m_terrainOpsByHash not found: cannot register the level operation");
            ops[PrefabName.GetStableHashCode()] = template.GetComponent<TerrainOp>();
        }

        // one terrain vertex (whole world metres) set to height y; operations on different vertices never stack (each adds
        // target - current height to the vertices it covers)
        public static void Vertex(float x, float z, float y)
        {
            Ensure();
            GameObject go = (GameObject)Object.Instantiate(template, new Vector3(x, y, z), Quaternion.identity);
            go.name = PrefabName;
            go.SetActive(true);
        }

        // the old hoe stamps (TerrainModifier objects) whose centre lies in the rectangle: removed, so they stop overriding
        public static int RemoveLegacy(float ax, float az, float bx, float bz)
        {
            int n = 0;
            foreach (TerrainModifier tm in TerrainModifier.GetAllInstances().ToArray())
            {
                if (tm == null) continue;
                Vector3 p = tm.transform.position;
                if (p.x < ax || p.x > bx || p.z < az || p.z > bz) continue;
                ZNetView nv = tm.GetComponent<ZNetView>();
                if (nv != null && nv.IsValid()) { nv.ClaimOwnership(); ZNetScene.instance.Destroy(tm.gameObject); }
                else Object.Destroy(tm.gameObject);
                n++;
            }
            return n;
        }
    }
}
