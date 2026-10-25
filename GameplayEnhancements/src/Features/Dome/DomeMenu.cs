// The ward menu (op=dome&step=menu, the one Protect entry of the spell bar; Maksym 2026-10-25), one screen in the look of the
// mines form: Wards placed (New, then your groups Group-1 ... Group-X), Allowed (you, then every player who has joined this
// world, ticked for the picked group), Status (Active, Deactivated, Deleted). Apply runs DomeCommand.Apply: New covers the
// coord start and end points plus 10 % with wards; a group gets the list and the status on every ward, or loses all its wards.
// While it is open the game sees a text input (TextInput.IsVisible, DomePatches): the cursor is free, the player stands still.
// Drawn on this PC only (an interface, not world graphics). Enter applies, Esc closes. Show domes (Hidden / Visible) switches
// the test-aid domes of DomeShow at once, for this PC only.
using System;
using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.Dome
{
    public sealed class DomeMenu : MonoBehaviour
    {
        internal static DomeMenu Current;   // the open menu, null when closed
        internal DomeCommand Cmd;

        static readonly string[] StatusNames = { "Active", "Deactivated", "Deleted" };
        const float W = 900f, H = 660f, Gap = 6f;

        List<DomeCommand.GroupView> groups = new List<DomeCommand.GroupView>();
        List<KeyValuePair<long, string>> players = new List<KeyValuePair<long, string>>();
        int sel;                                  // 0 = New, else the group number
        readonly HashSet<long> allowed = new HashSet<long>();
        int status;                               // index into StatusNames
        bool gaps;                                // New: place the wards that fit and leave gaps at blocked spots (partial=1)
        string newInfo = "", error = "";
        bool newReady;
        long me;
        string myName = "";
        Vector2 scroll;
        GUIStyle sTitle, sLabel, sBtn, sBtnOn, sHint, sErr, sErrWrap;

        internal void Open()
        {
            Current = this;
            error = "";
            Refresh(0);
        }

        internal static void Close() { Current = null; }

        // reads the groups, the players and the New line again, then picks 'pick' (0 = New)
        void Refresh(int pick)
        {
            Player p = Player.m_localPlayer;
            me = p == null ? 0L : p.GetPlayerID();
            myName = p == null ? "" : p.GetPlayerName();
            groups = Cmd.MyGroups();
            players = new List<KeyValuePair<long, string>>();
            foreach (KeyValuePair<long, string> kv in DomePlayers.Known()) if (kv.Key != me) players.Add(kv);
            newInfo = Cmd.NewInfo(out newReady);
            Select(pick);
        }

        void Select(int no)
        {
            DomeCommand.GroupView g = Group(no);
            sel = g == null ? 0 : no;
            allowed.Clear();
            if (g == null) { status = 0; gaps = false; return; }
            status = g.State.Enabled ? 0 : 1;
            foreach (KeyValuePair<long, string> kv in g.State.Permitted)
            {
                allowed.Add(kv.Key);
                bool known = false;
                foreach (KeyValuePair<long, string> pl in players) if (pl.Key == kv.Key) known = true;
                if (!known && kv.Key != me) players.Add(kv);
            }
        }

        DomeCommand.GroupView Group(int no)
        {
            foreach (DomeCommand.GroupView g in groups) if (g.No == no) return g;
            return null;
        }

        bool Ready { get { return sel == 0 ? newReady && status != 2 : true; } }

        void DoApply()
        {
            List<KeyValuePair<long, string>> list = new List<KeyValuePair<long, string>>();
            foreach (KeyValuePair<long, string> kv in players) if (allowed.Contains(kv.Key)) list.Add(kv);
            string answer;
            try { answer = Cmd.Apply(sel, list, StatusNames[status], sel == 0 && gaps); }
            catch (CommandException e) { error = e.Message; return; }
            catch (Exception e) { error = e.Message; HomeApi.LogError("dome menu: " + e); return; }
            HomeApi.Log("dome menu: " + answer);
            Notify.Center(answer);
            Close();
        }

        void Toggle(bool show)
        {
            try { HomeApi.Log("dome menu: " + Cmd.ShowDomes(show, 0.1f).Split('\n')[0]); error = ""; }
            catch (Exception e) { error = e.Message; }
        }

        void OnGUI()
        {
            if (Current != this || Cmd == null) return;
            if (Player.m_localPlayer == null) { Close(); return; }
            Styles();
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { e.Use(); Close(); return; }
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && Ready) { e.Use(); DoApply(); return; }

            float sc = Screen.height / 1080f;
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(sc, sc, 1f));
            float sw = Screen.width / sc, sh = Screen.height / sc;
            Rect win = new Rect((sw - W) / 2f, (sh - H) / 2f, W, H);
            Fill(win, new Color(0.06f, 0.05f, 0.04f, 0.94f));
            Frame(win, 2f, new Color(1f, 0.85f, 0.45f, 0.8f));
            float x = win.x + 20f, y = win.y + 14f, inner = W - 40f;
            GUI.Label(new Rect(x, y, inner, 30f), "Wards", sTitle);
            GUI.Label(new Rect(x, y + 30f, inner, 20f), "Pick New or a group, who is allowed and the status, then Apply (Enter). Esc closes.", sHint);
            y += 60f;

            // Wards placed
            GUI.Label(new Rect(x, y, 140f, 26f), "Wards placed", sLabel);
            float bx = x + 140f, by = y;
            List<KeyValuePair<int, string>> picks = new List<KeyValuePair<int, string>>();
            picks.Add(new KeyValuePair<int, string>(0, "New"));
            Vector3 me3 = Player.m_localPlayer.transform.position;
            foreach (DomeCommand.GroupView g in groups)   // "Group-1 (5 wards, 65 m)": the count and distance tell groups apart
                picks.Add(new KeyValuePair<int, string>(g.No, "Group-" + g.No + " (" + g.Wards + (g.Wards == 1 ? " ward, " : " wards, ")
                    + Fmt.F(DomeWards.DistanceXZ(me3, new Vector3(g.Cx, 0f, g.Cz)), 0) + " m)"));
            foreach (KeyValuePair<int, string> pk in picks)
            {
                float bw = Mathf.Max(80f, sBtn.CalcSize(new GUIContent(pk.Value)).x + 20f);
                if (bx + bw > x + inner) { bx = x + 140f; by += 34f; }
                bool on = sel == pk.Key;
                if (GUI.Button(new Rect(bx, by, bw, 28f), (on ? "> " : "") + pk.Value, on ? sBtnOn : sBtn) && !on) { Select(pk.Key); error = ""; }
                bx += bw + Gap;
            }
            y = by + 34f;
            DomeCommand.GroupView cur = Group(sel);
            string info = cur == null ? newInfo
                : cur.Wards + " ward(s), centre " + Fmt.F(cur.Cx, 0) + "," + Fmt.F(cur.Cz, 0) + ", " + (cur.State.Enabled ? "active" : "deactivated")
                  + ", " + cur.State.Permitted.Count + " allowed; "
                  + Fmt.F(DomeWards.DistanceXZ(Player.m_localPlayer.transform.position, new Vector3(cur.Cx, 0f, cur.Cz)), 0) + " m from you";
            GUI.Label(new Rect(x + 140f, y, inner - 140f, 22f), info, sHint);
            y += 34f;

            // Allowed
            GUI.Label(new Rect(x, y, 140f, 26f), "Allowed", sLabel);
            Rect view = new Rect(x + 140f, y, inner - 140f, 200f);
            int perRow = 4;
            float cw = (view.width - 20f - Gap * (perRow - 1)) / perRow;
            int count = players.Count + 1;
            Rect content = new Rect(0f, 0f, view.width - 20f, Mathf.Max(view.height, ((count + perRow - 1) / perRow) * 34f));
            scroll = GUI.BeginScrollView(view, scroll, content);
            for (int i = 0; i < count; i++)
            {
                Rect r = new Rect((i % perRow) * (cw + Gap), (i / perRow) * 34f, cw, 28f);
                if (i == 0)
                {
                    GUI.enabled = false;
                    GUI.Button(r, "[x] " + myName + " (you)", sBtnOn);
                    GUI.enabled = true;
                    continue;
                }
                KeyValuePair<long, string> pl = players[i - 1];
                bool on = allowed.Contains(pl.Key);
                if (GUI.Button(r, (on ? "[x] " : "[ ] ") + pl.Value, on ? sBtnOn : sBtn))
                {
                    if (on) allowed.Remove(pl.Key); else allowed.Add(pl.Key);
                }
            }
            GUI.EndScrollView();
            if (players.Count == 0) GUI.Label(new Rect(view.x, view.y + 34f, view.width, 22f), "no other player has joined this world yet", sHint);
            y += 210f;

            // Status
            GUI.Label(new Rect(x, y, 140f, 26f), "Status", sLabel);
            bx = x + 140f;
            for (int i = 0; i < StatusNames.Length; i++)
            {
                bool on = status == i;
                bool was = GUI.enabled;
                if (i == 2 && sel == 0) GUI.enabled = false;   // nothing to delete yet
                if (GUI.Button(new Rect(bx, y, 120f, 28f), (on ? "> " : "") + StatusNames[i], on ? sBtnOn : sBtn)) status = i;
                GUI.enabled = was;
                bx += 120f + Gap;
            }
            y += 40f;
            if (sel == 0)
            {
                // what happens when a ward spot is on water, a building, a slope or another player's ward
                GUI.Label(new Rect(x, y, 140f, 26f), "Blocked spots", sLabel);
                if (GUI.Button(new Rect(x + 140f, y, 200f, 28f), (gaps ? "" : "> ") + "Refuse (place nothing)", gaps ? sBtn : sBtnOn)) gaps = false;
                if (GUI.Button(new Rect(x + 140f + 200f + Gap, y, 200f, 28f), (gaps ? "> " : "") + "Leave gaps", gaps ? sBtnOn : sBtn)) gaps = true;
                y += 40f;
            }
            // a view of this PC only, applied at once (no Apply): the test-aid domes of DomeShow
            GUI.Label(new Rect(x, y, 140f, 26f), "Show domes", sLabel);
            bool showing = DomeShow.On;
            if (GUI.Button(new Rect(x + 140f, y, 120f, 28f), (showing ? "" : "> ") + "Hidden", showing ? sBtn : sBtnOn) && showing) Toggle(false);
            if (GUI.Button(new Rect(x + 140f + 120f + Gap, y, 120f, 28f), (showing ? "> " : "") + "Visible", showing ? sBtnOn : sBtn) && !showing) Toggle(true);
            GUI.Label(new Rect(x + 140f + 2f * (120f + Gap) + 8f, y + 4f, 360f, 22f), "only you see them, 10 % visible, active wards only", sHint);
            y += 40f;
            if (sel != 0 && status == 2) GUI.Label(new Rect(x + 140f, y, inner - 140f, 22f), "Apply removes all " + (cur == null ? 0 : cur.Wards) + " ward(s) of this group (no materials back)", sErr);

            float bottom = win.yMax - 56f;
            if (error.Length > 0) GUI.Label(new Rect(x, bottom - 92f, inner, 96f), error, sErrWrap);
            GUI.enabled = Ready;
            string apply = sel == 0 ? "Place" : (status == 2 ? "Delete" : "Apply");
            if (GUI.Button(new Rect(win.xMax - 260f, bottom + 8f, 120f, 36f), apply, sBtnOn) && Ready) { GUI.enabled = true; GUI.matrix = old; DoApply(); return; }
            GUI.enabled = true;
            if (GUI.Button(new Rect(win.xMax - 130f, bottom + 8f, 110f, 36f), "Cancel", sBtn)) { GUI.matrix = old; Close(); return; }
            GUI.Label(new Rect(x, bottom + 16f, inner - 300f, 24f), (sel == 0 ? "New" : "Group-" + sel) + "   allowed: " + (allowed.Count + 1) + "   " + StatusNames[status] + (sel == 0 && gaps ? "   leave gaps" : ""), sHint);
            GUI.matrix = old;
        }

        void Styles()
        {
            if (sTitle != null) return;
            sTitle = new GUIStyle(GUI.skin.label); sTitle.fontSize = 22; sTitle.fontStyle = FontStyle.Bold; sTitle.normal.textColor = new Color(1f, 0.85f, 0.45f);
            sLabel = new GUIStyle(GUI.skin.label); sLabel.fontSize = 15; sLabel.fontStyle = FontStyle.Bold; sLabel.normal.textColor = Color.white;
            sHint = new GUIStyle(GUI.skin.label); sHint.fontSize = 13; sHint.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            sErr = new GUIStyle(sHint); sErr.normal.textColor = new Color(1f, 0.45f, 0.35f);
            sErrWrap = new GUIStyle(sErr); sErrWrap.wordWrap = true; sErrWrap.alignment = TextAnchor.LowerLeft;
            sBtn = new GUIStyle(GUI.skin.button); sBtn.fontSize = 14; sBtn.alignment = TextAnchor.MiddleLeft; sBtn.padding.left = 10;
            sBtnOn = new GUIStyle(sBtn); sBtnOn.normal.textColor = new Color(1f, 0.85f, 0.45f); sBtnOn.fontStyle = FontStyle.Bold;
        }

        static void Fill(Rect r, Color c)
        {
            Color o = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = o;
        }

        static void Frame(Rect r, float t, Color c)
        {
            Fill(new Rect(r.x, r.y, r.width, t), c); Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
            Fill(new Rect(r.x, r.y, t, r.height), c); Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        void OnDestroy() { if (Current == this) Current = null; }
    }
}
