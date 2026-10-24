using System.Collections.Generic;
using System.Text;
using HomeCheat.Services;

namespace HomeCheat.Builtins
{
    // op=harmeffect&kind=<name>: the same as op=<name>, restricted to HarmEffect commands
    internal sealed class HarmEffectCommand : CommandBase
    {
        readonly CommandRegistry registry;
        public HarmEffectCommand(CommandRegistry r) { registry = r; }

        public override string Name { get { return "harmeffect"; } }
        protected override string Usage { get { return "harmeffect kind=<name> [parameters of that effect] - runs one of the loaded harm effects"; } }

        protected override string Execute(Args a)
        {
            string kind = a.Str("kind", "").ToLowerInvariant();
            IHomeCommand he;
            if (registry.TryGet(kind, out he) && he is HarmEffect) return he.Run(a.Raw);
            StringBuilder kinds = new StringBuilder();
            foreach (IHomeCommand c in registry.All()) if (c is HarmEffect) kinds.Append(c.Name).Append(' ');
            throw new CommandException("kind required; loaded harm effects: " + (kinds.Length == 0 ? "(none, op=reload)" : kinds.ToString().Trim()));
        }
    }
}
