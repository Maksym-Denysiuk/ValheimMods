using System;

namespace HomeCheat.Features.Spells
{
    // one answer of a question: the label shown in the radial, the value put into {key}; Group = category of a dynamic source (@prefabs)
    internal sealed class SpellAskOption
    {
        public string Label = "", Value = "", Group = "";
        public bool Off;   // value @off: shown greyed on the form, never picked (a choice that is planned, not built)
        public bool Default;   // written *value or *Label=value: the answer the form starts on (else the first one)
        public SpellAskOption(string label, string value, string group) { Label = label; Value = value; Group = group ?? ""; Off = value == "@off"; }
    }
}
