using System;
using System.Collections.Generic;

namespace HomeCheat.Features.Spells
{
    // one question of an answer chain: ask=<key>:<Label=value,value,...>  or  ask=<key>:@players|@crops|@trees|@prefabs
    internal sealed class SpellAsk
    {
        public string Key = "";
        public string Source = "";                                   // "" = the fixed options below; else @players, @crops, @trees, @prefabs
        public List<SpellAskOption> Options = new List<SpellAskOption>();
    }
}
