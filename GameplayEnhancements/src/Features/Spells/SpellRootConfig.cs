using System;
using System.Collections.Generic;
using UnityEngine;
using Valheim.UI;

namespace HomeCheat.Features.Spells
{
    // ---- the radial configs: our subclasses of the game's extension point ----
    // root level: section 1 is our Hotbar (its two panels of bar cells), 2-8 are our sections (or empty), more sections continue the spiral.
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
            // 1: Hotbar = the two panels of bar cells (replaces the game's item hotbar group in this radial)
            GroupElement hb = UnityEngine.Object.Instantiate<GroupElement>(RadialData.SO.GroupElement);
            hb.Init(new SpellPanelsConfig(bar), this, radial);
            els.Add(hb);
            // 2..8 keep their places (a missing section is an empty slot); sections above 8 follow in order (the spiral takes them)
            int next = 2;
            foreach (SpellNode n in bar.RootNode().Children)
            {
                if (n.Number < 2) continue;
                for (; next < n.Number && next <= 8; next++) els.Add(MakeEmpty());
                next = n.Number + 1;
                els.Add(bar.MakeNodeElement(n, this, radial));
            }
            for (; next <= 8; next++) els.Add(MakeEmpty());
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
