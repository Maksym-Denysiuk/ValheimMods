// op=keybinds: moves the network/debug stats window from F2 to F6 and frees F9 (vanilla F9 cycles the gamepad layout).
// Console key (F5 -> `) and the F9 world-save bind are NOT done here: they are the game's own settings
// (registry kbmBinding_Console, ConsoleBindings), see references/ValheimCustomMods/Keybinds/. C# 5 only.
// Patches are applied when the DLL loads (game start or op=reload); a newer build of this DLL removes the older one's patches.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace HomeCheat
{
    public class KeybindsCommand : IHomeCommand
    {
        internal const string HarmonyId = "com.homeos.keybinds";
        internal const int F2 = 283, F6 = 287, F9 = 290;
        internal static int StatsToggles, StatsF2Presses;
        static Harmony harmony;

        public string Name { get { return "keybinds"; } }
        public string Help { get { return "op=keybinds [console=<Input System key, e.g. backquote>] [status=1] [on=0|1] - stats window (ConnectPanel) opens on F6 instead of F2, F9 no longer cycles the gamepad layout; on by default from load, on=0 removes the patches"; } }

        public KeybindsCommand()
        {
            try { Harmony.UnpatchID(HarmonyId); } catch (Exception e) { HomeApi.Log("keybinds: unpatch on load: " + e.Message); }
            Enable();
        }

        static void Enable()
        {
            try
            {
                harmony = new Harmony(HarmonyId);
                harmony.PatchAll(typeof(KeybindsCommand).Assembly);
                HomeApi.Log("keybinds: ON (stats window F2 -> F6, F9 gamepad-layout key off)");
            }
            catch (Exception e) { HomeApi.Log("keybinds: patch failed: " + e); }
        }

        public string Run(Dictionary<string, string> a)
        {
            string con = HomeApi.S(a, "console", "");
            if (con.Length > 0) return RebindConsole(con);
            string on = HomeApi.S(a, "on", "").ToLowerInvariant();
            if (on == "0" || on == "off" || on == "false") { try { Harmony.UnpatchID(HarmonyId); } catch (Exception) { } harmony = null; HomeApi.Log("keybinds: OFF"); }
            else if (on == "1" || on == "on" || on == "true") { try { Harmony.UnpatchID(HarmonyId); } catch (Exception) { } Enable(); }
            return Status();
        }

        // the game's own key setting: button "Console" (default <Keyboard>/F5); ZInput.Save writes kbmBinding_Console to the registry like the settings menu does
        static string RebindConsole(string key)
        {
            if (ZInput.instance == null) return "ERROR: input not ready";
            object b = ZInput.instance.GetButtonDef("Console");
            if (b == null) return "ERROR: no button named Console";
            Type bt = b.GetType();
            MethodInfo path = bt.GetMethod("GetActionPath"), rebind = bt.GetMethod("Rebind");
            string before = (string)path.Invoke(b, new object[] { true });
            rebind.Invoke(b, new object[] { "<Keyboard>/" + key });
            ZInput.instance.Save();
            string after = (string)path.Invoke(b, new object[] { true });
            HomeApi.Log("keybinds: console key " + before + " -> " + after);
            return "console key " + before + " -> " + after + " (saved)";
        }

        static string Status()
        {
            int n = 0; StringBuilder sb = new StringBuilder();
            foreach (MethodBase m in Harmony.GetAllPatchedMethods())
            {
                Patches p = Harmony.GetPatchInfo(m);
                bool ours = false;
                foreach (Patch t in p.Transpilers) if (t.owner == HarmonyId) ours = true;
                foreach (Patch t in p.Postfixes) if (t.owner == HarmonyId) ours = true;
                if (ours) { n++; sb.Append(m.DeclaringType.Name).Append("::").Append(m.Name).Append(' '); }
            }
            return "keybinds: " + (n > 0 ? "ON" : "OFF") + ", patched " + sb + "; stats toggles on F6 seen: " + StatsToggles + ", F2 presses seen (ignored): " + StatsF2Presses;
        }

        internal static IEnumerable<CodeInstruction> Retarget(IEnumerable<CodeInstruction> ins, int from, int to, string what)
        {
            List<CodeInstruction> l = new List<CodeInstruction>(ins);
            int done = 0;
            for (int i = 0; i + 2 < l.Count; i++)
            {
                if (l[i].opcode == OpCodes.Ldc_I4 && (int)l[i].operand == from && l[i + 1].opcode == OpCodes.Ldc_I4_1 && l[i + 2].opcode == OpCodes.Call
                    && ((MethodInfo)l[i + 2].operand).Name == "GetKeyDown")
                {
                    if (to == 0) { l[i].opcode = OpCodes.Ldc_I4_0; l[i].operand = null; l[i + 1].opcode = OpCodes.Nop; l[i + 2].opcode = OpCodes.Nop; l[i + 1].operand = null; l[i + 2].operand = null; }
                    else l[i].operand = to;
                    done++;
                }
            }
            if (done != 1) HomeApi.Log("keybinds: WARNING " + what + ": expected 1 key check, found " + done + " (game updated?)");
            return l;
        }
    }

    // the stats window (World, Port, UID, ZDO, Connections, Players): F2 -> F6
    [HarmonyPatch(typeof(ConnectPanel), "Update")]
    static class ConnectPanelKeyPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> ins)
        {
            return KeybindsCommand.Retarget(ins, KeybindsCommand.F2, KeybindsCommand.F6, "ConnectPanel.Update");
        }

        // evidence for Player.log: every F6 / F2 press with the panel state after the frame
        static void Postfix(ConnectPanel __instance)
        {
            try
            {
                if (ZInput.GetKeyDown(KeyCode.F6, false)) { KeybindsCommand.StatsToggles++; HomeApi.Log("keybinds: F6 pressed, stats window visible = " + __instance.m_root.gameObject.activeSelf); }
                if (ZInput.GetKeyDown(KeyCode.F2, false)) { KeybindsCommand.StatsF2Presses++; HomeApi.Log("keybinds: F2 pressed (ignored), stats window visible = " + __instance.m_root.gameObject.activeSelf); }
            }
            catch (Exception) { }
        }
    }

    // vanilla F9 cycles the gamepad controller layout; with the key free it can be a console bind (F9 save)
    [HarmonyPatch(typeof(KeyHints), "Update")]
    static class KeyHintsF9Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> ins)
        {
            return KeybindsCommand.Retarget(ins, KeybindsCommand.F9, 0, "KeyHints.Update");
        }
    }
}
