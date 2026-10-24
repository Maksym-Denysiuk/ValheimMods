using System;

namespace HomeCheat.Features.Spells
{
    // one spell of the spell file: slot.entry | label | command | flags
    internal sealed class Spell
    {
        public int Slot, Entry;
        public string Label = "", Cmd = "", Shift = "", Pin = "";
        public bool Sticky;
        public float Next;      // cooldown
    }
}
