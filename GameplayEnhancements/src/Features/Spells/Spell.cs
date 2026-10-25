using System;
using System.Collections.Generic;

namespace HomeCheat.Features.Spells
{
    // one spell of the spell file: id | label | command | flags   (id = dotted path of any depth, 2.1 or 6.1.3)
    internal sealed class Spell
    {
        public string Id = "";
        public int Top;                 // first number of the id: the root section (its icon)
        public string Label = "", Cmd = "", Shift = "", Pin = "";
        public bool Sticky;             // own flag, or a sticky title above it (resolved by SpellParse)
        public List<SpellAsk> Asks = new List<SpellAsk>();   // answer chain, in the order of the ask= flags
        public float Next;              // cooldown
    }
}
