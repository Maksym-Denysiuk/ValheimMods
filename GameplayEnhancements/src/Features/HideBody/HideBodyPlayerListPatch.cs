// map position flag off while the server builds the player list
using System;
using HarmonyLib;

namespace HomeCheat.Features.HideBody
{
    [HarmonyPatch(typeof(ZNet), "SendPlayerList")]
    internal static class HideBodyPlayerListPatch
    {
        static readonly PublicPositionGuard Guard = new PublicPositionGuard("");

        static void Prefix(ZNet __instance) { Guard.Off(__instance); }
        static void Postfix(ZNet __instance) { Guard.Restore(__instance); }
        static Exception Finalizer(ZNet __instance, Exception __exception) { Guard.Restore(__instance); return __exception; }
    }
}
