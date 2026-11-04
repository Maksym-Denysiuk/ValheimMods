// bowcharge: a prefix and postfix on Player.UpdateAttackBowDraw(ItemData weapon, float dt), the local player's bow draw (probe
// 2026-11-03: called from PlayerAttackInput for a weapon whose attack has m_bowDraw; while the attack is held the draw time grows
// by fixedDeltaTime; on release with draw time > 0 it calls StartAttack (when stamina and eitr allow) and zeroes the draw time;
// blocking or a minor action sets it to -1). The prefix starts the timer at full draw and spots the release; the postfix fires the
// extra arrows only when the game's own shot started, so no ammo or no stamina = no shot = no extra arrows. The sign of a started
// shot is a new object in m_currentAttack (Humanoid.StartAttack stores its clone there only when Attack.Start succeeds). InAttack()
// cannot tell: it reads the animator's state, which changes only at a later animator update (first in-game test 2026-11-04: it said
// "no shot" on every release).
// Only while bowcharge is loaded (Current); any error leaves the game's step alone.
using System;
using HarmonyLib;
using UnityEngine;

namespace HomeCheat.Features.Spellshot
{
    [HarmonyPatch(typeof(Player), "UpdateAttackBowDraw")]
    internal static class BowChargePatch
    {
        static object attackBefore;   // m_currentAttack just before the release call, for the postfix

        // __state = seconds held past full draw when this call is the release, else -1
        static void Prefix(Player __instance, out float __state)
        {
            __state = -1f;
            BowChargeCommand c = BowChargeCommand.Current;
            if (c == null || __instance == null || !ReferenceEquals(__instance, Player.m_localPlayer)) return;
            if (BowChargeCommand.fDrawTime == null || BowChargeCommand.fHold == null || BowChargeCommand.fCurrentAttack == null) return;
            try
            {
                float draw = (float)BowChargeCommand.fDrawTime.GetValue(__instance);
                bool hold = (bool)BowChargeCommand.fHold.GetValue(__instance);
                if (hold && draw > 0f)
                {
                    if (c.FullAt < 0f && __instance.GetAttackDrawPercentage() >= 1f) c.FullAt = Time.time;
                    return;
                }
                if (!hold && draw > 0f && c.FullAt >= 0f)   // the release
                {
                    attackBefore = BowChargeCommand.fCurrentAttack.GetValue(__instance);
                    __state = Time.time - c.FullAt;
                }
                c.FullAt = -1f;
            }
            catch (Exception e) { HomeApi.LogError("bowcharge: " + e.Message); c.FullAt = -1f; }
        }

        static void Postfix(Player __instance, float __state)
        {
            if (__state < 0f) return;
            BowChargeCommand c = BowChargeCommand.Current;
            if (c == null) return;
            try
            {
                object attack = BowChargeCommand.fCurrentAttack.GetValue(__instance);
                bool started = attack != null && !ReferenceEquals(attack, attackBefore);
                attackBefore = null;
                if (!started) { HomeApi.Log("bowcharge: released without a shot (no ammo, stamina or eitr): no extra arrows"); return; }
                c.Release(__instance, __state);
            }
            catch (Exception e) { HomeApi.LogError("bowcharge: " + e); }
        }
    }
}
