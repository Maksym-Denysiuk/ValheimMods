using System;
using System.Collections.Generic;
using UnityEngine;
using Valheim.UI;

namespace HomeCheat.Features.Spells
{
    // Hotbar (radial slot 1): the two panels, Panel 1-5 and Panel 6-= (Maksym 2026-10-25); each opens its cells (SpellHotbarConfig).
    sealed class SpellPanelsConfig : IRadialConfig
    {
        readonly SpellBar bar;
        public SpellPanelsConfig(SpellBar b) { bar = b; }
        public string LocalizedName { get { return "Hotbar"; } }
        public Sprite Sprite { get { return SpellBar.IconFor("hotbar"); } }

        public void InitRadialConfig(RadialBase radial)
        {
            if (bar == null || SpellsSettings.ActiveBar != bar) { SpellPatches.OpenVanilla(radial); return; }   // config of a build that was replaced by op=reload
            try
            {
                List<RadialMenuElement> els = new List<RadialMenuElement>();
                bar.ClearCellMap();
                for (int i = 0; i < SpellParse.PanelNames.Length; i++)
                {
                    GroupElement ge = UnityEngine.Object.Instantiate<GroupElement>(RadialData.SO.GroupElement);
                    ge.Init(new SpellHotbarConfig(bar, i), bar.Root(), radial);
                    els.Add(ge);
                }
                radial.ConstructRadial(els);
            }
            catch (Exception e)
            {
                HomeApi.Log("spells: hotbar panels failed, spells switched off: " + e);
                SpellsSettings.TurnOff();
                try { radial.Open(RadialData.SO.MainGroupConfig, null); } catch (Exception e2) { HomeApi.Log("spells: vanilla fallback failed: " + e2.Message); }
            }
        }
    }
}
