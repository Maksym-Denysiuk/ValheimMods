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
    // area= (W2-world-field, 2026-10-25) plants on a world X/Z grid instead of the grid from your cell: area=coords every cultivated
    // cell in the rectangle between op=coord start and end (no connection rule), area=patch the whole cultivated patch you stand on
    // (FieldPatch.Sample). rows/perrow/dist are ignored there; crops= bands repeat along world Z.
    public sealed class FieldCommand : CommandBase
    {
        const int MaxCells = 40000, MaxPlanted = 2500;

        public override string Name { get { return "field"; } }
        protected override string Usage
        {
            get { return "field prefab=<Pickable_*> | crops=<a:n,b:n> [rows=1] [perrow=1] [spacing=<minimum for the crops, from the game: carrot 0.68 m>] [scale=1] [clear=0.4] [dist=<m from you to row 1, default 0 = your own cell>] [area=coords|patch (coords: every cultivated cell in the rectangle of op=coord start and end; patch: the whole cultivated patch you stand on; rows, perrow, dist ignored)] - crops on the cultivated soil connected to your cell (own grid, may be denser than op=cultivate); caps " + MaxCells + " cells examined, " + MaxPlanted + " planted"; }
        }

        protected override string Execute(Args a)
        {
            string prefab = a.Str("prefab", ""), crops = a.Str("crops", "");
            string area = a.Str("area", "").ToLowerInvariant();
            if (area.Length > 0 && area != "coords" && area != "patch") throw new CommandException("area must be coords or patch (or left out)");
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
            if (area.Length > 0) return AreaField(a, area, names, prefabs, counts, spacing, scale, clear, spacingNote);
            Anchor an =FieldAnchor.AheadOfPlayer(Players.Local, a, spacing);
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
            bool startSoil;
            bool[,] reached = FieldPatch.Connected(soil, 0, Mathf.FloorToInt(centre), Mathf.CeilToInt(centre), out startSoil);
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

        // area=coords | patch: a world X/Z grid over the rectangle (coords) or the patch's bounding box (patch), GridMath.RectCells
        string AreaField(Args a, string area, List<string> names, List<GameObject> prefabs, List<int> counts, float spacing, float scale, float clear, string spacingNote)
        {
            Host.RequireWorld();
            Player p = Players.Local;
            if (a.Flag("map", false)) throw new CommandException("map=1 shows the grid from your cell only: leave out area=");
            float ax, az, bx, bz;
            FieldPatch patch = null;
            string where;
            if (area == "coords")
            {
                Vector3 s = Coords.Require(Coords.Start), e = Coords.Require(Coords.End);
                ax = s.x; az = s.z; bx = e.x; bz = e.z;
                where = "the rectangle start " + Fmt.Vec(s) + " - end " + Fmt.Vec(e);
            }
            else
            {
                patch = FieldPatch.Sample(p.transform.position);
                if (!patch.StartSoil) return "nothing planted: the soil under you is not cultivated (stand on cultivated soil; op=cultivate)";
                patch.Bounds(out ax, out az, out bx, out bz);
                where = "the patch under you (about " + Fmt.F(patch.Area, 0) + " m2, " + patch.Count + " samples of " + Fmt.F(FieldPatch.Step, 1) + " m"
                    + (patch.OverCap ? "; sampling stopped at the cap of " + FieldPatch.MaxSamples + " cells, the rest of the patch is left out" : "") + ")";
            }
            int cols, rows;
            GridMath.RectSize(ax, az, bx, bz, spacing, out cols, out rows);
            long cells = (long)cols * rows;
            if (cells > MaxCells) throw new CommandException("cells=" + cells + " is over the cap " + MaxCells + " (" + cols + " x " + rows + " at " + Fmt.F(spacing, 2) + " m): a smaller area or a larger spacing=");
            List<GridPoint> pts = GridMath.RectCells(ax, az, bx, bz, spacing, MaxCells);
            // crop of a grid row: the bands of crops= repeat along world Z
            List<int> cropOfBand = new List<int>();
            for (int k = 0; k < counts.Count; k++) for (int i = 0; i < counts[k]; i++) cropOfBand.Add(k);
            if (cropOfBand.Count == 0) cropOfBand.Add(0);
            int made = 0, noSoil = 0, apart = 0, blocked = 0, capped = 0;
            int[] madeOf = new int[prefabs.Count];
            for (int i = 0; i < pts.Count; i++)
            {
                Vector3 pos = new Vector3(pts[i].X, 0f, pts[i].Z);
                pos.y = Ground.Height(pos);
                bool soil = Ground.IsCultivated(pos);
                if (!soil) { noSoil++; continue; }
                if (patch != null && !patch.Contains(pos)) { apart++; continue; }
                if (made >= MaxPlanted) { capped++; continue; }
                if (Ground.IsBlocked(pos, clear)) { blocked++; continue; }
                int k = cropOfBand[(i / cols) % cropOfBand.Count];
                GameObject inst = (GameObject)UnityEngine.Object.Instantiate(prefabs[k], pos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
                Trees.SetScale(inst, scale);
                made++; madeOf[k]++;
            }
            StringBuilder per = new StringBuilder();
            for (int k = 0; k < prefabs.Count; k++) per.Append(" ").Append(names[k]).Append("=").Append(madeOf[k]);
            bool ignored = a.Has("rows") || a.Has("perrow") || a.Has("cols") || a.Has("dist");
            return "planted " + made + " (" + per.ToString().Trim() + ") in " + where + ", grid " + cols + " x " + rows + "; "
                + (patch != null ? apart + " cultivated cells not in this patch, " : "") + noSoil + " cells without cultivated soil, " + blocked + " blocked by objects"
                + (capped > 0 ? ", " + capped + " over the cap of " + MaxPlanted : "") + "; " + spacingNote
                + (ignored ? "; rows/perrow/dist ignored with area=" : "");
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
