using System;
using System.Collections.Generic;
using UnityEngine;
using Valheim.UI;

namespace HomeCheat.Features.Spells
{
    // one question of an answer chain as a radial level. Its answers are leaves: a click stores the answer and opens the next
    // question (QueuedOpen, this level on the Back stack), or casts after the last one. Long lists: categories first (@prefabs),
    // then alphabetical blocks of at most MaxPerLevel answers, each block a sub-level of the same question.
    sealed class SpellAskConfig : IRadialConfig
    {
        public const int MaxPerLevel = 40;

        readonly SpellBar bar; readonly SpellAskChain chain; readonly int step;
        readonly List<SpellAskOption> subset;   // null = all answers of the question; else one category or block of it
        readonly string part;                    // the category or block name shown in the title

        public SpellAskConfig(SpellBar b, SpellAskChain c, int st, List<SpellAskOption> sub, string p) { bar = b; chain = c; step = st; subset = sub; part = p; }

        SpellAsk Ask { get { return chain.Spell.Asks[step]; } }
        public string LocalizedName { get { return chain.Spell.Label + ": " + Ask.Key + (part != null ? " " + part : "") + "?"; } }
        public Sprite Sprite { get { return SpellBar.IconFor(SpellBar.SlotIcon(chain.Spell.Top)); } }

        public void InitRadialConfig(RadialBase radial)
        {
            if (bar == null || SpellsSettings.ActiveBar != bar) { SpellPatches.OpenVanilla(radial); return; }   // config of a build that was replaced by op=reload
            try { Build(radial); }
            catch (Exception e)
            {
                HomeApi.Log("spells: question level failed, spells switched off: " + e);
                SpellsSettings.TurnOff();
                try { radial.Open(RadialData.SO.MainGroupConfig, null); } catch (Exception e2) { HomeApi.Log("spells: vanilla fallback failed: " + e2.Message); }
            }
        }

        void Build(RadialBase radial)
        {
            List<RadialMenuElement> els = new List<RadialMenuElement>();
            List<SpellAskOption> opts = subset != null ? subset : SpellAskSources.Resolve(Ask);
            if (subset == null && HasGroups(opts))
            {
                // categories (@prefabs): one sub-level per group, in the order the source sorted them
                List<string> names = new List<string>();
                Dictionary<string, List<SpellAskOption>> by = new Dictionary<string, List<SpellAskOption>>();
                foreach (SpellAskOption o in opts)
                {
                    List<SpellAskOption> l;
                    if (!by.TryGetValue(o.Group, out l)) { l = new List<SpellAskOption>(); by[o.Group] = l; names.Add(o.Group); }
                    l.Add(o);
                }
                foreach (string n in names) els.Add(SubLevel(by[n], n + " (" + by[n].Count + ")", radial));
            }
            else
            {
                List<List<SpellAskOption>> blocks = SpellParse.Split(opts, MaxPerLevel);
                if (blocks.Count == 1) foreach (SpellAskOption o in opts) els.Add(Answer(o, radial));
                else foreach (List<SpellAskOption> b in blocks) els.Add(SubLevel(b, SpellParse.BlockTitle(b), radial));
            }
            if (els.Count == 0) els.Add(bar.MakeNote("(no answers)", "nothing found for " + Ask.Source + "; right click goes back", this, radial));
            radial.ConstructRadial(els);
        }

        static bool HasGroups(List<SpellAskOption> opts)
        {
            foreach (SpellAskOption o in opts) if (o.Group.Length > 0) return true;
            return false;
        }

        RadialMenuElement SubLevel(List<SpellAskOption> opts, string name, RadialBase radial)
        {
            GroupElement ge = UnityEngine.Object.Instantiate<GroupElement>(RadialData.SO.GroupElement);
            ge.Init(new SpellAskConfig(bar, chain, step, opts, name), this, radial);
            return ge;
        }

        RadialMenuElement Answer(SpellAskOption o, RadialBase radial)
        {
            bool last = step + 1 >= chain.Spell.Asks.Count;
            string key = Ask.Key, value = o.Value;
            GroupElement ge = UnityEngine.Object.Instantiate<GroupElement>(RadialData.SO.GroupElement);
            ge.Init(new SpellLeafConfig(o.Label, Sprite), this, radial);
            SpellBar.SetSubTitle(ge, "question " + (step + 1) + "/" + chain.Spell.Asks.Count + ": " + key + "=" + value + (last ? "   click: cast" : "   right click: back"));
            ge.Interact = delegate()
            {
                chain.Answers[key] = value;
                if (last) bar.CastChain(chain);
                else bar.OpenLevel(radial, new SpellAskConfig(bar, chain, step + 1, null, null), this);
                return true;
            };
            ge.CloseOnInteract = delegate() { return last && !chain.Spell.Sticky; };
            return ge;
        }
    }
}
