using System;
using System.IO;
using BepInEx;
using UnityEngine;

namespace HomeCheat.Features.Spells
{
    // The state of the loaded spells build: on/off, placeholder mode, the bar and the patches. One instance per load (Current),
    // created in SpellsCommand.OnLoad and dropped in OnUnload, so a replaced build leaves nothing switched on.
    // spells.state keeps enabled= and placeholder= across game starts.
    internal sealed class SpellsSettings
    {
        public const string ObjName = "HomeCheat_SpellBar";

        // the loaded build's settings; null while unloaded
        public static SpellsSettings Current;

        public bool Enabled;
        public bool Placeholder = true;   // default ON: the bar reads spells-ui-test.txt and a cast only shows a toast
        public int ConsumedFrame = -1;
        public SpellBar Bar;
        public PatchSet Patches;

        public static string Dir { get { return Path.Combine(Paths.ConfigPath, "homecheat"); } }
        public static string RealFile { get { return Path.Combine(Dir, "spells.txt"); } }
        public static string TestFile { get { return Path.Combine(Dir, "spells-ui-test.txt"); } }
        public static string StateFile { get { return Path.Combine(Dir, "spells.state"); } }

        // ---- static accessors for the bar, the radial configs and the patches (safe while unloaded) ----
        public static bool On { get { return Current != null && Current.Enabled; } }
        public static bool IsPlaceholder { get { return Current == null || Current.Placeholder; } }
        public static SpellBar ActiveBar { get { return Current == null ? null : Current.Bar; } }
        public static string ActiveFile { get { return IsPlaceholder ? TestFile : RealFile; } }

        public static bool ShiftHeld()
        {
            return ZInput.GetKey(KeyCode.LeftShift, false) || ZInput.GetKey(KeyCode.RightShift, false);
        }

        public static void MarkConsumed() { if (Current != null) Current.ConsumedFrame = Time.frameCount; }

        // switch the spell system off and remember it (radial build failed, or a cast changed a cheat flag)
        public static void TurnOff()
        {
            if (Current == null) return;
            Current.Enabled = false;
            Current.Save();
        }

        // reads spells.state (enabled=, placeholder=); a missing file or line means enabled=0, placeholder=1
        public void Load()
        {
            Enabled = false; Placeholder = true;
            try
            {
                if (File.Exists(StateFile))
                {
                    string t = File.ReadAllText(StateFile);
                    Enabled = t.Contains("enabled=1");
                    Placeholder = !t.Contains("placeholder=0");
                }
            }
            catch (Exception) { }
        }

        // writes both settings
        public void Save()
        {
            try { Directory.CreateDirectory(Dir); File.WriteAllText(StateFile, "enabled=" + (Enabled ? "1" : "0") + "\nplaceholder=" + (Placeholder ? "1" : "0") + "\n"); }
            catch (Exception e) { HomeApi.Log("spells: cannot save state: " + e.Message); }
        }
    }
}
