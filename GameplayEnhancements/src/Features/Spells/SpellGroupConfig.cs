using System;
using System.Collections.Generic;
using UnityEngine;
using Valheim.UI;

namespace HomeCheat.Features.Spells
{
    // one group or sub-section (a title of the spell file) at any depth: its entries as leaves (spells) or GroupElements (sub-sections).
    // Any number of entries: RadialBase.ConstructRadial lays them out in layers (the spiral) past MaxElementsPerLayer.
    sealed class SpellGroupConfig : IRadialConfig
    {
        readonly SpellBar bar; readonly string id;
        public SpellGroupConfig(SpellBar b, string nodeId) { bar = b; id = nodeId; }
        public string LocalizedName { get { SpellNode n = bar.NodeAt(id); return n == null ? "?" : n.Title; } }
        public Sprite Sprite { get { return SpellBar.IconFor(SpellBar.SlotIcon(SpellBar.TopOf(id))); } }

        public void InitRadialConfig(RadialBase radial)
        {
            if (bar == null || SpellsSettings.ActiveBar != bar) { SpellPatches.OpenVanilla(radial); return; }   // config of a build that was replaced by op=reload
            try { Build(radial); }
            catch (Exception e)
            {
                HomeApi.Log("spells: group build failed, spells switched off: " + e);
                SpellsSettings.TurnOff();
                try { radial.Open(RadialData.SO.MainGroupConfig, null); } catch (Exception e2) { HomeApi.Log("spells: vanilla fallback failed: " + e2.Message); }
            }
        }

        void Build(RadialBase radial)
        {
            SpellNode n = bar.NodeAt(id);
            List<RadialMenuElement> els = new List<RadialMenuElement>();
            if (n != null && n.Spell == null)
                foreach (SpellNode c in n.Children) els.Add(bar.MakeNodeElement(c, this, radial));
            // a title with nothing below it (Construct), or a group that vanished on a file reload: one note, never an empty radial
            if (els.Count == 0) els.Add(bar.MakeNote("(nothing here yet)", id + ": add lines " + id + ".1, " + id + ".2 ... to the spell file", this, radial));
            radial.ConstructRadial(els);
        }
    }
}
