using System;
using System.Collections.Generic;
using UnityEngine;
using Valheim.UI;

namespace HomeCheat.Features.Spells
{
    // One Hotbar panel (Maksym 2026-10-25): Panel 1-5 = Shift+1..5 and F1..F5, Panel 6-= = Shift+6..Shift+= and F6..F12, as radial
    // sections (more than the game's ring size continue as its spiral). Click = cast, Shift+RMB = empty the cell, and while a spell
    // waits to be placed (Shift+click on it in a group) a click puts it into the clicked cell.
    sealed class SpellHotbarConfig : IRadialConfig
    {
        readonly SpellBar bar;
        readonly int panel;
        public SpellHotbarConfig(SpellBar b, int panelIndex) { bar = b; panel = panelIndex; }
        public string LocalizedName { get { return SpellParse.PanelNames[panel]; } }
        public Sprite Sprite { get { return SpellBar.IconFor("hotbar"); } }

        public void InitRadialConfig(RadialBase radial)
        {
            if (bar == null || SpellsSettings.ActiveBar != bar) { SpellPatches.OpenVanilla(radial); return; }   // config of a build that was replaced by op=reload
            try
            {
                List<RadialMenuElement> els = new List<RadialMenuElement>();
                bar.ClearCellMap();
                bar.SuppressBackOnShift(radial);
                foreach (string cell in SpellFile.PinOrder)
                    if (SpellParse.PanelOf(cell) == panel) els.Add(bar.MakeCellElement(cell, new SpellPanelsConfig(bar), radial));
                radial.ConstructRadial(els);
            }
            catch (Exception e)
            {
                HomeApi.Log("spells: hotbar build failed, spells switched off: " + e);
                SpellsSettings.TurnOff();
                try { radial.Open(RadialData.SO.MainGroupConfig, null); } catch (Exception e2) { HomeApi.Log("spells: vanilla fallback failed: " + e2.Message); }
            }
        }
    }
}
