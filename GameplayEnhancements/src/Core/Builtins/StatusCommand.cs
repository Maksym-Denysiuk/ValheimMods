using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Builtins
{
    internal sealed class StatusCommand : CommandBase
    {
        public override string Name { get { return "status"; } }
        protected override string Usage { get { return "status - player name and position, world, host or client"; } }

        protected override string Execute(Args a)
        {
            Player p = Players.Local;
            return "player=" + p.GetPlayerName() + " pos=" + Fmt.Vec(p.transform.position)
                + " isServer=" + ZNet.instance.IsServer() + " world=" + ZNet.instance.GetWorldName()
                + " dedicated=" + ZNet.instance.IsDedicated();
        }
    }
}
