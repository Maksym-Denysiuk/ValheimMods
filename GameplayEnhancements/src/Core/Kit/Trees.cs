// Object and tree scale saved in the ZDO key "scale". Vanilla ignores a saved scale on trees when they load;
// the core patch TreeScalePatch calls ApplyScale so big trees stay big. Size, hit points and drops follow the factor.
using UnityEngine;

namespace HomeCheat.Kit
{
    public static class Trees
    {
        public const string ScaleKey = "scale";

        // scale an instance and save the scale with the world (no-op at 1)
        public static void SetScale(GameObject inst, float scale)
        {
            if (Mathf.Abs(scale - 1f) <= 0.001f) return;
            inst.transform.localScale = Vector3.one * scale;
            ZNetView nv = inst.GetComponent<ZNetView>();
            if (nv != null && nv.IsValid()) nv.GetZDO().Set(ScaleKey, Vector3.one * scale);
        }

        // size + hit points + drops, all from the prefab values times factor
        public static void ApplyScale(TreeBase t, float factor)
        {
            t.transform.localScale = Vector3.one * factor;
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(Prefabs.BaseName(t.gameObject)) : null;
            TreeBase baseTree = prefab != null ? prefab.GetComponent<TreeBase>() : null;
            if (baseTree == null) return;
            t.m_health = baseTree.m_health * factor;
            t.m_dropWhenDestroyed.m_dropMin = Mathf.Max(1, Mathf.RoundToInt(baseTree.m_dropWhenDestroyed.m_dropMin * factor));
            t.m_dropWhenDestroyed.m_dropMax = Mathf.Max(1, Mathf.RoundToInt(baseTree.m_dropWhenDestroyed.m_dropMax * factor));
        }
    }
}
