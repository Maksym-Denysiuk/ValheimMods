// The constructions in BepInEx/config/homecheat/blueprints/: every *.blueprint file directly inside is one construction,
// every subfolder is one construction made of the *.blueprint files in it (A2). Read fresh on every call: the folder is small
// and the player may drop new files in while the game runs. Unity-free.
using System;
using System.Collections.Generic;
using System.IO;

namespace HomeCheat.Features.Construction
{
    internal static class BlueprintLibrary
    {
        public const string Extension = "*.blueprint";

        public static List<BlueprintEntry> Scan(string dir)
        {
            List<BlueprintEntry> found = new List<BlueprintEntry>();
            if (!Directory.Exists(dir)) return found;
            string[] files = Directory.GetFiles(dir, Extension);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            foreach (string f in files)
            {
                BlueprintEntry e = new BlueprintEntry();
                Blueprint b = BlueprintParse.Parse(File.ReadAllText(f), f);
                e.Parts.Add(b);
                e.Name = b.Name;
                e.Source = Path.GetFileName(f);
                found.Add(e);
            }
            string[] dirs = Directory.GetDirectories(dir);
            Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);
            foreach (string d in dirs)
            {
                string[] parts = Directory.GetFiles(d, Extension);
                if (parts.Length == 0) continue;
                Array.Sort(parts, StringComparer.OrdinalIgnoreCase);
                BlueprintEntry e = new BlueprintEntry();
                foreach (string f in parts) e.Parts.Add(BlueprintParse.Parse(File.ReadAllText(f), f));
                e.Name = Path.GetFileName(d);
                e.Source = Path.GetFileName(d) + "/";
                found.Add(e);
            }
            return found;
        }

        // by name, file name (with or without .blueprint) or folder name, ignoring case; null when none matches
        public static BlueprintEntry Find(List<BlueprintEntry> all, string name)
        {
            string n = name.Trim().TrimEnd('/');
            foreach (BlueprintEntry e in all)
            {
                string src = e.Source.TrimEnd('/');
                if (string.Equals(e.Name, n, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(src, n, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(Path.GetFileNameWithoutExtension(src), n, StringComparison.OrdinalIgnoreCase))
                    return e;
            }
            return null;
        }
    }
}
