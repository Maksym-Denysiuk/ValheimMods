using System;
using System.Collections.Generic;

namespace HomeCheat.Features.Spells
{
    // one run of a spell's questions: made when the spell is picked, filled on the one-screen form (SpellForm), cast with Cast.
    // Esc or Cancel drops it, nothing is cast. Skip: questions left out of the command (a quantity when placing by coordinates).
    internal sealed class SpellAskChain
    {
        public readonly Spell Spell;
        public readonly bool Shift;      // cast from an F-key pin with Shift held: the shift= form is added
        public readonly string PinKey;   // the bar cell that started it (for the flash), or null
        public readonly Dictionary<string, string> Answers = new Dictionary<string, string>();
        public readonly HashSet<string> Skip = new HashSet<string>();
        public SpellAskChain(Spell s, bool shift, string pinKey) { Spell = s; Shift = shift; PinKey = pinKey; }
    }
}
