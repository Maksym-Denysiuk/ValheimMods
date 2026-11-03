// chat: sends prepared chat lines from BepInEx/config/homecheat/chat.txt, one section at a time (spell bar group 10 "Chat commands").
// Page: docs/HomeCheat-plugin/chat.md. Lines go out through the game's own Chat.SendText, so every player sees them as
// normal chat or a shout from the local player; nothing here touches the console or a cheat flag.
//
// chat.txt format: a section starts with [name]; each non-empty line under it is one message; '#' starts a comment line;
// a line starting "/s " goes as a shout (Talker.Type.Shout), any other line as normal chat (Talker.Type.Normal).
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using HomeCheat.Kit;

namespace HomeCheat.Features.Chat
{
    public sealed class ChatCommand : CommandBase
    {
        const int MaxLines = 10;        // lines sent per section
        const int MaxChars = 250;       // characters per line
        const float Gap = 1.5f;         // seconds between two lines

        public override string Name { get { return "chat"; } }
        protected override string Usage
        {
            get
            {
                return "chat msg=<section> - sends each line of [section] in BepInEx/config/homecheat/chat.txt, " + Gap.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                    + " s apart; a line starting '/s ' is a shout, others normal chat; caps " + MaxLines + " lines per section, " + MaxChars + " characters per line"
                    + " | chat list=1 - lists the sections and their line counts (sends nothing)";
            }
        }

        static string FilePath
        {
            get { return Path.Combine(Path.Combine(Paths.ConfigPath, "homecheat"), "chat.txt"); }
        }

        protected override string Execute(Args a)
        {
            Host.RequireWorld();
            Dictionary<string, List<string>> sections = Read();

            if (a.Flag("list", false))
            {
                if (sections.Count == 0) return "chat: no sections in " + FilePath;
                StringBuilder sb = new StringBuilder("chat: " + sections.Count + " section(s):");
                foreach (KeyValuePair<string, List<string>> s in sections)
                    sb.Append(" ").Append(s.Key).Append(" (").Append(s.Value.Count).Append(")");
                return sb.ToString();
            }

            string msg = a.Required("msg", "a section name of chat.txt (chat list=1 lists them)").Trim().ToLowerInvariant();
            List<string> lines;
            if (!sections.TryGetValue(msg, out lines))
                throw new CommandException("no section [" + msg + "] in chat.txt (chat list=1 lists them)");
            if (lines.Count == 0) throw new CommandException("section [" + msg + "] has no lines");
            if (global::Chat.instance == null) throw new CommandException("the chat is not ready yet: try again in a moment");
            Player me = Players.Local;
            if (me.IsDead()) throw new CommandException("you are dead");

            string note;
            int count = Caps.Clamp("lines", lines.Count, MaxLines, out note);
            int shouts = 0, cut = 0;
            for (int i = 0; i < count; i++)
            {
                string line = lines[i];
                bool shout = line.StartsWith("/s ", StringComparison.OrdinalIgnoreCase);
                string text = shout ? line.Substring(3).Trim() : line;
                if (text.Length > MaxChars) { text = text.Substring(0, MaxChars); cut++; }
                if (shout) shouts++;
                Talker.Type type = shout ? Talker.Type.Shout : Talker.Type.Normal;
                string send = text;
                int n = i + 1;
                Later(i * Gap, delegate
                {
                    if (global::Chat.instance == null || Player.m_localPlayer == null) { HomeApi.Log("chat: line " + n + " dropped (no chat or player)"); return; }
                    global::Chat.instance.SendText(type, send);
                    HomeApi.Log("chat: [" + msg + "] line " + n + " sent as " + type);
                });
            }

            StringBuilder ans = new StringBuilder("chat: [" + msg + "] " + count + " line(s), " + shouts + " shout(s), " + Gap.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " s apart");
            if (note.Length > 0) ans.Append("; ").Append(note);
            if (cut > 0) ans.Append("; ").Append(cut).Append(" line(s) cut at ").Append(MaxChars).Append(" characters");
            return ans.ToString();
        }

        // section name (lower case) -> its message lines, in file order
        static Dictionary<string, List<string>> Read()
        {
            if (!File.Exists(FilePath)) throw new CommandException("no chat.txt at BepInEx/config/homecheat/chat.txt (install.ps1 puts the sample there)");
            Dictionary<string, List<string>> map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            List<string> cur = null;
            foreach (string raw in File.ReadAllLines(FilePath, Encoding.UTF8))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    string name = line.Substring(1, line.Length - 2).Trim().ToLowerInvariant();
                    if (!map.TryGetValue(name, out cur)) { cur = new List<string>(); map[name] = cur; }
                    continue;
                }
                if (cur != null) cur.Add(line);
            }
            return map;
        }
    }
}
