// op=keybinds: moves the network/debug stats window from F2 to F6 and frees F9 (vanilla F9 cycles the gamepad layout).
// Console key (F5 -> `) and the F9 world-save bind are NOT done here: they are the game's own settings
// (registry kbmBinding_Console, ConsoleBindings), see docs/Keybinds/keybinds.md.
// The patches are applied on load (game start or op=reload) and removed on unload, so a newer build replaces them cleanly.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace HomeCheat.Features.Keybinds
{
    public sealed class KeybindsCommand : CommandBase
    {
        internal const int F2 = 283, F6 = 287, F9 = 290;

        // the loaded instance, for the patch classes; null while unloaded
        internal static KeybindsCommand Current;
        internal int StatsToggles, StatsF2Presses;

        public KeybindsCommand()
        {
            SubAction("console", "console=<Input System key, e.g. backquote> (rebinds the game's console key and saves it)", delegate (Args a) { return RebindConsole(a.Str("console", "")); });
        }

        public override string Name { get { return "keybinds"; } }
        protected override string Usage { get { return "keybinds [status=1] [on=0|1] - stats window (ConnectPanel) opens on F6 instead of F2, F9 no longer cycles the gamepad layout; on by default from load, on=0 removes the patches"; } }

        protected override void OnLoad()
        {
            Current = this;
            Enable();
        }

        protected override void OnUnload()
        {
            if (Current == this) Current = null;
        }

        void Enable()
        {
            try
            {
                Patches.Apply(typeof(ConnectPanelKeyPatch));
                Patches.Apply(typeof(KeyHintsF9Patch));
                HomeApi.Log("keybinds: ON (stats window F2 -> F6, F9 gamepad-layout key off)");
            }
            catch (Exception e) { HomeApi.LogError("keybinds: patch failed: " + e); }
        }

        protected override string Execute(Args a)
        {
            string on = a.Str("on", "").ToLowerInvariant();
            if (on == "0" || on == "off" || on == "false") { Patches.RemoveAll(); HomeApi.Log("keybinds: OFF"); }
            else if (on == "1" || on == "on" || on == "true") { Patches.RemoveAll(); Enable(); }
            return Status();
        }

        // the game's own key setting: button "Console" (default <Keyboard>/F5); ZInput.Save writes kbmBinding_Console to the registry like the settings menu does
        static string RebindConsole(string key)
        {
            if (ZInput.instance == null) throw new CommandException("input not ready");
            object b = ZInput.instance.GetButtonDef("Console");
            if (b == null) throw new CommandException("no button named Console");
            Type bt = b.GetType();
            MethodInfo path = bt.GetMethod("GetActionPath"), rebind = bt.GetMethod("Rebind");
            string before = (string)path.Invoke(b, new object[] { true });
            rebind.Invoke(b, new object[] { "<Keyboard>/" + key });
            ZInput.instance.Save();
            string after = (string)path.Invoke(b, new object[] { true });
            HomeApi.Log("keybinds: console key " + before + " -> " + after);
            return "console key " + before + " -> " + after + " (saved)";
        }

        string Status()
        {
            bool on = Patches.IsActive;
            return "keybinds: " + (on ? "ON" : "OFF") + ", patched " + (on ? Patches.Describe() + " " : "") + "; stats toggles on F6 seen: " + StatsToggles + ", F2 presses seen (ignored): " + StatsF2Presses;
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
}
