using System;
using System.Collections.Generic;

namespace HomeCheat.Features.Spells
{
    // one place in the menu tree: a title (group or sub-section, with children) or a spell (a leaf). Replaces v3's SpellGroup.
    internal sealed class SpellNode
    {
        public string Id = "";
        public int Number;                                        // last number of the id: the order among its siblings
        public string Title = "";
        public bool Sticky;                                       // sticky on a title = every spell below it
        public bool Declared;                                     // a title or spell line exists (else made for a deeper id)
        public Spell Spell;                                       // null = title
        public List<SpellNode> Children = new List<SpellNode>();  // sorted by Number
    }
}
