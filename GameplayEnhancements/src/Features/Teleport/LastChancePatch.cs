// lastchance: a prefix on Character.ApplyDamage (HitData first; the method that sets the health after armour and resistances).
// Only the local player's own hits, only while lastchance is loaded (Current); false = the game's ApplyDamage is skipped for
// this hit (no damage, no stagger, no hit effect). Any error lets the hit through.
using System;
using HarmonyLib;

namespace HomeCheat.Features.TeleportCommands
{
    [HarmonyPatch(typeof(Character), "ApplyDamage")]
    internal static class LastChancePatch
    {
        static bool Prefix(Character __instance, HitData __0)
        {
            LastChanceCommand c = LastChanceCommand.Current;
            if (c == null || __0 == null) return true;
            Player p = Player.m_localPlayer;
            if (p == null || !ReferenceEquals(__instance, p)) return true;
            try { return !c.Save(p, __0); }
            catch (Exception e) { HomeApi.LogError("lastchance: " + e); return true; }
        }
    }
}
