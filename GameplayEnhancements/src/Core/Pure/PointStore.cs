// Named points per world, as text: one line "world|name|x,y,z" (InvariantCulture), '#' lines are comments.
// Kit.Coords keeps it in BepInEx/config/homecheat/coords.state. No Unity reference: also compiled into HomeCheat.SelfTest.exe.
// Names are lower case letters, digits, '_' or '-', at most 32 characters; world and point names compare case-insensitively.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HomeCheat
{
    public sealed class PointStore
    {
        public const int MaxNameLength = 32;
        public const int MaxPointsPerWorld = 64;

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        readonly Dictionary<string, Dictionary<string, float[]>> worlds =
            new Dictionary<string, Dictionary<string, float[]>>(StringComparer.OrdinalIgnoreCase);

        // lines that do not parse are counted in 'skipped' and dropped
        public static PointStore Parse(string text, out int skipped)
        {
            PointStore st = new PointStore();
            skipped = 0;
            if (text == null) return st;
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                string[] p = line.Split('|');
                float[] v = p.Length == 3 ? ParseVec(p[2]) : null;
                if (v == null || p[0].Trim().Length == 0 || !ValidName(p[1].Trim())) { skipped++; continue; }
                st.Points(p[0].Trim(), true)[p[1].Trim().ToLowerInvariant()] = v;
            }
            return st;
        }

        public string Format()
        {
            StringBuilder sb = new StringBuilder("# HomeCheat coord points (op=coord): world|name|x,y,z\n");
            List<string> ws = new List<string>(worlds.Keys);
            ws.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (string w in ws)
                foreach (string n in Names(w))
                {
                    float[] v = worlds[w][n];
                    sb.Append(w).Append('|').Append(n).Append('|')
                      .Append(v[0].ToString("R", Inv)).Append(',').Append(v[1].ToString("R", Inv)).Append(',').Append(v[2].ToString("R", Inv)).Append('\n');
                }
            return sb.ToString();
        }

        public static bool ValidName(string name)
        {
            if (name == null || name.Length == 0 || name.Length > MaxNameLength) return false;
            foreach (char c in name)
                if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-')) return false;
            return true;
        }

        public bool TryGet(string world, string name, out float x, out float y, out float z)
        {
            x = y = z = 0f;
            Dictionary<string, float[]> d = Points(world, false);
            float[] v;
            if (d == null || name == null || !d.TryGetValue(name.ToLowerInvariant(), out v)) return false;
            x = v[0]; y = v[1]; z = v[2];
            return true;
        }

        // throws CommandException on a bad name, a bad world key or a full world
        public void Set(string world, string name, float x, float y, float z)
        {
            if (!ValidName(name)) throw new CommandException("point name '" + name + "': letters, digits, _ or -, at most " + MaxNameLength);
            if (world == null || world.Trim().Length == 0 || world.IndexOf('|') >= 0 || world.IndexOf('\n') >= 0)
                throw new CommandException("bad world name for points: '" + world + "'");
            Dictionary<string, float[]> d = Points(world.Trim(), true);
            string key = name.ToLowerInvariant();
            if (!d.ContainsKey(key) && d.Count >= MaxPointsPerWorld)
                throw new CommandException("world " + world + " already has " + MaxPointsPerWorld + " points (cap): clear some first");
            d[key] = new float[] { x, y, z };
        }

        public bool Clear(string world, string name)
        {
            Dictionary<string, float[]> d = Points(world, false);
            return d != null && name != null && d.Remove(name.ToLowerInvariant());
        }

        public int ClearWorld(string world)
        {
            Dictionary<string, float[]> d = Points(world, false);
            if (d == null) return 0;
            int n = d.Count;
            worlds.Remove(world.Trim());
            return n;
        }

        public List<string> Names(string world)
        {
            Dictionary<string, float[]> d = Points(world, false);
            List<string> l = d == null ? new List<string>() : new List<string>(d.Keys);
            l.Sort(StringComparer.Ordinal);
            return l;
        }

        Dictionary<string, float[]> Points(string world, bool create)
        {
            if (world == null) return null;
            Dictionary<string, float[]> d;
            if (worlds.TryGetValue(world.Trim(), out d)) return d;
            if (!create) return null;
            d = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase);
            worlds[world.Trim()] = d;
            return d;
        }

        static float[] ParseVec(string s)
        {
            string[] p = s.Split(',');
            if (p.Length != 3) return null;
            float[] v = new float[3];
            for (int i = 0; i < 3; i++)
                if (!float.TryParse(p[i].Trim(), NumberStyles.Float, Inv, out v[i]) || float.IsNaN(v[i]) || float.IsInfinity(v[i])) return null;
            return v;
        }
    }
}
