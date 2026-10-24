using System;
using System.Collections.Generic;
using UnityEngine;
using Valheim.UI;

namespace HomeCheat.Features.Spells
{
    // Hotbar: the 10 cells of the action bar as radial sections (Shift+1..5, F1..F5). Click = cast, RMB = empty the cell,
    // and while a spell waits to be placed (Shift+click on it in a group) a click puts it into the clicked cell.
    sealed class SpellHotbarConfig : IRadialConfig
    {
        readonly SpellBar bar;
        public SpellHotbarConfig(SpellBar b) { bar = b; }
        public string LocalizedName { get { return "Hotbar"; } }
        public Sprite Sprite { get { return SpellBar.IconFor("hotbar"); } }

        public void InitRadialConfig(RadialBase radial)
        {
            if (bar == null || SpellsSettings.ActiveBar != bar) { SpellPatches.OpenVanilla(radial); return; }   // config of a build that was replaced by op=reload
            try
            {
                List<RadialMenuElement> els = new List<RadialMenuElement>();
                bar.ClearCellMap();
                bar.SuppressBackOnShift(radial);
                foreach (string cell in SpellFile.PinOrder) els.Add(bar.MakeCellElement(cell, SpellsSettings.ActiveBar.Root(), radial));
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
