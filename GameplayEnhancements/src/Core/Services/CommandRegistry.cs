// Every command by name, and which feature (DLL) provides it.
//   built-ins   registered by the plugin, feature "core", never replaced
//   features    BepInEx/homecheat-commands/HomeCheat.Cmd.<feature>.v<stamp>.dll; only the newest stamp of each feature is loaded.
// Reload: for each feature whose newest file is newer than the loaded one, the new DLL is loaded and its commands constructed
// (constructors have no side effects), then the old commands of that feature are unloaded and removed, the new ones registered,
// and Load is called on each. A DLL cannot be unloaded from memory (.NET 4); the old one just stops being used.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace HomeCheat.Services
{
    internal sealed class CommandRegistry
    {
        public const string CoreFeature = "core";

        sealed class Feature
        {
            public string Name, File;
            public long Stamp;
            public readonly List<IHomeCommand> Commands = new List<IHomeCommand>();
        }

        readonly string commandsDir;
        readonly Dictionary<string, IHomeCommand> byName = new Dictionary<string, IHomeCommand>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, string> featureOf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, Feature> features = new Dictionary<string, Feature>(StringComparer.OrdinalIgnoreCase);

        public CommandRegistry(string dir) { commandsDir = dir; }

        public string Directory { get { return commandsDir; } }

        public void RegisterBuiltin(IHomeCommand c)
        {
            byName[c.Name] = c;
            featureOf[c.Name] = CoreFeature;
            ICommandLifecycle lc = c as ICommandLifecycle;
            if (lc != null) lc.Load();
        }

        public bool TryGet(string name, out IHomeCommand c) { return byName.TryGetValue(name, out c); }

        public string FeatureOf(string name)
        {
            string f;
            return featureOf.TryGetValue(name, out f) ? f : "?";
        }

        public List<string> Names()
        {
            List<string> l = new List<string>(byName.Keys);
            l.Sort(StringComparer.OrdinalIgnoreCase);
            return l;
        }

        public List<IHomeCommand> All()
        {
            List<IHomeCommand> l = new List<IHomeCommand>();
            foreach (string n in Names()) l.Add(byName[n]);
            return l;
        }

        // newest file per feature in the commands folder
        Dictionary<string, KeyValuePair<string, long>> Newest(StringBuilder report)
        {
            Dictionary<string, KeyValuePair<string, long>> best = new Dictionary<string, KeyValuePair<string, long>>(StringComparer.OrdinalIgnoreCase);
            System.IO.Directory.CreateDirectory(commandsDir);
            foreach (string f in System.IO.Directory.GetFiles(commandsDir, "*.dll"))
            {
                string feature; long stamp;
                if (!BuildStamp.TryParse(Path.GetFileName(f), out feature, out stamp))
                {
                    report.Append("skipped ").Append(Path.GetFileName(f)).Append(" (name is not HomeCheat.Cmd.<feature>.v<stamp>.dll)\n");
                    continue;
                }
                KeyValuePair<string, long> cur;
                if (!best.TryGetValue(feature, out cur) || stamp > cur.Value) best[feature] = new KeyValuePair<string, long>(f, stamp);
            }
            return best;
        }

        public string Reload()
        {
            StringBuilder sb = new StringBuilder();
            List<string> order = new List<string>();
            Dictionary<string, KeyValuePair<string, long>> best = Newest(sb);
            order.AddRange(best.Keys);
            order.Sort(StringComparer.OrdinalIgnoreCase);
            int loaded = 0;
            foreach (string feature in order)
            {
                KeyValuePair<string, long> file = best[feature];
                Feature old;
                if (features.TryGetValue(feature, out old) && old.Stamp >= file.Value) continue;
                if (string.Equals(feature, CoreFeature, StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append("FAILED ").Append(Path.GetFileName(file.Key)).Append(": feature name 'core' is reserved\n");
                    continue;
                }
                if (LoadFeature(feature, file.Key, file.Value, old, sb)) loaded++;
            }
            if (loaded == 0 && sb.Length == 0) return "no new builds in " + commandsDir;
            return sb.ToString();
        }

        bool LoadFeature(string feature, string file, long stamp, Feature old, StringBuilder sb)
        {
            string fileName = Path.GetFileName(file);
            List<IHomeCommand> made = new List<IHomeCommand>();
            try
            {
                Assembly asm = Assembly.Load(File.ReadAllBytes(file));
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e)
                {
                    StringBuilder why = new StringBuilder();
                    foreach (Exception le in e.LoaderExceptions) if (le != null) why.Append(le.Message).Append("; ");
                    throw new Exception("types failed to load: " + why);
                }
                foreach (Type t in types)
                {
                    if (t.IsAbstract || !typeof(IHomeCommand).IsAssignableFrom(t) || t.GetConstructor(Type.EmptyTypes) == null) continue;
                    made.Add((IHomeCommand)Activator.CreateInstance(t));
                }
            }
            catch (Exception e)
            {
                sb.Append("FAILED ").Append(fileName).Append(": ").Append(e.Message).Append('\n');
                HomeApi.LogError("load " + fileName + ": " + e);
                return false;
            }
            // a name owned by another feature (or a built-in) is refused, the rest of the DLL still loads
            List<IHomeCommand> accepted = new List<IHomeCommand>();
            foreach (IHomeCommand c in made)
            {
                string owner;
                if (featureOf.TryGetValue(c.Name, out owner) && !string.Equals(owner, feature, StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append("FAILED ").Append(c.Name).Append(" from ").Append(fileName).Append(": name already provided by '").Append(owner).Append("'\n");
                    continue;
                }
                accepted.Add(c);
            }
            if (old != null) UnloadFeature(old, sb);
            Feature f = new Feature { Name = feature, File = fileName, Stamp = stamp };
            foreach (IHomeCommand c in accepted)
            {
                byName[c.Name] = c;
                featureOf[c.Name] = feature;
                f.Commands.Add(c);
            }
            features[feature] = f;
            foreach (IHomeCommand c in accepted)
            {
                ICommandLifecycle lc = c as ICommandLifecycle;
                try { if (lc != null) lc.Load(); }
                catch (Exception e)
                {
                    sb.Append("FAILED load of ").Append(c.Name).Append(": ").Append(e.Message).Append('\n');
                    HomeApi.LogError("Load " + c.Name + ": " + e);
                }
                sb.Append("registered ").Append(c.Name).Append(" from ").Append(fileName).Append('\n');
            }
            if (accepted.Count == 0) sb.Append("no commands in ").Append(fileName).Append('\n');
            return true;
        }

        void UnloadFeature(Feature old, StringBuilder sb)
        {
            foreach (IHomeCommand c in old.Commands)
            {
                ICommandLifecycle lc = c as ICommandLifecycle;
                try { if (lc != null) lc.Unload(); }
                catch (Exception e) { HomeApi.LogError("Unload " + c.Name + ": " + e); }
                IHomeCommand cur;
                if (byName.TryGetValue(c.Name, out cur) && ReferenceEquals(cur, c)) { byName.Remove(c.Name); featureOf.Remove(c.Name); }
            }
            sb.Append("unloaded ").Append(old.Name).Append(" (").Append(old.File).Append(")\n");
        }

        public string Describe()
        {
            StringBuilder sb = new StringBuilder();
            List<string> names = new List<string>(features.Keys);
            names.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (string n in names) sb.Append(n).Append(": ").Append(features[n].File).Append('\n');
            return sb.ToString();
        }
    }
}
