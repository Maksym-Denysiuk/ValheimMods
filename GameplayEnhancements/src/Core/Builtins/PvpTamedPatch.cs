// The tamed rule of attack commands (Kit.PvpHits.HitsTamed, card pvp-tamed): a marked Aoe (PvpHits.ArmAoe) or a marked
// projectile (PvpHits.MarkProjectile) does not hit a tamed creature that HitsTamed spares - the caster's own while his PvP
// is on, every tamed one while it is off. Both postfixes only turn a hit into a miss; unmarked Aoes and shots (a normal
// bow, a monster's attack) are never touched. Each spared creature is logged at most once per 2 s (test evidence).
using System.Collections.Generic;
using HarmonyLib;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Builtins
{
    [HarmonyPatch(typeof(Aoe), "ShouldHit")]
    internal static class PvpTamedAoePatch
    {
        static void Postfix(Aoe __instance, Collider __0, ref bool __result)
        {
            if (!__result || __0 == null || !PvpHits.IsMarked(__instance)) return;
            Character c = __0.GetComponentInParent<Character>();
            if (c != null && !PvpHits.HitsTamed(c)) { __result = false; PvpTamedLog.Spared(c, "area"); }
        }
    }

    [HarmonyPatch(typeof(Projectile), "IsValidTarget")]
    internal static class PvpTamedProjectilePatch
    {
        static void Postfix(Projectile __instance, IDestructible __0, ref bool __result)
        {
            if (!__result || !PvpHits.IsMarked(__instance)) return;
            Character c = __0 as Character;
            if (c != null && !PvpHits.HitsTamed(c)) { __result = false; PvpTamedLog.Spared(c, "shot"); }
        }
    }

    internal static class PvpTamedLog
    {
        static readonly Dictionary<int, float> last = new Dictionary<int, float>();

        internal static void Spared(Character c, string by)
        {
            int id = c.GetInstanceID();
            float t, now = Time.time;
            if (last.TryGetValue(id, out t) && now - t < 2f) return;
            if (last.Count > 200) last.Clear();
            last[id] = now;
            Player me = Player.m_localPlayer;
            string why = me == null || !me.IsPVPEnabled() ? "your PvP off" : "yours";
            HomeApi.Log("pvp tamed rule: " + by + " spared tamed " + Prefabs.BaseName(c.gameObject) + " (" + why + ")");
        }
    }
}
