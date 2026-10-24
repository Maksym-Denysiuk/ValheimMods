using HomeCheat.Services;

namespace HomeCheat.Builtins
{
    internal sealed class ReloadCommand : CommandBase
    {
        readonly CommandRegistry registry;
        public ReloadCommand(CommandRegistry r) { registry = r; }

        public override string Name { get { return "reload"; } }
        protected override string Usage { get { return "reload - loads command DLLs newer than the loaded build of their feature (no game restart); the replaced build is unloaded"; } }
        protected override string Execute(Args a) { return registry.Reload(); }
    }
}
