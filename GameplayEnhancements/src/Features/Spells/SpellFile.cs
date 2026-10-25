using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HomeCheat.Features.Spells
{
    // The active spell file (spells.txt, or spells-ui-test.txt in placeholder mode) and the bar cells:
    // parsing, the defaults written when a file is missing, the .pins file of placed cells, list and pin answers.
    internal sealed class SpellFile
    {
        SpellGroup[] groups = new SpellGroup[9];
        Dictionary<string, Spell> pins = new Dictionary<string, Spell>();
        DateTime fileStamp;

        public Dictionary<string, Spell> Pins { get { return pins; } }
        public SpellGroup GroupAt(int slot) { return slot >= 1 && slot <= 8 ? groups[slot] : null; }

        const string DefaultText =
"# HomeCheat spell bar. One spell per line:  slot.entry | label | command | flags\n" +
"#   slot alone + 2 fields = group title;  slot alone + 3 fields = the slot is itself a spell;  slot.entry = a spell inside the group (entry 1-8)\n" +
"#   command = the form of an inbox file (op=...&k=v); chain calls with ;;   special: @dummy only shows a toast\n" +
"#   flags (space separated): sticky (radial stays open after the cast)  pin=F1..F5|S1..S5 (S = Shift+digit, shown as 1-5 in row one)  shift=<form added when Shift is held while casting>\n" +
"# F5 is also the game console key: a pin on F5 opens the console as well. Slot 1 is always the game radial Hotbar group (not configurable).\n" +
"2 | Attack\n" +
"2.1 | Arrow | op=spellshot&type=arrow | pin=F1 shift=perrow=3&rows=3\n" +
"2.2 | Arrow 3x3 | op=spellshot&type=arrow&perrow=3&rows=3\n" +
"2.3 | Balista | op=spellshot&type=balistashot\n" +
"2.4 | Thunderbolt | op=spellshot&type=thunderbolt\n" +
"2.5 | Quake (damage) | op=earthquake&damage=1&noStones=1\n" +
"2.6 | Slime ring | op=slime\n" +
"3 | Protect\n" +
"3.1 | Push away | op=earthquake&noStones=1 | pin=F2 shift=mode=strong\n" +
"3.2 | Stone circle | op=earthquake&damage=1 | pin=S2 shift=mode=strong\n" +
"3.3 | Strong push | op=earthquake&mode=strong&noStones=1\n" +
"3.4 | Slime moat | op=slime&gap=3\n" +
"4 | Fun\n" +
"4.1 | Raven 60 s | op=transform&type=raven&duration=60 | pin=S4\n" +
"4.2 | Serpent 60 s | op=transform&type=serpent&duration=60\n" +
"4.3 | Fish 60 s | op=transform&type=fish&duration=60\n" +
"4.4 | Human | op=transform&type=human | pin=S5\n" +
"4.5 | Oaks x2 | op=scale&filter=Oak&factor=2&radius=30\n" +
"4.6 | Reset sky | op=visualeffect&name=reset\n" +
"5 | Heal and bless\n" +
"5.1 | Heal | op=heal | pin=F3\n" +
"5.2 | Cleanse | op=cleanse\n" +
"5.3 | Bonemass | op=bless&name=bonemass | pin=S3\n" +
"5.4 | Eikthyr | op=bless&name=eikthyr\n" +
"5.5 | The Elder | op=bless&name=elder\n" +
"5.6 | Moder | op=bless&name=moder\n" +
"5.7 | Yagluth | op=bless&name=yagluth\n" +
"5.8 | The Queen | op=bless&name=queen\n" +
"6 | Build\n" +
"6.1 | Flatten | op=groundlevel&delta=0\n" +
"6.2 | Raise 1 m | op=groundlevel&delta=1\n" +
"6.3 | Lower 1 m | op=groundlevel&delta=-1\n" +
"6.4 | Dirt path 5 m | op=path&type=dirt&length=5\n" +
"6.5 | Stone path 5 m | op=path&type=stone&length=5\n" +
"7 | Field\n" +
"7.1 | Cultivate 3x5 | op=cultivate&rows=3&perrow=5 | pin=S1\n" +
"7.2 | Carrots 3x5 | op=field&prefab=Pickable_Carrot&rows=3&perrow=5\n" +
"7.3 | Oak grove | op=plant&prefab=Oak_Sapling&rows=3&perrow=5&scale=3\n" +
"7.4 | Place wall | op=place&prefab=woodwall&perrow=3\n" +
"7.5 | Place campfire | op=place&prefab=fire_pit\n" +
"8 | Repair\n" +
"8.1 | Repair (aim) | op=repair | sticky\n" +
"8.2 | Repair r10 | op=repair&aim=0&radius=10 | sticky pin=F4\n" +
"8.3 | Refill (aim) | op=refill | sticky\n" +
"8.4 | Refill r10 | op=refill&aim=0&radius=10 | sticky pin=F5\n" +
"8.5 | Repair + refill r10 | op=repair&aim=0&radius=10 ;; op=refill&aim=0&radius=10 | sticky\n";

        // spells-ui-test.txt: fake spells for all 8 slots, every command is @dummy. In placeholder mode a cast never runs any command anyway.
        const string TestText =
"# HomeCheat spell bar UI test. Placeholder spells only: a cast shows 'placeholder: <label> (<slot.entry>)' and logs it, nothing is executed.\n" +
"# Format as in spells.txt:  slot.entry | label | command | flags   (command @dummy; flags: sticky  pin=F1..F5|S1..S5  shift=<text>)\n" +
"2 | Attack\n" +
"2.1 | Arrow | @dummy | pin=F1 shift=perrow=3&rows=3\n" +
"2.2 | Arrow 3x3 | @dummy\n" +
"2.3 | Balista | @dummy | shift=perrow=3\n" +
"2.4 | Thunderbolt | @dummy\n" +
"2.5 | Quake (damage) | @dummy | shift=mode=strong\n" +
"2.6 | Slime ring | @dummy\n" +
"3 | Protect\n" +
"3.1 | Push away | @dummy | pin=F2 shift=mode=strong\n" +
"3.2 | Stone circle | @dummy | pin=S2\n" +
"3.3 | Strong push | @dummy\n" +
"3.4 | Slime moat | @dummy\n" +
"3.5 | Ward | @dummy | sticky\n" +
"4 | Fun\n" +
"4.1 | Raven 60 s | @dummy | pin=S4\n" +
"4.2 | Serpent 60 s | @dummy\n" +
"4.3 | Fish 60 s | @dummy\n" +
"4.4 | Human | @dummy | pin=S5\n" +
"4.5 | Oaks x2 | @dummy | shift=factor=4\n" +
"4.6 | Reset sky | @dummy\n" +
"4.7 | Rain | @dummy | sticky\n" +
"4.8 | Night | @dummy | sticky\n" +
"5 | Heal and bless\n" +
"5.1 | Heal | @dummy | pin=F3\n" +
"5.2 | Cleanse | @dummy\n" +
"5.3 | Bonemass | @dummy | pin=S3\n" +
"5.4 | Eikthyr | @dummy\n" +
"5.5 | The Elder | @dummy\n" +
"5.6 | Moder | @dummy\n" +
"5.7 | Yagluth | @dummy\n" +
"5.8 | The Queen | @dummy\n" +
"6 | Build\n" +
"6.1 | Flatten | @dummy | sticky\n" +
"6.2 | Raise 1 m | @dummy | sticky shift=delta=3\n" +
"6.3 | Lower 1 m | @dummy | sticky shift=delta=-3\n" +
"6.4 | Dirt path 5 m | @dummy\n" +
"6.5 | Stone path 5 m | @dummy\n" +
"7 | Field\n" +
"7.1 | Cultivate 3x5 | @dummy | pin=S1\n" +
"7.2 | Carrots 3x5 | @dummy\n" +
"7.3 | Oak grove | @dummy\n" +
"7.4 | Place wall | @dummy | shift=perrow=5\n" +
"7.5 | Place campfire | @dummy\n" +
"7.6 | Place torch | @dummy\n" +
"8 | Repair\n" +
"8.1 | Repair (aim) | @dummy | sticky\n" +
"8.2 | Repair r10 | @dummy | sticky pin=F4 shift=radius=30\n" +
"8.3 | Refill (aim) | @dummy | sticky\n" +
"8.4 | Refill r10 | @dummy | sticky pin=F5\n" +
"8.5 | Repair + refill r10 | @dummy | sticky\n";

        public static string ActiveDefault { get { return SpellsSettings.IsPlaceholder ? TestText : DefaultText; } }

        internal static readonly string[] PinOrder = new string[] { "S1", "S2", "S3", "S4", "S5", "F1", "F2", "F3", "F4", "F5" };

        public static string PinLabel(string pin)
        {
            return pin.Length == 2 && pin[0] == 'S' ? "[Shift+" + pin[1] + "]" : "[" + pin + "]";
        }

        public string PinOf(Spell s)
        {
            for (int i = 0; i < PinOrder.Length; i++)
            {
                Spell p;
                if (pins.TryGetValue(PinOrder[i], out p) && p == s) return PinOrder[i];
            }
            return "";
        }

        public static string IdOf(Spell s) { return s.Entry > 0 ? s.Slot + "." + s.Entry : "" + s.Slot; }

        string OverrideFile { get { return SpellsSettings.ActiveFile + ".pins"; } }

        // the cells you placed: one line per cell, "S1=2.1" or "F3=5.1"; once the file exists it replaces the pin= flags of the spell file
        public void SaveOverrides()
        {
            try
            {
                StringBuilder sb = new StringBuilder("# written by the spell bar when you place a spell: cell=slot.entry (S1..S5 = Shift+1..5, F1..F5). Delete this file (or op=spells&pins=reset) to go back to the pin= flags of the spell file.\n");
                foreach (string k in PinOrder) { Spell s; if (pins.TryGetValue(k, out s)) sb.Append(k).Append('=').Append(IdOf(s)).Append('\n'); }
                Directory.CreateDirectory(SpellsSettings.Dir);
                File.WriteAllText(OverrideFile, sb.ToString());
            }
            catch (Exception e) { HomeApi.Log("spells: cannot save the cells: " + e.Message); }
        }

        // after Parse: if a .pins file exists its cells replace the pin= flags
        bool ApplyOverrides()
        {
            try
            {
                if (!File.Exists(OverrideFile)) return false;
                Dictionary<string, Spell> pn = new Dictionary<string, Spell>();
                foreach (string raw in File.ReadAllLines(OverrideFile, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line[0] == '#') continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = line.Substring(0, eq).Trim().ToUpperInvariant();
                    Spell s = FindSpell(line.Substring(eq + 1).Trim());
                    if (s != null && Array.IndexOf(PinOrder, k) >= 0) pn[k] = s;
                }
                pins = pn;
                return true;
            }
            catch (Exception e) { HomeApi.Log("spells: cannot read the cells file: " + e.Message); return false; }
        }

        public string ResetPins()
        {
            try { if (File.Exists(OverrideFile)) File.Delete(OverrideFile); }
            catch (Exception e) { return "ERROR: " + e.Message; }
            return "placed cells forgotten. " + Load(true);
        }

        // ---- spell files ------------------------------------------------------------------------------------------

        public string ResetFile()
        {
            try
            {
                Directory.CreateDirectory(SpellsSettings.Dir);
                if (File.Exists(SpellsSettings.ActiveFile)) File.Copy(SpellsSettings.ActiveFile, SpellsSettings.ActiveFile + ".bak", true);
                File.WriteAllText(SpellsSettings.ActiveFile, ActiveDefault);
            }
            catch (Exception e) { return "ERROR: " + e.Message; }
            return Path.GetFileName(SpellsSettings.ActiveFile) + " rewritten with the defaults (old one: .bak). " + Load(true);
        }

        public string Load(bool report)
        {
            try
            {
                if (!File.Exists(SpellsSettings.ActiveFile)) { Directory.CreateDirectory(SpellsSettings.Dir); File.WriteAllText(SpellsSettings.ActiveFile, ActiveDefault); }
                fileStamp = File.GetLastWriteTimeUtc(SpellsSettings.ActiveFile);
                string msg = Parse(File.ReadAllText(SpellsSettings.ActiveFile, Encoding.UTF8));
                if (ApplyOverrides()) msg += "; cells from " + Path.GetFileName(OverrideFile) + " (" + pins.Count + " placed)";
                return msg;
            }
            catch (Exception e)
            {
                HomeApi.Log("spells: cannot read the spell file: " + e.Message);
                return "ERROR: " + e.Message;
            }
        }

        public void CheckFile()
        {
            try { if (File.Exists(SpellsSettings.ActiveFile) && File.GetLastWriteTimeUtc(SpellsSettings.ActiveFile) != fileStamp) Load(false); }
            catch (Exception) { }
        }

        string Parse(string text)
        {
            SpellGroup[] g = new SpellGroup[9];
            Dictionary<string, Spell> pn = new Dictionary<string, Spell>();
            int spells = 0; StringBuilder bad = new StringBuilder();
            int lineNo = 0;
            foreach (string raw in text.Split('\n'))
            {
                lineNo++;
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                string[] f = line.Split('|');
                for (int i = 0; i < f.Length; i++) f[i] = f[i].Trim();
                int slot, entry = 0;
                string key = f[0];
                int dot = key.IndexOf('.');
                bool ok;
                if (dot > 0) ok = int.TryParse(key.Substring(0, dot), out slot) && int.TryParse(key.Substring(dot + 1), out entry);
                else ok = int.TryParse(key, out slot);
                if (!ok || slot < 1 || slot > 8 || (dot > 0 && (entry < 1 || entry > 8))) { bad.Append(" line ").Append(lineNo); continue; }
                if (g[slot] == null) g[slot] = new SpellGroup();
                if (dot < 0 && f.Length == 2) { g[slot].Title = f[1]; continue; }
                if (f.Length < 3) { bad.Append(" line ").Append(lineNo); continue; }
                Spell s = new Spell();
                s.Slot = slot; s.Entry = entry; s.Label = f[1]; s.Cmd = f[2];
                if (f.Length > 3)
                    foreach (string tok in f[3].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (tok == "sticky") s.Sticky = true;
                        else if (tok.StartsWith("pin=")) s.Pin = tok.Substring(4).ToUpperInvariant();
                        else if (tok.StartsWith("shift=")) s.Shift = tok.Substring(6);
                    }
                if (s.Pin.Length > 0 && Array.IndexOf(PinOrder, s.Pin) < 0) { bad.Append(" line ").Append(lineNo).Append(" (pin ").Append(s.Pin).Append(")"); s.Pin = ""; }
                if (dot < 0) { g[slot].Direct = s; if (g[slot].Title.Length == 0) g[slot].Title = s.Label; }
                else g[slot].Entries[entry] = s;
                if (s.Pin.Length > 0) pn[s.Pin] = s;
                spells++;
            }
            groups = g; pins = pn;
            string msg = Path.GetFileName(SpellsSettings.ActiveFile) + ": " + spells + " spells, " + pn.Count + " pins" + (bad.Length > 0 ? "; skipped bad" + bad : "") + " (slot 1 is always the game Hotbar group)";
            if (bad.Length > 0) HomeApi.Log("spells: " + msg);
            return msg;
        }

        public string List()
        {
            StringBuilder sb = new StringBuilder("1: (the game's own Hotbar group)\n");
            for (int s = 2; s <= 8; s++)
            {
                SpellGroup g = groups[s];
                if (g == null) { sb.Append(s).Append(": (empty)\n"); continue; }
                sb.Append(s).Append(": ").Append(g.Title);
                if (g.Direct != null) sb.Append("  -> ").Append(g.Direct.Cmd);
                sb.Append('\n');
                for (int e = 1; e <= 8; e++)
                {
                    Spell sp = g.Entries[e];
                    if (sp == null) continue;
                    string pk = PinOf(sp);
                    sb.Append("   ").Append(s).Append('.').Append(e).Append(' ').Append(sp.Label).Append("  -> ").Append(sp.Cmd).Append(pk.Length > 0 ? "  " + PinLabel(pk) : "").Append('\n');
                }
            }
            return sb.ToString();
        }

        public string SetPin(string key, string spellId)
        {
            if (Array.IndexOf(PinOrder, key) < 0) return "ERROR: pin must be one of " + string.Join(" ", PinOrder);
            Spell s = FindSpell(spellId);
            if (s == null) return "ERROR: no spell '" + spellId + "' (spells list=1)";
            pins[key] = s;
            return "pin " + key + " = " + s.Label + " (this session; edit the spell file to keep it)";
        }

        public Spell FindSpell(string id)
        {
            int slot, entry = 0;
            int dot = id.IndexOf('.');
            bool ok = dot > 0 ? int.TryParse(id.Substring(0, dot), out slot) && int.TryParse(id.Substring(dot + 1), out entry) : int.TryParse(id, out slot);
            if (!ok || slot < 1 || slot > 8 || groups[slot] == null) return null;
            return dot > 0 && entry >= 1 && entry <= 8 ? groups[slot].Entries[entry] : groups[slot].Direct;
        }

        public int CountSlots() { int n = 0; for (int i = 2; i <= 8; i++) if (groups[i] != null) n++; return n; }
    }
}
