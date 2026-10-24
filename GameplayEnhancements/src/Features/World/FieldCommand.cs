using System;
using System.Collections.Generic;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // field: crops on the cells of a block that already have cultivated soil (op=cultivate makes it); other cells are skipped.
    // crops = "Pickable_Carrot:2,Pickable_Onion:1": consecutive bands of rows, one crop per band. Without crops, prefab fills all rows.
    public sealed class FieldCommand : CommandBase
    {
        const int MaxCells = 40000, MaxPlanted = 2500;

        public override string Name { get { return "field"; } }
        protected override string Usage
        {
            get { return "field prefab=<Pickable_*> | crops=<a:n,b:n> [rows=1] [perrow=1] [spacing=1.5] [scale=1] [clear=0.4] [dist=<m ahead instead of the crosshair>] - crops on cultivated soil only"; }
        }

        protected override string Execute(Args a)
        {
            string prefab = a.Str("prefab", ""), crops = a.Str("crops", "");
            if (prefab.Length == 0 && crops.Length == 0) throw new CommandException("prefab (or crops) required, e.g. prefab=Pickable_Carrot");
            int rows = a.Int("rows", 1), cols = a.Int("perrow", a.Int("cols", 1));
            float spacing = a.Float("spacing", 1.5f), scale = a.Float("scale", 1f), clear = a.Float("clear", 0.4f);
            Anchor an = Anchor.FromArgs(Players.Local, a);

            List<string> names = new List<string>();
            List<GameObject> prefabs = new List<GameObject>();
            List<int> counts = new List<int>();
            if (crops.Length == 0) crops = prefab + ":" + rows;
            foreach (string part in crops.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] kv = part.Split(':');
                string nm = kv[0].Trim();
                int n; if (kv.Length < 2 || !int.TryParse(kv[1].Trim(), out n)) n = 1;
                GameObject go = Prefabs.Get(nm);
                if (go == null) throw new CommandException("no prefab '" + nm + "' (try op=list&filter=carrot); nothing planted");
                names.Add(nm); prefabs.Add(go); counts.Add(n);
            }
            int totalRows = 0;
            foreach (int n in counts) totalRows += n;
            // the grid may be huge (100 rows) while soil exists on a few: cells are only examined, the cap applies to objects planted
            if (totalRows * cols > MaxCells) throw new CommandException("grid too large (max " + MaxCells + " cells examined)");
            float centre = GridMath.CentreColumn(cols);
            int made = 0, skipped = 0, blocked = 0, row = 0;
            StringBuilder per = new StringBuilder();
            for (int k = 0; k < prefabs.Count; k++)
            {
                int madeK = 0;
                for (int r = 0; r < counts[k]; r++, row++)
                    for (int c = 0; c < cols; c++)
                    {
                        Vector3 pos = an.Cell(row, c, centre, spacing);
                        if (made >= MaxPlanted) { skipped++; continue; }
                        if (!Ground.IsCultivated(pos)) { skipped++; continue; }
                        if (Ground.IsBlocked(pos, clear)) { blocked++; continue; }
                        GameObject inst = (GameObject)UnityEngine.Object.Instantiate(prefabs[k], pos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
                        Trees.SetScale(inst, scale);
                        made++; madeK++;
                    }
                per.Append(" ").Append(names[k]).Append("=").Append(madeK);
            }
            if (made == 0) return "nothing planted: no cultivated soil in the " + totalRows + "x" + cols + " grid (use op=cultivate first)";
            return "planted " + made + " (" + per.ToString().Trim() + "), skipped " + skipped + " cells without cultivated soil, "
                + blocked + " cells blocked by objects";
        }
    }
}
