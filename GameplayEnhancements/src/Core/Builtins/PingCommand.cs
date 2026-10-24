namespace HomeCheat.Builtins
{
    internal sealed class PingCommand : CommandBase
    {
        public override string Name { get { return "ping"; } }
        protected override string Usage { get { return "ping - answers pong (the channel works)"; } }
        protected override string Execute(Args a) { return "pong"; }
    }
}
