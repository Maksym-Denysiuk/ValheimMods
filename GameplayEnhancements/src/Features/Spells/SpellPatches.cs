using System;
using System.Reflection;
using UnityEngine;
using Valheim.UI;

namespace HomeCheat.Features.Spells
{
    // The Harmony patches (two prefixes, one postfix), applied through the command's PatchSet (id com.homeos.homecheat.spells).
    public static class SpellPatches
    {
        // RadialBase.Open(config, backConfig): the vanilla main radial (ValheimRadialConfig) is replaced by ours. Everything else
        // (T = emote radial, hover menus, the Hotbar group inside ours, Back) opens exactly as before. Shift held = vanilla.
        public static bool OpenPrefix(ref IRadialConfig config)
        {
            try
            {
                SpellBar bar = SpellsSettings.ActiveBar;
                if (!SpellsSettings.On || bar == null || config == null) return true;
                if (config.GetType().Name != "ValheimRadialConfig") return true;
                if (SpellsSettings.ShiftHeld()) return true;
                config = bar.Root();
            }
            catch (Exception e) { HomeApi.Log("spells: OpenPrefix: " + e.Message); }
            return true;
        }

        // Player.UseHotbarItem(int): no weapon switch in the frame a Shift+digit pin was fired by the bar
        public static bool HotbarPrefix(int index)
        {
            SpellsSettings s = SpellsSettings.Current;
            if (s == null || !s.Enabled) return true;
            return Time.frameCount != s.ConsumedFrame;
        }

        // TextInput.IsVisible: true while the answer form is open, so the game frees the cursor and the player takes no input
        // (GameCamera.UpdateMouseCapture, Player.TakeInput and PlayerController.TakeInput all ask it)
        public static void TextInputPostfix(ref bool __result)
        {
            if (SpellForm.Current != null) __result = true;
        }

        // a radial that still holds a config from a replaced build (op=reload while it was open): show the vanilla radial, change no settings
        internal static void OpenVanilla(RadialBase radial)
        {
            HomeApi.Log("spells: stale radial config of a replaced build, vanilla radial opened");
            try { radial.Open(RadialData.SO.MainGroupConfig, null); } catch (Exception e) { HomeApi.Log("spells: vanilla fallback failed: " + e.Message); }
        }

        // Patching RadialBase.Open / UseHotbarItem while the game is still starting hangs the main-scene load (verified
        // 2026-10-25: launch with spells.state enabled=1 froze at "Unloading 3 Unused Serialized files"; with enabled=0 it reached
        // the menu). So the patches go in only once a world is loaded: from SpellBar.Update, or op=spells&enabled=1 in a world.
        internal static void EnsureApplied()
        {
            SpellsSettings s = SpellsSettings.Current;
            if (s == null || !s.Enabled || s.Patched || Player.m_localPlayer == null) return;
            string r;
            try { r = Apply(true); }
            catch (Exception e) { r = "ERROR: " + e.Message; }
            HomeApi.Log("spells: world loaded, " + r);
            if (r.StartsWith("ERROR")) SpellsSettings.TurnOff();
        }

        internal static string Apply(bool on)
        {
            SpellsSettings s = SpellsSettings.Current;
            if (s == null || s.Patches == null) return "ERROR: spells not loaded";
            s.Patches.RemoveAll();
            s.Patched = false;
            if (!on) return "patches removed";
            if (Player.m_localPlayer == null) return "patches wait for a loaded world";
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo m1 = null;
            foreach (MethodInfo m in typeof(RadialBase).GetMethods(all))
                if (m.Name == "Open" && m.GetParameters().Length == 2) { m1 = m; break; }
            MethodInfo m2 = typeof(Player).GetMethod("UseHotbarItem", all);
            MethodInfo m3 = typeof(TextInput).GetMethod("IsVisible", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (m1 == null || m2 == null || m3 == null) return "ERROR: target methods not found (game version changed?)";
            s.Patches.Patch(m1, typeof(SpellPatches).GetMethod("OpenPrefix"), null);
            s.Patches.Patch(m2, typeof(SpellPatches).GetMethod("HotbarPrefix"), null);
            s.Patches.Patch(m3, null, typeof(SpellPatches).GetMethod("TextInputPostfix"));
            s.Patched = true;
            return "patches applied (RadialBase.Open, UseHotbarItem, TextInput.IsVisible)";
        }
    }
}
