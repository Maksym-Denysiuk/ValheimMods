// Messages to the player in the game's own style (Maksym 2026-10-25): the big text in the middle of the screen, the one the game
// uses for "The ground is shaking" (MessageHud, MessageType.Center). Local screen only; nothing when no HUD is up (main menu,
// loading). Every command error goes through Error (CommandBase.Run), the spell bar's messages through Center.
using System;
using System.Text;

namespace HomeCheat.Kit
{
    public static class Notify
    {
        const int MaxLength = 140;   // the whole message
        const int LineWidth = 50;    // characters per line: a longer line runs off the screen (Maksym 2026-10-25)
        const string TextSize = "70%";   // smaller than the game's own centre text (Maksym 2026-10-25); a TextMeshPro size tag

        public static void Center(string text)
        {
            if (string.IsNullOrEmpty(text) || MessageHud.instance == null || Player.m_localPlayer == null) return;
            try { MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "<size=" + TextSize + ">" + Short(text) + "</size>"); }
            catch (Exception e) { HomeApi.Log("notify: " + e.Message); }
        }

        // an answer starting with "ERROR: ": shown without that prefix
        public static void Error(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            string t = text.StartsWith("ERROR: ") ? text.Substring(7) : text;
            Center(t);
        }

        // first line, cut to MaxLength, wrapped at spaces into lines of LineWidth (a longer word is split)
        static string Short(string text)
        {
            string t = text.Split('\n')[0].Trim();
            if (t.Length > MaxLength) t = t.Substring(0, MaxLength - 3) + "...";
            StringBuilder sb = new StringBuilder();
            int line = 0;
            foreach (string w0 in t.Split(' '))
            {
                string w = w0;
                while (w.Length > LineWidth)
                {
                    if (line > 0) { sb.Append('\n'); line = 0; }
                    sb.Append(w.Substring(0, LineWidth)).Append('\n');
                    w = w.Substring(LineWidth);
                }
                if (w.Length == 0) continue;
                if (line > 0 && line + 1 + w.Length > LineWidth) { sb.Append('\n'); line = 0; }
                else if (line > 0) { sb.Append(' '); line++; }
                sb.Append(w);
                line += w.Length;
            }
            return sb.ToString();
        }
    }
}
