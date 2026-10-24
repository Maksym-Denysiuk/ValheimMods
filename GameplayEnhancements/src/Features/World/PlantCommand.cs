using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // plant: saplings in a rows x perrow block. Row 1 is placed so that column 'aimcol' (0-based) stands on the crosshair point;
    // further rows go away from the player. Spacing defaults to twice the sapling's own m_growRadius, the free space a Plant
    // needs to be allowed to grow.
    public sealed class PlantCommand : CommandBase
    {
        const int MaxSaplings = 400;

        public override string Name { get { return "plant"; } }
        protected override string Usage
        {
            get { return "plant prefab=<Sapling> [rows=1] [perrow=1] [spacing=<2 x grow radius>] [aimcol=<middle>] [scale=1] [clear=<auto>] - saplings at the crosshair"; }
        }

        protected override string Execute(Args a)
        {
            string prefab = a.Required("prefab", "e.g. prefab=Birch_Sapling (op=list&filter=sapling)");
            int rows = a.Int("rows", 1), cols = a.Int("perrow", a.Int("cols", 1));
            int aimcol = a.Int("aimcol", (cols - 1) / 2);
            float spacing = a.Float("spacing", 0f), scale = a.Float("scale", 1f), clear = a.Float("clear", 0f);
            if (scale <= 0f) throw new CommandException("scale must be > 0");
            Player p = Players.Local;
            GameObject go = Prefabs.Require(prefab, "(try op=list&filter=sapling)");
            if (rows * cols > MaxSaplings) throw new CommandException("max " + MaxSaplings + " saplings");
            Plant pl = go.GetComponent<Plant>();
            float need = pl != null ? pl.m_growRadius : 1f;
            if (spacing <= 0f) spacing = need * 2f * Mathf.Max(1f, scale);
            if (clear <= 0f) clear = Mathf.Max(0.5f, need * 0.5f);   // free radius around each trunk
            Anchor an = Anchor.AtAim(p);
            int made = 0, blocked = 0;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    Vector3 pos = an.Cell(r, c, aimcol, spacing);
                    if (Ground.IsBlocked(pos, clear)) { blocked++; continue; }
                    GameObject inst = (GameObject)UnityEngine.Object.Instantiate(go, pos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
                    Trees.SetScale(inst, scale);   // the grown tree copies this scale when the sapling grows
                    made++;
                }
            return "planted " + made + " " + prefab + " x" + scale.ToString("F2") + " (" + rows + " rows x " + cols + "), " + blocked
                + " blocked by objects (clear radius " + clear.ToString("F2") + "), spacing " + spacing.ToString("F2")
                + " m (growRadius " + need.ToString("F2") + "), tree " + (aimcol + 1) + " of row 1 at "
                + Fmt.Vec(an.Origin) + (an.Aimed ? " (crosshair)" : " (no crosshair hit, 8 m ahead)");
        }
    }
}
