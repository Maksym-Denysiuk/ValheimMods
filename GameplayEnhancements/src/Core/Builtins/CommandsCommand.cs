using System.Text;
using HomeCheat.Services;

namespace HomeCheat.Builtins
{
    internal sealed class CommandsCommand : CommandBase
    {
        readonly CommandRegistry registry;
        public CommandsCommand(CommandRegistry r) { registry = r; }

        public override string Name { get { return "commands"; } }
        protected override string Usage { get { return "commands [builds=1 (the loaded DLL of each feature)] - every command with its feature and help"; } }

        protected override string Execute(Args a)
        {
            if (a.Flag("builds", false)) return registry.Describe();
            StringBuilder sb = new StringBuilder();
            foreach (IHomeCommand c in registry.All())
                sb.Append(c.Name).Append(" [").Append(registry.FeatureOf(c.Name)).Append("] - ").Append(c.Help).Append('\n');
            return sb.ToString();
        }
    }
}
