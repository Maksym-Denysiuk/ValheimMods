using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // place: any prefab (item on the ground, object, building piece, creature) at the crosshair, optionally a block.
    // Column 'aimcol' of row 1 stands on the crosshair point; further rows go away from the player.
    // amount = stack size for item prefabs, level = star level for creatures, lift = metres above ground,
    // clear > 0 skips spots with solid objects (default 0 = no check, so pieces can be placed anywhere).
    public sealed class PlaceCommand : CommandBase
    {
        const int MaxObjects = 400;

        public override string Name { get { return "place"; } }
        protected override string Usage
        {
            get { return "place prefab=<name> [rows=1] [perrow=1] [spacing=1.5] [aimcol=<middle>] [scale=1] [amount=1] [level=0] [lift=0] [clear=0] - any prefab at the crosshair"; }
        }

        protected override string Execute(Args a)
        {
            string prefab = a.Str("prefab", "");
            int rows = a.Int("rows", 1), cols = a.Int("perrow", a.Int("cols", 1));
            int aimcol = a.Int("aimcol", (cols - 1) / 2), amount = a.Int("amount", 1), level = a.Int("level", 0);
            float spacing = a.Float("spacing", 1.5f), scale = a.Float("scale", 1f), lift = a.Float("lift", 0f), clear = a.Float("clear", 0f);
            Player p = Players.Local;
            GameObject go = Prefabs.Require(prefab, "(try op=list&filter=...)");
            if (rows * cols > MaxObjects) throw new CommandException("max " + MaxObjects + " objects");
            if (scale <= 0f) throw new CommandException("scale must be > 0");
            Anchor an = Anchor.AtAim(p);
            int made = 0, blocked = 0;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    Vector3 pos = an.Cell(r, c, aimcol, spacing);
                    pos.y += lift;
                    if (clear > 0f && Ground.IsBlocked(pos, clear)) { blocked++; continue; }
                    GameObject inst = (GameObject)UnityEngine.Object.Instantiate(go, pos, Quaternion.LookRotation(-an.Fwd));
                    Trees.SetScale(inst, scale);
                    ItemDrop drop = inst.GetComponent<ItemDrop>();
                    if (drop != null && amount > 1) drop.SetStack(Mathf.Min(amount, Mathf.Max(1, drop.m_itemData.m_shared.m_maxStackSize)));
                    Character ch = inst.GetComponent<Character>();
                    if (ch != null && level > 0) ch.SetLevel(level);
                    made++;
                }
            return "placed " + made + " " + prefab + " x" + scale.ToString("F2") + " (" + rows + " rows x " + cols + "), " + blocked
                + " blocked; first row, item " + (aimcol + 1) + " at " + Fmt.Vec(an.Origin)
                + (an.Aimed ? " (crosshair)" : " (no crosshair hit, 8 m ahead)");
        }
    }
}
