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
"#     shift=<form added when Shift is held while casting> or shift=@<id> (Shift casts that spell)  shiftlabel=<name of the Shift version, \"_\" = space>\n" +
"#     (Shift+F on an F cell casts the Shift version; the bar shows its name under the cell; Shift+1..= cells have none)\n" +
"#     ask=<key>:<Label=value,...> one radial level per ask flag, in order; the answer replaces {key} in the command; \"_\" in a\n" +
"#     label shows as a space; dynamic answers: @players @crops @trees @prefabs (more than 40 answers are split A-Z into sub-levels).\n" +
"#     Back (right click) goes to the previous question, closing the radial cancels.\n" +
"# F5 is also the game console key: a pin on F5 opens the console as well.\n" +
"# \"(soon)\" lines: the command arrives later; the line above each one is its real form, swapped in by the session that builds it.\n" +
"2 | Attack\n" +
"2.1 | Arrows\n" +
"2.1.1 | Arrow | op=spellshot&type=arrow&pvp=1 | pin=F1 shift=@2.1.2\n" +
"2.1.2 | Arrow 3x3 | op=spellshot&type=arrow&perrow=3&rows=3&pvp=1 | shift=spread=10 shiftlabel=Arrow_3x3_fan\n" +
"2.2 | Balista\n" +
"2.2.1 | Balista | op=spellshot&type=balistashot&pvp=1 | shift=@2.2.2\n" +
"2.2.2 | Balista 3x3 | op=spellshot&type=balistashot&perrow=3&rows=3&pvp=1 | shift=spread=10 shiftlabel=Balista_3x3_fan\n" +
"2.3 | Thunderbolt\n" +
"2.3.1 | Thunderbolt | op=spellshot&type=thunderbolt&pvp=1 | shift=@2.3.2\n" +
"2.3.2 | Thunderbolt wall 5x2 | op=spellshot&type=thunderbolt&perrow=5&rows=2&pvp=1 | shift=perrow=7&rows=3 shiftlabel=Thunderbolt_wall_7x3\n" +
"2.4 | Slime\n" +
"2.4.1 | Slime ring | op=slime&pvp=1 | shift=rings=3 shiftlabel=Slime_ring_x3\n" +
"2.4.2 | Slime moat | op=slime&gap=3&cloudr=3.5&pvp=1 | shift=rings=3 shiftlabel=Slime_moat_x3\n" +
"2.5 | Stone circle\n" +
"2.5.1 | Stone circle | op=earthquake&damage=1&pvp=1 | shift=@2.5.2\n" +
"2.5.2 | Stone circle (trolls) | op=earthquake&damage=1&mode=strong&pvp=1\n" +
"2.6 | Harpoon\n" +
"2.6.1 | Harpoon | op=spellshot&type=harpoon&pvp=1 | shift=@2.6.2\n" +
"2.6.2 | Harpoon 3x3 fan | op=spellshot&type=harpoon&perrow=3&rows=3&spread=15&pvp=1\n" +
"2.7 | Mines\n" +
"2.7.1 | Place mine | op=trap&type=random&pvp=1 | shift=type=tar shiftlabel=Tar_mine\n" +
"2.7.2 | Remove mine | op=trap&remove=1 | shift=radius=50 shiftlabel=Remove_mines_r50\n" +
"2.7.3 | Place mine field | op=trap&type={kind}&field={field}&safe={safe}&radius={radius}&rows={rows}&perrow={perrow}&scatter=1&at=front&fxboom={fx}&prefab={look}&pvp=1 | button=Set_mines ask=field/Layout:*Radius=1,Rows=0 ask=kind/Mine_type:Random=random,Explosive=mine,Tar=tar,Fence_(soon)=@off ask=safe/Safe_radius?field=1:5,10,30,60 ask=radius/Minefield_radius>safe?field=1:10,20,30,40,50,60,70,80,90,100 ask=rows/Rows?field=0:1,2,*3,5,9 ask=perrow/Items_in_row?field=0:1,3,*5,7,9 ask=fx/Mine_effect:Random=random,Dynamite=dynamite,Grenade=grenade,Lava=lava,Siege=siege,Fireball=fireball,Glacier=glacier,Wrath=wrath,Blue=blue,Shock=shock,Gas=gas ask=look/Mine_texture:Floor_tile=wood_floor_1x1,Sapling=Beech_Sapling,Trap=piece_trap_troll\n" +
"2.7.4 | Detonate mines | op=trap&detonate=1&radius=100\n" +
"2.8 | Bow charge | op=bowcharge&on=1&pvp=1 | shift=on=0 shiftlabel=Bow_charge_off\n" +
"4 | Fun\n" +
"4.1 | Transform\n" +
"4.1.1 | Raven | op=transform&type=raven&toggle=1 | pin=S4\n" +
"4.1.2 | Serpent | op=transform&type=serpent&toggle=1\n" +
"4.1.3 | Fish | op=transform&type=fish&toggle=1\n" +
"4.1.4 | Human | op=transform&type=human | pin=S5\n" +
"4.2 | Trees\n" +
"4.2.1 | Tree normal size | op=scale&aim=1&factor=1\n" +
"4.2.2 | Tree x3 | op=scale&aim=1&factor=3 | shift=@4.2.3\n" +
"4.2.3 | Tree x5 | op=scale&aim=1&factor=5 | shift=@4.2.4\n" +
"4.2.4 | Tree x10 | op=scale&aim=1&factor=10 | shift=factor=20 shiftlabel=Tree_x20\n" +
"4.3 | Ship\n" +
"4.3.1 | ShipSpeed (x3) | op=ship&speed=3 | shift=@4.3.2\n" +
"4.3.2 | ShipSpeed+ (x5) | op=ship&speed=5 | shift=@4.3.3\n" +
"4.3.3 | ShipSpeed++ (x10) | op=ship&speed=10\n" +
"4.3.4 | Ship normal | op=ship&speed=1\n" +
"4.4 | Reset sky | op=visualeffect&name=reset\n" +
"5 | Heal, defence, bless\n" +
"5.1 | Heal | op=heal | pin=F3\n" +
"5.2 | Cleanse | op=cleanse | shift=@5.4\n" +
"5.3 | Blessings\n" +
"5.3.1 | Bonemass | op=bless&name=bonemass | pin=S3 shift=duration=900 shiftlabel=Bonemass_15_min\n" +
"5.3.2 | Eikthyr | op=bless&name=eikthyr | shift=duration=900 shiftlabel=Eikthyr_15_min\n" +
"5.3.3 | The Elder | op=bless&name=elder | shift=duration=900 shiftlabel=The_Elder_15_min\n" +
"5.3.4 | Moder | op=bless&name=moder | shift=duration=900 shiftlabel=Moder_15_min\n" +
"5.3.5 | Yagluth | op=bless&name=yagluth | shift=duration=900 shiftlabel=Yagluth_15_min\n" +
"5.3.6 | The Queen | op=bless&name=queen | shift=duration=900 shiftlabel=The_Queen_15_min\n" +
"5.3.7 | Fader | op=bless&name=fader | shift=duration=900 shiftlabel=Fader_15_min\n" +
"5.4 | Remove all effects | op=cleanse&all=1\n" +
"6 | Build\n" +
"6.1 | Flatten... | op=groundlevel&area={area}&delta={level}&radius={radius}&avoid=1 | ask=area:*From_me=self,Start-end=coords ask=level/Flatten_level:-10,-8,-5,-3,-1,*0,1,3,5,8,10 ask=radius/Radius:2,*4,6,8,12\n" +
"6.2 | Coordinates\n" +
"6.2.1 | Start point | op=coord&set=start\n" +
"6.2.2 | End point | op=coord&set=end\n" +
"6.2.3 | Show points | op=coord&show=1\n" +
"6.2.4 | Clear points | op=coord&clear=1\n" +
"6.3 | Paths\n" +
"6.3.1 | Dirt path start-end | op=path&type=dirt&from=coords&avoid=1 | shift=@6.3.2\n" +
"6.3.2 | Stone path start-end | op=path&type=stone&from=coords&avoid=1\n" +
"6.5 | Repair and refill | | sticky\n" +
"6.5.1 | Repair | op=repair&aim=0&radius=50\n" +
"6.5.2 | Refill | op=refill&aim=0&radius=50\n" +
"6.6 | Ward | op=dome&step=menu&cost=nearby\n" +
"6.7 | Construct\n" +
"6.7.1 | Construction... | op=construct&panel=1&cost=nearby\n" +
"6.7.2 | Construction status | op=construct&step=status\n" +
"6.8 | Despawn | op=despawn&others=1&tamed=1&boss=1&inloc=1 | shift=undo=1 shiftlabel=Undo_despawn\n" +
"7 | Field\n" +
"7.1 | Cultivate... | op=cultivate&area={area}&rows={rows}&perrow={perrow} | ask=area:*From_me=self,Start-end=coords ask=rows:1,*3,5,7,9 ask=perrow/Items_in_row:1,3,*5,7,9\n" +
"7.2 | Plant food... | op=field&area={area}&prefab={crop}&rows={rows}&perrow={perrow}&cost=nearby | ask=area:Start-end=coords,This_patch=patch,From_me=aim ask=crop:@crops ask=rows:1,3,5,7,9 ask=perrow:1,3,5,7,9\n" +
"7.3 | Harvest patch | op=harvest&collect=1\n" +
"7.4 | Plant trees... | op=plant&area={area}&prefab={tree}&scale={size}&avoid=1&rows={rows}&perrow={perrow}&cost=nearby | ask=area:Start-end=coords,From_me=self ask=tree:@trees ask=size:1,3,5,10,20 ask=rows:1,3,5,7,9 ask=perrow:1,3,5,7,9\n" +
"\n" +
"8 | Spawn... | op=spawn&prefab={prefab}&quality=max&where={where}&count={count}&yaw={yaw}&pitch={pitch}&player={player}&cost=nearby | ask=prefab:@prefabs ask=where:Crosshair=crosshair,Inventory=inventory ask=count:1,5,10,50 ask=yaw:0,90,180,270 ask=pitch:0,90 ask=player:@players\n" +
"9 | Teleport\n" +
"9.1 | Last death | op=teleport&to=death\n" +
"9.2 | Map point | op=teleport&to=map\n" +
"9.3 | Blink | op=blink&pvp=1 | pin=F6\n" +
"9.4 | To player... | op=teleport&to=player&player={player} | ask=player:@players\n" +
"9.5 | Last chance | op=lastchance&on=1 | shift=on=0 shiftlabel=Last_chance_off\n" +
"10 | Chat commands\n" +
"10.1 | Intro | op=chat&msg=intro\n";

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
