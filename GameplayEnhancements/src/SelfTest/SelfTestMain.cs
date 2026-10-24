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
    }
}
