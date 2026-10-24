// op=hidebody: hides the local player's human body from OTHER players on our own world.
// While this game sends the local player's ZDO to other peers, the position written into the ZDO header gets y = 10000
// (viewers draw the remote player 10 km up); the real value is written back right after each send. The map position flag
// of the player list is switched off for the same calls. Default off. C# 5 only.
// See plan-hidebody.md for the verified facts (installed assembly_valheim.dll) and what is not verified.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace HomeCheat
{
    public class HideBodyCommand : IHomeCommand
    {
        internal const string HarmonyId = "com.homeos.hidebody";
        internal static bool On;
        internal static float LiftY = 10000f;
        internal static int Lifts, PubOff;
        static Harmony harmony;
        static bool quitHooked;

        public string Name { get { return "hidebody"; } }
        public string Help { get { return "op=hidebody&on=1|0 | status=1 [y=10000] - hides your body from other players (the player object is sent 10 km up, the map pin is off); off by default, restored on off, on reload and on quit"; } }

        public HideBodyCommand()
        {
            // a newer build of this DLL removes the patches of the older one (same Harmony id) and starts off
            try { Harmony.UnpatchID(HarmonyId); } catch (Exception e) { HomeApi.Log("hidebody: unpatch on load: " + e.Message); }
            if (!quitHooked) { quitHooked = true; Application.quitting += delegate { Disable("game quitting"); }; }
        }

        public string Run(Dictionary<string, string> a)
        {
            if (HomeApi.Flag(a, "status", false)) return Status();
            string on = HomeApi.S(a, "on", "").ToLowerInvariant();
            if (on.Length == 0) return Status() + "\nuse op=hidebody&on=1 or on=0";
            if (on == "1" || on == "true" || on == "yes" || on == "on")
            {
                if (Player.m_localPlayer == null) return "ERROR: no local player (load a world first)";
                LiftY = HomeApi.F(a, "y", 10000f);
                return Enable();
            }
            Disable("op=hidebody&on=0");
            return "hidebody OFF: patches removed, nothing left changed\n" + Status();
        }

        static string Enable()
        {
            if (harmony == null)
            {
                harmony = new Harmony(HarmonyId);
                harmony.PatchAll(typeof(HideBodyCommand).Assembly);
            }
            On = true;
            HomeApi.Log("hidebody: ON (player ZDO sent at y=" + LiftY.ToString("F0") + ", map position off)");
            return "hidebody ON: other players should now see your body " + LiftY.ToString("F0") + " m up and no map pin. Revert: op=hidebody&on=0\n" + Status();
        }

        static void Disable(string why)
        {
            bool was = On || harmony != null;
            On = false;
            if (harmony != null)
            {
                try { Harmony.UnpatchID(HarmonyId); } catch (Exception e) { HomeApi.Log("hidebody: unpatch: " + e.Message); }
                harmony = null;
            }
            if (was) HomeApi.Log("hidebody: OFF (" + why + ")");
        }

        static string Status()
        {
            StringBuilder sb = new StringBuilder("hidebody: ").Append(On ? "ON" : "off").Append(", patches installed=").Append(harmony != null)
                .Append(", ZDO sends lifted=").Append(Lifts).Append(", player-list calls with pin off=").Append(PubOff).Append(", lift y=").Append(LiftY.ToString("F0"));
            Player p = Player.m_localPlayer;
            if (p != null)
            {
                ZNetView nv = p.GetComponent<ZNetView>();
                ZDO z = nv != null && nv.IsValid() ? nv.GetZDO() : null;
                sb.Append("\nlocal player ZDO ").Append(z == null ? "(none)" : z.m_uid.ToString() + " stored position " + HomeApi.V(z.GetPosition()) + " (real, restored after each send)");
                sb.Append("\nthis game is ").Append(ZNet.instance != null && ZNet.instance.IsServer() ? "the host" : "a client");
                sb.Append("\nplayers online: ");
                foreach (Player pl in Player.GetAllPlayers()) sb.Append(pl.GetPlayerName()).Append("; ");
            }
            return sb.ToString();
        }
    }

    // writes y = LiftY into the local player's ZDO position field for the duration of each send, then restores it
    [HarmonyPatch(typeof(ZDOMan), "SendZDOs")]
    static class HideBodySendZdosPatch
    {
        static readonly FieldInfo fPos = typeof(ZDO).GetField("m_position", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        static ZDO lifted;
        static Vector3 saved;

        static void Prefix()
        {
            if (!HideBodyCommand.On || fPos == null) return;
            try
            {
                Player p = Player.m_localPlayer;
                if (p == null) return;
                ZNetView nv = p.GetComponent<ZNetView>();
                ZDO z = nv != null && nv.IsValid() ? nv.GetZDO() : null;
                if (z == null) return;
                saved = (Vector3)fPos.GetValue(z);
                Vector3 v = saved;
                v.y = HideBodyCommand.LiftY;
                fPos.SetValue(z, (object)v);
                lifted = z;
                HideBodyCommand.Lifts++;
            }
            catch (Exception e) { HomeApi.Log("hidebody lift: " + e.Message); }
        }

        static void Restore()
        {
            if (lifted == null) return;
            try { fPos.SetValue(lifted, (object)saved); } catch (Exception e) { HomeApi.Log("hidebody restore: " + e.Message); }
            lifted = null;
        }

        static void Postfix() { Restore(); }
        static Exception Finalizer(Exception __exception) { Restore(); return __exception; }
    }

    // map position flag off while the player list (server) or the sync data (client) is built
    [HarmonyPatch(typeof(ZNet), "SendPlayerList")]
    static class HideBodyPlayerListPatch
    {
        static readonly FieldInfo fPub = typeof(ZNet).GetField("m_publicReferencePosition", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        static bool changed, saved;

        static void Prefix(ZNet __instance)
        {
            if (!HideBodyCommand.On || fPub == null) return;
            try { saved = (bool)fPub.GetValue(__instance); fPub.SetValue(__instance, false); changed = true; HideBodyCommand.PubOff++; }
            catch (Exception e) { HomeApi.Log("hidebody pin off: " + e.Message); }
        }

        static void Restore(ZNet inst)
        {
            if (!changed) return;
            try { fPub.SetValue(inst, saved); } catch (Exception e) { HomeApi.Log("hidebody pin restore: " + e.Message); }
            changed = false;
        }

        static void Postfix(ZNet __instance) { Restore(__instance); }
        static Exception Finalizer(ZNet __instance, Exception __exception) { Restore(__instance); return __exception; }
    }

    [HarmonyPatch(typeof(ZNet), "SendServerSyncPlayerData")]
    static class HideBodySyncDataPatch
    {
        static readonly FieldInfo fPub = typeof(ZNet).GetField("m_publicReferencePosition", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        static bool changed, saved;

        static void Prefix(ZNet __instance)
        {
            if (!HideBodyCommand.On || fPub == null) return;
            try { saved = (bool)fPub.GetValue(__instance); fPub.SetValue(__instance, false); changed = true; HideBodyCommand.PubOff++; }
            catch (Exception e) { HomeApi.Log("hidebody pin off (sync): " + e.Message); }
        }

        static void Restore(ZNet inst)
        {
            if (!changed) return;
            try { fPub.SetValue(inst, saved); } catch (Exception e) { HomeApi.Log("hidebody pin restore (sync): " + e.Message); }
            changed = false;
        }

        static void Postfix(ZNet __instance) { Restore(__instance); }
        static Exception Finalizer(ZNet __instance, Exception __exception) { Restore(__instance); return __exception; }
    }
}
