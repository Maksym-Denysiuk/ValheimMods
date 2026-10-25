using System;
using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.Spells
{
    // The answers of a spell with questions (ask= flags), all on one screen like the hammer's build menu (Maksym 2026-10-25;
    // replaces the radial answer chain). Fixed answers are a row of buttons, the first one picked; @players is a row of names;
    // @crops, @trees and @prefabs are a grid of tiles with the item's icon and name; @prefabs adds a search line and its
    // categories. Placement by an area (an answer "coords" or "patch") needs no quantity: count, rows and perrow stay on screen
    // greyed out and are left out of the command, which works the number out from the spacing and the area (Maksym 2026-10-25).
    // Enter or Cast casts, Esc or Cancel closes.
    // While it is open the game sees a text input (TextInput.IsVisible, SpellPatches): the cursor is free, the player stands still.
    internal sealed class SpellForm
    {
        public static SpellForm Current;

        static readonly string[] Quantity = new string[] { "count", "rows", "perrow", "radius" };   // greyed when an area sets the size
        static readonly string[] AreaAnswers = new string[] { "coords", "patch" };   // an area that sets the number itself
        const float W = 1180f, H = 760f, Tile = 92f, TileH = 108f, Gap = 6f;

        readonly SpellBar bar;
        readonly SpellAskChain chain;
        readonly List<SpellAsk> asks;
        readonly List<List<SpellAskOption>> options = new List<List<SpellAskOption>>();
        readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
        string search = "", group = "";
        Vector2 scroll;
        bool focusSearch = true;
        GUIStyle sTitle, sLabel, sBtn, sBtnOn, sTile, sHint;

        SpellForm(SpellBar b, SpellAskChain c)
        {
            bar = b; chain = c; asks = c.Spell.Asks;
            foreach (SpellAsk a in asks)
            {
                List<SpellAskOption> o = SpellAskSources.Resolve(a);
                options.Add(o);
                if (a.Source.Length == 0)   // fixed answers start on the *default, else the first one that can be picked
                {
                    foreach (SpellAskOption op in o) if (op.Default && !op.Off) { chain.Answers[a.Key] = op.Value; break; }
                    if (!chain.Answers.ContainsKey(a.Key))
                        foreach (SpellAskOption op in o) if (!op.Off) { chain.Answers[a.Key] = op.Value; break; }
                }
            }
        }

        public static void Open(SpellBar b, SpellAskChain c)
        {
            Current = new SpellForm(b, c);
            HomeApi.Log("spells: form opened for " + c.Spell.Label);
        }

        public static void Close() { Current = null; }

        bool ByArea
        {
            get { foreach (string v in chain.Answers.Values) if (Array.IndexOf(AreaAnswers, v) >= 0) return true; return false; }
        }

        // shown greyed, not needed, left out of the command: a quantity question while the area sets the number, or a question
        // whose ?other=value condition does not hold
        bool Disabled(SpellAsk a)
        {
            if (a.WhenKey.Length > 0)
            {
                string v;
                if (!chain.Answers.TryGetValue(a.WhenKey, out v) || v != a.WhenValue) return true;
            }
            return ByArea && Array.IndexOf(Quantity, a.Key.ToLowerInvariant()) >= 0;
        }

        bool Ready
        {
            get
            {
                foreach (SpellAsk a in asks) if (!Disabled(a) && !chain.Answers.ContainsKey(a.Key)) return false;
                return true;
            }
        }

        void Cast()
        {
            chain.Skip.Clear();
            foreach (SpellAsk a in asks) if (Disabled(a)) chain.Skip.Add(a.Key);
            Close();
            bar.CastChain(chain);
        }

        void Styles()
        {
            if (sTitle != null) return;
            sTitle = new GUIStyle(GUI.skin.label); sTitle.fontSize = 22; sTitle.fontStyle = FontStyle.Bold; sTitle.normal.textColor = new Color(1f, 0.85f, 0.45f);
            sLabel = new GUIStyle(GUI.skin.label); sLabel.fontSize = 15; sLabel.fontStyle = FontStyle.Bold; sLabel.normal.textColor = Color.white;
            sHint = new GUIStyle(GUI.skin.label); sHint.fontSize = 13; sHint.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            sBtn = new GUIStyle(GUI.skin.button); sBtn.fontSize = 14;
            sBtnOn = new GUIStyle(sBtn); sBtnOn.normal.textColor = new Color(1f, 0.85f, 0.45f); sBtnOn.fontStyle = FontStyle.Bold;
            sTile = new GUIStyle(GUI.skin.label); sTile.fontSize = 11; sTile.alignment = TextAnchor.UpperCenter; sTile.wordWrap = true; sTile.normal.textColor = Color.white;
        }

        // drawn from SpellBar.OnGUI (every event, so buttons and the text field work)
        public void Draw()
        {
            Styles();
            Event e = Event.current;
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Escape)) { Close(); e.Use(); return; }
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && Ready) { e.Use(); Cast(); return; }

            float sc = Screen.height / 1080f;
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(sc, sc, 1f));
            float sw = Screen.width / sc, sh = Screen.height / sc;
            Rect win = new Rect((sw - W) / 2f, (sh - H) / 2f, W, H);
            SpellBar.Fill(win, new Color(0.06f, 0.05f, 0.04f, 0.94f));
            SpellBar.Frame(win, 2f, new Color(1f, 0.85f, 0.45f, 0.8f));
            float x = win.x + 20f, y = win.y + 14f, inner = W - 40f;
            GUI.Label(new Rect(x, y, inner, 30f), chain.Spell.Label, sTitle);
            GUI.Label(new Rect(x, y + 30f, inner, 20f), "Pick the answers, then Cast (Enter). Esc closes without casting." + (ByArea ? "  By area: the number comes from the spacing and the area." : ""), sHint);
            y += 58f;

            int gridAsk = -1;
            for (int i = 0; i < asks.Count; i++)
            {
                SpellAsk a = asks[i];
                if (a.Source == "@crops" || a.Source == "@trees" || a.Source == "@prefabs") { if (gridAsk < 0) gridAsk = i; continue; }
                bool off = Disabled(a);
                if (off) GUI.enabled = false;
                y = ButtonRow(a, options[i], x, y, inner);
                GUI.enabled = true;
            }
            float bottom = win.yMax - 56f;
            if (gridAsk >= 0) Grid(asks[gridAsk], options[gridAsk], new Rect(x, y, inner, bottom - y - 8f));

            GUI.enabled = Ready;
            string castText = chain.Spell.Button.Length > 0 ? chain.Spell.Button : "Cast";
            if (GUI.Button(new Rect(win.xMax - 260f, bottom + 8f, 120f, 36f), castText, sBtnOn) && Ready) { GUI.enabled = true; GUI.matrix = old; Cast(); return; }
            GUI.enabled = true;
            if (GUI.Button(new Rect(win.xMax - 130f, bottom + 8f, 110f, 36f), "Cancel", sBtn)) { GUI.matrix = old; Close(); return; }
            GUI.Label(new Rect(x, bottom + 16f, inner - 300f, 24f), Summary(), sHint);
            GUI.matrix = old;
        }

        string Summary()
        {
            List<string> parts = new List<string>();
            foreach (SpellAsk a in asks)
            {
                if (Disabled(a)) continue;
                string v;
                parts.Add(Name(a) + ": " + (chain.Answers.TryGetValue(a.Key, out v) ? v : "-"));
            }
            return string.Join("   ", parts.ToArray());
        }

        // an answer that can be picked now: not @off, and above the answer of the question it must exceed (ask=key>other)
        bool Allowed(SpellAsk a, SpellAskOption o)
        {
            if (o.Off) return false;
            if (a.GreaterThan.Length == 0) return true;
            string other; float ov, v;
            if (!chain.Answers.TryGetValue(a.GreaterThan, out other)) return true;
            if (!float.TryParse(other, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out ov)) return true;
            if (!float.TryParse(o.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v)) return true;
            return v > ov;
        }

        // a question with few answers: a label and a wrapping row of buttons
        float ButtonRow(SpellAsk a, List<SpellAskOption> opts, float x, float y, float inner)
        {
            // an answer no longer allowed (the other one changed): move to the first allowed
            string now;
            if (chain.Answers.TryGetValue(a.Key, out now))
            {
                SpellAskOption cur0 = null;
                foreach (SpellAskOption o in opts) if (o.Value == now) cur0 = o;
                if (cur0 != null && !Allowed(a, cur0))
                {
                    chain.Answers.Remove(a.Key);
                    foreach (SpellAskOption o in opts) if (Allowed(a, o)) { chain.Answers[a.Key] = o.Value; break; }
                }
            }
            GUI.Label(new Rect(x, y, 130f, 26f), Name(a), sLabel);
            float bx = x + 130f, by = y;
            string cur;
            chain.Answers.TryGetValue(a.Key, out cur);
            foreach (SpellAskOption o in opts)
            {
                float bw = Mathf.Max(70f, sBtn.CalcSize(new GUIContent(o.Label)).x + 20f);
                if (bx + bw > x + inner) { bx = x + 130f; by += 34f; }
                bool on = cur == o.Value;
                bool was = GUI.enabled;
                if (!Allowed(a, o)) GUI.enabled = false;
                if (GUI.Button(new Rect(bx, by, bw, 28f), (on ? "> " : "") + o.Label, on ? sBtnOn : sBtn)) chain.Answers[a.Key] = o.Value;
                GUI.enabled = was;
                bx += bw + Gap;
            }
            if (opts.Count == 0) GUI.Label(new Rect(bx, by, 300f, 26f), "(no answers)", sHint);
            return by + 40f;
        }

        // a question with many answers: search line (@prefabs), categories, a scrolling grid of icon tiles
        void Grid(SpellAsk a, List<SpellAskOption> opts, Rect area)
        {
            float y = area.y;
            GUI.Label(new Rect(area.x, y, 120f, 26f), Name(a), sLabel);
            if (a.Source == "@prefabs")
            {
                GUI.Label(new Rect(area.x + 120f, y + 2f, 60f, 24f), "Search", sHint);
                GUI.SetNextControlName("hc_spell_search");
                search = GUI.TextField(new Rect(area.x + 180f, y, 320f, 26f), search ?? "");
                if (focusSearch) { GUI.FocusControl("hc_spell_search"); focusSearch = false; }
                // categories
                List<string> groups = new List<string>();
                foreach (SpellAskOption o in opts) if (o.Group.Length > 0 && !groups.Contains(o.Group)) groups.Add(o.Group);
                float gx = area.x + 520f, gy = y;
                foreach (string g in new string[] { "" })
                    if (GUI.Button(new Rect(gx, gy, 50f, 26f), "All", group.Length == 0 ? sBtnOn : sBtn)) group = g;
                gx += 56f;
                foreach (string g in groups)
                {
                    string lbl = g.StartsWith("Items: ") ? g.Substring(7) : g;
                    float bw = sBtn.CalcSize(new GUIContent(lbl)).x + 14f;
                    if (gx + bw > area.xMax) { gx = area.x + 520f; gy += 30f; }
                    if (GUI.Button(new Rect(gx, gy, bw, 26f), lbl, group == g ? sBtnOn : sBtn)) group = g;
                    gx += bw + 4f;
                }
                y = gy + 34f;
            }
            else y += 30f;

            List<SpellAskOption> shown = new List<SpellAskOption>();
            foreach (SpellAskOption o in opts)
            {
                if (group.Length > 0 && o.Group != group) continue;
                if (search.Length > 0 && o.Label.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                shown.Add(o);
            }
            Rect view = new Rect(area.x, y, area.width, area.yMax - y);
            int perRow = Mathf.Max(1, Mathf.FloorToInt((view.width - 20f) / (Tile + Gap)));
            int rows = (shown.Count + perRow - 1) / perRow;
            Rect content = new Rect(0f, 0f, view.width - 20f, Mathf.Max(view.height, rows * (TileH + Gap)));
            string cur;
            chain.Answers.TryGetValue(a.Key, out cur);
            scroll = GUI.BeginScrollView(view, scroll, content);
            for (int i = 0; i < shown.Count; i++)
            {
                SpellAskOption o = shown[i];
                Rect r = new Rect((i % perRow) * (Tile + Gap), (i / perRow) * (TileH + Gap), Tile, TileH);
                if (r.yMax < scroll.y || r.y > scroll.y + view.height) continue;   // only what is in view
                bool on = cur == o.Value;
                SpellBar.Fill(r, on ? new Color(0.35f, 0.27f, 0.08f, 0.95f) : new Color(0.12f, 0.11f, 0.10f, 0.9f));
                if (on) SpellBar.Frame(r, 2f, new Color(1f, 0.85f, 0.45f, 1f));
                Sprite sp = Icon(o.Value);
                if (sp != null) SpellBar.DrawSprite(new Rect(r.x + (Tile - 56f) / 2f, r.y + 6f, 56f, 56f), sp, 1f);
                GUI.Label(new Rect(r.x + 2f, r.y + 64f, Tile - 4f, TileH - 64f), o.Label, sTile);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) chain.Answers[a.Key] = o.Value;
            }
            GUI.EndScrollView();
            if (shown.Count == 0) GUI.Label(new Rect(view.x, view.y, 300f, 24f), "nothing matches", sHint);
        }

        // the icon of a prefab: an item's own icon, a piece's icon, a pickable's item, a creature's trophy; cached
        Sprite Icon(string prefab)
        {
            Sprite sp;
            if (icons.TryGetValue(prefab, out sp)) return sp;
            sp = null;
            try
            {
                GameObject g = Prefabs.Get(prefab);
                if (g == null && ObjectDB.instance != null) g = ObjectDB.instance.GetItemPrefab(prefab);
                if (g != null)
                {
                    sp = ItemIcon(g);
                    Piece pc = g.GetComponent<Piece>();
                    if (sp == null && pc != null) sp = pc.m_icon;
                    Pickable pk = g.GetComponent<Pickable>();
                    if (sp == null && pk != null && pk.m_itemPrefab != null) sp = ItemIcon(pk.m_itemPrefab);
                    CharacterDrop cd = g.GetComponent<CharacterDrop>();
                    if (sp == null && cd != null && cd.m_drops != null)
                        foreach (CharacterDrop.Drop d in cd.m_drops)
                            if (d != null && d.m_prefab != null && d.m_prefab.name.StartsWith("Trophy")) { sp = ItemIcon(d.m_prefab); break; }
                }
            }
            catch (Exception) { }
            icons[prefab] = sp;
            return sp;
        }

        static Sprite ItemIcon(GameObject g)
        {
            ItemDrop d = g.GetComponent<ItemDrop>();
            if (d == null || d.m_itemData == null || d.m_itemData.m_shared == null) return null;
            Sprite[] ic = d.m_itemData.m_shared.m_icons;
            return ic != null && ic.Length > 0 ? ic[0] : null;
        }

        static string Name(SpellAsk a) { return a.Label.Length > 0 ? a.Label : Cap(a.Key); }

        // the name of a question on screen when the spell line gives none
        static string Cap(string k)
        {
            switch (k.ToLowerInvariant())
            {
                case "perrow": return "Items in row";
                case "rows": return "Rows";
                case "count": return "Quantity";
                case "size": return "Item Size";
            }
            return k.Length == 0 ? k : char.ToUpperInvariant(k[0]) + k.Substring(1);
        }
    }
}
