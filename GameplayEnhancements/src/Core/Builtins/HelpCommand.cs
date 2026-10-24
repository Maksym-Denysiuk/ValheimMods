using HomeCheat.Services;

namespace HomeCheat.Builtins
{
    internal sealed class HelpCommand : CommandBase
    {
        readonly CommandRegistry registry;
        public HelpCommand(CommandRegistry r) { registry = r; }

        public override string Name { get { return "help"; } }
        protected override string Usage { get { return "help cmd=<name> - the full help of one command (op=commands lists them all)"; } }

        protected override string Execute(Args a)
        {
            string name = a.Required("cmd", "e.g. cmd=earthquake (op=commands lists the names)");
            IHomeCommand c;
            if (!registry.TryGet(name, out c)) throw new CommandException("unknown command '" + name + "'. ops: " + string.Join(" ", registry.Names().ToArray()));
            return c.Name + " [" + registry.FeatureOf(c.Name) + "]: " + c.Help;
        }
    }
}
