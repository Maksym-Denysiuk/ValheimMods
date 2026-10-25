using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // cultivate: cultivated soil under a block, one 'cultivate' piece per cell (what the Cultivator tool places)
    public sealed class CultivateCommand : CommandBase
    {
        const int MaxPieces = 2500;

        public override string Name { get { return "cultivate"; } }
        protected override string Usage { get { return "cultivate [rows=1] [perrow=1] [spacing=1.5] [dist=<m from you to row 1, default 0 = your own cell>] - cultivated soil from your cell along your look direction, for op=field"; } }

        protected override string Execute(Args a)
        {
            int rows = a.Int("rows", 1), cols = a.Int("perrow", a.Int("cols", 1));
            float spacing = a.Float("spacing", 1.5f);
            Anchor an = FieldAnchor.AheadOfPlayer(Players.Local, a, spacing);
            GameObject go = Prefabs.Get("cultivate");
            if (go == null) throw new CommandException("no 'cultivate' prefab");
            if (rows * cols > MaxPieces) throw new CommandException("max " + MaxPieces + " pieces");
            float centre = GridMath.CentreColumn(cols);
            int made = 0;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    UnityEngine.Object.Instantiate(go, an.Cell(r, c, centre, spacing), Quaternion.identity);
                    made++;
                }
            return "cultivated " + made + " cells, row 1 at " + Fmt.Vec(an.Origin) + " (" + Vector3.Distance(Players.Local.transform.position, an.Origin).ToString("F1") + " m from you)";
        }
    }
}
