using System;
using System.Collections.Generic;
using System.Text;
using HomeCheat.Kit;

namespace HomeCheat.Features.Heal
{
    // cleanse: removes harmful status effects from the local player. Default list: Burning, Poison, Frost, Lightning, Spirit,
    // Tared, Smoked, Wet. names= replaces the list (case-insensitive, matched against the effect's prefab name and its name key).
    public sealed class CleanseCommand : CommandBase
    {
        static readonly string[] Defaults = new string[] { "Burning", "Poison", "Frost", "Lightning", "Spirit", "Tared", "Smoked", "Wet" };

        public CleanseCommand()
        {
            SubFlag("list", "list=1 (print your current effects, change nothing)", delegate (Args a) { return Describe(Sem()); });
            SubFlag("all", "all=1 (every status effect)", delegate (Args a)
            {
                SEMan sem = Sem();
                int n = sem.GetStatusEffects().Count;
                sem.RemoveAllStatusEffects(false);
                return "cleanse: removed all " + n + " status effects";
            });
        }

        public override string Name { get { return "cleanse"; } }
        protected override string Usage { get { return "cleanse [names=Burning,Poison,Frost,...]"; } }

        static SEMan Sem()
        {
            SEMan sem = Players.Local.GetSEMan();
            if (sem == null) throw new CommandException("no status effect manager");
            return sem;
        }

        protected override string Execute(Args a)
        {
            SEMan sem = Sem();
            string given = a.Str("names", "");
            string[] names = given.Length > 0 ? given.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries) : Defaults;
            List<StatusEffect> now = new List<StatusEffect>(sem.GetStatusEffects());
            StringBuilder sb = new StringBuilder();
            int removed = 0;
            foreach (StatusEffect se in now)
            {
                if (se == null || !Matches(se, names)) continue;
                if (sem.RemoveStatusEffect(se, false)) { removed++; sb.Append(' ').Append(se.name.Replace("(Clone)", "")); }
            }
            return "cleanse: removed " + removed + (removed > 0 ? " (" + sb.ToString().Trim() + ")" : "") + " of " + now.Count + " effects";
        }

        static bool Matches(StatusEffect se, string[] names)
        {
            string n1 = se.name == null ? "" : se.name.ToLowerInvariant();
            string n2 = se.m_name == null ? "" : se.m_name.ToLowerInvariant();
            foreach (string raw in names)
            {
                string w = raw.Trim().ToLowerInvariant();
                if (w.Length == 0) continue;
                if (n1.Contains(w) || n2.Contains(w)) return true;
            }
            return false;
        }

        static string Describe(SEMan sem)
        {
            StringBuilder sb = new StringBuilder("current status effects:\n");
            foreach (StatusEffect se in sem.GetStatusEffects())
                if (se != null) sb.Append("  ").Append(se.name.Replace("(Clone)", "")).Append("  key=").Append(se.m_name).Append("  ttl=").Append(se.m_ttl.ToString("F0")).Append('\n');
            return sb.ToString();
        }
    }
}
