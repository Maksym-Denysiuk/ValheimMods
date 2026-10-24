// the stats window (World, Port, UID, ZDO, Connections, Players): F2 -> F6
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace HomeCheat.Features.Keybinds
{
    [HarmonyPatch(typeof(ConnectPanel), "Update")]
    internal static class ConnectPanelKeyPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> ins)
        {
            return KeybindsCommand.Retarget(ins, KeybindsCommand.F2, KeybindsCommand.F6, "ConnectPanel.Update");
        }

        // evidence for Player.log: every F6 / F2 press with the panel state after the frame
        static void Postfix(ConnectPanel __instance)
        {
            KeybindsCommand cmd = KeybindsCommand.Current;
            if (cmd == null) return;
            try
            {
                if (ZInput.GetKeyDown(KeyCode.F6, false)) { cmd.StatsToggles++; HomeApi.Log("keybinds: F6 pressed, stats window visible = " + __instance.m_root.gameObject.activeSelf); }
                if (ZInput.GetKeyDown(KeyCode.F2, false)) { cmd.StatsF2Presses++; HomeApi.Log("keybinds: F2 pressed (ignored), stats window visible = " + __instance.m_root.gameObject.activeSelf); }
            }
            catch (Exception) { }
        }
    }
}
