// Typed access to the parameters of one command. Semantics of the old S/I/F/Flag helpers:
//   Str   the value when the key exists and is not empty, else the default
//   Int   Float   parsed with InvariantCulture, else the default
//   Flag  empty or missing = default; 1, true, yes, aim = true; any other word = false
//   Has   the key exists, even with an empty value
// No Unity reference: also compiled into HomeCheat.SelfTest.exe.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace HomeCheat
{
    public sealed class Args
    {
        readonly Dictionary<string, string> raw;

        public Args(Dictionary<string, string> values)
        {
            raw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (values != null) foreach (KeyValuePair<string, string> kv in values) raw[kv.Key] = kv.Value;
        }

        // a copy, for forwarding to another command
        public Dictionary<string, string> Raw { get { return new Dictionary<string, string>(raw, StringComparer.OrdinalIgnoreCase); } }

        public bool Has(string key) { return raw.ContainsKey(key); }

        public string Str(string key, string def)
        {
            string v;
            return raw.TryGetValue(key, out v) && v != null && v.Length > 0 ? v : def;
        }

        public int Int(string key, int def)
        {
            int v;
            return int.TryParse(Str(key, ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : def;
        }

        public float Float(string key, float def)
        {
            float v;
            return float.TryParse(Str(key, ""), NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : def;
        }

        public bool Flag(string key, bool def)
        {
            string v = Str(key, "");
            if (v.Length == 0) return def;
            return IsTrue(v);
        }

        // the value, or a CommandException "<key> required, <hint>"
        public string Required(string key, string hint)
        {
            string v = Str(key, "");
            if (v.Length == 0) throw new CommandException(key + " required" + (hint == null || hint.Length == 0 ? "" : ", " + hint));
            return v;
        }

        public static bool IsTrue(string v)
        {
            v = v.Trim().ToLowerInvariant();
            return v == "1" || v == "true" || v == "yes" || v == "aim";
        }
    }
}
