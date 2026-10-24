// heal, cleanse, bless: slot 5 of the spell bar (healing and blessings). Own game / own world only.
// None of these touches the console, god mode or any cheat flag: plain Player/Character/SEMan methods.
using System.Text;
using HomeCheat.Kit;

namespace HomeCheat.Features.Heal
{
    // heal: health, stamina and eitr of the local player, to full or by an amount
    public sealed class HealCommand : CommandBase
    {
        public override string Name { get { return "heal"; } }
        protected override string Usage { get { return "heal [hp=<n, default to full>] [stamina=<n, default to full>] [eitr=<n, default to full>] [only=hp|stamina|eitr] - the local player"; } }

        protected override string Execute(Args a)
        {
            Player p = Players.Local;
            if (p.IsDead()) throw new CommandException("you are dead");
            string only = a.Str("only", "").ToLowerInvariant();
            StringBuilder sb = new StringBuilder("healed:");
            if (only.Length == 0 || only == "hp")
            {
                float before = p.GetHealth(), max = p.GetMaxHealth();
                float want = a.Float("hp", 0f);
                float add = want > 0f ? want : max - before;
                if (add > 0f) p.Heal(add, true);
                sb.Append(" hp ").Append(before.ToString("F0")).Append(" -> ").Append(p.GetHealth().ToString("F0")).Append(" / ").Append(max.ToString("F0")).Append(";");
            }
            if (only.Length == 0 || only == "stamina")
            {
                float before = p.GetStamina(), max = p.GetMaxStamina();
                float want = a.Float("stamina", 0f);
                p.AddStamina(want > 0f ? want : max - before);
                sb.Append(" stamina ").Append(before.ToString("F0")).Append(" -> ").Append(p.GetStamina().ToString("F0")).Append(" / ").Append(max.ToString("F0")).Append(";");
            }
            if ((only.Length == 0 || only == "eitr") && p.GetMaxEitr() > 0f)
            {
                float before = p.GetEitr(), max = p.GetMaxEitr();
                float want = a.Float("eitr", 0f);
                p.AddEitr(want > 0f ? want : max - before);
                sb.Append(" eitr ").Append(before.ToString("F0")).Append(" -> ").Append(p.GetEitr().ToString("F0")).Append(" / ").Append(max.ToString("F0")).Append(";");
            }
            return sb.ToString();
        }
    }
}
