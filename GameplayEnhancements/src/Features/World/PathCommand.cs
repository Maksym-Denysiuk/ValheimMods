using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // path: a dirt or stone strip, 'length' m along the direction from the player to the start point and 'width' m wide,
    // starting at the crosshair (or 'dist' m ahead), centred on its axis
    public sealed class PathCommand : CommandBase
    {
        const int MaxStamps = 400;

        public override string Name { get { return "path"; } }
        protected override string Usage { get { return "path [type=dirt|stone] [length=5] [width=2] [tool=<terrain prefab>] [dist=<m ahead instead of the crosshair>]"; } }

        protected override string Execute(Args a)
        {
            string type = a.Str("type", "dirt").ToLowerInvariant(), tool = a.Str("tool", "");
            float length = a.Float("length", 5f), width = a.Float("width", 2f);
            Anchor an = Anchor.FromArgs(Players.Local, a);
            if (tool.Length == 0) tool = type == "stone" ? "paved_road" : "path";
            if (type != "dirt" && type != "stone") throw new CommandException("type must be dirt or stone");
            if (length <= 0f || width <= 0f) throw new CommandException("length and width must be > 0");
            GameObject go = Prefabs.Get(tool);
            if (go == null) throw new CommandException("no terrain prefab '" + tool + "'");
            float rr = Mathf.Clamp(width / 2f, 0.5f, 4f);
            int across = Mathf.Max(1, Mathf.CeilToInt(width / (2f * rr)));
            int along = Mathf.Max(1, Mathf.CeilToInt(length / rr));
            if (across * along > MaxStamps) throw new CommandException("too many stamps (" + (across * along) + " > " + MaxStamps + "); shorten the path");
            int made = 0;
            for (int i = 0; i < along; i++)
            {
                float d = along == 1 ? length / 2f : rr + i * ((length - 2f * rr) / Mathf.Max(1, along - 1));
                if (length < 2f * rr) d = length / 2f;
                for (int k = 0; k < across; k++)
                {
                    float lat = across == 1 ? 0f : -(width / 2f - rr) + k * ((width - 2f * rr) / (across - 1));
                    Vector3 pos = an.Origin + an.Fwd * d + an.Right * lat;
                    pos.y = Ground.Height(pos);
                    TerrainStamp.At(go, pos, 0f, rr);
                    made++;
                }
            }
            return "path " + type + " " + length.ToString("F1") + " x " + width.ToString("F1") + " m from " + Fmt.Vec(an.Origin)
                + (an.Aimed ? " (crosshair)" : "") + ": " + made + " stamps of r=" + rr.ToString("F1") + " (tool " + tool + ")";
        }
    }
}
