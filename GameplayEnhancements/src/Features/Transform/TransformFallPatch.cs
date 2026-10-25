// raven form: no fall damage for the local player. Character.UpdateGroundContact turns the drop below m_maxAirAltitude into
// damage (Clamp01((drop - 4) / 16) * 100) and passes it through SEMan.ModifyFallDamage(baseDamage, ref damage) before
// Damage() is called only when damage > 0 (status effects use the same hook: SE_Stats.m_fallDamageModifier); the landing itself (effects, ground reset) stays.
using HarmonyLib;
using UnityEngine;

namespace HomeCheat.Features.TransformCommands
{
    [HarmonyPatch(typeof(SEMan), "ModifyFallDamage")]
    internal static class TransformFallPatch
    {
        static void Postfix(SEMan __instance, ref float __1)
        {
            if (TransformCommand.Current == null || __1 <= 0f) return;
            Player p = Player.m_localPlayer;
            if (p == null || p.GetSEMan() != __instance) return;
            TransformState st = p.GetComponent<TransformState>();
            if (st == null || !(st.Form is RavenForm)) return;
            HomeApi.Log("transform: raven fall damage " + __1.ToString("F1") + " -> 0");
            __1 = 0f;
        }
    }
}
