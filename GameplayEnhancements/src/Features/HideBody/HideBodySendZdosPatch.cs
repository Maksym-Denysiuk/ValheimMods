// writes y = LiftY into the local player's ZDO position field for the duration of each send, then restores it
using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HomeCheat.Features.HideBody
{
    [HarmonyPatch(typeof(ZDOMan), "SendZDOs")]
    internal static class HideBodySendZdosPatch
    {
        static readonly FieldInfo fPos = typeof(ZDO).GetField("m_position", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        static ZDO lifted;
        static Vector3 saved;

        static void Prefix()
        {
            if (!HideBodyCommand.Active || fPos == null) return;
            try
            {
                Player p = Player.m_localPlayer;
                if (p == null) return;
                ZNetView nv = p.GetComponent<ZNetView>();
                ZDO z = nv != null && nv.IsValid() ? nv.GetZDO() : null;
                if (z == null) return;
                saved = (Vector3)fPos.GetValue(z);
                Vector3 v = saved;
                v.y = HideBodyCommand.Current.LiftY;
                fPos.SetValue(z, (object)v);
                lifted = z;
                HideBodyCommand.Current.Lifts++;
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
}
