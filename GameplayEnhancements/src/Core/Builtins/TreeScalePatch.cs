// Vanilla ignores a saved scale on trees when they load; re-apply it so big trees stay big. In the core, not in the world DLL:
// world persistence must not depend on a DLL that may fail to load.
using HarmonyLib;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Builtins
{
    [HarmonyPatch(typeof(ZNetView), "Awake")]
    internal static class TreeScalePatch
    {
        static void Postfix(ZNetView __instance)
        {
            try
            {
                ZDO z = __instance.GetZDO();
                if (z == null) return;
                TreeBase t = __instance.GetComponent<TreeBase>();
                if (t == null) return;
                Vector3 s = z.GetVec3(Trees.ScaleKey, Vector3.zero);
                if (s.x > 0.01f && Mathf.Abs(s.x - 1f) > 0.001f) Trees.ApplyScale(t, s.x);
            }
            catch { }
        }
    }
}
