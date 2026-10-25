// Puts HitData.m_ignorePVP back on the hits of projectiles marked by PvpHits.MarkProjectile (the game's Projectile drops it,
// see PvpHits). While a marked projectile's OnHit runs, Character.Damage(HitData) sets the flag on the hit it is given;
// the hit then travels to the victim's owner with it. Unmarked shots (a normal bow) are never touched.
using System;
using System.Reflection;
using HarmonyLib;
using HomeCheat.Kit;

namespace HomeCheat.Builtins
{
    [HarmonyPatch]
    internal static class PvpProjectileHitPatch
    {
        static MethodBase TargetMethod()
        {
            foreach (MethodInfo m in typeof(Projectile).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                if (m.Name == "OnHit") return m;
            return null;
        }

        static void Prefix(Projectile __instance, out bool __state)
        {
            __state = PvpHits.IsMarked(__instance);
            if (__state) PvpHits.EnterHit();
        }

        static void Postfix(bool __state)
        {
            if (__state) PvpHits.LeaveHit();
        }
    }

    [HarmonyPatch(typeof(Character), "Damage", new Type[] { typeof(HitData) })]
    internal static class PvpProjectileDamagePatch
    {
        static void Prefix(HitData hit)
        {
            if (hit != null && PvpHits.InMarkedHit) hit.m_ignorePVP = true;
        }
    }
}
