using System;

namespace HomeCheat.Features.Spells
{
    // one radial slot: either itself a spell (Direct) or a group of up to 8 spells
    internal sealed class SpellGroup
    {
        public string Title = "";
        public Spell Direct;                         // slot is itself a spell
        public Spell[] Entries = new Spell[9];       // 1..8
        public bool HasEntries { get { for (int i = 1; i <= 8; i++) if (Entries[i] != null) return true; return false; } }
    }
}
