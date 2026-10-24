using System;
using System.Collections.Generic;
using UnityEngine;
using Valheim.UI;

namespace HomeCheat.Features.Spells
{
    // a leaf only carries a name and an icon; it never opens a level
    sealed class SpellLeafConfig : IRadialConfig
    {
        readonly string name; readonly Sprite sprite;
        public SpellLeafConfig(string n, Sprite s) { name = n; sprite = s; }
        public string LocalizedName { get { return name; } }
        public Sprite Sprite { get { return sprite; } }
        public void InitRadialConfig(RadialBase radial) { }
    }
}
