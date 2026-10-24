// Parses the body of an inbox file: key=value pairs joined with '&' or new lines, '+' = space, %xx decoded.
// Keys are case-insensitive. No Unity reference: also compiled into HomeCheat.SelfTest.exe.
using System;
using System.Collections.Generic;

namespace HomeCheat
{
    public static class FormParser
    {
        public static Dictionary<string, string> Parse(string body)
        {
            Dictionary<string, string> d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (body == null) return d;
            foreach (string pair in body.Split(new char[] { '&', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int i = pair.IndexOf('=');
                string k = i < 0 ? pair : pair.Substring(0, i);
                string v = i < 0 ? "" : pair.Substring(i + 1);
                string key = Decode(k).Trim();
                if (key.Length == 0) continue;
                d[key] = Decode(v).Trim();
            }
            return d;
        }

        static string Decode(string s)
        {
            return Uri.UnescapeDataString(s.Replace('+', ' '));
        }
    }
}
