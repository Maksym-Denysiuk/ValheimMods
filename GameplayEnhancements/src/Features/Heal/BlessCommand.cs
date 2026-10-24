using System;
using System.Collections.Generic;
using System.Text;
using HomeCheat.Kit;

namespace HomeCheat.Features.Heal
{
    // bless: a guardian-power status effect (GP_Eikthyr, GP_TheElder, GP_Bonemass, ...) by part of its name: "bonemass", "elder".
    // mode=effect (default) applies the status effect straight to you; mode=power equips it as your guardian power and uses it,
    // so the game's own cooldown applies. The names are read from this game's ObjectDB at run time, nothing is hard-coded.
    public sealed class BlessCommand : CommandBase
    {
        public BlessCommand()
        {
            SubFlag("list", "list=1 [filter=<text>] (status effects of this game)", delegate (Args a) { RequireDb(); return List(a.Str("filter", "gp_")); });
        }

        public override string Name { get { return "bless"; } }
        protected override string Usage { get { return "bless name=<bonemass|eikthyr|elder|moder|yagluth|queen|...> [duration=<s, default the effect's own>] [mode=effect|power]"; } }

        static void RequireDb()
        {
            if (ObjectDB.instance == null) throw new CommandException("ObjectDB not ready (load a world)");
        }

        protected override string Execute(Args a)
        {
            RequireDb();
            string want = a.Required("name", "e.g. name=bonemass (bless list=1 shows the names)").ToLowerInvariant();
            Player p = Players.Local;
            StatusEffect se = Find(want);
            if (se == null) throw new CommandException("no status effect matching '" + want + "' (bless list=1)");
            string mode = a.Str("mode", "effect").ToLowerInvariant();
            string clean = se.name.Replace("(Clone)", "");
            if (mode == "power")
            {
                p.SetGuardianPower(clean);
                bool ok = p.StartGuardianPower();
                return ok ? "bless: guardian power " + clean + " used (the game's cooldown applies)"
                          : "bless: guardian power " + clean + " equipped, but not started (cooldown or conditions)";
            }
            StatusEffect added = p.GetSEMan().AddStatusEffect(se.NameHash(), true, 0, 0f, (short)0);
            if (added == null) throw new CommandException("the game refused the effect " + clean);
            float dur = a.Float("duration", 0f);
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
            List<string> names = new List<string>();
            foreach (StatusEffect se in ObjectDB.instance.m_StatusEffects)
                if (se != null && se.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    names.Add(se.name + "  key=" + se.m_name + "  ttl=" + se.m_ttl.ToString("F0"));
            names.Sort();
            return names.Count == 0 ? "no status effect matches '" + filter + "'" : Prefabs.Lines(names, 150, "... (more)\n");
        }
    }
}
