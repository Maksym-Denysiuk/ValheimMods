using System;
using System.Collections.Generic;
using UnityEngine;
using Valheim.UI;

namespace HomeCheat.Features.Spells
{
    // ---- the radial configs: our subclasses of the game's extension point ----
    // root level: 8 sections. Section 1 is our Hotbar (the 10 bar cells), 2-8 are our groups (or empty).
    sealed class SpellRootConfig : IRadialConfig
    {
        readonly SpellBar bar;
        public SpellRootConfig(SpellBar b) { bar = b; }
        public string LocalizedName { get { return "Spells"; } }
        public Sprite Sprite { get { return SpellBar.IconFor("hotbar"); } }

        public void InitRadialConfig(RadialBase radial)
        {
            if (bar == null || SpellsSettings.ActiveBar != bar) { SpellPatches.OpenVanilla(radial); return; }   // config of a build that was replaced by op=reload
            try { Build(radial); }
            catch (Exception e)
            {
                // never leave the player in a half-built radial: switch the spell system off and show the vanilla radial instead
                HomeApi.Log("spells: radial build failed, spells switched off: " + e);
                SpellsSettings.TurnOff();
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
                SpellGroup g = bar.GroupAt(n);
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
}
