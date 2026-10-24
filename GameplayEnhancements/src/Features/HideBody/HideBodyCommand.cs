// op=hidebody: hides the local player's human body from OTHER players on our own world.
// While this game sends the local player's ZDO to other peers, the position written into the ZDO header gets y = 10000
// (viewers draw the remote player 10 km up); the real value is written back right after each send. The map position flag
// of the player list is switched off for the same calls. Default off; off again on unload (reload) and on quit.
// See docs/Transform/hidebody.md for the verified facts and what is not verified.
using System;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.HideBody
{
    public sealed class HideBodyCommand : CommandBase
    {
        // the loaded instance, for the patch classes; null while unloaded
        internal static HideBodyCommand Current;
        static bool quitHooked;

        internal bool On;
        internal float LiftY = 10000f;
        internal int Lifts, PubOff;

        public HideBodyCommand()
        {
            SubFlag("status", "status=1", delegate (Args a) { return Status(); });
        }

        public override string Name { get { return "hidebody"; } }
        protected override string Usage { get { return "hidebody on=1|0 [y=10000] - hides your body from other players (the player object is sent 10 km up, the map pin is off); off by default, restored on off, on reload and on quit"; } }

        protected override void OnLoad()
        {
            Current = this;
            // one handler per loaded build; after an unload its Current is null and it does nothing
            if (!quitHooked) { quitHooked = true; Application.quitting += delegate { if (Current != null) Current.Disable("game quitting"); }; }
        }

        protected override void OnUnload()
        {
            Disable("unloaded by a newer build");
            if (Current == this) Current = null;
        }

        protected override string Execute(Args a)
        {
            string on = a.Str("on", "").ToLowerInvariant();
            if (on.Length == 0) return Status() + "\nuse op=hidebody&on=1 or on=0";
            if (on == "1" || on == "true" || on == "yes" || on == "on")
            {
                if (Player.m_localPlayer == null) throw new CommandException("no local player (load a world first)");
                LiftY = a.Float("y", 10000f);
                return Enable();
            }
            Disable("op=hidebody&on=0");
            return "hidebody OFF: patches removed, nothing left changed\n" + Status();
        }

        string Enable()
        {
            if (!Patches.IsActive)
            {
                Patches.Apply(typeof(HideBodySendZdosPatch));
                Patches.Apply(typeof(HideBodyPlayerListPatch));
                Patches.Apply(typeof(HideBodySyncDataPatch));
            }
            On = true;
            HomeApi.Log("hidebody: ON (player ZDO sent at y=" + LiftY.ToString("F0") + ", map position off)");
            return "hidebody ON: other players should now see your body " + LiftY.ToString("F0") + " m up and no map pin. Revert: op=hidebody&on=0\n" + Status();
        }

        internal void Disable(string why)
        {
            bool was = On || Patches.IsActive;
            On = false;
            Patches.RemoveAll();
            if (was) HomeApi.Log("hidebody: OFF (" + why + ")");
        }

        string Status()
        {
            StringBuilder sb = new StringBuilder("hidebody: ").Append(On ? "ON" : "off").Append(", patches installed=").Append(Patches.IsActive)
                .Append(", ZDO sends lifted=").Append(Lifts).Append(", player-list calls with pin off=").Append(PubOff).Append(", lift y=").Append(LiftY.ToString("F0"));
            Player p = Player.m_localPlayer;
            if (p != null)
            {
                ZNetView nv = p.GetComponent<ZNetView>();
                ZDO z = nv != null && nv.IsValid() ? nv.GetZDO() : null;
                sb.Append("\nlocal player ZDO ").Append(z == null ? "(none)" : z.m_uid.ToString() + " stored position " + Fmt.Vec(z.GetPosition()) + " (real, restored after each send)");
                sb.Append("\nthis game is ").Append(ZNet.instance != null && ZNet.instance.IsServer() ? "the host" : "a client");
                sb.Append("\nplayers online: ");
                foreach (Player pl in Player.GetAllPlayers()) sb.Append(pl.GetPlayerName()).Append("; ");
            }
            return sb.ToString();
        }

        // the loaded command is on: patches act only then
        internal static bool Active { get { return Current != null && Current.On; } }
    }
}
