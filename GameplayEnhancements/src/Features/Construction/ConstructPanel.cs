// The construct panel (card A1, Maksym 2026-10-25): the constructions, their versions and materials, the ground mode, the
// state of this world's plan, and one button per step. Every button sends the same form as the inbox (HomeApi.Exec
// "op=construct&step=..."), so the panel holds no building logic of its own and its answer line is the command's answer.
// While it is open the game sees a text input (ConstructPatches): the cursor is free and the player stands still.
// Drawn from OnGUI on an object owned for the world (OwnInWorld); Esc or Close hides it.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HomeCheat.Features.Construction
{
    public sealed class ConstructPanel : MonoBehaviour
    {
        public static ConstructPanel Current;
        public bool Visible;

        static readonly string[] Grounds = new string[] { "auto", "fit", "flatten", "none" };
        const float W = 1100f, H = 720f;

        List<BlueprintEntry> entries = new List<BlueprintEntry>();
        List<KeyValuePair<string, int>> materials = new List<KeyValuePair<string, int>>();
        int selected;
        string version = "", material = MaterialSwap.Blueprint, ground = "auto", answer = "", state = "";
        bool undoArmed;
        Vector2 listScroll;
        GUIStyle sTitle, sLabel, sText, sBtn, sBtnOn;

        public void Open()
        {
            Visible = true;
            undoArmed = false;
            Refresh();
        }

        void Refresh()
        {
            try
            {
                entries = BlueprintLibrary.Scan(ConstructCommand.Dir);
                if (selected >= entries.Count) selected = 0;
                materials = entries.Count > 0 ? MaterialSwap.Options(Chosen()) : new List<KeyValuePair<string, int>>();
                bool known = material == MaterialSwap.Blueprint;
                foreach (KeyValuePair<string, int> kv in materials) if (kv.Key == material) known = true;
                if (!known) material = MaterialSwap.Blueprint;
                state = HomeApi.Exec("op=construct&step=status");
            }
            catch (Exception e) { state = "ERROR: " + e.Message; }
        }

        // the entry to build: a version of a folder when one is picked
        BlueprintEntry Chosen()
        {
            BlueprintEntry e = entries[selected];
            if (e.Parts.Count == 1) return e;
            string note;
            BlueprintEntry one = BlueprintLibrary.Resolve(entries, NameArg(), out note);
            return one != null ? one : e;
        }

        string NameArg()
        {
            BlueprintEntry e = entries[selected];
            return e.Parts.Count > 1 && version.Length > 0 ? e.Name + "/" + version : e.Name;
        }

        void Send(string form)
        {
            try { answer = HomeApi.Exec(form); }
            catch (Exception e) { answer = "ERROR: " + e.Message; }
            Refresh();
        }

        void Styles()
        {
            if (sTitle != null) return;
            sTitle = new GUIStyle(GUI.skin.label); sTitle.fontSize = 22; sTitle.fontStyle = FontStyle.Bold; sTitle.normal.textColor = new Color(1f, 0.85f, 0.45f);
            sLabel = new GUIStyle(GUI.skin.label); sLabel.fontSize = 15; sLabel.fontStyle = FontStyle.Bold; sLabel.normal.textColor = Color.white;
            sText = new GUIStyle(GUI.skin.label); sText.fontSize = 13; sText.wordWrap = true; sText.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            sBtn = new GUIStyle(GUI.skin.button); sBtn.fontSize = 14;
            sBtnOn = new GUIStyle(sBtn); sBtnOn.normal.textColor = new Color(1f, 0.85f, 0.45f); sBtnOn.fontStyle = FontStyle.Bold;
        }

        void OnGUI()
        {
            if (!Visible) return;
            Event ev = Event.current;
            if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Escape) { Visible = false; ev.Use(); return; }
            try { Draw(); }
            catch (Exception e) { HomeApi.Log("construct: panel failed: " + e.Message); Visible = false; }
        }

        void Draw()
        {
            Styles();
            float sc = Screen.height / 1080f;
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(sc, sc, 1f));
            float sw = Screen.width / sc, sh = Screen.height / sc;
            Rect win = new Rect((sw - W) / 2f, (sh - H) / 2f, W, H);
            GUI.Box(win, "");
            GUI.Box(win, "");
            float x = win.x + 20f, y = win.y + 14f;
            GUI.Label(new Rect(x, y, 400f, 30f), "Construct", sTitle);
            if (GUI.Button(new Rect(win.xMax - 120f, y, 100f, 30f), "Close", sBtn)) { Visible = false; GUI.matrix = old; return; }
            y += 40f;

            // left: the constructions (and the versions of a folder)
            GUI.Label(new Rect(x, y, 340f, 24f), "Constructions", sLabel);
            Rect list = new Rect(x, y + 28f, 340f, 300f);
            float rowH = 32f;
            int rows = 0;
            foreach (BlueprintEntry en in entries) rows += 1 + (en.Parts.Count > 1 ? en.Parts.Count : 0);
            listScroll = GUI.BeginScrollView(list, listScroll, new Rect(0f, 0f, 320f, Mathf.Max(300f, rows * rowH)));
            float ly = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                BlueprintEntry en = entries[i];
                string label = en.Name + "  (" + en.PieceCount + (en.Parts.Count > 1 ? ", " + en.Parts.Count + " versions" : "") + ")";
                if (GUI.Button(new Rect(0f, ly, 320f, rowH - 4f), label, i == selected ? sBtnOn : sBtn)) { selected = i; version = ""; Refresh(); }
                ly += rowH;
                if (en.Parts.Count > 1)
                    foreach (Blueprint b in en.Parts)
                    {
                        string stem = System.IO.Path.GetFileNameWithoutExtension(b.File);
                        bool on = i == selected && version == stem;
                        if (GUI.Button(new Rect(24f, ly, 296f, rowH - 4f), "version " + stem + " (" + b.Pieces.Count + ")", on ? sBtnOn : sBtn)) { selected = i; version = stem; Refresh(); }
                        ly += rowH;
                    }
            }
            GUI.EndScrollView();

            // right: material and ground
            float rx = x + 370f, rw = win.xMax - rx - 20f, ry = y;
            GUI.Label(new Rect(rx, ry, rw, 24f), "Material", sLabel);
            ry += 28f;
            float bx = rx;
            List<KeyValuePair<string, int>> opts = new List<KeyValuePair<string, int>>();
            opts.Add(new KeyValuePair<string, int>(MaterialSwap.Blueprint, -1));
            opts.AddRange(materials);
            foreach (KeyValuePair<string, int> kv in opts)
            {
                string label = kv.Value < 0 ? "as captured" : kv.Key + " (" + kv.Value + ")";
                float bw = Mathf.Max(110f, label.Length * 8f + 20f);
                if (bx + bw > rx + rw) { bx = rx; ry += 34f; }
                if (GUI.Button(new Rect(bx, ry, bw, 30f), label, material == kv.Key ? sBtnOn : sBtn)) material = kv.Key;
                bx += bw + 6f;
            }
            ry += 44f;
            GUI.Label(new Rect(rx, ry, rw, 24f), "Ground", sLabel);
            ry += 28f;
            bx = rx;
            foreach (string g in Grounds)
            {
                if (GUI.Button(new Rect(bx, ry, 100f, 30f), g, ground == g ? sBtnOn : sBtn)) ground = g;
                bx += 106f;
            }
            ry += 38f;
            GUI.Label(new Rect(rx, ry, rw, 40f), "auto: on terrain levelled and raised under stone (fit), on a piece or a rock unchanged; flatten: one flat level; none: as it is", sText);

            // the state of this world's plan and builds (step=status)
            float sy = y + 344f;
            GUI.Label(new Rect(x, sy, 300f, 24f), "State", sLabel);
            GUI.Label(new Rect(x, sy + 26f, W - 40f, 110f), Short(state, 5), sText);

            // the steps
            float by = win.yMax - 150f;
            bool any = entries.Count > 0;
            GUI.enabled = any;
            string name = any ? NameArg() : "";
            if (GUI.Button(new Rect(x, by, 170f, 36f), "Preview here", sBtn))
                Send("op=construct&step=preview&name=" + name + "&material=" + material + "&ground=" + ground);
            GUI.enabled = true;
            if (GUI.Button(new Rect(x + 180f, by, 120f, 36f), "Rotate 90", sBtn)) Send("op=construct&step=rotate&by=90");
            if (GUI.Button(new Rect(x + 310f, by, 130f, 36f), "Rotate 22.5", sBtn)) Send("op=construct&step=rotate&by=22.5");
            if (GUI.Button(new Rect(x + 450f, by, 200f, 36f), "Start construction", sBtnOn)) Send("op=construct&step=start&confirm=1");
            if (GUI.Button(new Rect(x + 660f, by, 170f, 36f), "Remove poles", sBtn)) Send("op=construct&step=remove");
            if (GUI.Button(new Rect(x + 840f, by, 220f, 36f), undoArmed ? "Click again: undo" : "Undo last build", undoArmed ? sBtnOn : sBtn))
            {
                if (undoArmed) { undoArmed = false; Send("op=construct&step=undo&items=1&confirm=1"); }
                else { undoArmed = true; answer = "undo removes the last build and its dropped items (the ground stays): click again to confirm"; }
            }
            GUI.Label(new Rect(x, by + 46f, W - 40f, 90f), Short(answer, 4), sText);
            GUI.matrix = old;
        }

        static string Short(string text, int lines)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string[] l = text.Split('\n');
            return l.Length <= lines ? text : string.Join("\n", l, 0, lines) + "\n...";
        }
    }
}
