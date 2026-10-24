// HomeCheat command API: what an extra command DLL (BepInEx/homecheat-commands/*.dll) compiles against.
// A command is a class with a public parameterless constructor that implements IHomeCommand; 'op=reload'
// registers it under Name (lower case), then 'op=<name>&k=v...' reaches Run on the Unity main thread.
// C# 5 only (the only compiler on this machine is csc 4.0): no ?., no $"", no expression bodies.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HomeCheat
{
    public interface IHomeCommand
    {
        string Name { get; }
        string Help { get; }
        string Run(Dictionary<string, string> a);
    }

    // thin public facade over the core helpers, so command DLLs never touch internals
    public static class HomeApi
    {
        public static Player P() { return HomeCheatPlugin.P(); }
        public static string S(Dictionary<string, string> a, string k, string def) { return HomeCheatPlugin.S(a, k, def); }
        public static int I(Dictionary<string, string> a, string k, int def) { return HomeCheatPlugin.I(a, k, def); }
        public static float F(Dictionary<string, string> a, string k, float def) { return HomeCheatPlugin.F(a, k, def); }
        public static bool Flag(Dictionary<string, string> a, string k, bool def) { return HomeCheatPlugin.Flag(a, k, def); }
        public static string V(Vector3 v) { return HomeCheatPlugin.V(v); }

        // crosshair point (camera ray, 60 m), else 8 m ahead of the player
        public static Vector3 AimPoint(Player p, out bool aimed) { return HomeCheatPlugin.AimPoint(p, out aimed); }
        public static bool IsBlocked(Vector3 pos, float radius) { return HomeCheatPlugin.IsBlocked(pos, radius); }
        public static float GroundHeight(Vector3 pos) { return ZoneSystem.instance.GetGroundHeight(pos); }
        public static GameObject Prefab(string name) { return ZNetScene.instance.GetPrefab(name); }

        // spawn a prefab by name; null when the name is unknown
        public static GameObject Spawn(string prefab, Vector3 pos, Quaternion rot)
        {
            GameObject go = Prefab(prefab);
            return go == null ? null : (GameObject)UnityEngine.Object.Instantiate(go, pos, rot);
        }

        public static void SetScale(GameObject inst, float scale) { HomeCheatPlugin.SetScale(inst, scale); }

        // run on the main thread after 'seconds' (0 = next frame)
        public static void Later(float seconds, Action act) { HomeCheatPlugin.Later(seconds, act); }

        public static Player FindPlayer(string name)
        {
            foreach (Player pl in Player.GetAllPlayers())
                if (string.Equals(pl.GetPlayerName(), name, StringComparison.OrdinalIgnoreCase)) return pl;
            return null;
        }

        public static void Log(string msg) { HomeCheatPlugin.LogInfo(msg); }
    }

    // Base of every "harm" effect (slime, earthquake, ...). Subclasses implement Run; this class holds what they share:
    // target resolution, ring placement and delayed repeats. Reachable as op=<kind> and as op=harmeffect&kind=<kind>.
    public abstract class HarmEffect : IHomeCommand
    {
        public abstract string Name { get; }
        public abstract string Help { get; }
        public abstract string Run(Dictionary<string, string> a);

        // the player the effect is centred on: parameter 'target' (nickname), default the local player
        protected static Player Target(Dictionary<string, string> a, out string error)
        {
            error = null;
            string n = HomeApi.S(a, "target", "");
            if (n.Length == 0) return HomeApi.P();
            Player p = HomeApi.FindPlayer(n);
            if (p == null) error = "ERROR: no player named '" + n + "' is online";
            return p;
        }

        // equally spaced points on a circle around 'center' (y = ground). Count = circumference / itemDiameter
        // rounded down, at least 1; 'startAngleDeg' rotates the whole ring.
        public static List<Vector3> Ring(Vector3 center, float ringRadius, float itemDiameter, float startAngleDeg)
        {
            List<Vector3> pts = new List<Vector3>();
            int n = Mathf.Max(1, Mathf.FloorToInt(2f * Mathf.PI * ringRadius / Mathf.Max(0.1f, itemDiameter)));
            for (int i = 0; i < n; i++)
            {
                float ang = (startAngleDeg + 360f * i / n) * Mathf.Deg2Rad;
                Vector3 pos = center + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * ringRadius;
                pos.y = HomeApi.GroundHeight(pos);
                pts.Add(pos);
            }
            return pts;
        }
    }
}
