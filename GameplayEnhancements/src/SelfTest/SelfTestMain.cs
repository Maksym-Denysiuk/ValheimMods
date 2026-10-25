// Logic tests that run without the game: bash build.sh selftest. Covers the Unity-free core files in Core/Pure.
// Exit code 0 = all passed. C# 5.
using System;
using System.Collections.Generic;

namespace HomeCheat.SelfTest
{
    static class SelfTestMain
    {
        static int failed, passed;

        static void Check(bool ok, string what)
        {
            if (ok) passed++;
            else { failed++; Console.WriteLine("FAIL " + what); }
        }

        static void Eq(object want, object got, string what)
        {
            Check(Equals(want, got), what + ": want '" + want + "', got '" + got + "'");
        }

        static int Main()
        {
            FormParserTests();
            ArgsTests();
            BuildStampTests();
            GridMathTests();
            RectCellsTests();
            GridRouteTests();
            PointStoreTests();
            Console.WriteLine("selftest: " + passed + " passed, " + failed + " failed");
            return failed == 0 ? 0 : 1;
        }

        static void FormParserTests()
        {
            Dictionary<string, string> d = FormParser.Parse("op=give&prefab=Wood&amount=50");
            Eq("give", d["op"], "form op");
            Eq("50", d["amount"], "form amount");
            Check(d.ContainsKey("PREFAB"), "form keys are case-insensitive");
            d = FormParser.Parse("op=spells\nname=a+b%26c");
            Eq("a b&c", d["name"], "form '+' and %xx decode, new-line separator");
            d = FormParser.Parse("flag&&=x& k = v ");
            Eq("", d["flag"], "form key without value");
            Check(!d.ContainsKey(""), "form drops an empty key");
            Eq("v", d["k"], "form trims");
            Eq(0, FormParser.Parse(null).Count, "form null body");
        }

        static void ArgsTests()
        {
            Dictionary<string, string> raw = new Dictionary<string, string>();
            raw["a"] = "1.5"; raw["n"] = "7"; raw["empty"] = ""; raw["yes"] = "YES"; raw["word"] = "maybe"; raw["aim"] = "aim";
            Args a = new Args(raw);
            Eq(1.5f, a.Float("a", 0f), "args float invariant");
            Eq(7, a.Int("n", 0), "args int");
            Eq(3, a.Int("a", 3), "args int of a fraction falls back");
            Eq("def", a.Str("empty", "def"), "args empty value = default");
            Check(a.Has("empty"), "args Has an empty key");
            Check(a.Flag("yes", false), "args flag YES");
            Check(a.Flag("aim", false), "args flag aim");
            Check(!a.Flag("word", true), "args other word = false even with default true");
            Check(a.Flag("missing", true), "args missing flag = default");
            Check(a.Flag("empty", true), "args empty flag = default");
            bool threw = false;
            try { a.Required("prefab", "e.g. prefab=Wood"); }
            catch (CommandException e) { threw = e.Message == "prefab required, e.g. prefab=Wood"; }
            Check(threw, "args Required throws CommandException with the hint");
            Dictionary<string, string> copy = a.Raw;
            copy["n"] = "8";
            Eq(7, a.Int("n", 0), "args Raw is a copy");
        }

        static void BuildStampTests()
        {
            string f; long s;
            Check(BuildStamp.TryParse("HomeCheat.Cmd.earthquake.v212810.dll", out f, out s), "stamp old name parses");
            Eq("earthquake", f, "stamp feature");
            Eq(212810L, s, "stamp old value");
            Check(BuildStamp.TryParse("homecheat.cmd.World.v20261025091500.dll", out f, out s), "stamp new name parses, any case");
            Eq("world", f, "stamp feature lower case");
            Check(20261025091500L > 221149L, "a new 14-digit stamp beats an old 6-digit one");
            long before, after; string x;
            BuildStamp.TryParse(BuildStamp.FileName("heal", new DateTime(2026, 10, 24, 23, 59, 59)), out x, out before);
            BuildStamp.TryParse(BuildStamp.FileName("heal", new DateTime(2026, 10, 25, 0, 0, 1)), out x, out after);
            Check(after > before, "a build after midnight beats one before it");
            Check(!BuildStamp.TryParse("HomeCheat.dll", out f, out s), "stamp rejects the core name");
            Check(!BuildStamp.TryParse("HomeCheat.Cmd.heal.dll", out f, out s), "stamp rejects a name without v<stamp>");
        }

        static void GridMathTests()
        {
            float along, across;
            Eq(1f, GridMath.CentreColumn(3), "grid centre of 3 columns");
            GridMath.Offset(0, 0, GridMath.CentreColumn(3), 1.5f, out along, out across);
            Eq(0f, along, "grid row 0 along");
            Eq(-1.5f, across, "grid col 0 of 3 is left of centre");
            GridMath.Offset(2, 2, 0f, 2f, out along, out across);
            Eq(4f, along, "grid row 2 along");
            Eq(4f, across, "grid aim column 0: col 2 is 2 cells right");
        }

        static void RectCellsTests()
        {
            int cols, rows;
            GridMath.RectSize(0f, 0f, 4f, 2f, 1f, out cols, out rows);
            Eq(5, cols, "rect 4 m wide, spacing 1: 5 columns (both edges)");
            Eq(3, rows, "rect 2 m deep: 3 rows");
            List<GridPoint> c = GridMath.RectCells(4f, 2f, 0f, 0f, 1f, 1000);
            Eq(15, c.Count, "rect cells, corners in any order");
            Eq(0f, c[0].X, "rect first cell at the low-X edge");
            Eq(0f, c[0].Z, "rect first cell at the low-Z edge");
            Eq(4f, c[14].X, "rect last cell at the high-X edge");
            Eq(1f, c[5].Z, "rect second row starts at z=1");
            c = GridMath.RectCells(0f, 0f, 2.5f, 0f, 1f, 1000);
            Eq(3, c.Count, "rect 2.5 m: 3 cells");
            Eq(0.25f, c[0].X, "rect leftover split evenly: first cell 0.25 m in");
            Eq(1, GridMath.RectCells(3f, 3f, 3f, 3f, 1f, 1000).Count, "rect of one point = one cell");
            GridMath.RectSize(0f, 0f, 300f, 300f, 1f, out cols, out rows);
            c = GridMath.RectCells(0f, 0f, 300f, 300f, 1f, 2500);
            Eq(2500, c.Count, "rect cells stop at max");
            Check(c.Count < cols * rows, "rect cap visible against RectSize");
            bool threw = false;
            try { GridMath.RectCells(0f, 0f, 1f, 1f, 0f, 10); } catch (CommandException) { threw = true; }
            Check(threw, "rect spacing 0 throws");
        }

        static bool Adjacent(GridCell a, GridCell b) { return Math.Abs(a.X - b.X) <= 1 && Math.Abs(a.Z - b.Z) <= 1 && !(a.X == b.X && a.Z == b.Z); }

        static void GridRouteTests()
        {
            List<GridCell> path;
            // straight: open 11 x 3 grid, start and end on the middle row
            GridRouteStatus st = GridRoute.Find(11, 3, null, new GridCell(0, 1), new GridCell(10, 1), out path);
            Eq(GridRouteStatus.Found, st, "route straight found");
            Eq(11, path.Count, "route straight: 11 cells");
            bool row = true;
            foreach (GridCell g in path) if (g.Z != 1) row = false;
            Check(row, "route straight stays on its row");
            Eq(2, GridRoute.Corners(path).Count, "route straight: 2 corners (start, end)");

            // wall with a gap: wall at x=5 on every row but z=8
            Func<int, int, bool> wall = delegate (int x, int z) { return x == 5 && z != 8; };
            st = GridRoute.Find(11, 11, wall, new GridCell(0, 5), new GridCell(10, 5), out path);
            Eq(GridRouteStatus.Found, st, "route wall with gap found");
            bool throughGap = false, clean = true, steps = true;
            for (int i = 0; i < path.Count; i++)
            {
                if (path[i].X == 5 && path[i].Z == 8) throughGap = true;
                if (wall(path[i].X, path[i].Z)) clean = false;
                if (i > 0 && !Adjacent(path[i - 1], path[i])) steps = false;
            }
            Check(throughGap, "route goes through the gap");
            Check(clean, "route never enters the wall");
            Check(steps, "route moves one cell at a time");
            Eq(0, path[0].X, "route starts at start");
            Eq(10, path[path.Count - 1].X, "route ends at end");
            Check(GridRoute.Corners(path).Count >= 3, "route around a wall has a corner");

            // no corner cutting: two blocked cells touching at a corner close the diagonal between them
            Func<int, int, bool> pinch = delegate (int x, int z) { return (x == 1 && z == 0) || (x == 0 && z == 1); };
            st = GridRoute.Find(2, 2, pinch, new GridCell(0, 0), new GridCell(1, 1), out path);
            Eq(GridRouteStatus.NoRoute, st, "route never squeezes between two blocked corners");

            // no route: a full wall
            st = GridRoute.Find(11, 11, delegate (int x, int z) { return x == 5; }, new GridCell(0, 5), new GridCell(10, 5), out path);
            Eq(GridRouteStatus.NoRoute, st, "route full wall: no route");
            Check(path == null, "route no route: path null");

            // start and end are passable even when the predicate blocks them
            st = GridRoute.Find(3, 1, delegate (int x, int z) { return x != 1; }, new GridCell(0, 0), new GridCell(2, 0), out path);
            Eq(GridRouteStatus.Found, st, "route endpoints always passable");

            // cap: 201 x 200 = 40 200 cells > 40 000
            st = GridRoute.Find(201, 200, null, new GridCell(0, 0), new GridCell(200, 199), out path);
            Eq(GridRouteStatus.OverCap, st, "route over the cell cap is refused");
            st = GridRoute.Find(200, 200, null, new GridCell(0, 0), new GridCell(199, 199), out path);
            Eq(GridRouteStatus.Found, st, "route at the cap (200 x 200) is searched");
            Eq(200, path.Count, "route open diagonal: 200 cells");

            st = GridRoute.Find(5, 5, null, new GridCell(0, 0), new GridCell(5, 0), out path);
            Eq(GridRouteStatus.Invalid, st, "route end outside the grid is invalid");
        }

        static void PointStoreTests()
        {
            PointStore st = new PointStore();
            st.Set("ClaudeTest", "start", 1.5f, 30f, -2.25f);
            st.Set("ClaudeTest", "End", 21.5f, 31f, -2.25f);
            st.Set("Other", "start", 7f, 8f, 9f);
            int skipped;
            PointStore back = PointStore.Parse(st.Format(), out skipped);
            Eq(0, skipped, "points round trip: no bad lines");
            float x, y, z;
            Check(back.TryGet("claudetest", "START", out x, out y, out z), "points world and name ignore case");
            Eq(1.5f, x, "points x");
            Eq(-2.25f, z, "points z");
            Check(back.TryGet("ClaudeTest", "end", out x, out y, out z), "points name stored lower case");
            Eq(21.5f, x, "points end x");
            Check(back.TryGet("Other", "start", out x, out y, out z) && x == 7f, "points per world");
            Eq(2, back.Names("ClaudeTest").Count, "points names of one world");
            back = PointStore.Parse("# c\nw|a|1,2,3\nbroken line\nw|bad name|1,2,3\nw|b|1,2\nw|c|1,x,3\n", out skipped);
            Eq(4, skipped, "points bad lines skipped");
            Eq(1, back.Names("w").Count, "points good line kept");
            Check(back.Clear("w", "A"), "points clear");
            Check(!back.Clear("w", "a"), "points clear twice = false");
            bool threw = false;
            try { st.Set("ClaudeTest", "a|b", 0f, 0f, 0f); } catch (CommandException) { threw = true; }
            Check(threw, "points bad name throws");
            for (int i = 0; i < PointStore.MaxPointsPerWorld; i++) st.Set("Full", "p" + i, 0f, 0f, 0f);
            threw = false;
            try { st.Set("Full", "one-more", 0f, 0f, 0f); } catch (CommandException) { threw = true; }
            Check(threw, "points cap per world");
            st.Set("Full", "p0", 1f, 1f, 1f);
            Eq(PointStore.MaxPointsPerWorld, st.ClearWorld("Full"), "points overwrite within the cap, clear world");
        }
    }
}
