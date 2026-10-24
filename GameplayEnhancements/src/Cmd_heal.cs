// heal, cleanse, bless: slot 5 of the spell bar (healing and blessings). Own game / own world only.
// None of these touches the console, god mode or any cheat flag: plain Player/Character/SEMan methods.
// C# 5 (csc 4.0). Build: bash build-cmd.sh heal
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HomeCheat
{
    // heal [hp=<n, 0 = to full>] [stamina=<n, 0 = to full>] [eitr=<n, 0 = to full>] [only=hp|stamina|eitr]
    public class HealCommand : IHomeCommand
    {
        public string Name { get { return "heal"; } }
        public string Help { get { return "heal [hp=<n, default to full>] [stamina=<n, default to full>] [eitr=<n, default to full>] [only=hp|stamina|eitr] - the local player"; } }

        public string Run(Dictionary<string, string> a)
        {
            Player p = HomeApi.P();
            if (p.IsDead()) return "ERROR: you are dead";
            string only = HomeApi.S(a, "only", "").ToLowerInvariant();
            StringBuilder sb = new StringBuilder("healed:");
            if (only.Length == 0 || only == "hp")
            {
                float before = p.GetHealth(), max = p.GetMaxHealth();
                float want = HomeApi.F(a, "hp", 0f);
                float add = want > 0f ? want : max - before;
                if (add > 0f) p.Heal(add, true);
                sb.Append(" hp ").Append(before.ToString("F0")).Append(" -> ").Append(p.GetHealth().ToString("F0")).Append(" / ").Append(max.ToString("F0")).Append(";");
            }
            if (only.Length == 0 || only == "stamina")
            {
                float before = p.GetStamina(), max = p.GetMaxStamina();
                float want = HomeApi.F(a, "stamina", 0f);
                p.AddStamina(want > 0f ? want : max - before);
                sb.Append(" stamina ").Append(before.ToString("F0")).Append(" -> ").Append(p.GetStamina().ToString("F0")).Append(" / ").Append(max.ToString("F0")).Append(";");
            }
            if ((only.Length == 0 || only == "eitr") && p.GetMaxEitr() > 0f)
            {
                float before = p.GetEitr(), max = p.GetMaxEitr();
                float want = HomeApi.F(a, "eitr", 0f);
                p.AddEitr(want > 0f ? want : max - before);
                sb.Append(" eitr ").Append(before.ToString("F0")).Append(" -> ").Append(p.GetEitr().ToString("F0")).Append(" / ").Append(max.ToString("F0")).Append(";");
            }
            return sb.ToString();
        }
    }

    // cleanse [names=Burning,Poison,...] [all=1] [list=1]
    // Removes harmful status effects from the local player. Default list: Burning, Poison, Frost, Lightning, Spirit, Tared, Smoked, Wet.
    // names= replaces the list (case-insensitive, matched against the effect's prefab name and its name key); all=1 removes everything.
    public class CleanseCommand : IHomeCommand
    {
        public string Name { get { return "cleanse"; } }
        public string Help { get { return "cleanse [names=Burning,Poison,Frost,...] [all=1 (every status effect)] [list=1 (print your current effects, change nothing)]"; } }

        static readonly string[] Defaults = new string[] { "Burning", "Poison", "Frost", "Lightning", "Spirit", "Tared", "Smoked", "Wet" };

        public string Run(Dictionary<string, string> a)
        {
            Player p = HomeApi.P();
            SEMan sem = p.GetSEMan();
            if (sem == null) return "ERROR: no status effect manager";
            if (HomeApi.Flag(a, "list", false)) return Describe(sem);
            if (HomeApi.Flag(a, "all", false))
            {
                int n = sem.GetStatusEffects().Count;
                sem.RemoveAllStatusEffects(false);
                return "cleanse: removed all " + n + " status effects";
            }
            string given = HomeApi.S(a, "names", "");
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

    // bless name=<boss power> [duration=<s>] [mode=effect|power] | list=1 [filter=<text>]
    // name matches the prefab name of a guardian-power status effect (GP_Eikthyr, GP_TheElder, GP_Bonemass, ...): "bonemass", "elder", "gp_moder".
    // mode=effect (default) applies the status effect straight to you; mode=power equips it as your guardian power and uses it,
    // so the game's own cooldown applies. The names are read from this game's ObjectDB at run time (list=1), nothing is hard-coded.
    public class BlessCommand : IHomeCommand
    {
        public string Name { get { return "bless"; } }
        public string Help { get { return "bless name=<bonemass|eikthyr|elder|moder|yagluth|queen|...> [duration=<s, default the effect's own>] [mode=effect|power] | list=1 [filter=<text>] (status effects of this game)"; } }

        public string Run(Dictionary<string, string> a)
        {
            if (ObjectDB.instance == null) return "ERROR: ObjectDB not ready (load a world)";
            if (HomeApi.Flag(a, "list", false)) return List(HomeApi.S(a, "filter", "gp_"));
            string want = HomeApi.S(a, "name", "").ToLowerInvariant();
            if (want.Length == 0) return "ERROR: name required, e.g. name=bonemass (bless list=1 shows the names)";
            Player p = HomeApi.P();
            StatusEffect se = Find(want);
            if (se == null) return "ERROR: no status effect matching '" + want + "' (bless list=1)";
            string mode = HomeApi.S(a, "mode", "effect").ToLowerInvariant();
            string clean = se.name.Replace("(Clone)", "");
            if (mode == "power")
            {
                p.SetGuardianPower(clean);
                bool ok = p.StartGuardianPower();
                return ok ? "bless: guardian power " + clean + " used (the game's cooldown applies)"
                          : "bless: guardian power " + clean + " equipped, but not started (cooldown or conditions)";
            }
            StatusEffect added = p.GetSEMan().AddStatusEffect(se.NameHash(), true, 0, 0f, (short)0);
            if (added == null) return "ERROR: the game refused the effect " + clean;
            float dur = HomeApi.F(a, "duration", 0f);
            if (dur > 0f) added.m_ttl = dur;
            return "bless: " + clean + " applied for " + (added.m_ttl > 0f ? added.m_ttl.ToString("F0") + " s" : "its own time");
        }

        // exact prefab name, then gp_<name>, then any GP_ effect containing the text, then any effect containing it
        static StatusEffect Find(string want)
        {
            StatusEffect partialGp = null, partialAny = null;
            foreach (StatusEffect se in ObjectDB.instance.m_StatusEffects)
            {
                if (se == null) continue;
                string n = se.name.ToLowerInvariant();
                if (n == want || n == "gp_" + want) return se;
                if (n.StartsWith("gp_") && n.Contains(want) && partialGp == null) partialGp = se;
                else if (n.Contains(want) && partialAny == null) partialAny = se;
            }
            return partialGp != null ? partialGp : partialAny;
        }

        static string List(string filter)
        {
            StringBuilder sb = new StringBuilder();
            List<string> names = new List<string>();
            foreach (StatusEffect se in ObjectDB.instance.m_StatusEffects)
                if (se != null && se.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    names.Add(se.name + "  key=" + se.m_name + "  ttl=" + se.m_ttl.ToString("F0"));
            names.Sort();
            int n = 0;
            foreach (string s in names) { if (n++ >= 150) { sb.Append("... (more)\n"); break; } sb.Append(s).Append('\n'); }
            return names.Count == 0 ? "no status effect matches '" + filter + "'" : sb.ToString();
        }
    }
}
