// The players who have joined a world, kept per world so the dome menu can offer them for a ward's permitted list while
// they are offline too (Maksym 2026-10-25: "the joining list with historical data for the current world"). The host only
// knows a player's id while that player is connected, so the list grows as players are seen: every player online (the
// host's player list, id from the character's ZDO) and every player on a ward's permitted list in the world.
// Kept on this PC only, in BepInEx/config/homecheat/dome-players.state: one line per player, world <tab> id <tab> name.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using HomeCheat.Kit;

namespace HomeCheat.Features.Dome
{
    internal static class DomePlayers
    {
        static readonly Dictionary<string, Dictionary<long, string>> worlds = new Dictionary<string, Dictionary<long, string>>();
        static bool loaded;

        static string FilePath { get { return Path.Combine(Path.Combine(Paths.ConfigPath, "homecheat"), "dome-players.state"); } }

        // the players of the loaded world, sorted by name
        public static List<KeyValuePair<long, string>> Known()
        {
            List<KeyValuePair<long, string>> r = new List<KeyValuePair<long, string>>(World().Count);
            foreach (KeyValuePair<long, string> kv in World()) r.Add(kv);
            r.Sort(delegate (KeyValuePair<long, string> x, KeyValuePair<long, string> y) { return string.Compare(x.Value, y.Value, StringComparison.OrdinalIgnoreCase); });
            return r;
        }

        // the id of a known player by name (case-insensitive); 0 when unknown
        public static long IdOf(string name)
        {
            foreach (KeyValuePair<long, string> kv in World()) if (string.Equals(kv.Value, name, StringComparison.OrdinalIgnoreCase)) return kv.Key;
            return 0L;
        }

        // adds the players online now and the given ward lists; saves when something is new
        public static void Record(List<ZDO> wards)
        {
            if (ZNet.instance == null || ZDOMan.instance == null) return;
            bool changed = false;
            Player me = Player.m_localPlayer;
            if (me != null) changed |= Add(me.GetPlayerID(), me.GetPlayerName());
            foreach (ZNet.PlayerInfo pi in ZNet.instance.GetPlayerList())
            {
                ZDO z = ZDOMan.instance.GetZDO(pi.m_characterID);
                if (z == null) continue;
                changed |= Add(z.GetLong(ZDOVars.s_playerID, 0L), pi.m_name);
            }
            if (wards != null)
                foreach (ZDO w in wards)
                    foreach (KeyValuePair<long, string> kv in DomeWards.Read(w).Permitted) changed |= Add(kv.Key, kv.Value);
            if (changed) Save();
        }

        static bool Add(long id, string name)
        {
            if (id == 0L || string.IsNullOrEmpty(name)) return false;
            Dictionary<long, string> w = World();
            string old;
            if (w.TryGetValue(id, out old) && old == name) return false;
            w[id] = name;
            return true;
        }

        static Dictionary<long, string> World()
        {
            Load();
            string key = ZNet.instance == null ? "" : ZNet.instance.GetWorldName();
            Dictionary<long, string> w;
            if (!worlds.TryGetValue(key, out w)) { w = new Dictionary<long, string>(); worlds[key] = w; }
            return w;
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
                    long id;
                    if (p.Length != 3 || !long.TryParse(p[1], out id) || id == 0L) continue;
                    Dictionary<long, string> w;
                    if (!worlds.TryGetValue(p[0], out w)) { w = new Dictionary<long, string>(); worlds[p[0]] = w; }
                    w[id] = p[2];
                }
            }
            catch (Exception e) { HomeApi.LogError("dome: reading " + FilePath + ": " + e.Message); }
        }

        static void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, Dictionary<long, string>> w in worlds)
                    foreach (KeyValuePair<long, string> kv in w.Value)
                        sb.Append(w.Key).Append('\t').Append(kv.Key).Append('\t').Append(kv.Value.Replace('\t', ' ')).Append('\n');
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllText(FilePath, sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception e) { HomeApi.LogError("dome: writing " + FilePath + ": " + e.Message); }
        }
    }
}
