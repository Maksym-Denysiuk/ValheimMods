using System;

namespace HomeCheat.Features.Spells
{
    // one answer of a question: the label shown in the radial, the value put into {key}; Group = category of a dynamic source (@prefabs)
    internal sealed class SpellAskOption
    {
        public string Label = "", Value = "", Group = "";
        public SpellAskOption(string label, string value, string group) { Label = label; Value = value; Group = group ?? ""; }
    }
}
