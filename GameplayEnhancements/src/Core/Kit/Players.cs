// The local player and other players by nickname.
using System;
using UnityEngine;

namespace HomeCheat.Kit
{
    public static class Players
    {
        // the local player, or a CommandException when no world is loaded
        public static Player Local
        {
            get
            {
                if (Player.m_localPlayer == null) throw new CommandException("no local player: load a world first");
                return Player.m_localPlayer;
            }
        }

        public static Player Find(string name)
        {
            foreach (Player pl in Player.GetAllPlayers())
                if (string.Equals(pl.GetPlayerName(), name, StringComparison.OrdinalIgnoreCase)) return pl;
            return null;
        }
    }
}
