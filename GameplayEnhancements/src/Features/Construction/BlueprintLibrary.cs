// The constructions in BepInEx/config/homecheat/blueprints/: every *.blueprint file directly inside is one construction,
// every subfolder is one construction whose *.blueprint files are its versions (2026-10-25: the gumdrop folder holds the
// same house twice, v2 and endgame, captured 22.5 degrees apart; built together they stood inside each other, so card
// assumption A2 "parts around one origin" was wrong). One version is built: name=<folder>/<version>, or the largest.
// Read fresh on every call: the folder is small and the player may drop new files in while the game runs. Unity-free.
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

        // the construction to build: a single file as it is; a folder's version named after the slash (part of its file name,
        // ignoring case), else its largest version. Null when nothing matches; note says which version was taken
        public static BlueprintEntry Resolve(List<BlueprintEntry> all, string name, out string note)
        {
            note = "";
            string n = name.Trim().TrimEnd('/');
            string version = "";
            int slash = n.IndexOf('/');
            if (slash >= 0) { version = n.Substring(slash + 1); n = n.Substring(0, slash); }
            BlueprintEntry e = Find(all, n);
            if (e == null || e.Parts.Count == 1) return e;
            Blueprint pick = null;
            if (version.Length > 0)
            {
                foreach (Blueprint b in e.Parts)
                    if (Path.GetFileNameWithoutExtension(b.File).IndexOf(version, StringComparison.OrdinalIgnoreCase) >= 0 || string.Equals(b.Name, version, StringComparison.OrdinalIgnoreCase)) { pick = b; break; }
                if (pick == null) return null;
            }
            else
            {
                foreach (Blueprint b in e.Parts) if (pick == null || b.Pieces.Count > pick.Pieces.Count) pick = b;
                note = "the largest of " + e.Parts.Count + " versions (" + Versions(e) + "; choose with name=" + e.Name + "/<version>)";
            }
            BlueprintEntry one = new BlueprintEntry();
            one.Name = e.Name + "/" + Path.GetFileNameWithoutExtension(pick.File);
            one.Source = e.Source + Path.GetFileName(pick.File);
            one.Parts.Add(pick);
            return one;
        }

        // "Brueplint_Gumdropv2 1477, Brueplint_gumdropendgame 1989"
        public static string Versions(BlueprintEntry e)
        {
            List<string> v = new List<string>();
            foreach (Blueprint b in e.Parts) v.Add(Path.GetFileNameWithoutExtension(b.File) + " " + b.Pieces.Count);
            return string.Join(", ", v.ToArray());
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
