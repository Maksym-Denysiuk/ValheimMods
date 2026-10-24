// map position flag off while a client builds its sync data
using System;
using HarmonyLib;

namespace HomeCheat.Features.HideBody
{
    [HarmonyPatch(typeof(ZNet), "SendServerSyncPlayerData")]
    internal static class HideBodySyncDataPatch
    {
        static readonly PublicPositionGuard Guard = new PublicPositionGuard(" (sync)");

        static void Prefix(ZNet __instance) { Guard.Off(__instance); }
        static void Postfix(ZNet __instance) { Guard.Restore(__instance); }
        static Exception Finalizer(ZNet __instance, Exception __exception) { Guard.Restore(__instance); return __exception; }
    }
}
