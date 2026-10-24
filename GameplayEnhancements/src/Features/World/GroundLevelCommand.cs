using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // groundlevel: level the ground around the player to the player's own ground height + delta metres.
    // delta 0 = flatten at the current level; +1 / -1 = one metre up / down (raise like the Hoe's raise, lower like a pickaxe).
    public sealed class GroundLevelCommand : CommandBase
    {
        public override string Name { get { return "groundlevel"; } }
        protected override string Usage { get { return "groundlevel [delta=0 (m up or down from where you stand)] [radius=4 (max 12)] [tool=raise (terrain prefab)]"; } }

        protected override string Execute(Args a)
        {
            float delta = a.Float("delta", 0f), radius = a.Float("radius", 4f);
            string tool = a.Str("tool", "raise");
            Player p = Players.Local;
            if (radius <= 0f || radius > 12f) throw new CommandException("radius must be 0..12 m");
            GameObject go = Prefabs.Require(tool, "(try op=list&filter=raise)");
            TerrainModifier tm = go.GetComponent<TerrainModifier>();
            if (tm == null) throw new CommandException("'" + tool + "' has no TerrainModifier (use op=inspect&prefab=" + tool + ")");
            Vector3 c = p.transform.position;
            float baseY = Ground.Height(c);
            float target = baseY + delta;
            // the piece levels to its own y + m_levelOffset, so place it at target - offset
            float off = Refl.GetFloat(tm, "m_levelOffset", 0f);
            TerrainStamp.At(go, new Vector3(c.x, target - off, c.z), radius, 0f);
            return "ground levelled around " + Fmt.Vec(c) + " r=" + radius.ToString("F1") + " m: " + baseY.ToString("F2") + " -> " + target.ToString("F2")
                + " (delta " + delta.ToString("F1") + ", tool " + tool + ", levelOffset " + off.ToString("F2") + ")";
        }
    }
}
