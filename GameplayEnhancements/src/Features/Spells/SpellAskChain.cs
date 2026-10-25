using System;
using System.Collections.Generic;

namespace HomeCheat.Features.Spells
{
    // one run of a spell's questions: made when the spell is picked, filled one answer per radial level, cast after the last one.
    // Back re-opens the previous level (its answer is simply overwritten); closing the radial drops the chain, nothing is cast.
    internal sealed class SpellAskChain
    {
        public readonly Spell Spell;
        public readonly bool Shift;      // cast from an F-key pin with Shift held: the shift= form is added
        public readonly string PinKey;   // the bar cell that started it (for the flash), or null
        public readonly Dictionary<string, string> Answers = new Dictionary<string, string>();
        public SpellAskChain(Spell s, bool shift, string pinKey) { Spell = s; Shift = shift; PinKey = pinKey; }
    }
}
