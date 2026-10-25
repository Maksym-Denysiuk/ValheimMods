// The highest Group-N number ever given in a world, so a deleted group's number is never given again (Maksym 2026-10-25: after
// deleting Group-1 and Group-2 a new zone was called Group-1 again and was taken for the old one). Kept on this PC only, in
// BepInEx/config/homecheat/dome-groups.state: one line per world, world <tab> highest number.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using HomeCheat.Kit;

namespace HomeCheat.Features.Dome
{
    internal static class DomeNumbers
    {
        static readonly Dictionary<string, int> highest = new Dictionary<string, int>();
        static bool loaded;

        static string FilePath { get { return Path.Combine(Path.Combine(Paths.ConfigPath, "homecheat"), "dome-groups.state"); } }

        static string World { get { return ZNet.instance == null ? "" : ZNet.instance.GetWorldName(); } }

        public static int Highest
        {
            get
            {
                Load();
                int n;
                return highest.TryGetValue(World, out n) ? n : 0;
            }
        }

        // remembers 'no' as given in the loaded world (saved when it is a new highest)
        public static void Used(int no)
        {
            if (no <= Highest) return;
            highest[World] = no;
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, int> kv in highest) sb.Append(kv.Key).Append('\t').Append(kv.Value).Append('\n');
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllText(FilePath, sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception e) { HomeApi.LogError("dome: writing " + FilePath + ": " + e.Message); }
        }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    string[] p = line.Split('\t');
                    int n;
                    if (p.Length == 2 && int.TryParse(p[1], out n)) highest[p[0]] = n;
                }
            }
            catch (Exception e) { HomeApi.LogError("dome: reading " + FilePath + ": " + e.Message); }
        }
    }
}
