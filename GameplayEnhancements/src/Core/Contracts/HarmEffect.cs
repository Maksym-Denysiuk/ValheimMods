// Base of every effect aimed at a player (slime, earthquake): target resolution and ring placement; the PvP rule from AttackCommand.
// Reachable as op=<name> and as op=harmeffect&kind=<name>.
using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat
{
    public abstract class HarmEffect : AttackCommand
    {
        // the player the effect is centred on: parameter 'target' (nickname), default the local player
        protected static Player Target(Args a)
        {
            string n = a.Str("target", "");
            if (n.Length == 0) return Players.Local;
            Player p = Players.Find(n);
            if (p == null) throw new CommandException("no player named '" + n + "' is online");
            return p;
        }

        // equally spaced points on a circle around 'center' (y = ground). Count = circumference / itemDiameter
        // rounded down, at least 1; 'startAngleDeg' rotates the whole ring.
        public static List<Vector3> Ring(Vector3 center, float ringRadius, float itemDiameter, float startAngleDeg)
        {
            List<Vector3> pts = new List<Vector3>();
            int n = Mathf.Max(1, Mathf.FloorToInt(2f * Mathf.PI * ringRadius / Mathf.Max(0.1f, itemDiameter)));
            for (int i = 0; i < n; i++)
            {
                float ang = (startAngleDeg + 360f * i / n) * Mathf.Deg2Rad;
                Vector3 pos = center + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * ringRadius;
                pos.y = Ground.Height(pos);
                pts.Add(pos);
            }
            return pts;
        }
    }
}
