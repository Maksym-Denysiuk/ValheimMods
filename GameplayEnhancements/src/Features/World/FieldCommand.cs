using System;
using System.Collections.Generic;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // field: crops on the cultivated cells of a block that are connected to the player's own cell (row 1, under the player) through
    // cultivated cells; neighbours are all 8 cells around (Maksym 2026-10-25). A gap of uncultivated soil ends the planting there:
    // with cells 1-4-7 in a line and 4 not cultivated, only 1 is planted.
    // crops = "Pickable_Carrot:2,Pickable_Onion:1": consecutive bands of rows, one crop per band. Without crops, prefab fills all rows.
    public sealed class FieldCommand : CommandBase
    {
        const int MaxCells = 40000, MaxPlanted = 2500;

        public override string Name { get { return "field"; } }
        protected override string Usage
        {
            get { return "field prefab=<Pickable_*> | crops=<a:n,b:n> [rows=1] [perrow=1] [spacing=<minimum for the crops, from the game: carrot 0.68 m>] [scale=1] [clear=0.4] [dist=<m from you to row 1, default 0 = your own cell>] - crops on the cultivated soil connected to your cell (own grid, may be denser than op=cultivate)"; }
        }

        protected override string Execute(Args a)
        {
            string prefab = a.Str("prefab", ""), crops = a.Str("crops", "");
            if (prefab.Length == 0 && crops.Length == 0) throw new CommandException("prefab (or crops) required, e.g. prefab=Pickable_Carrot");
            int rows = a.Int("rows", 1), cols = a.Int("perrow", a.Int("cols", 1));
            float spacing = a.Float("spacing", 0f), scale = a.Float("scale", 1f), clear = a.Float("clear", 0.4f);

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
            // spacing: the largest of the crops' minimum legal distances, so every crop of the field may grow there
            string spacingNote = "spacing " + spacing.ToString("F2") + " m (given)";
            if (spacing <= 0f)
            {
                StringBuilder src = new StringBuilder();
                foreach (GameObject go in prefabs)
                {
                    string why; float m = CropSpacing.Minimum(go, out why);
                    if (m > spacing) spacing = m;
                    src.Append(src.Length > 0 ? "; " : "").Append(why);
                }
                if (spacing <= 0f) spacing = 1.5f;
                spacingNote = "spacing " + spacing.ToString("F2") + " m (minimum: " + src + ")";
            }
            Anchor an = FieldAnchor.AheadOfPlayer(Players.Local, a, spacing);
            int totalRows = 0;
            foreach (int n in counts) totalRows += n;
            // the grid may be huge (100 rows) while soil exists on a few: cells are only examined, the cap applies to objects planted
            if (totalRows * cols > MaxCells) throw new CommandException("grid too large (max " + MaxCells + " cells examined)");
            float centre = GridMath.CentreColumn(cols);
            // the crop of each row: consecutive bands
            int[] cropOfRow = new int[totalRows];
            for (int k = 0, rr = 0; k < counts.Count; k++) for (int i = 0; i < counts[k]; i++, rr++) cropOfRow[rr] = k;
            Vector3[,] pos = new Vector3[totalRows, cols];
            bool[,] soil = new bool[totalRows, cols];
            for (int r = 0; r < totalRows; r++)
                for (int c = 0; c < cols; c++) { pos[r, c] = an.Cell(r, c, centre, spacing); soil[r, c] = Ground.IsCultivated(pos[r, c]); }
            // start: the player's cell = row 1, the middle column (both middle columns when the count is even)
            bool[,] reached = new bool[totalRows, cols];
            Queue<int> queue = new Queue<int>();
            for (int c = Mathf.FloorToInt(centre); c <= Mathf.CeilToInt(centre); c++)
                if (soil[0, c] && !reached[0, c]) { reached[0, c] = true; queue.Enqueue(c); }
            bool startSoil = queue.Count > 0;
            while (queue.Count > 0)
            {
                int cell = queue.Dequeue(), r0 = cell / cols, c0 = cell % cols;
                for (int dr = -1; dr <= 1; dr++)
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        int r = r0 + dr, c = c0 + dc;
                        if (r < 0 || r >= totalRows || c < 0 || c >= cols || reached[r, c] || !soil[r, c]) continue;
                        reached[r, c] = true;
                        queue.Enqueue(r * cols + c);
                    }
            }
            if (a.Flag("map", false)) return Map(soil, reached, totalRows, cols, centre, an, spacing, spacingNote);
            if (!startSoil) return "nothing planted: the soil under you is not cultivated (stand on cultivated soil; op=cultivate; op=field&map=1 shows the grid)";
            int made = 0, noSoil = 0, apart = 0, blocked = 0, capped = 0;
            int[] madeOf = new int[prefabs.Count];
            for (int r = 0; r < totalRows; r++)
                for (int c = 0; c < cols; c++)
                {
                    if (!soil[r, c]) { noSoil++; continue; }
                    if (!reached[r, c]) { apart++; continue; }
                    if (made >= MaxPlanted) { capped++; continue; }
                    if (Ground.IsBlocked(pos[r, c], clear)) { blocked++; continue; }
                    int k = cropOfRow[r];
                    GameObject inst = (GameObject)UnityEngine.Object.Instantiate(prefabs[k], pos[r, c], Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
                    Trees.SetScale(inst, scale);
                    made++; madeOf[k]++;
                }
            StringBuilder per = new StringBuilder();
            for (int k = 0; k < prefabs.Count; k++) per.Append(" ").Append(names[k]).Append("=").Append(madeOf[k]);
            return "planted " + made + " (" + per.ToString().Trim() + ") on the soil connected to your cell; " + apart + " cultivated cells not connected, "
                + noSoil + " cells without cultivated soil, " + blocked + " blocked by objects" + (capped > 0 ? ", " + capped + " over the cap of " + MaxPlanted : "") + "; " + spacingNote;
        }

        // map=1: the grid as text, farthest row on top; @ your cell, # cultivated and connected, o cultivated but cut off, . no soil
        static string Map(bool[,] soil, bool[,] reached, int rows, int cols, float centre, Anchor an, float spacing, string spacingNote)
        {
            StringBuilder sb = new StringBuilder("field map (" + rows + " rows x " + cols + ", " + spacingNote + "), farthest row on top:\n");
            for (int r = rows - 1; r >= 0; r--)
            {
                sb.Append("  ");
                for (int c = 0; c < cols; c++)
                {
                    bool me = r == 0 && (c == Mathf.FloorToInt(centre) || c == Mathf.CeilToInt(centre));
                    char ch = !soil[r, c] ? '.' : reached[r, c] ? '#' : 'o';
                    sb.Append(me ? (soil[r, c] ? '@' : '!') : ch).Append(' ');
                }
                sb.Append('\n');
            }
            Player p = Players.Local;
            Heightmap hm = Heightmap.FindHeightmap(p.transform.position);
            sb.Append("your position ").Append(Fmt.Vec(p.transform.position)).Append(", your cell at ").Append(Fmt.Vec(an.Cell(0, Mathf.FloorToInt(centre), centre, spacing)))
              .Append(", cultivated under your feet: ").Append(hm != null && hm.IsCultivated(p.transform.position)).Append(" (! = your cell without soil)");
            return sb.ToString();
        }
    }
}
