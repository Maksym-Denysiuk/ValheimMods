using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // groundlevel: level the ground around the player to the player's own ground height + delta metres.
    // delta 0 = flatten at the current level; +1 / -1 = one metre up / down (raise like the Hoe's raise, lower like a pickaxe).
    // avoid=1 keeps away from player-built pieces: it refuses when one stands inside the area the stamp changes
    // (level radius, or the smooth radius around it, whichever is larger), or with shrink=1 levels a smaller circle instead.
    public sealed class GroundLevelCommand : CommandBase
    {
        const float MaxRadius = 12f;
        const float MinShrunkRadius = 1f;
        const float MaxDelta = 8f;   // the game keeps terrain within ±8 m of the original ground (TerrainComp)

        public override string Name { get { return "groundlevel"; } }
        protected override string Usage
        {
            get
            {
                return "groundlevel [delta=0 (m up or down from where you stand; the game clamps at ±8 m from the original ground)] [radius=4 (max 12)] [tool=raise (terrain prefab)]"
                    + " [avoid=0 (1 = refuse near player-built pieces)] [shrink=0 (with avoid=1: level a smaller circle instead, min 1 m)]";
            }
        }

        protected override string Execute(Args a)
        {
            float delta = a.Float("delta", 0f), radius = a.Float("radius", 4f);
            string tool = a.Str("tool", "raise");
            bool avoid = a.Flag("avoid", false), shrink = a.Flag("shrink", false);
            Host.RequireWorld();
            Player p = Players.Local;
            if (radius <= 0f || radius > MaxRadius) throw new CommandException("radius must be 0..12 m");
            GameObject go = Prefabs.Require(tool, "(try op=list&filter=raise)");
            TerrainModifier tm = go.GetComponent<TerrainModifier>();
            if (tm == null) throw new CommandException("'" + tool + "' has no TerrainModifier (use op=inspect&prefab=" + tool + ")");
            Vector3 c = p.transform.position;
            float smooth = Refl.GetFloat(tm, "m_smoothRadius", 0f);
            string shrunk = "";
            if (avoid)
            {
                // TerrainStamp smooths out to max(prefab smooth radius, radius + 1); a piece's distance is to its pivot
                float reach = Mathf.Max(smooth, radius + 1f);
                Piece nearest;
                int n = Constructions.Near(c, reach, out nearest);
                if (n > 0)
                {
                    float d = Vector3.Distance(c, nearest.transform.position);
                    float r = d - 1.1f;
                    if (!shrink)
                        throw new CommandException("piece " + Constructions.Describe(nearest, c) + " inside the radius (" + n + " piece(s) within "
                            + Fmt.F(reach, 1) + " m): move away, lower radius= or add shrink=1");
                    if (r < MinShrunkRadius || smooth >= d)
                        throw new CommandException("piece " + Constructions.Describe(nearest, c) + " is too close to level even a " + Fmt.F(MinShrunkRadius, 0)
                            + " m circle: move away");
                    shrunk = ", radius shrunk " + Fmt.F(radius, 1) + " -> " + Fmt.F(r, 1) + " m (" + Constructions.Describe(nearest, c) + ")";
                    radius = r;
                }
            }
            float baseY = Ground.Height(c);
            float target = baseY + delta;
            // the piece levels to its own y + m_levelOffset, so place it at target - offset
            float off = Refl.GetFloat(tm, "m_levelOffset", 0f);
            TerrainStamp.At(go, new Vector3(c.x, target - off, c.z), radius, 0f);
            string clamp = Mathf.Abs(delta) > MaxDelta ? "; the game clamps terrain at ±8 m from the original ground, so delta " + Fmt.F(delta, 1) + " stops there" : "";
            return "ground levelled around " + Fmt.Vec(c) + " r=" + Fmt.F(radius, 1) + " m: " + Fmt.F(baseY, 2) + " -> " + Fmt.F(target, 2)
                + " (delta " + Fmt.F(delta, 1) + ", tool " + tool + ", levelOffset " + Fmt.F(off, 2) + ")" + shrunk + clamp;
        }
    }
}
