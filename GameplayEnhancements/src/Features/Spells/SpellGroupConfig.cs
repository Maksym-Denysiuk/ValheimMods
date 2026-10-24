using System;
using System.Collections.Generic;
using UnityEngine;
using Valheim.UI;

namespace HomeCheat.Features.Spells
{
    // one group: its spells as leaves
    sealed class SpellGroupConfig : IRadialConfig
    {
        readonly SpellBar bar; readonly int slot;
        public SpellGroupConfig(SpellBar b, int s) { bar = b; slot = s; }
        public string LocalizedName { get { SpellGroup g = bar.GroupAt(slot); return g == null ? "?" : g.Title; } }
        public Sprite Sprite { get { return SpellBar.IconFor(SpellBar.SlotIcon(slot)); } }

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
            SpellGroup g = bar.GroupAt(slot);
            List<RadialMenuElement> els = new List<RadialMenuElement>();
            if (g != null)
                for (int e = 1; e <= 8; e++)
                {
                    Spell s = g.Entries[e];
                    if (s == null) continue;
                    els.Add(bar.MakeLeaf(s, SpellsSettings.ActiveBar.Root(), radial, Sprite, bar.SubtitleFor(s, slot + "." + e)));
                }
            radial.ConstructRadial(els);
        }
    }
}
