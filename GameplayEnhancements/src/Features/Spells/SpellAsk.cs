using System;
using System.Collections.Generic;

namespace HomeCheat.Features.Spells
{
    // one question of an answer chain: ask=<key>:<Label=value,value,...>  or  ask=<key>:@players|@crops|@trees|@prefabs
    internal sealed class SpellAsk
    {
        public string Key = "";
        public string Label = "";                                    // the name on the form: ask=key/Label_text:...; "" = from the key
        public string GreaterThan = "";                              // ask=key>other:... = only answers above the answer of 'other'
        public string WhenKey = "", WhenValue = "";                  // ask=key?other=value:... = asked only while 'other' is answered 'value'
                                                                     // (greyed otherwise and left out of the command; Maksym 2026-10-25)

        public string Source = "";                                   // "" = the fixed options below; else @players, @crops, @trees, @prefabs
        public List<SpellAskOption> Options = new List<SpellAskOption>();
    }
}
