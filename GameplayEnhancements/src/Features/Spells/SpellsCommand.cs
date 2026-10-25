// spells: the HomeCheat spell system, view v3, spell file v4 (design: docs/Spells/spellbar-design.md). Own game / own world only.
//   G                 opens the game's own radial menu, filled with our sections (slot 1 = our Hotbar); Shift+G = vanilla radial, T = emotes.
//                     Spell file v4: dotted ids of any depth (sub-sections), any number of entries per level (the game's spiral),
//                     sticky titles, answer chains (ask= flags: one radial level per question, {key} takes the answer)
//   action bar        always-visible bar of 24 cells in two rows: F1-F12 on top, 1..= (fire on Shift+1..Shift+=) below; radial Hotbar = Panel 1-5 and Panel 6-=; each cell shows the spell icon with its name under it.
//                     Shift+click on a spell in the radial = place it: the radial closes, the cursor is freed, click a cell (right click empties it, Esc cancels).
//                     Placements are saved next to the spell file (<file>.pins); without that file the cells come from the pin=... flags of the spell file
// The radial is the game's own RadialBase; we only implement its extension point IRadialConfig and swap the config that RadialBase.Open
// receives for the main radial (Harmony prefix), so opening, animation, input blocking, back button, closing stay vanilla.
// Never touches Terminal / Console / god / ghost / fly flags. Calls the existing op=... commands in-process (HomeApi.Exec).
// Placeholder mode (default ON): spells-ui-test.txt, a cast only shows a toast. Starts DISABLED unless spells.state says enabled=1.
// Files: SpellsSettings (state of the loaded build), SpellFile (file, defaults, pins), SpellParse (v4 parser, no Unity), Spell/SpellNode/SpellAsk*
// (data), Spell*Config (radial levels: root, hotbar, group, question), SpellAskSources (@players @crops @trees @prefabs), SpellPatches, SpellBar (UI).
using System;
using UnityEngine;

namespace HomeCheat.Features.Spells
{
    public sealed class SpellsCommand : CommandBase
    {
        SpellsSettings settings;

        public SpellsCommand()
        {
            SubAction("pins", "pins=reset (forget the cells you placed, back to the pin= flags of the spell file)", delegate (Args a)
            {
                if (a.Str("pins", "") != "reset") throw new CommandException("pins=reset is the only value");
                return Bar().ResetPins();
            });
            SubFlag("reset", "reset=1 (rewrite the active file with the defaults, old one saved as .bak)", delegate (Args a) { return Bar().ResetFile(); });
            SubFlag("reload", "reload=1 (re-read the spell file)", delegate (Args a) { return Bar().LoadFile(true); });
            SubFlag("list", "list=1", delegate (Args a) { return Bar().List(); });
            SubFlag("open", "open=1 (open the radial)", delegate (Args a)
            {
                SpellBar bar = Bar();
                if (!settings.Enabled) throw new CommandException("spells are off (enabled=1)");
                return "radial: " + bar.OpenRadial();
            });
            SubAction("pin", "pin=F1&spell=6.5.3 (this session only)", delegate (Args a) { return Bar().SetPin(a.Str("pin", "").ToUpperInvariant(), a.Str("spell", "")); });
        }

        public override string Name { get { return "spells"; } }
        protected override string Usage
        {
            get
            {
                return "spells [enabled=1|0] [status=1 (also the radial's MaxElementsRange)] [placeholder=1|0 (default 1: fake spells from spells-ui-test.txt, a cast only shows a toast; 0: real spells from spells.txt)] - "
                    + "G = radial with our sections and sub-sections (Shift+G vanilla, T emotes; long levels continue as a spiral; ask= spells ask one level per question, right click = previous question); "
                    + "always-on bar of 24 cells in two rows: F1-F12, and 1..= on Shift+1..Shift+= (radial Hotbar: Panel 1-5 = Shift+1..5 and F1-F5, Panel 6-= = Shift+6..Shift+= and F6-F12); Shift+click a spell in the radial, then click a cell to place it; files BepInEx/config/homecheat/spells-ui-test.txt and spells.txt (format v4, + .pins)";
            }
        }

        protected override void OnLoad()
        {
            settings = new SpellsSettings();
            settings.Load();               // also sets Placeholder; must come before the bar reads its file
            settings.Patches = Patches;
            SpellsSettings.Current = settings;
            GameObject go = Own(new GameObject(SpellsSettings.ObjName));
            UnityEngine.Object.DontDestroyOnLoad(go);
            settings.Bar = go.AddComponent<SpellBar>();
            // no patches here: at game start they hang the scene load; SpellBar.Update applies them once a world is loaded
            // (immediately on an op=reload inside a world)
            SpellPatches.EnsureApplied();
            HomeApi.Log("spells: loaded, enabled=" + settings.Enabled + ", placeholder=" + settings.Placeholder
                + (settings.Enabled && !settings.Patched ? " (patches wait for a loaded world)" : ""));
        }

        // patches and the bar object are released by the base class
        protected override void OnUnload()
        {
            if (SpellsSettings.Current == settings) SpellsSettings.Current = null;
        }

        SpellBar Bar()
        {
            if (settings == null || settings.Bar == null) throw new CommandException("spell bar object missing (op=reload)");
            return settings.Bar;
        }

        protected override string Execute(Args a)
        {
            SpellBar bar = Bar();
            if (a.Str("enabled", "").Length > 0)
            {
                bool on = a.Flag("enabled", false);
                string r = SpellPatches.Apply(on);
                settings.Enabled = on && !r.StartsWith("ERROR");
                settings.Save();
                if (settings.Enabled) bar.Baseline();
                return "spells " + (settings.Enabled ? "ON" : "OFF") + ": " + r + (settings.Enabled ? ". Press G for the radial (Shift+G = vanilla radial); the 10-cell bar is on screen." : ". G opens the vanilla radial again.");
            }
            if (a.Str("placeholder", "").Length > 0)
            {
                settings.Placeholder = a.Flag("placeholder", true);
                settings.Save();
                return "placeholder " + (settings.Placeholder ? "ON: fake spells from spells-ui-test.txt, casts only show a toast" : "OFF: real spells from spells.txt, casts run commands") + ". " + bar.LoadFile(true);
            }
            return bar.Status();
        }
    }
}
