// spells: the HomeCheat spell system, view v3 (design: spells-design.md). Own game / own world only.
//   G                 opens the game's own radial menu, filled with our groups (slot 1 = the game's Hotbar group); Shift+G = vanilla radial, T = emotes
//   action bar        one always-visible row of 10 cells: 1-5 (fire on Shift+1..5) and F1-F5; each cell shows the spell icon with its name under it.
//                     Shift+click on a spell in the radial = place it: the radial closes, the cursor is freed, click a cell (right click empties it, Esc cancels).
//                     Placements are saved next to the spell file (<file>.pins); without that file the cells come from the pin=... flags of the spell file
// The radial is the game's own RadialBase; we only implement its extension point IRadialConfig and swap the config that RadialBase.Open
// receives for the main radial (Harmony prefix), so opening, animation, input blocking, back button, closing stay vanilla.
// Never touches Terminal / Console / god / ghost / fly flags. Calls the existing op=... commands in-process.
// Placeholder mode (default ON): spells-ui-test.txt, a cast only shows a toast. Starts DISABLED unless spells.state says enabled=1.
// C# 5 (csc 4.0). Build: bash build-cmd.sh spells -r:$M/UnityEngine.IMGUIModule.dll -r:$M/UnityEngine.TextRenderingModule.dll -r:$M/UnityEngine.UI.dll -r:$M/Unity.TextMeshPro.dll -r:$M/assembly_utils.dll
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Valheim.UI;

namespace HomeCheat
{
    // ---- one spell and its group --------------------------------------------------------------

    class Spell
    {
        public int Slot, Entry;
        public string Label = "", Cmd = "", Shift = "", Pin = "";
        public bool Sticky;
        public float Next;      // cooldown
    }

    class Group
    {
        public string Title = "";
        public Spell Direct;                         // slot is itself a spell
        public Spell[] Entries = new Spell[9];       // 1..8
        public bool HasEntries { get { for (int i = 1; i <= 8; i++) if (Entries[i] != null) return true; return false; } }
    }

    // ---- shared state, read by the patches ------------------------------------------------------

    static class SpellsState
    {
        public const string HarmonyId = "com.homeos.homecheat.spells";
        public const string ObjName = "HomeCheat_SpellBar";
        public static bool Enabled;
        public static int ConsumedFrame = -1;
        public static Harmony H;
        public static SpellBar Bar;

        public static string Dir { get { return Path.Combine(Paths.ConfigPath, "homecheat"); } }

        // placeholder mode (default ON): the bar reads spells-ui-test.txt and a cast only shows a toast, it never runs a command
        public static bool Placeholder = true;
        public static string RealFile { get { return Path.Combine(Dir, "spells.txt"); } }
        public static string TestFile { get { return Path.Combine(Dir, "spells-ui-test.txt"); } }
        public static string ActiveFile { get { return Placeholder ? TestFile : RealFile; } }
        public static string StateFile { get { return Path.Combine(Dir, "spells.state"); } }

        public static bool ShiftHeld()
        {
            return ZInput.GetKey(KeyCode.LeftShift, false) || ZInput.GetKey(KeyCode.RightShift, false);
        }

        // reads spells.state (enabled=, placeholder=); a missing file or line means enabled=0, placeholder=1
        public static bool LoadEnabled()
        {
            Enabled = false; Placeholder = true;
            try
            {
                if (File.Exists(StateFile))
                {
                    string t = File.ReadAllText(StateFile);
                    Enabled = t.Contains("enabled=1");
                    Placeholder = !t.Contains("placeholder=0");
                }
            }
            catch (Exception) { }
            return Enabled;
        }

        // writes both settings (callers set Enabled / Placeholder first)
        public static void SaveEnabled(bool on)
        {
            try { Directory.CreateDirectory(Dir); File.WriteAllText(StateFile, "enabled=" + (Enabled ? "1" : "0") + "\nplaceholder=" + (Placeholder ? "1" : "0") + "\n"); }
            catch (Exception e) { HomeApi.Log("spells: cannot save state: " + e.Message); }
        }
    }

    // ---- the two Harmony prefixes ---------------------------------------------------------------------

    public static class SpellPatches
    {
        // RadialBase.Open(config, backConfig): the vanilla main radial (ValheimRadialConfig) is replaced by ours. Everything else
        // (T = emote radial, hover menus, the Hotbar group inside ours, Back) opens exactly as before. Shift held = vanilla.
        public static bool OpenPrefix(ref IRadialConfig config)
        {
            try
            {
                if (!SpellsState.Enabled || SpellsState.Bar == null || config == null) return true;
                if (config.GetType().Name != "ValheimRadialConfig") return true;
                if (SpellsState.ShiftHeld()) return true;
                config = SpellsState.Bar.Root();
            }
            catch (Exception e) { HomeApi.Log("spells: OpenPrefix: " + e.Message); }
            return true;
        }

        // Player.UseHotbarItem(int): no weapon switch in the frame a Shift+digit pin was fired by the bar
        public static bool HotbarPrefix(int index)
        {
            if (!SpellsState.Enabled) return true;
            if (Time.frameCount == SpellsState.ConsumedFrame) return false;
            return true;
        }

        // a radial that still holds a config from a replaced build (op=reload while it was open): show the vanilla radial, change no settings
        public static void OpenVanilla(RadialBase radial)
        {
            HomeApi.Log("spells: stale radial config of a replaced build, vanilla radial opened");
            try { radial.Open(RadialData.SO.MainGroupConfig, null); } catch (Exception e) { HomeApi.Log("spells: vanilla fallback failed: " + e.Message); }
        }

        public static string Apply(bool on)
        {
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            if (SpellsState.H == null) SpellsState.H = new Harmony(SpellsState.HarmonyId);
            try { Harmony.UnpatchID(SpellsState.HarmonyId); } catch (Exception) { }
            if (!on) return "patches removed";
            MethodInfo m1 = null;
            foreach (MethodInfo m in typeof(RadialBase).GetMethods(all))
                if (m.Name == "Open" && m.GetParameters().Length == 2) { m1 = m; break; }
            MethodInfo m2 = typeof(Player).GetMethod("UseHotbarItem", all);
            if (m1 == null || m2 == null) return "ERROR: target methods not found (game version changed?)";
            SpellsState.H.Patch(m1, new HarmonyMethod(typeof(SpellPatches).GetMethod("OpenPrefix")), null);
            SpellsState.H.Patch(m2, new HarmonyMethod(typeof(SpellPatches).GetMethod("HotbarPrefix")), null);
            return "patches applied (RadialBase.Open, UseHotbarItem)";
        }
    }

    // ---- the radial configs: our subclasses of the game's extension point ------------------------------------

    // root level: 8 sections. Section 1 is our Hotbar (the 10 bar cells), 2-8 are our groups (or empty).
    class SpellRootConfig : IRadialConfig
    {
        readonly SpellBar bar;
        public SpellRootConfig(SpellBar b) { bar = b; }
        public string LocalizedName { get { return "Spells"; } }
        public Sprite Sprite { get { return SpellBar.IconFor("hotbar"); } }

        public void InitRadialConfig(RadialBase radial)
        {
            if (bar == null || SpellsState.Bar != bar) { SpellPatches.OpenVanilla(radial); return; }   // config of a build that was replaced by op=reload
            try { Build(radial); }
            catch (Exception e)
            {
                // never leave the player in a half-built radial: switch the spell system off and show the vanilla radial instead
                HomeApi.Log("spells: radial build failed, spells switched off: " + e);
                SpellsState.Enabled = false; SpellsState.SaveEnabled(false);
                try { radial.Open(RadialData.SO.MainGroupConfig, null); } catch (Exception e2) { HomeApi.Log("spells: vanilla fallback failed: " + e2.Message); }
            }
        }

        void Build(RadialBase radial)
        {
            bar.CheckFile();
            List<RadialMenuElement> els = new List<RadialMenuElement>();
            // 1: Hotbar = the 10 cells of the action bar (replaces the game's item hotbar group in this radial)
            GroupElement hb = UnityEngine.Object.Instantiate<GroupElement>(RadialData.SO.GroupElement);
            hb.Init(new SpellHotbarConfig(bar), this, radial);
            els.Add(hb);
            for (int n = 2; n <= 8; n++)
            {
                Group g = bar.GroupAt(n);
                if (g == null || (g.Direct == null && !g.HasEntries)) { els.Add(MakeEmpty()); continue; }
                if (g.Direct != null) { els.Add(bar.MakeLeaf(g.Direct, this, radial, SpellBar.IconFor(SpellBar.SlotIcon(n)), bar.SubtitleFor(g.Direct, "" + g.Direct.Slot))); continue; }
                GroupElement ge = UnityEngine.Object.Instantiate<GroupElement>(RadialData.SO.GroupElement);
                ge.Init(new SpellGroupConfig(bar, n), this, radial);
                els.Add(ge);
            }
            radial.ConstructRadial(els);
        }

        static RadialMenuElement MakeEmpty()
        {
            EmptyElement e = UnityEngine.Object.Instantiate<EmptyElement>(RadialData.SO.EmptyElement);
            e.Init();
            return e;
        }
    }

    // Hotbar: the 10 cells of the action bar as radial sections (Shift+1..5, F1..F5). Click = cast, RMB = empty the cell,
    // and while a spell waits to be placed (Shift+click on it in a group) a click puts it into the clicked cell.
    class SpellHotbarConfig : IRadialConfig
    {
        readonly SpellBar bar;
        public SpellHotbarConfig(SpellBar b) { bar = b; }
        public string LocalizedName { get { return "Hotbar"; } }
        public Sprite Sprite { get { return SpellBar.IconFor("hotbar"); } }

        public void InitRadialConfig(RadialBase radial)
        {
            if (bar == null || SpellsState.Bar != bar) { SpellPatches.OpenVanilla(radial); return; }   // config of a build that was replaced by op=reload
            try
            {
                List<RadialMenuElement> els = new List<RadialMenuElement>();
                bar.ClearCellMap();
                SpellBar.SuppressBackOnShift(radial);
                foreach (string cell in SpellBar.PinOrder) els.Add(bar.MakeCellElement(cell, SpellsState.Bar.Root(), radial));
                radial.ConstructRadial(els);
            }
            catch (Exception e)
            {
                HomeApi.Log("spells: hotbar build failed, spells switched off: " + e);
                SpellsState.Enabled = false; SpellsState.SaveEnabled(false);
                try { radial.Open(RadialData.SO.MainGroupConfig, null); } catch (Exception e2) { HomeApi.Log("spells: vanilla fallback failed: " + e2.Message); }
            }
        }
    }

    // one group: its spells as leaves
    class SpellGroupConfig : IRadialConfig
    {
        readonly SpellBar bar; readonly int slot;
        public SpellGroupConfig(SpellBar b, int s) { bar = b; slot = s; }
        public string LocalizedName { get { Group g = bar.GroupAt(slot); return g == null ? "?" : g.Title; } }
        public Sprite Sprite { get { return SpellBar.IconFor(SpellBar.SlotIcon(slot)); } }

        public void InitRadialConfig(RadialBase radial)
        {
            if (bar == null || SpellsState.Bar != bar) { SpellPatches.OpenVanilla(radial); return; }   // config of a build that was replaced by op=reload
            try { Build(radial); }
            catch (Exception e)
            {
                HomeApi.Log("spells: group build failed, spells switched off: " + e);
                SpellsState.Enabled = false; SpellsState.SaveEnabled(false);
                try { radial.Open(RadialData.SO.MainGroupConfig, null); } catch (Exception e2) { HomeApi.Log("spells: vanilla fallback failed: " + e2.Message); }
            }
        }

        void Build(RadialBase radial)
        {
            Group g = bar.GroupAt(slot);
            List<RadialMenuElement> els = new List<RadialMenuElement>();
            if (g != null)
                for (int e = 1; e <= 8; e++)
                {
                    Spell s = g.Entries[e];
                    if (s == null) continue;
                    els.Add(bar.MakeLeaf(s, SpellsState.Bar.Root(), radial, Sprite, bar.SubtitleFor(s, slot + "." + e)));
                }
            radial.ConstructRadial(els);
        }
    }

    // a leaf only carries a name and an icon; it never opens a level
    class SpellLeafConfig : IRadialConfig
    {
        readonly string name; readonly Sprite sprite;
        public SpellLeafConfig(string n, Sprite s) { name = n; sprite = s; }
        public string LocalizedName { get { return name; } }
        public Sprite Sprite { get { return sprite; } }
        public void InitRadialConfig(RadialBase radial) { }
    }

    // ---- the command -----------------------------------------------------------------------------------------

    public class SpellsCommand : IHomeCommand
    {
        public string Name { get { return "spells"; } }
        public string Help
        {
            get
            {
                return "spells [enabled=1|0] [status=1] [open=1 (open the radial)] [reload=1 (re-read the spell file)] [list=1] [reset=1 (rewrite the active file with the defaults, old one saved as .bak)] [pin=F1&spell=7.2 (this session only)] [placeholder=1|0 (default 1: fake spells from spells-ui-test.txt, a cast only shows a toast; 0: real spells from spells.txt)] - "
                    + "[pins=reset (forget the cells you placed, back to the pin= flags of the spell file)] "
                    + "G = radial with our groups (Shift+G vanilla, T emotes); always-on bar of 10 cells: 1-5 (fire on Shift+1..5) and F1-F5; Shift+click a spell in the radial, then click a cell to place it; files BepInEx/config/homecheat/spells-ui-test.txt and spells.txt (+ .pins)";
            }
        }

        public SpellsCommand()
        {
            // a reload creates a new instance: remove the bar and patches of the previous build first
            GameObject old;
            while ((old = GameObject.Find(SpellsState.ObjName)) != null) UnityEngine.Object.DestroyImmediate(old);
            SpellsState.Enabled = SpellsState.LoadEnabled();   // also sets Placeholder; must come before the bar reads its file
            GameObject go = new GameObject(SpellsState.ObjName);
            UnityEngine.Object.DontDestroyOnLoad(go);
            SpellsState.Bar = go.AddComponent<SpellBar>();
            try { SpellPatches.Apply(SpellsState.Enabled); } catch (Exception e) { HomeApi.Log("spells: patch failed: " + e.Message); SpellsState.Enabled = false; }
            HomeApi.Log("spells: loaded, enabled=" + SpellsState.Enabled + ", placeholder=" + SpellsState.Placeholder);
        }

        public string Run(Dictionary<string, string> a)
        {
            SpellBar bar = SpellBar.Find();
            if (bar == null) return "ERROR: spell bar object missing (op=reload)";
            string en = HomeApi.S(a, "enabled", "");
            if (en.Length > 0)
            {
                bool on = HomeApi.Flag(a, "enabled", false);
                string r;
                try { r = SpellPatches.Apply(on); } catch (Exception e) { return "ERROR: " + e.Message; }
                SpellsState.Enabled = on && !r.StartsWith("ERROR");
                SpellsState.SaveEnabled(SpellsState.Enabled);
                if (SpellsState.Enabled) bar.Baseline();
                return "spells " + (SpellsState.Enabled ? "ON" : "OFF") + ": " + r + (SpellsState.Enabled ? ". Press G for the radial (Shift+G = vanilla radial); the 10-cell bar is on screen." : ". G opens the vanilla radial again.");
            }
            string ph = HomeApi.S(a, "placeholder", "");
            if (ph.Length > 0)
            {
                SpellsState.Placeholder = HomeApi.Flag(a, "placeholder", true);
                SpellsState.SaveEnabled(SpellsState.Enabled);
                return "placeholder " + (SpellsState.Placeholder ? "ON: fake spells from spells-ui-test.txt, casts only show a toast" : "OFF: real spells from spells.txt, casts run commands") + ". " + bar.LoadFile(true);
            }
            if (HomeApi.S(a, "pins", "") == "reset") return bar.ResetPins();
            if (HomeApi.Flag(a, "reset", false)) return bar.ResetFile();
            if (HomeApi.Flag(a, "reload", false)) return bar.LoadFile(true);
            if (HomeApi.Flag(a, "list", false)) return bar.List();
            if (HomeApi.Flag(a, "open", false)) { if (!SpellsState.Enabled) return "ERROR: spells are off (enabled=1)"; return "radial: " + bar.OpenRadial(); }
            string pin = HomeApi.S(a, "pin", "");
            if (pin.Length > 0) return bar.SetPin(pin.ToUpperInvariant(), HomeApi.S(a, "spell", ""));
            return bar.Status();
        }
    }

    // ---- the bar: spell data, pins, action bar drawing, casting ---------------------------------------------------

    [DefaultExecutionOrder(-1000)]
    public class SpellBar : MonoBehaviour
    {
        const BindingFlags AllF = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

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
"3.2 | Stone circle | op=earthquake | pin=S2\n" +
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
"7.4 | Place wall | op=place&prefab=wood_wall&perrow=3\n" +
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

        static string ActiveDefault { get { return SpellsState.Placeholder ? TestText : DefaultText; } }

        internal static readonly string[] PinOrder = new string[] { "S1", "S2", "S3", "S4", "S5", "F1", "F2", "F3", "F4", "F5" };

        Group[] groups = new Group[9];
        Dictionary<string, Spell> pins = new Dictionary<string, Spell>();
        Dictionary<string, float> flash = new Dictionary<string, float>();
        DateTime fileStamp;
        SpellRootConfig root;
        bool barVisible, inputOk;
        string toast = ""; float toastUntil; bool toastErr;
        string baseline = "";
        string lastCast = "-";
        GUIStyle sKey, sName, sToast;
        Spell assigning;                        // the spell waiting for a Hotbar cell (Shift+click on it in a radial group)
        float radialGoneAt;                     // since when the radial has been closed while a spell waits (0 = it is open)
        Dictionary<RadialMenuElement, string> cellOf = new Dictionary<RadialMenuElement, string>();   // radial Hotbar element -> cell, for RMB
        MethodInfo mQueuedOpen;
        Dictionary<int, Sprite> iconCache = new Dictionary<int, Sprite>();

        public static SpellBar Find()
        {
            GameObject go = GameObject.Find(SpellsState.ObjName);
            return go == null ? null : go.GetComponent<SpellBar>();
        }

        internal SpellRootConfig Root() { if (root == null) root = new SpellRootConfig(this); return root; }
        internal Group GroupAt(int slot) { return slot >= 1 && slot <= 8 ? groups[slot] : null; }

        void Awake() { LoadFile(false); }

        void OnDestroy()
        {
            try { if (SpellsState.H != null) Harmony.UnpatchID(SpellsState.HarmonyId); } catch (Exception) { }
            if (SpellsState.Bar == this) SpellsState.Bar = null;
        }

        // ---- icons: the game's own sprites ------------------------------------------------------------------------

        // slot -> icon key. Reuses the icons of the vanilla radial for now (hotbar, emote, item-group mappings).
        public static string SlotIcon(int slot)
        {
            switch (slot)
            {
                case 2: return "weaponstools";
                case 3: return "armor_utility";
                case 4: return "emote";
                case 5: return "consumables";
                case 6: return "allitems";
                case 7: return "consumables";
                case 8: return "weaponstools";
                default: return "hotbar";
            }
        }

        public static Sprite IconFor(string key)
        {
            try
            {
                RadialDataSO so = RadialData.SO;
                if (key == "hotbar") return so.HotbarGroupConfig.Sprite;
                if (key == "emote") return so.EmoteGroupConfig.Sprite;
                return so.ItemGroupMappings.GetMapping(key).Sprite;
            }
            catch (Exception) { return null; }
        }

        public static string PinLabel(string pin)
        {
            return pin.Length == 2 && pin[0] == 'S' ? "[Shift+" + pin[1] + "]" : "[" + pin + "]";
        }

        // ---- radial leaf: a game element that calls our code ------------------------------------------------------------------

        // A leaf is the game's GroupElement prefab with a name and an icon; its Interact and CloseOnInteract are replaced, so selecting it
        // casts the spell (CloseOnInteract false = sticky: the radial stays open).
        // Shift+click does not cast: it picks the spell up and opens the Hotbar section to choose a cell (see BeginPlace).
        internal RadialMenuElement MakeLeaf(Spell s, IRadialConfig back, RadialBase radial, Sprite icon, string subtitle)
        {
            GroupElement ge = UnityEngine.Object.Instantiate<GroupElement>(RadialData.SO.GroupElement);
            ge.Init(new SpellLeafConfig(s.Label, icon), back, radial);
            SetSubTitle(ge, subtitle);
            ge.Interact = delegate()
            {
                if (SpellsState.ShiftHeld()) BeginPlace(s, radial); else CastSpell(s, false, null);
                return true;
            };
            ge.CloseOnInteract = delegate() { return SpellsState.ShiftHeld() ? false : !s.Sticky; };
            return ge;
        }

        // a Hotbar section: the spell of one bar cell (or an empty slot). Click casts it; while a spell waits to be placed a click puts it here.
        internal RadialMenuElement MakeCellElement(string cell, IRadialConfig back, RadialBase radial)
        {
            Spell s;
            bool has = pins.TryGetValue(cell, out s);
            bool placing = assigning != null;
            string key = cell[0] == 'S' ? "Shift+" + cell[1] : cell;
            GroupElement ge = UnityEngine.Object.Instantiate<GroupElement>(RadialData.SO.GroupElement);
            ge.Init(new SpellLeafConfig(has ? s.Label : "(empty)", has ? IconOf(s) : null), back, radial);
            SetSubTitle(ge, placing ? key + "   click: place \"" + assigning.Label + "\" here" : key + (has ? "   Shift+RMB: empty this cell" : "   Shift+click a spell to place it"));
            ge.Interact = delegate()
            {
                if (assigning != null) AssignTo(cell);
                else if (has) CastSpell(s, false, cell);
                return true;
            };
            ge.CloseOnInteract = delegate() { return placing || !has || !s.Sticky; };
            cellOf[ge] = cell;
            return ge;
        }

        internal void ClearCellMap() { cellOf.Clear(); }

        // text under the name in the radial centre: id, the cell the spell sits in, how to move it
        internal string SubtitleFor(Spell s, string id)
        {
            string k = PinOf(s);
            return id + (k.Length > 0 ? "  " + PinLabel(k) : "") + "   Shift+click: to the bar";
        }

        string PinOf(Spell s)
        {
            for (int i = 0; i < PinOrder.Length; i++)
            {
                Spell p;
                if (pins.TryGetValue(PinOrder[i], out p) && p == s) return PinOrder[i];
            }
            return "";
        }

        static string IdOf(Spell s) { return s.Entry > 0 ? s.Slot + "." + s.Entry : "" + s.Slot; }

        // ---- placing a spell on the bar ---------------------------------------------------------------------------------------

        // (re)opens the Hotbar section so it shows the current cells
        void OpenHotbar(RadialBase radial)
        {
            IRadialConfig hb = new SpellHotbarConfig(this);
            if (mQueuedOpen == null) mQueuedOpen = typeof(RadialBase).GetMethod("QueuedOpen", AllF);
            if (mQueuedOpen != null) mQueuedOpen.Invoke(radial, new object[] { hb, Root() });
            else radial.Open(hb, Root());
        }

        // RMB is the radial's Back; with Shift held it must not go back, because Shift+RMB empties a Hotbar cell
        static RadialBase backWrappedFor;
        internal static void SuppressBackOnShift(RadialBase radial)
        {
            if (backWrappedFor == radial) return;
            try
            {
                PropertyInfo pr = typeof(RadialBase).GetProperty("GetBack", AllF);
                if (pr == null) return;
                MethodInfo get = pr.GetGetMethod(true), set = pr.GetSetMethod(true);
                Func<bool> orig = get == null ? null : (Func<bool>)get.Invoke(radial, null);
                if (orig == null || set == null) return;
                set.Invoke(radial, new object[] { (Func<bool>)delegate() { return !(SpellsState.Enabled && SpellsState.ShiftHeld()) && orig(); } });
                backWrappedFor = radial;
            }
            catch (Exception e) { HomeApi.Log("spells: cannot wrap Back: " + e.Message); }
        }

        // Shift+click on a spell: remember it and switch the open radial to the Hotbar section; a click on a cell there places it
        void BeginPlace(Spell s, RadialBase radial)
        {
            assigning = s;
            radialGoneAt = 0f;
            Toast("", false);
            try { OpenHotbar(radial); }
            catch (Exception e)
            {
                assigning = null;
                HomeApi.Log("spells: cannot open the Hotbar section: " + (e.InnerException != null ? e.InnerException.Message : e.Message));
            }
        }

        // Shift+RMB on a Hotbar section empties its cell (plain RMB is the radial's Back, see SuppressBackOnShift)
        void HandleRadialSecondary()
        {
            if (cellOf.Count == 0 || !SpellsState.ShiftHeld()) return;
            bool rmb = false;
            try { rmb = ZInput.GetButtonDown("RadialSecondaryInteract"); } catch (Exception) { }
            if (!rmb) { try { rmb = Input.GetMouseButtonDown(1); } catch (Exception) { } }
            if (!rmb) return;
            try
            {
                RadialBase radial = (RadialBase)typeof(Hud).GetField("m_radialMenu", AllF).GetValue(Hud.instance);
                RadialMenuElement sel = radial == null ? null : radial.Selected;
                string cell;
                if (sel == null || !cellOf.TryGetValue(sel, out cell)) return;
                ClearCell(cell);
                OpenHotbar(radial);
            }
            catch (Exception e) { HomeApi.Log("spells: RMB clear failed: " + e.Message); }
        }

        // puts the pending spell into a cell; one spell sits in one cell only (it moves)
        void AssignTo(string cell)
        {
            Spell s = assigning;
            if (s == null) return;
            foreach (string k in PinOrder) { Spell p; if (pins.TryGetValue(k, out p) && p == s) pins.Remove(k); }
            pins[cell] = s;
            SaveOverrides();
            string msg = s.Label + " placed on " + (cell[0] == 'S' ? "Shift+" + cell[1] : cell);
            HomeApi.Log("spells: " + msg);
            assigning = null;
            Toast(msg, false);
        }

        void ClearCell(string cell)
        {
            if (!pins.Remove(cell)) return;
            SaveOverrides();
            Toast("cell " + (cell[0] == 'S' ? "Shift+" + cell[1] : cell) + " emptied", false);
        }

        string OverrideFile { get { return SpellsState.ActiveFile + ".pins"; } }

        // the cells you placed: one line per cell, "S1=2.1" or "F3=5.1"; once the file exists it replaces the pin= flags of the spell file
        void SaveOverrides()
        {
            try
            {
                StringBuilder sb = new StringBuilder("# written by the spell bar when you place a spell: cell=slot.entry (S1..S5 = Shift+1..5, F1..F5). Delete this file (or op=spells&pins=reset) to go back to the pin= flags of the spell file.\n");
                foreach (string k in PinOrder) { Spell s; if (pins.TryGetValue(k, out s)) sb.Append(k).Append('=').Append(IdOf(s)).Append('\n'); }
                Directory.CreateDirectory(SpellsState.Dir);
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
            return "placed cells forgotten. " + LoadFile(true);
        }

        // SubTitle has a non-public setter in the game: set it by reflection (text under the name in the radial centre)
        static void SetSubTitle(RadialMenuElement e, string text)
        {
            try
            {
                PropertyInfo p = typeof(RadialMenuElement).GetProperty("SubTitle", AllF);
                MethodInfo m = p == null ? null : p.GetSetMethod(true);
                if (m != null) m.Invoke(e, new object[] { text });
            }
            catch (Exception) { }
        }

        // same call vanilla HandleRadialInput makes: Hud.m_radialMenu.Open(Hud.m_config, null); the Open prefix swaps in our config
        public string OpenRadial()
        {
            try
            {
                Hud hud = Hud.instance;
                RadialBase radial = (RadialBase)typeof(Hud).GetField("m_radialMenu", AllF).GetValue(hud);
                object cfg = typeof(Hud).GetField("m_config", AllF).GetValue(hud);
                if (radial == null || cfg == null) return "radial menu not available";
                PropertyInfo can = radial.GetType().GetProperty("CanOpen", AllF);
                if (can != null && can.CanWrite) can.SetValue(radial, true, null);
                radial.Open((IRadialConfig)cfg, null);
                return "opened";
            }
            catch (Exception e) { return "failed: " + (e.InnerException != null ? e.InnerException.Message : e.Message); }
        }

        // ---- spell files ------------------------------------------------------------------------------------------

        public string ResetFile()
        {
            try
            {
                Directory.CreateDirectory(SpellsState.Dir);
                if (File.Exists(SpellsState.ActiveFile)) File.Copy(SpellsState.ActiveFile, SpellsState.ActiveFile + ".bak", true);
                File.WriteAllText(SpellsState.ActiveFile, ActiveDefault);
            }
            catch (Exception e) { return "ERROR: " + e.Message; }
            return Path.GetFileName(SpellsState.ActiveFile) + " rewritten with the defaults (old one: .bak). " + LoadFile(true);
        }

        public string LoadFile(bool report)
        {
            try
            {
                if (!File.Exists(SpellsState.ActiveFile)) { Directory.CreateDirectory(SpellsState.Dir); File.WriteAllText(SpellsState.ActiveFile, ActiveDefault); }
                fileStamp = File.GetLastWriteTimeUtc(SpellsState.ActiveFile);
                string msg = Parse(File.ReadAllText(SpellsState.ActiveFile, Encoding.UTF8));
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
            try { if (File.Exists(SpellsState.ActiveFile) && File.GetLastWriteTimeUtc(SpellsState.ActiveFile) != fileStamp) LoadFile(false); }
            catch (Exception) { }
        }

        string Parse(string text)
        {
            Group[] g = new Group[9];
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
                if (g[slot] == null) g[slot] = new Group();
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
            string msg = Path.GetFileName(SpellsState.ActiveFile) + ": " + spells + " spells, " + pn.Count + " pins" + (bad.Length > 0 ? "; skipped bad" + bad : "") + " (slot 1 is always the game Hotbar group)";
            if (bad.Length > 0) HomeApi.Log("spells: " + msg);
            return msg;
        }

        public string List()
        {
            StringBuilder sb = new StringBuilder("1: (the game's own Hotbar group)\n");
            for (int s = 2; s <= 8; s++)
            {
                Group g = groups[s];
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

        Spell FindSpell(string id)
        {
            int slot, entry = 0;
            int dot = id.IndexOf('.');
            bool ok = dot > 0 ? int.TryParse(id.Substring(0, dot), out slot) && int.TryParse(id.Substring(dot + 1), out entry) : int.TryParse(id, out slot);
            if (!ok || slot < 1 || slot > 8 || groups[slot] == null) return null;
            return dot > 0 && entry >= 1 && entry <= 8 ? groups[slot].Entries[entry] : groups[slot].Direct;
        }

        public string Status()
        {
            return "spells " + (SpellsState.Enabled ? "ON" : "OFF") + (SpellsState.Placeholder ? " (PLACEHOLDER mode)" : " (real spells)")
                + ", patches " + PatchInfo() + ", last cast: " + lastCast + ", flags: " + Snap() + "\n" + CountSlots() + " groups, " + pins.Count + " pins. file " + SpellsState.ActiveFile;
        }

        int CountSlots() { int n = 0; for (int i = 2; i <= 8; i++) if (groups[i] != null) n++; return n; }

        static string PatchInfo()
        {
            try
            {
                MethodInfo m1 = null;
                foreach (MethodInfo m in typeof(RadialBase).GetMethods(AllF)) if (m.Name == "Open" && m.GetParameters().Length == 2) { m1 = m; break; }
                MethodInfo m2 = typeof(Player).GetMethod("UseHotbarItem", AllF);
                HarmonyLib.Patches p1 = Harmony.GetPatchInfo(m1), p2 = Harmony.GetPatchInfo(m2);
                int n1 = p1 == null ? 0 : p1.Prefixes.Count, n2 = p2 == null ? 0 : p2.Prefixes.Count;
                return "prefixes radialOpen=" + n1 + " hotbar=" + n2;
            }
            catch (Exception e) { return "? (" + e.Message + ")"; }
        }

        // ---- input: the action bar pins ---------------------------------------------------------------------------------------

        static readonly MethodInfo mTake = typeof(Player).GetMethod("TakeInput", AllF);
        static readonly MethodInfo mHidden = typeof(Hud).GetMethod("IsUserHidden", AllF);
        static readonly MethodInfo mInRadial = typeof(Hud).GetMethod("InRadial", AllF);

        static bool TakeInput(Player p)
        {
            if (mTake == null) return true;
            try { return (bool)mTake.Invoke(p, null); } catch (Exception) { return true; }
        }

        static bool HudHidden()
        {
            if (mHidden == null) return false;
            try { return (bool)mHidden.Invoke(null, null); } catch (Exception) { return false; }
        }

        static bool InRadial()
        {
            if (mInRadial == null) return false;
            try { return (bool)mInRadial.Invoke(null, null); } catch (Exception) { return false; }
        }

        void Update()
        {
            barVisible = false;
            if (!SpellsState.Enabled) { assigning = null; return; }
            Player p = Player.m_localPlayer;
            if (p == null || HudHidden()) { assigning = null; return; }
            barVisible = true;
            bool inRadial = InRadial();
            if (inRadial) { radialGoneAt = 0f; HandleRadialSecondary(); }
            else if (assigning != null)
            {
                // a spell waits for a Hotbar cell: closing the radial (or ~0.6 s without it) cancels
                if (radialGoneAt == 0f) radialGoneAt = Time.unscaledTime;
                else if (Time.unscaledTime - radialGoneAt > 0.6f) { assigning = null; radialGoneAt = 0f; Toast("placing cancelled", false); }
            }
            inputOk = TakeInput(p) && !p.IsDead() && !inRadial;
            if (!inputOk) return;
            bool shift = SpellsState.ShiftHeld();
            for (int i = 0; i < PinOrder.Length; i++)
            {
                string k = PinOrder[i];
                Spell ps;
                if (!pins.TryGetValue(k, out ps)) continue;
                bool hit;
                if (k[0] == 'F') hit = ZInput.GetKeyDown(FKey(k), false);
                else hit = shift && ZInput.GetButtonDown("Hotbar" + k.Substring(1));
                if (hit)
                {
                    // Shift is part of the key for cells 1-5, so it does not select the spell's shift= variant there
                    if (k[0] == 'S') SpellsState.ConsumedFrame = Time.frameCount;
                    CastSpell(ps, k[0] == 'F' && shift, k);
                    return;
                }
            }
        }

        static KeyCode FKey(string k)
        {
            switch (k)
            {
                case "F1": return KeyCode.F1;
                case "F2": return KeyCode.F2;
                case "F3": return KeyCode.F3;
                case "F4": return KeyCode.F4;
                default: return KeyCode.F5;
            }
        }

        // ---- casting -----------------------------------------------------------------------------------------------------------

        // pinKey: the bar slot that fired (for the flash), null when the spell was picked in the radial
        void CastSpell(Spell s, bool shift, string pinKey)
        {
            if (Time.unscaledTime < s.Next) return;
            s.Next = Time.unscaledTime + 0.3f;
            if (pinKey != null) flash[pinKey] = Time.unscaledTime + 0.25f;
            // placeholder mode (or a @dummy line): show a toast and log it, never call a command, never touch the game
            if (SpellsState.Placeholder || s.Cmd.Trim() == "@dummy")
            {
                string id = s.Entry > 0 ? s.Slot + "." + s.Entry : "" + s.Slot;
                string pm = "placeholder: " + s.Label + " (" + id + ")" + (shift && s.Shift.Length > 0 ? " +shift=" + s.Shift : "");
                lastCast = pm;
                HomeApi.Log("spells: " + pm);
                Toast(pm, false);
                return;
            }
            string form = s.Cmd;
            if (shift && s.Shift.Length > 0) form = form + "&" + s.Shift;
            string before = Snap();
            StringBuilder res = new StringBuilder();
            foreach (string part in form.Split(new string[] { ";;" }, StringSplitOptions.RemoveEmptyEntries))
            {
                string r = Exec(part.Trim());
                if (res.Length > 0) res.Append(" | ");
                res.Append(FirstLine(r));
            }
            string after = Snap();
            lastCast = s.Label + " (" + form + ")";
            bool err = res.ToString().StartsWith("ERROR");
            HomeApi.Log("spells: " + s.Label + " -> " + res);
            if (before != after)
            {
                HomeApi.Log("spells: FLAG CHANGED by '" + s.Label + "': " + before + " => " + after);
                Toast("FLAG CHANGED by " + s.Label + ", spells switched off", true);
                SpellsState.Enabled = false; SpellsState.SaveEnabled(false);
                try { SpellPatches.Apply(false); } catch (Exception) { }
                return;
            }
            Toast(s.Label + ": " + res, err);
        }

        // toast text: the first line, except for results that start with progress lines (earthquake: "stage 0 ..."), then the last line
        static string FirstLine(string r)
        {
            if (r == null) return "";
            r = r.Trim();
            int nl = r.IndexOf('\n');
            if (nl < 0) return r;
            string first = r.Substring(0, nl).Trim();
            if (first.StartsWith("stage ") || first.StartsWith("probe"))
            {
                string[] lines = r.Split('\n');
                for (int i = lines.Length - 1; i >= 0; i--) if (lines[i].Trim().Length > 0) return lines[i].Trim();
            }
            return first;
        }

        // runs "op=...&k=v" through the core Run on this thread, the same code the inbox poll uses
        static string Exec(string form)
        {
            try
            {
                if (ZNet.instance == null || ZNet.instance.IsServer() == false)
                    return "ERROR: spells only run in single player or a world this PC hosts";
                MethodInfo viaApi = typeof(HomeApi).GetMethod("Exec", BindingFlags.Static | BindingFlags.Public);
                if (viaApi != null) return (string)viaApi.Invoke(null, new object[] { form });
                Type t = typeof(IHomeCommand).Assembly.GetType("HomeCheat.HomeCheatPlugin");
                object inst = t.GetField("Inst", AllF).GetValue(null);
                MethodInfo parse = t.GetMethod("ParseForm", AllF);
                MethodInfo run = t.GetMethod("Run", AllF);
                object dict = parse.Invoke(null, new object[] { form });
                return (string)run.Invoke(inst, new object[] { dict });
            }
            catch (TargetInvocationException e) { return "ERROR: " + (e.InnerException != null ? e.InnerException.Message : e.Message); }
            catch (Exception e) { return "ERROR: " + e.Message; }
        }

        void Toast(string t, bool err) { toast = t; toastErr = err; toastUntil = Time.unscaledTime + 3.5f; }

        // ---- cheat-state snapshot (read only), compared before/after every cast -------------------------------------------------

        public void Baseline() { baseline = Snap(); }

        static string Snap()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                FieldInfo tc = typeof(Terminal).GetField("m_cheat", AllF);
                sb.Append("cheat=").Append(tc == null ? "?" : "" + tc.GetValue(null));
            }
            catch (Exception) { sb.Append("cheat=?"); }
            try
            {
                PlayerProfile pp = Game.instance == null ? null : Game.instance.GetPlayerProfile();
                FieldInfo uc = typeof(PlayerProfile).GetField("m_usedCheats", AllF);
                sb.Append(" used=").Append(pp == null || uc == null ? "?" : "" + uc.GetValue(pp));
            }
            catch (Exception) { sb.Append(" used=?"); }
            Player p = Player.m_localPlayer;
            foreach (string n in new string[] { "m_godMode", "m_ghostMode", "m_debugFly", "m_noPlacementCost" })
            {
                try
                {
                    FieldInfo f = typeof(Player).GetField(n, AllF);
                    sb.Append(' ').Append(n.Substring(2)).Append('=');
                    if (f == null) sb.Append('?'); else sb.Append(f.IsStatic ? f.GetValue(null) : (p == null ? (object)"?" : f.GetValue(p)));
                }
                catch (Exception) { sb.Append('?'); }
            }
            return sb.ToString();
        }

        // ---- drawing: the always-on action bar (IMGUI), one row of 10 cells --------------------------------------------------------

        const float PanelScale = 0.70f;     // whole bar, banner and toast (was 1.0, then 0.85)
        const float CellW = 72f, CellH = 88f, CellGap = 6f, GroupGap = 20f;

        void MakeStyles()
        {
            if (sKey != null) return;
            sKey = new GUIStyle(GUI.skin.label); sKey.fontSize = 13; sKey.fontStyle = FontStyle.Bold; sKey.alignment = TextAnchor.UpperLeft; sKey.normal.textColor = new Color(1f, 0.85f, 0.45f);
            sName = new GUIStyle(GUI.skin.label); sName.fontSize = 12; sName.alignment = TextAnchor.UpperCenter; sName.wordWrap = true; sName.clipping = TextClipping.Clip; sName.normal.textColor = Color.white;
            sToast = new GUIStyle(GUI.skin.label); sToast.fontSize = 16; sToast.alignment = TextAnchor.MiddleCenter; sToast.wordWrap = true; sToast.normal.textColor = Color.white;
        }

        static void Fill(Rect r, Color c)
        {
            Color o = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = o;
        }

        static void Frame(Rect r, float t, Color c)
        {
            Fill(new Rect(r.x, r.y, r.width, t), c); Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
            Fill(new Rect(r.x, r.y, t, r.height), c); Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        // cell i: 0-4 = Shift+1..5 (labelled 1-5), 5-9 = F1-F5; a wider gap between the two groups of five
        static Rect CellRect(int i, float W, float H)
        {
            float total = 10 * CellW + 8 * CellGap + GroupGap;
            float x = (W - total) / 2f + i * (CellW + CellGap) + (i >= 5 ? GroupGap - CellGap : 0f);
            return new Rect(x, H - 36f - CellH, CellW, CellH);
        }

        // the spell's icon: the sprite of its group in the game's radial (spells have no icon of their own yet)
        Sprite IconOf(Spell s)
        {
            Sprite sp;
            if (iconCache.TryGetValue(s.Slot, out sp) && sp != null) return sp;
            sp = IconFor(SlotIcon(s.Slot));
            iconCache[s.Slot] = sp;
            return sp;
        }

        static void DrawSprite(Rect r, Sprite sp, float alpha)
        {
            if (sp == null) return;
            try
            {
                Texture2D t = sp.texture;
                if (t == null) return;
                Rect tr = sp.textureRect;
                Rect uv = new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height);
                Color o = GUI.color; GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.DrawTextureWithTexCoords(r, t, uv, true);
                GUI.color = o;
            }
            catch (Exception) { }
        }

        void OnGUI()
        {
            bool asg = SpellsState.Enabled && assigning != null;
            bool showToast = SpellsState.Enabled && toast.Length > 0 && Time.unscaledTime < toastUntil;
            if (!(SpellsState.Enabled && barVisible) && !showToast) return;
            if (Event.current.type != EventType.Repaint) return;
            MakeStyles();
            float sc = Screen.height / 1080f * PanelScale;
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(sc, sc, 1f));
            float W = Screen.width / sc, H = Screen.height / sc;
            Rect first = CellRect(0, W, H), last = CellRect(9, W, H);
            if (SpellsState.Enabled && barVisible)
            {
                float a = (inputOk || asg || InRadial()) ? 1f : 0.45f;
                // only filled cells, packed and centred (empty cells are not shown)
                List<string> shown = new List<string>();
                foreach (string cell in PinOrder) if (pins.ContainsKey(cell)) shown.Add(cell);
                float rowW = shown.Count * CellW + (shown.Count - 1) * CellGap;
                for (int i = 0; i < shown.Count; i++)
                {
                    string cell = shown[i];
                    Rect r = new Rect((W - rowW) / 2f + i * (CellW + CellGap), first.y, CellW, CellH);
                    DrawSlot(r, cell, cell[0] == 'S' ? cell.Substring(1) : cell, a, false, false);
                }
            }
            if (asg)
            {
                Rect br = new Rect(first.x - 40f, first.y - 46f, last.xMax - first.x + 80f, 34f);
                Fill(br, new Color(0.30f, 0.22f, 0.04f, 0.92f));
                GUI.Label(br, "Place \"" + assigning.Label + "\": click a cell in the radial Hotbar (closing the radial cancels)", sToast);
            }
            else if (showToast)
            {
                Rect tr = new Rect(first.x - 40f, first.y - 46f, last.xMax - first.x + 80f, 34f);
                Fill(tr, toastErr ? new Color(0.45f, 0.08f, 0.08f, 0.88f) : new Color(0.06f, 0.18f, 0.08f, 0.88f));
                GUI.Label(tr, toast, sToast);
            }
            GUI.matrix = old;
        }

        void DrawSlot(Rect r, string pinKey, string keyLabel, float alpha, bool picking, bool hover)
        {
            Spell s;
            bool has = pins.TryGetValue(pinKey, out s);
            float fl;
            bool flashing = flash.TryGetValue(pinKey, out fl) && Time.unscaledTime < fl;
            Color bg = has ? new Color(0.08f, 0.08f, 0.10f, 0.82f * alpha) : new Color(0.08f, 0.08f, 0.10f, 0.35f * alpha);
            if (flashing) bg = new Color(0.75f, 0.58f, 0.2f, 0.95f * alpha);
            if (hover) bg = new Color(0.32f, 0.26f, 0.08f, 0.95f);
            Fill(r, bg);
            Fill(new Rect(r.x, r.y, r.width, 3f), has ? new Color(1f, 0.85f, 0.45f, 0.9f * alpha) : new Color(0.4f, 0.4f, 0.4f, 0.5f * alpha));
            if (picking) Frame(r, hover ? 3f : 1.5f, new Color(1f, 0.85f, 0.45f, hover ? 1f : 0.6f));
            GUI.Label(new Rect(r.x + 5f, r.y + 3f, 40f, 20f), keyLabel, sKey);
            if (!has) return;
            DrawSprite(new Rect(r.x + (r.width - 38f) / 2f, r.y + 18f, 38f, 38f), IconOf(s), alpha);
            GUI.Label(new Rect(r.x + 2f, r.y + 58f, r.width - 4f, r.height - 58f), s.Label, sName);
            float cd = s.Next - Time.unscaledTime;
            if (cd > 0f) Fill(new Rect(r.x, r.y, r.width, r.height * Mathf.Clamp01(cd / 0.3f)), new Color(0f, 0f, 0f, 0.5f));
        }
    }
}
