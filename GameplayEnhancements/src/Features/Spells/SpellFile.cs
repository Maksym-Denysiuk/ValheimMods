using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HomeCheat.Features.Spells
{
    // The active spell file (spells.txt, or spells-ui-test.txt in placeholder mode) and the bar cells:
    // parsing (SpellParse, file format v4), the defaults written when a file is missing, the .pins file of placed cells, list and pin answers.
    internal sealed class SpellFile
    {
        SpellParse parsed = SpellParse.Run("");
        Dictionary<string, Spell> pins = new Dictionary<string, Spell>();
        DateTime fileStamp;

        public Dictionary<string, Spell> Pins { get { return pins; } }
        public SpellNode Root { get { return parsed.Root; } }
        public SpellNode NodeAt(string id) { return parsed.Find(id); }

        // the menu tree of the work plan (backlog-plan.md 2.2); spells.txt in the game folder is the source, this is the copy written when it is missing
        const string DefaultText =
"# HomeCheat spell bar, spell file v4. One line per entry:  id | label | command | flags\n" +
"#   id: dotted numbers of any depth (2, 2.1, 6.1.3). 1 is the Hotbar (the 10 bar cells), not configurable. A level holds any number\n" +
"#     of entries: past the game's per-layer limit the radial continues as a spiral (like the emote wheel).\n" +
"#   title (a group or a sub-section): \"id | label\", or \"id | label | | sticky\" (sticky on a title = every spell below it is sticky)\n" +
"#   command: the form of an inbox file (op=...&k=v); chain commands with ;;   @dummy only shows a toast (a spell not built yet)\n" +
"#   flags (space separated): sticky (the radial stays on its level after the cast)  pin=F1..F10|S1..S10 (S = Shift+key: S1..S9 = Shift+1..9, S10 = Shift+0)\n" +
"#     shift=<form added when Shift is held while casting>\n" +
"#     ask=<key>:<Label=value,...> one radial level per ask flag, in order; the answer replaces {key} in the command; \"_\" in a\n" +
"#     label shows as a space; dynamic answers: @players @crops @trees @prefabs (more than 40 answers are split A-Z into sub-levels).\n" +
"#     Back (right click) goes to the previous question, closing the radial cancels.\n" +
"# F5 is also the game console key: a pin on F5 opens the console as well.\n" +
"# \"(soon)\" lines: the command arrives later; the line above each one is its real form, swapped in by the session that builds it.\n" +
"2 | Attack\n" +
"2.1 | Arrow | op=spellshot&type=arrow | pin=F1 shift=perrow=3&rows=3\n" +
"2.2 | Arrow 3x3 | op=spellshot&type=arrow&perrow=3&rows=3\n" +
"2.3 | Balista | op=spellshot&type=balistashot\n" +
"2.4 | Thunderbolt | op=spellshot&type=thunderbolt\n" +
"2.5 | Quake (damage) | op=earthquake&damage=1&noStones=1&players=0\n" +
"2.6 | Slime ring | op=slime&hurtself=0\n" +
"2.7 | Stone circle | op=earthquake&damage=1&players=0\n" +
"2.8 | Stone circle (trolls) | op=earthquake&damage=1&mode=strong&players=0\n" +
"2.9 | Thunderbolt wall 5x2 | op=spellshot&type=thunderbolt&perrow=5&rows=2\n" +
"# real form (W1-harpoon): 2.10 | Harpoon | op=spellshot&type=harpoon\n" +
"2.10 | Harpoon (soon) | @dummy\n" +
"# real form (W3-mines): 2.11 | Place mine | op=mine\n" +
"2.11 | Place mine (soon) | @dummy\n" +
"# real form (W3-mines): 2.12 | Remove mines | op=mine&remove=1&radius=10\n" +
"2.12 | Remove mines (soon) | @dummy\n" +
"3 | Protect\n" +
"3.1 | Push away | op=earthquake&noStones=1&players=0 | pin=F2 shift=mode=strong\n" +
"3.2 | Stone circle | op=earthquake&damage=1&players=0 | pin=S2 shift=mode=strong\n" +
"3.3 | Strong push | op=earthquake&mode=strong&noStones=1&players=0\n" +
"3.4 | Slime moat | op=slime&gap=3&hurtself=0\n" +
"3.5 | Shield\n" +
"# real form (W1-world-build): 3.5.1 | Start point | op=coord&set=start\n" +
"3.5.1 | Start point (soon) | @dummy\n" +
"# real form (W1-world-build): 3.5.2 | End point | op=coord&set=end\n" +
"3.5.2 | End point (soon) | @dummy\n" +
"# real form (W2-dome): 3.5.3 | Place shield | op=dome&from=coords\n" +
"3.5.3 | Place shield (soon) | @dummy\n" +
"# real form (W2-dome): 3.5.4 | List shields | op=dome&step=list\n" +
"3.5.4 | List shields (soon) | @dummy\n" +
"# real form (W2-dome): 3.5.5 | Remove shield | op=dome&step=remove\n" +
"3.5.5 | Remove shield (soon) | @dummy\n" +
"4 | Fun\n" +
"4.1 | Raven 60 s | op=transform&type=raven&duration=60 | pin=S4\n" +
"4.2 | Serpent 60 s | op=transform&type=serpent&duration=60\n" +
"4.3 | Fish 60 s | op=transform&type=fish&duration=60\n" +
"4.4 | Human | op=transform&type=human | pin=S5\n" +
"4.5 | Oaks x2 | op=scale&filter=Oak&factor=2&radius=30\n" +
"4.6 | Reset sky | op=visualeffect&name=reset\n" +
"# real form (W3-world-plant): 4.7 | Tree x3 (only you see) | op=scale&aim=1&factor=3\n" +
"4.7 | Tree x3 (soon) | @dummy\n" +
"# real form (W3-world-plant): 4.8 | Tree x5 (only you see) | op=scale&aim=1&factor=5\n" +
"4.8 | Tree x5 (soon) | @dummy\n" +
"# real form (W3-world-plant): 4.9 | Tree x10 (only you see) | op=scale&aim=1&factor=10\n" +
"4.9 | Tree x10 (soon) | @dummy\n" +
"# real form (W1-ship): 4.10 | Ship x5 | op=ship&speed=5\n" +
"4.10 | Ship x5 (soon) | @dummy\n" +
"# real form (W1-ship): 4.11 | Ship x10 | op=ship&speed=10\n" +
"4.11 | Ship x10 (soon) | @dummy\n" +
"# real form (W1-ship): 4.12 | Ship normal | op=ship&speed=1\n" +
"4.12 | Ship normal (soon) | @dummy\n" +
"5 | Heal, defence, bless\n" +
"5.1 | Heal | op=heal | pin=F3\n" +
"5.2 | Cleanse | op=cleanse\n" +
"5.3 | Bonemass | op=bless&name=bonemass | pin=S3\n" +
"5.4 | Eikthyr | op=bless&name=eikthyr\n" +
"5.5 | The Elder | op=bless&name=elder\n" +
"5.6 | Moder | op=bless&name=moder\n" +
"5.7 | Yagluth | op=bless&name=yagluth\n" +
"5.8 | The Queen | op=bless&name=queen\n" +
"5.9 | Fader | op=bless&name=fader\n" +
"5.10 | Remove all effects | op=cleanse&all=1\n" +
"6 | Build\n" +
"6.1 | Flatten | | sticky\n" +
"# real form (W1-world-build): 6.1.1 | Flatten here | op=groundlevel&delta=0&avoid=1\n" +
"6.1.1 | Flatten here (soon) | @dummy\n" +
"# real form (W1-world-build): 6.1.2 | Flatten +5 | op=groundlevel&delta=5&avoid=1\n" +
"6.1.2 | Flatten +5 (soon) | @dummy\n" +
"# real form (W1-world-build): 6.1.3 | Flatten +8 (max) | op=groundlevel&delta=8&avoid=1\n" +
"6.1.3 | Flatten +8 (soon) | @dummy\n" +
"# real form (W1-world-build): 6.1.4 | Flatten -3 | op=groundlevel&delta=-3&avoid=1\n" +
"6.1.4 | Flatten -3 (soon) | @dummy\n" +
"# real form (W1-world-build): 6.1.5 | Flatten -5 | op=groundlevel&delta=-5&avoid=1\n" +
"6.1.5 | Flatten -5 (soon) | @dummy\n" +
"6.2 | Coordinates\n" +
"# real form (W1-world-build): 6.2.1 | Start point | op=coord&set=start\n" +
"6.2.1 | Start point (soon) | @dummy\n" +
"# real form (W1-world-build): 6.2.2 | End point | op=coord&set=end\n" +
"6.2.2 | End point (soon) | @dummy\n" +
"# real form (W1-world-build): 6.2.3 | Show points | op=coord&show=1\n" +
"6.2.3 | Show points (soon) | @dummy\n" +
"# real form (W1-world-build): 6.2.4 | Clear points | op=coord&clear=1\n" +
"6.2.4 | Clear points (soon) | @dummy\n" +
"6.3 | Paths\n" +
"# real form (W1-world-build): 6.3.1 | Dirt path start-end | op=path&type=dirt&from=coords&avoid=1\n" +
"6.3.1 | Dirt path start-end (soon) | @dummy\n" +
"# real form (W1-world-build): 6.3.2 | Stone path start-end | op=path&type=stone&from=coords&avoid=1\n" +
"6.3.2 | Stone path start-end (soon) | @dummy\n" +
"6.3.3 | Dirt path 5 m | op=path&type=dirt&length=5\n" +
"6.3.4 | Stone path 5 m | op=path&type=stone&length=5\n" +
"6.4 | Spawn\n" +
"# real form (W1-world-build): 6.4.1 | Spawn... | op=spawn&prefab={prefab}&count={count}&yaw={yaw}&pitch={pitch}&player={player} | ask=prefab:@prefabs ask=count:1,5,10,50 ask=yaw:0,90,180,270 ask=pitch:0,90 ask=player:@players\n" +
"6.4.1 | Spawn... (soon) | @dummy | ask=prefab:@prefabs ask=count:1,5,10,50 ask=yaw:0,90,180,270 ask=pitch:0,90 ask=player:@players\n" +
"6.5 | Repair and refill | | sticky\n" +
"6.5.1 | Repair (aim) | op=repair\n" +
"6.5.2 | Refill (aim) | op=refill\n" +
"6.5.3 | Repair r10 | op=repair&aim=0&radius=10 | pin=F4\n" +
"6.5.4 | Refill r10 | op=refill&aim=0&radius=10\n" +
"6.5.5 | Repair r50 | op=repair&aim=0&radius=50\n" +
"6.5.6 | Refill r50 | op=refill&aim=0&radius=50\n" +
"6.5.7 | Repair+refill r10 | op=repair&aim=0&radius=10 ;; op=refill&aim=0&radius=10\n" +
"6.6 | Shield\n" +
"# real form (W1-world-build): 6.6.1 | Start point | op=coord&set=start\n" +
"6.6.1 | Start point (soon) | @dummy\n" +
"# real form (W1-world-build): 6.6.2 | End point | op=coord&set=end\n" +
"6.6.2 | End point (soon) | @dummy\n" +
"# real form (W2-dome): 6.6.3 | Place shield | op=dome&from=coords\n" +
"6.6.3 | Place shield (soon) | @dummy\n" +
"# real form (W2-dome): 6.6.4 | List shields | op=dome&step=list\n" +
"6.6.4 | List shields (soon) | @dummy\n" +
"# real form (W2-dome): 6.6.5 | Remove shield | op=dome&step=remove\n" +
"6.6.5 | Remove shield (soon) | @dummy\n" +
"7 | Field\n" +
"7.1 | Cultivate 3x5 | op=cultivate&rows=3&perrow=5 | pin=S1\n" +
"7.2 | Carrots 3x5 | op=field&prefab=Pickable_Carrot&rows=3&perrow=5\n" +
"7.3 | Oak grove | op=plant&prefab=Oak_Sapling&rows=3&perrow=5&scale=3\n" +
"7.4 | Place wall | op=place&prefab=woodwall&perrow=3\n" +
"7.5 | Place campfire | op=place&prefab=fire_pit\n" +
"# real form (W2-world-field): 7.6 | Cultivate start-end | op=cultivate&area=coords\n" +
"7.6 | Cultivate start-end (soon) | @dummy\n" +
"# real form (W2-world-field): 7.7 | Plant food... | op=field&area={area}&prefab={crop} | ask=area:Start-end=coords,This_patch=patch ask=crop:@crops\n" +
"7.7 | Plant food... (soon) | @dummy | ask=area:Start-end=coords,This_patch=patch ask=crop:@crops\n" +
"# real form (W2-world-field): 7.8 | Harvest patch | op=harvest\n" +
"7.8 | Harvest patch (soon) | @dummy\n" +
"# real form (W3-world-plant): 7.9 | Plant trees... | op=plant&area={area}&prefab={tree}&scale={size}&avoid=1 | ask=area:Start-end=coords,Crosshair=aim ask=tree:@trees ask=size:1,3,5,10,20\n" +
"7.9 | Plant trees... (soon) | @dummy | ask=area:Start-end=coords,Crosshair=aim ask=tree:@trees ask=size:1,3,5,10,20\n" +
"8 | Construct\n";

        // spells-ui-test.txt: the same tree, every command @dummy. In placeholder mode a cast never runs any command anyway.
        const string TestText =
"# HomeCheat spell bar UI test, spell file v4. Placeholder spells only: a cast shows \"placeholder: <label> (<id>)\" and logs it, nothing is executed.\n" +
"# The same tree as spells.txt with every command @dummy; answer chains (ask=) run their questions, the toast shows the answers.\n" +
"# Format as in spells.txt:  id | label | command | flags   (titles: id | label  or  id | label | | sticky;  flags: sticky  pin=  shift=  ask=<key>:<answers>|@source)\n" +
"2 | Attack\n" +
"2.1 | Arrow | @dummy | pin=F1 shift=perrow=3&rows=3\n" +
"2.2 | Arrow 3x3 | @dummy\n" +
"2.3 | Balista | @dummy\n" +
"2.4 | Thunderbolt | @dummy\n" +
"2.5 | Quake (damage) | @dummy\n" +
"2.6 | Slime ring | @dummy\n" +
"2.7 | Stone circle | @dummy\n" +
"2.8 | Stone circle (trolls) | @dummy\n" +
"2.9 | Thunderbolt wall 5x2 | @dummy\n" +
"2.10 | Harpoon | @dummy\n" +
"2.11 | Place mine | @dummy\n" +
"2.12 | Remove mines | @dummy\n" +
"3 | Protect\n" +
"3.1 | Push away | @dummy | pin=F2 shift=mode=strong\n" +
"3.2 | Stone circle | @dummy | pin=S2 shift=mode=strong\n" +
"3.3 | Strong push | @dummy\n" +
"3.4 | Slime moat | @dummy\n" +
"3.5 | Shield\n" +
"3.5.1 | Start point | @dummy\n" +
"3.5.2 | End point | @dummy\n" +
"3.5.3 | Place shield | @dummy\n" +
"3.5.4 | List shields | @dummy\n" +
"3.5.5 | Remove shield | @dummy\n" +
"4 | Fun\n" +
"4.1 | Raven 60 s | @dummy | pin=S4\n" +
"4.2 | Serpent 60 s | @dummy\n" +
"4.3 | Fish 60 s | @dummy\n" +
"4.4 | Human | @dummy | pin=S5\n" +
"4.5 | Oaks x2 | @dummy\n" +
"4.6 | Reset sky | @dummy\n" +
"4.7 | Tree x3 | @dummy\n" +
"4.8 | Tree x5 | @dummy\n" +
"4.9 | Tree x10 | @dummy\n" +
"4.10 | Ship x5 | @dummy\n" +
"4.11 | Ship x10 | @dummy\n" +
"4.12 | Ship normal | @dummy\n" +
"5 | Heal, defence, bless\n" +
"5.1 | Heal | @dummy | pin=F3\n" +
"5.2 | Cleanse | @dummy\n" +
"5.3 | Bonemass | @dummy | pin=S3\n" +
"5.4 | Eikthyr | @dummy\n" +
"5.5 | The Elder | @dummy\n" +
"5.6 | Moder | @dummy\n" +
"5.7 | Yagluth | @dummy\n" +
"5.8 | The Queen | @dummy\n" +
"5.9 | Fader | @dummy\n" +
"5.10 | Remove all effects | @dummy\n" +
"6 | Build\n" +
"6.1 | Flatten | | sticky\n" +
"6.1.1 | Flatten here | @dummy\n" +
"6.1.2 | Flatten +5 | @dummy\n" +
"6.1.3 | Flatten +8 | @dummy\n" +
"6.1.4 | Flatten -3 | @dummy\n" +
"6.1.5 | Flatten -5 | @dummy\n" +
"6.2 | Coordinates\n" +
"6.2.1 | Start point | @dummy\n" +
"6.2.2 | End point | @dummy\n" +
"6.2.3 | Show points | @dummy\n" +
"6.2.4 | Clear points | @dummy\n" +
"6.3 | Paths\n" +
"6.3.1 | Dirt path start-end | @dummy\n" +
"6.3.2 | Stone path start-end | @dummy\n" +
"6.3.3 | Dirt path 5 m | @dummy\n" +
"6.3.4 | Stone path 5 m | @dummy\n" +
"6.4 | Spawn\n" +
"6.4.1 | Spawn... | @dummy | ask=prefab:@prefabs ask=count:1,5,10,50 ask=yaw:0,90,180,270 ask=pitch:0,90 ask=player:@players\n" +
"6.5 | Repair and refill | | sticky\n" +
"6.5.1 | Repair (aim) | @dummy\n" +
"6.5.2 | Refill (aim) | @dummy\n" +
"6.5.3 | Repair r10 | @dummy | pin=F4\n" +
"6.5.4 | Refill r10 | @dummy\n" +
"6.5.5 | Repair r50 | @dummy\n" +
"6.5.6 | Refill r50 | @dummy\n" +
"6.5.7 | Repair+refill r10 | @dummy\n" +
"6.6 | Shield\n" +
"6.6.1 | Start point | @dummy\n" +
"6.6.2 | End point | @dummy\n" +
"6.6.3 | Place shield | @dummy\n" +
"6.6.4 | List shields | @dummy\n" +
"6.6.5 | Remove shield | @dummy\n" +
"7 | Field\n" +
"7.1 | Cultivate 3x5 | @dummy | pin=S1\n" +
"7.2 | Carrots 3x5 | @dummy\n" +
"7.3 | Oak grove | @dummy\n" +
"7.4 | Place wall | @dummy\n" +
"7.5 | Place campfire | @dummy\n" +
"7.6 | Cultivate start-end | @dummy\n" +
"7.7 | Plant food... | @dummy | ask=area:Start-end=coords,This_patch=patch ask=crop:@crops\n" +
"7.8 | Harvest patch | @dummy\n" +
"7.9 | Plant trees... | @dummy | ask=area:Start-end=coords,Crosshair=aim ask=tree:@trees ask=size:1,3,5,10,20\n" +
"8 | Construct\n";

        public static string ActiveDefault { get { return SpellsSettings.IsPlaceholder ? TestText : DefaultText; } }

        internal static string[] PinOrder { get { return SpellParse.PinOrder; } }

        public static string PinLabel(string pin)
        {
            return "[" + SpellParse.KeyName(pin) + "]";
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

        public static string IdOf(Spell s) { return s.Id; }

        string OverrideFile { get { return SpellsSettings.ActiveFile + ".pins"; } }

        // the cells you placed: one line per cell, "S1=2.1" or "F3=6.5.3"; once the file exists it replaces the pin= flags of the spell file
        public void SaveOverrides()
        {
            try
            {
                StringBuilder sb = new StringBuilder("# written by the spell bar when you place a spell: cell=id (S1..S10 = Shift+1..9, Shift+0; F1..F10). Delete this file (or op=spells&pins=reset) to go back to the pin= flags of the spell file.\n");
                foreach (string k in PinOrder) { Spell s; if (pins.TryGetValue(k, out s)) sb.Append(k).Append('=').Append(IdOf(s)).Append('\n'); }
                Directory.CreateDirectory(SpellsSettings.Dir);
                File.WriteAllText(OverrideFile, sb.ToString());
            }
            catch (Exception e) { HomeApi.Log("spells: cannot save the cells: " + e.Message); }
        }

        // after Parse: if a .pins file exists its cells replace the pin= flags; returns the number of cells whose spell id is gone (-1 = no file)
        int ApplyOverrides()
        {
            try
            {
                if (!File.Exists(OverrideFile)) return -1;
                Dictionary<string, Spell> pn = new Dictionary<string, Spell>();
                int lost = 0;
                foreach (string raw in File.ReadAllLines(OverrideFile, Encoding.UTF8))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line[0] == '#') continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = line.Substring(0, eq).Trim().ToUpperInvariant();
                    Spell s = FindSpell(line.Substring(eq + 1).Trim());
                    if (s != null && Array.IndexOf(PinOrder, k) >= 0) pn[k] = s; else lost++;
                }
                pins = pn;
                return lost;
            }
            catch (Exception e) { HomeApi.Log("spells: cannot read the cells file: " + e.Message); return -1; }
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
                int lost = ApplyOverrides();
                if (lost >= 0) msg += "; cells from " + Path.GetFileName(OverrideFile) + " (" + pins.Count + " placed" + (lost > 0 ? ", " + lost + " with an id no longer in the file" : "") + ")";
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
            SpellParse p = SpellParse.Run(text);
            parsed = p; pins = new Dictionary<string, Spell>(p.Pins);
            string msg = Path.GetFileName(SpellsSettings.ActiveFile) + ": " + p.Spells.Count + " spells, " + p.Titles + " titles, " + pins.Count + " pins"
                + (p.Bad.Count > 0 ? "; skipped " + p.Bad.Count + " bad: " + string.Join(", ", p.Bad.ToArray()) : "; 0 bad lines") + " (1 is always the Hotbar)";
            if (p.Bad.Count > 0) HomeApi.Log("spells: " + msg);
            return msg;
        }

        public string List()
        {
            StringBuilder sb = new StringBuilder("1: Hotbar (the 10 bar cells)\n");
            foreach (SpellNode n in parsed.Root.Children) ListNode(sb, n, 0);
            return sb.ToString();
        }

        void ListNode(StringBuilder sb, SpellNode n, int depth)
        {
            sb.Append(' ', depth * 3).Append(n.Id).Append(' ').Append(n.Title);
            if (n.Spell == null)
            {
                sb.Append(n.Sticky ? "  (sticky)" : "").Append(n.Children.Count == 0 ? "  (empty)" : "").Append('\n');
                foreach (SpellNode c in n.Children) ListNode(sb, c, depth + 1);
                return;
            }
            Spell s = n.Spell;
            string pk = PinOf(s);
            sb.Append("  -> ").Append(s.Cmd);
            foreach (SpellAsk a in s.Asks) sb.Append("  ask ").Append(a.Key).Append(':').Append(a.Source.Length > 0 ? a.Source : a.Options.Count + " answers");
            sb.Append(s.Sticky ? "  sticky" : "").Append(pk.Length > 0 ? "  " + PinLabel(pk) : "").Append(s.ShiftName.Length > 0 ? "  Shift+F: " + s.ShiftName : "").Append('\n');
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
            Spell s;
            return id != null && parsed.Spells.TryGetValue(id.Trim(), out s) ? s : null;
        }

        public int CountSections() { return parsed.Root.Children.Count; }
    }
}
