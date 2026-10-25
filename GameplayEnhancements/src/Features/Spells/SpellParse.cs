using System;
using System.Collections.Generic;
using System.Text;

namespace HomeCheat.Features.Spells
{
    // Spell file v4 parser. No Unity or game reference, so it can be compiled into a console check outside the game.
    //   id | label | command | flags      id = dotted numbers of any depth (2, 2.1, 6.1.3); 1 is the Hotbar and cannot be used
    //   a line with an empty command (or only id | label) is a title: a group or sub-section; "6.1 | Flatten | | sticky"
    //   flags: sticky  pin=F1..F12|S1..S12 (S10..S12 = Shift+0, Shift+-, Shift+=)  shift=<form>  ask=<key>:<Label=value,...>|@players|@crops|@trees|@prefabs (repeatable)
    // Every level may hold any number of entries (the game's radial lays them out as a spiral).
    internal sealed class SpellParse
    {
        public const int MaxDepth = 6, MaxSpells = 1000, MaxNumber = 999;
        // the 24 bar cells (Maksym 2026-10-25): Shift+1..Shift+= and F1..F12; Panel 1-5 = S1-S5 + F1-F5, Panel 6-= = S6-S12 + F6-F12
        public static readonly string[] PinOrder = new string[] { "S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8", "S9", "S10", "S11", "S12",
            "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12" };
        public static readonly string[] PanelNames = new string[] { "Panel 1-5", "Panel 6-=" };

        // the key a cell fires on, as printed on the bar: "1".."9", "0", "-", "=" for S1..S12; "F1".."F12"
        public static string KeyOf(string cell)
        {
            if (cell.Length < 2 || cell[0] != 'S') return cell;
            int n = CellNumber(cell);
            return n <= 9 ? n.ToString() : n == 10 ? "0" : n == 11 ? "-" : "=";
        }

        // "Shift+1" .. "Shift+=", "F1" .. "F12"
        public static string KeyName(string cell) { return cell.Length > 1 && cell[0] == 'S' ? "Shift+" + KeyOf(cell) : cell; }

        public static int CellNumber(string cell) { int n; return cell.Length > 1 && int.TryParse(cell.Substring(1), out n) ? n : 0; }

        // 0 = Panel 1-5 (cells 1-5), 1 = Panel 6-= (cells 6-12)
        public static int PanelOf(string cell) { return CellNumber(cell) <= 5 ? 0 : 1; }
        public static readonly string[] Sources = new string[] { "@players", "@crops", "@trees", "@prefabs" };

        public SpellNode Root = new SpellNode();                                   // children = the root sections 2, 3, ...
        public Dictionary<string, Spell> Spells = new Dictionary<string, Spell>();
        public Dictionary<string, Spell> Pins = new Dictionary<string, Spell>();
        public int Titles;
        public List<string> Bad = new List<string>();                              // "line 12 (why)"

        Dictionary<string, SpellNode> nodes = new Dictionary<string, SpellNode>();

        public static SpellParse Run(string text)
        {
            SpellParse p = new SpellParse();
            p.Root.Declared = true;
            p.nodes[""] = p.Root;
            int lineNo = 0;
            foreach (string raw in (text ?? "").Split('\n'))
            {
                lineNo++;
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                string why = p.Line(line);
                if (why != null) p.Bad.Add("line " + lineNo + " (" + why + ")");
            }
            p.Finish(p.Root, false);
            return p;
        }

        // one line; returns null when taken, else why it was skipped
        string Line(string line)
        {
            string[] f = line.Split('|');
            for (int i = 0; i < f.Length; i++) f[i] = f[i].Trim();
            if (f.Length < 2) return "needs at least: id | label";
            int[] nums = ParseId(f[0]);
            if (nums == null) return "id '" + f[0] + "' is not dotted numbers 1-" + MaxNumber + ", depth " + MaxDepth + " at most";
            if (nums[0] == 1) return "1 is the Hotbar, use 2 and up";
            string id = string.Join(".", Array.ConvertAll(nums, delegate (int n) { return n.ToString(); }));
            if (f[1].Length == 0) return "empty label";
            SpellNode existing;
            if (nodes.TryGetValue(id, out existing) && existing.Declared) return "id " + id + " used twice";
            bool title = f.Length == 2 || f[2].Length == 0;
            if (!title && Spells.Count >= MaxSpells) return "more than " + MaxSpells + " spells";
            string parentId = id.LastIndexOf('.') < 0 ? "" : id.Substring(0, id.LastIndexOf('.'));
            SpellNode parent;
            if (nodes.TryGetValue(parentId, out parent) && parent.Spell != null) return "id " + id + " sits under the spell " + parent.Id + "; make " + parent.Id + " a title";
            if (!title && existing != null && existing.Children.Count > 0) return "id " + id + " has entries below it, so it must be a title (empty command)";

            string flags = f.Length > 3 ? string.Join("|", f, 3, f.Length - 3) : "";
            bool sticky = false; string pin = "", shift = "";
            List<SpellAsk> asks = new List<SpellAsk>();
            foreach (string tok in flags.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (tok == "sticky") sticky = true;
                else if (tok.StartsWith("pin=")) pin = tok.Substring(4).ToUpperInvariant();
                else if (tok.StartsWith("shift=")) shift = tok.Substring(6);
                else if (tok.StartsWith("ask="))
                {
                    string err;
                    SpellAsk a = ParseAsk(tok.Substring(4), out err);
                    if (a == null) return err;
                    foreach (SpellAsk o in asks) if (o.Key == a.Key) return "ask key '" + a.Key + "' twice";
                    asks.Add(a);
                }
            }
            if (title && (pin.Length > 0 || shift.Length > 0 || asks.Count > 0)) return "a title takes only the sticky flag";
            if (!title && pin.Length > 0 && Array.IndexOf(PinOrder, pin) < 0) return "pin " + pin + " is not one of " + string.Join(" ", PinOrder);
            string missing = title ? null : MissingAnswer(f[2] + "&" + shift, asks);
            if (missing != null) return "{" + missing + "} in the command has no ask=" + missing + ":...";

            SpellNode node = Node(nums);           // only now: a skipped line leaves no node behind
            if (title)
            {
                node.Title = f[1]; node.Sticky = sticky; node.Declared = true;
                Titles++;
                return null;
            }
            Spell s = new Spell();
            s.Id = id; s.Top = nums[0]; s.Label = f[1]; s.Cmd = f[2]; s.Shift = shift; s.Pin = pin; s.Sticky = sticky; s.Asks = asks;
            node.Title = f[1]; node.Spell = s; node.Declared = true;
            Spells[id] = s;
            if (pin.Length > 0) Pins[pin] = s;
            return null;
        }

        // "6.1.3" -> {6,1,3}; null when not valid
        public static int[] ParseId(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            string[] parts = id.Split('.');
            if (parts.Length > MaxDepth) return null;
            int[] r = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                int n;
                if (parts[i].Length == 0 || parts[i].Length > 3 || !int.TryParse(parts[i], out n) || n < 1 || n > MaxNumber) return null;
                for (int c = 0; c < parts[i].Length; c++) if (parts[i][c] < '0' || parts[i][c] > '9') return null;
                r[i] = n;
            }
            return r;
        }

        // ask=<key>:<options>; options "Label=value" or "value", comma separated, "_" in a label shows as a space; or one @source
        public static SpellAsk ParseAsk(string body, out string err)
        {
            err = null;
            int c = body.IndexOf(':');
            if (c <= 0) { err = "ask needs key:options (ask=count:1,5,10)"; return null; }
            SpellAsk a = new SpellAsk();
            a.Key = body.Substring(0, c).Trim().ToLowerInvariant();
            for (int i = 0; i < a.Key.Length; i++)
            {
                char ch = a.Key[i];
                if (!((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_')) { err = "ask key '" + a.Key + "' may hold a-z 0-9 _ only"; return null; }
            }
            string rest = body.Substring(c + 1).Trim();
            if (rest.StartsWith("@"))
            {
                if (Array.IndexOf(Sources, rest.ToLowerInvariant()) < 0) { err = "unknown source " + rest + " (" + string.Join(" ", Sources) + ")"; return null; }
                a.Source = rest.ToLowerInvariant();
                return a;
            }
            foreach (string opt in rest.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = opt.IndexOf('=');
                string label = eq > 0 ? opt.Substring(0, eq) : opt;
                string value = eq > 0 ? opt.Substring(eq + 1) : opt;
                if (value.Length == 0) { err = "ask " + a.Key + ": option '" + opt + "' has no value"; return null; }
                a.Options.Add(new SpellAskOption(label.Replace('_', ' '), value, ""));
            }
            if (a.Options.Count == 0) { err = "ask " + a.Key + " has no options"; return null; }
            return a;
        }

        // the first {key} of the form with no matching ask, or null
        static string MissingAnswer(string form, List<SpellAsk> asks)
        {
            int i = 0;
            while ((i = form.IndexOf('{', i)) >= 0)
            {
                int j = form.IndexOf('}', i);
                if (j < 0) return form.Substring(i + 1);
                string k = form.Substring(i + 1, j - i - 1).ToLowerInvariant();
                bool found = false;
                foreach (SpellAsk a in asks) if (a.Key == k) found = true;
                if (!found) return k;
                i = j + 1;
            }
            return null;
        }

        // the node of an id, created (with its parents) when missing
        SpellNode Node(int[] nums)
        {
            string id = "";
            SpellNode cur = Root;
            for (int d = 0; d < nums.Length; d++)
            {
                id = d == 0 ? nums[0].ToString() : id + "." + nums[d];
                SpellNode n;
                if (!nodes.TryGetValue(id, out n))
                {
                    n = new SpellNode();
                    n.Id = id; n.Number = nums[d]; n.Title = id;
                    nodes[id] = n;
                    cur.Children.Add(n);
                }
                cur = n;
            }
            return cur;
        }

        // sorts the children and passes sticky titles down to the spells below them
        void Finish(SpellNode n, bool sticky)
        {
            bool st = sticky || n.Sticky;
            if (n.Spell != null && st) n.Spell.Sticky = true;
            n.Children.Sort(delegate (SpellNode a, SpellNode b) { return a.Number.CompareTo(b.Number); });
            foreach (SpellNode c in n.Children) Finish(c, st);
        }

        public SpellNode Find(string id)
        {
            SpellNode n;
            return id != null && nodes.TryGetValue(id, out n) ? n : null;
        }

        // replaces {key} with the answers (escaped for the form); unknown keys stay as they are
        public static string Substitute(string form, Dictionary<string, string> answers)
        {
            if (answers == null || form.IndexOf('{') < 0) return form;
            StringBuilder sb = new StringBuilder();
            int i = 0;
            while (i < form.Length)
            {
                int a = form.IndexOf('{', i);
                int b = a < 0 ? -1 : form.IndexOf('}', a);
                if (a < 0 || b < 0) { sb.Append(form, i, form.Length - i); break; }
                sb.Append(form, i, a - i);
                string k = form.Substring(a + 1, b - a - 1).ToLowerInvariant();
                string v;
                if (answers.TryGetValue(k, out v)) sb.Append(Uri.EscapeDataString(v));
                else sb.Append(form, a, b - a + 1);
                i = b + 1;
            }
            return sb.ToString();
        }

        // splits a long option list into alphabetical blocks of at most max (each block may itself be split again)
        public static List<List<SpellAskOption>> Split(List<SpellAskOption> opts, int max)
        {
            List<List<SpellAskOption>> r = new List<List<SpellAskOption>>();
            if (opts.Count <= max) { r.Add(opts); return r; }
            int size = Math.Max(max, (opts.Count + max - 1) / max);
            for (int i = 0; i < opts.Count; i += size) r.Add(opts.GetRange(i, Math.Min(size, opts.Count - i)));
            return r;
        }

        // "Abomination - BlackMetal": the first and the last label of a block, each cut to 12 characters
        public static string BlockTitle(List<SpellAskOption> block)
        {
            if (block.Count == 0) return "-";
            return Head(block[0].Label) + " - " + Head(block[block.Count - 1].Label);
        }

        static string Head(string s) { return s.Length <= 12 ? s : s.Substring(0, 12); }
    }
}
