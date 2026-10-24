using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;
using Valheim.UI;

namespace HomeCheat.Features.Spells
{
    // ---- the bar: action bar drawing, input, casting, placing spells on cells -------------------------------------------
    [DefaultExecutionOrder(-1000)]
    public sealed class SpellBar : MonoBehaviour
    {
        const BindingFlags AllF = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;


        SpellFile file = new SpellFile();

        Dictionary<string, float> flash = new Dictionary<string, float>();

        SpellRootConfig root;
        bool barVisible, inputOk;
        string toast = ""; float toastUntil; bool toastErr;
        string baseline = "";
        string lastCast = "-";
        GUIStyle sKey, sName, sToast;
        Spell assigning;                        // the spell waiting for a Hotbar cell (Shift+click on it in a radial group)
        float radialGoneAt;                     // since when the radial has been closed while a spell waits (0 = it is open)
        Dictionary<RadialMenuElement, string> cellOf = new Dictionary<RadialMenuElement, string>();   // radial Hotbar element -> cell, for RMB
        MethodInfo mQueuedOpen;
        Dictionary<int, Sprite> iconCache = new Dictionary<int, Sprite>();

        // the bar of the loaded build (SpellsSettings.Current)
        public static SpellBar Find() { return SpellsSettings.ActiveBar; }

        internal SpellRootConfig Root() { if (root == null) root = new SpellRootConfig(this); return root; }
        internal SpellGroup GroupAt(int slot) { return file.GroupAt(slot); }

        void Awake() { file.Load(false); }

        // the patches belong to the command (removed on unload); the bar only forgets itself
        void OnDestroy()
        {
            if (SpellsSettings.Current != null && SpellsSettings.Current.Bar == this) SpellsSettings.Current.Bar = null;
        }

        // ---- the spell file (SpellFile does the work) ----
        public string LoadFile(bool report) { return file.Load(report); }
        public void CheckFile() { file.CheckFile(); }
        public string ResetFile() { return file.ResetFile(); }
        public string ResetPins() { return file.ResetPins(); }
        public string List() { return file.List(); }
        public string SetPin(string key, string spellId) { return file.SetPin(key, spellId); }

        // ---- icons: the game's own sprites ------------------------------------------------------------------------

        // slot -> icon key. Reuses the icons of the vanilla radial for now (hotbar, emote, item-group mappings).
        public static string SlotIcon(int slot)
        {
            switch (slot)
            {
                case 2: return "weaponstools";
                case 3: return "armor_utility";
                case 4: return "emote";
                case 5: return "consumables";
                case 6: return "allitems";
                case 7: return "consumables";
                case 8: return "weaponstools";
                default: return "hotbar";
            }
        }

        public static Sprite IconFor(string key)
        {
            try
            {
                RadialDataSO so = RadialData.SO;
                if (key == "hotbar") return so.HotbarGroupConfig.Sprite;
                if (key == "emote") return so.EmoteGroupConfig.Sprite;
                return so.ItemGroupMappings.GetMapping(key).Sprite;
            }
            catch (Exception) { return null; }
        }

        // ---- radial leaf: a game element that calls our code ------------------------------------------------------------------

        // A leaf is the game's GroupElement prefab with a name and an icon; its Interact and CloseOnInteract are replaced, so selecting it
        // casts the spell (CloseOnInteract false = sticky: the radial stays open).
        // Shift+click does not cast: it picks the spell up and opens the Hotbar section to choose a cell (see BeginPlace).
        internal RadialMenuElement MakeLeaf(Spell s, IRadialConfig back, RadialBase radial, Sprite icon, string subtitle)
        {
            GroupElement ge = UnityEngine.Object.Instantiate<GroupElement>(RadialData.SO.GroupElement);
            ge.Init(new SpellLeafConfig(s.Label, icon), back, radial);
            SetSubTitle(ge, subtitle);
            ge.Interact = delegate()
            {
                if (SpellsSettings.ShiftHeld()) BeginPlace(s, radial); else CastSpell(s, false, null);
                return true;
            };
            ge.CloseOnInteract = delegate() { return SpellsSettings.ShiftHeld() ? false : !s.Sticky; };
            return ge;
        }

        // a Hotbar section: the spell of one bar cell (or an empty slot). Click casts it; while a spell waits to be placed a click puts it here.
        internal RadialMenuElement MakeCellElement(string cell, IRadialConfig back, RadialBase radial)
        {
            Spell s;
            bool has = file.Pins.TryGetValue(cell, out s);
            bool placing = assigning != null;
            string key = cell[0] == 'S' ? "Shift+" + cell[1] : cell;
            GroupElement ge = UnityEngine.Object.Instantiate<GroupElement>(RadialData.SO.GroupElement);
            ge.Init(new SpellLeafConfig(has ? s.Label : "(empty)", has ? IconOf(s) : null), back, radial);
            SetSubTitle(ge, placing ? key + "   click: place \"" + assigning.Label + "\" here" : key + (has ? "   Shift+RMB: empty this cell" : "   Shift+click a spell to place it"));
            ge.Interact = delegate()
            {
                if (assigning != null) AssignTo(cell);
                else if (has) CastSpell(s, false, cell);
                return true;
            };
            ge.CloseOnInteract = delegate() { return placing || !has || !s.Sticky; };
            cellOf[ge] = cell;
            return ge;
        }

        internal void ClearCellMap() { cellOf.Clear(); }

        // text under the name in the radial centre: id, the cell the spell sits in, how to move it
        internal string SubtitleFor(Spell s, string id)
        {
            string k = file.PinOf(s);
            return id + (k.Length > 0 ? "  " + SpellFile.PinLabel(k) : "") + "   Shift+click: to the bar";
        }

        // ---- placing a spell on the bar ---------------------------------------------------------------------------------------

        // (re)opens the Hotbar section so it shows the current cells
        void OpenHotbar(RadialBase radial)
        {
            IRadialConfig hb = new SpellHotbarConfig(this);
            if (mQueuedOpen == null) mQueuedOpen = typeof(RadialBase).GetMethod("QueuedOpen", AllF);
            if (mQueuedOpen != null) mQueuedOpen.Invoke(radial, new object[] { hb, Root() });
            else radial.Open(hb, Root());
        }

        // RMB is the radial's Back; with Shift held it must not go back, because Shift+RMB empties a Hotbar cell
        RadialBase backWrappedFor;   // the radial whose Back is already wrapped by this bar
        internal void SuppressBackOnShift(RadialBase radial)
        {
            if (backWrappedFor == radial) return;
            try
            {
                PropertyInfo pr = typeof(RadialBase).GetProperty("GetBack", AllF);
                if (pr == null) return;
                MethodInfo get = pr.GetGetMethod(true), set = pr.GetSetMethod(true);
                Func<bool> orig = get == null ? null : (Func<bool>)get.Invoke(radial, null);
                if (orig == null || set == null) return;
                set.Invoke(radial, new object[] { (Func<bool>)delegate() { return !(SpellsSettings.On && SpellsSettings.ShiftHeld()) && orig(); } });
                backWrappedFor = radial;
            }
            catch (Exception e) { HomeApi.Log("spells: cannot wrap Back: " + e.Message); }
        }

        // Shift+click on a spell: remember it and switch the open radial to the Hotbar section; a click on a cell there places it
        void BeginPlace(Spell s, RadialBase radial)
        {
            assigning = s;
            radialGoneAt = 0f;
            Toast("", false);
            try { OpenHotbar(radial); }
            catch (Exception e)
            {
                assigning = null;
                HomeApi.Log("spells: cannot open the Hotbar section: " + (e.InnerException != null ? e.InnerException.Message : e.Message));
            }
        }

        // Shift+RMB on a Hotbar section empties its cell (plain RMB is the radial's Back, see SuppressBackOnShift)
        void HandleRadialSecondary()
        {
            if (cellOf.Count == 0 || !SpellsSettings.ShiftHeld()) return;
            bool rmb = false;
            try { rmb = ZInput.GetButtonDown("RadialSecondaryInteract"); } catch (Exception) { }
            if (!rmb) { try { rmb = Input.GetMouseButtonDown(1); } catch (Exception) { } }
            if (!rmb) return;
            try
            {
                RadialBase radial = (RadialBase)typeof(Hud).GetField("m_radialMenu", AllF).GetValue(Hud.instance);
                RadialMenuElement sel = radial == null ? null : radial.Selected;
                string cell;
                if (sel == null || !cellOf.TryGetValue(sel, out cell)) return;
                ClearCell(cell);
                OpenHotbar(radial);
            }
            catch (Exception e) { HomeApi.Log("spells: RMB clear failed: " + e.Message); }
        }

        // puts the pending spell into a cell; one spell sits in one cell only (it moves)
        void AssignTo(string cell)
        {
            Spell s = assigning;
            if (s == null) return;
            foreach (string k in SpellFile.PinOrder) { Spell p; if (file.Pins.TryGetValue(k, out p) && p == s) file.Pins.Remove(k); }
            file.Pins[cell] = s;
            file.SaveOverrides();
            string msg = s.Label + " placed on " + (cell[0] == 'S' ? "Shift+" + cell[1] : cell);
            HomeApi.Log("spells: " + msg);
            assigning = null;
            Toast(msg, false);
        }

        void ClearCell(string cell)
        {
            if (!file.Pins.Remove(cell)) return;
            file.SaveOverrides();
            Toast("cell " + (cell[0] == 'S' ? "Shift+" + cell[1] : cell) + " emptied", false);
        }

        // SubTitle has a non-public setter in the game: set it by reflection (text under the name in the radial centre)
        static void SetSubTitle(RadialMenuElement e, string text)
        {
            try
            {
                PropertyInfo p = typeof(RadialMenuElement).GetProperty("SubTitle", AllF);
                MethodInfo m = p == null ? null : p.GetSetMethod(true);
                if (m != null) m.Invoke(e, new object[] { text });
            }
            catch (Exception) { }
        }

        // same call vanilla HandleRadialInput makes: Hud.m_radialMenu.Open(Hud.m_config, null); the Open prefix swaps in our config
        public string OpenRadial()
        {
            try
            {
                Hud hud = Hud.instance;
                RadialBase radial = (RadialBase)typeof(Hud).GetField("m_radialMenu", AllF).GetValue(hud);
                object cfg = typeof(Hud).GetField("m_config", AllF).GetValue(hud);
                if (radial == null || cfg == null) return "radial menu not available";
                PropertyInfo can = radial.GetType().GetProperty("CanOpen", AllF);
                if (can != null && can.CanWrite) can.SetValue(radial, true, null);
                radial.Open((IRadialConfig)cfg, null);
                return "opened";
            }
            catch (Exception e) { return "failed: " + (e.InnerException != null ? e.InnerException.Message : e.Message); }
        }

        public string Status()
        {
            return "spells " + (SpellsSettings.On ? "ON" : "OFF") + (SpellsSettings.IsPlaceholder ? " (PLACEHOLDER mode)" : " (real spells)")
                + ", patches " + PatchInfo() + ", last cast: " + lastCast + ", flags: " + Snap() + "\n" + file.CountSlots() + " groups, " + file.Pins.Count + " pins. file " + SpellsSettings.ActiveFile;
        }

        static string PatchInfo()
        {
            try
            {
                MethodInfo m1 = null;
                foreach (MethodInfo m in typeof(RadialBase).GetMethods(AllF)) if (m.Name == "Open" && m.GetParameters().Length == 2) { m1 = m; break; }
                MethodInfo m2 = typeof(Player).GetMethod("UseHotbarItem", AllF);
                HarmonyLib.Patches p1 = Harmony.GetPatchInfo(m1), p2 = Harmony.GetPatchInfo(m2);
                int n1 = p1 == null ? 0 : p1.Prefixes.Count, n2 = p2 == null ? 0 : p2.Prefixes.Count;
                return "prefixes radialOpen=" + n1 + " hotbar=" + n2;
            }
            catch (Exception e) { return "? (" + e.Message + ")"; }
        }

        // ---- input: the action bar file.Pins ---------------------------------------------------------------------------------------

        static readonly MethodInfo mTake = typeof(Player).GetMethod("TakeInput", AllF);
        static readonly MethodInfo mHidden = typeof(Hud).GetMethod("IsUserHidden", AllF);
        static readonly MethodInfo mInRadial = typeof(Hud).GetMethod("InRadial", AllF);

        static bool TakeInput(Player p)
        {
            if (mTake == null) return true;
            try { return (bool)mTake.Invoke(p, null); } catch (Exception) { return true; }
        }

        static bool HudHidden()
        {
            if (mHidden == null) return false;
            try { return (bool)mHidden.Invoke(null, null); } catch (Exception) { return false; }
        }

        static bool InRadial()
        {
            if (mInRadial == null) return false;
            try { return (bool)mInRadial.Invoke(null, null); } catch (Exception) { return false; }
        }

        void Update()
        {
            barVisible = false;
            if (SpellsSettings.Current == null) return;
            if (!SpellsSettings.On) { assigning = null; return; }
            Player p = Player.m_localPlayer;
            if (p == null || HudHidden()) { assigning = null; return; }
            SpellPatches.EnsureApplied();
            barVisible = true;
            bool inRadial = InRadial();
            if (inRadial) { radialGoneAt = 0f; HandleRadialSecondary(); }
            else if (assigning != null)
            {
                // a spell waits for a Hotbar cell: closing the radial (or ~0.6 s without it) cancels
                if (radialGoneAt == 0f) radialGoneAt = Time.unscaledTime;
                else if (Time.unscaledTime - radialGoneAt > 0.6f) { assigning = null; radialGoneAt = 0f; Toast("placing cancelled", false); }
            }
            inputOk = TakeInput(p) && !p.IsDead() && !inRadial;
            if (!inputOk) return;
            bool shift = SpellsSettings.ShiftHeld();
            for (int i = 0; i < SpellFile.PinOrder.Length; i++)
            {
                string k = SpellFile.PinOrder[i];
                Spell ps;
                if (!file.Pins.TryGetValue(k, out ps)) continue;
                bool hit;
                if (k[0] == 'F') hit = ZInput.GetKeyDown(FKey(k), false);
                else hit = shift && ZInput.GetButtonDown("Hotbar" + k.Substring(1));
                if (hit)
                {
                    // Shift is part of the key for cells 1-5, so it does not select the spell's shift= variant there
                    if (k[0] == 'S') SpellsSettings.MarkConsumed();
                    CastSpell(ps, k[0] == 'F' && shift, k);
                    return;
                }
            }
        }

        static KeyCode FKey(string k)
        {
            switch (k)
            {
                case "F1": return KeyCode.F1;
                case "F2": return KeyCode.F2;
                case "F3": return KeyCode.F3;
                case "F4": return KeyCode.F4;
                default: return KeyCode.F5;
            }
        }

        // ---- casting -----------------------------------------------------------------------------------------------------------

        // pinKey: the bar slot that fired (for the flash), null when the spell was picked in the radial
        void CastSpell(Spell s, bool shift, string pinKey)
        {
            if (Time.unscaledTime < s.Next) return;
            s.Next = Time.unscaledTime + 0.3f;
            if (pinKey != null) flash[pinKey] = Time.unscaledTime + 0.25f;
            // placeholder mode (or a @dummy line): show a toast and log it, never call a command, never touch the game
            if (SpellsSettings.IsPlaceholder || s.Cmd.Trim() == "@dummy")
            {
                string id = s.Entry > 0 ? s.Slot + "." + s.Entry : "" + s.Slot;
                string pm = "placeholder: " + s.Label + " (" + id + ")" + (shift && s.Shift.Length > 0 ? " +shift=" + s.Shift : "");
                lastCast = pm;
                HomeApi.Log("spells: " + pm);
                Toast(pm, false);
                return;
            }
            string form = s.Cmd;
            if (shift && s.Shift.Length > 0) form = form + "&" + s.Shift;
            string before = Snap();
            StringBuilder res = new StringBuilder();
            foreach (string part in form.Split(new string[] { ";;" }, StringSplitOptions.RemoveEmptyEntries))
            {
                string r = Exec(part.Trim());
                if (res.Length > 0) res.Append(" | ");
                res.Append(FirstLine(r));
            }
            string after = Snap();
            lastCast = s.Label + " (" + form + ")";
            bool err = res.ToString().StartsWith("ERROR");
            HomeApi.Log("spells: " + s.Label + " -> " + res);
            if (before != after)
            {
                HomeApi.Log("spells: FLAG CHANGED by '" + s.Label + "': " + before + " => " + after);
                Toast("FLAG CHANGED by " + s.Label + ", spells switched off", true);
                SpellsSettings.TurnOff();
                try { SpellPatches.Apply(false); } catch (Exception e) { HomeApi.Log("spells: unpatch failed: " + e.Message); }
                return;
            }
            Toast(s.Label + ": " + res, err);
        }

        // toast text: the first line, except for results that start with progress lines (earthquake: "stage 0 ..."), then the last line
        static string FirstLine(string r)
        {
            if (r == null) return "";
            r = r.Trim();
            int nl = r.IndexOf('\n');
            if (nl < 0) return r;
            string first = r.Substring(0, nl).Trim();
            if (first.StartsWith("stage ") || first.StartsWith("probe"))
            {
                string[] lines = r.Split('\n');
                for (int i = lines.Length - 1; i >= 0; i--) if (lines[i].Trim().Length > 0) return lines[i].Trim();
            }
            return first;
        }

        // runs "op=...&k=v" through the core dispatcher on this thread, the same path the inbox uses
        static string Exec(string form)
        {
            if (ZNet.instance == null || ZNet.instance.IsServer() == false)
                return "ERROR: spells only run in single player or a world this PC hosts";
            return HomeApi.Exec(form);
        }

        void Toast(string t, bool err) { toast = t; toastErr = err; toastUntil = Time.unscaledTime + 3.5f; }

        // ---- cheat-state snapshot (read only), compared before/after every cast -------------------------------------------------

        public void Baseline() { baseline = Snap(); }

        static string Snap()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                FieldInfo tc = typeof(Terminal).GetField("m_cheat", AllF);
                sb.Append("cheat=").Append(tc == null ? "?" : "" + tc.GetValue(null));
            }
            catch (Exception) { sb.Append("cheat=?"); }
            try
            {
                PlayerProfile pp = Game.instance == null ? null : Game.instance.GetPlayerProfile();
                FieldInfo uc = typeof(PlayerProfile).GetField("m_usedCheats", AllF);
                sb.Append(" used=").Append(pp == null || uc == null ? "?" : "" + uc.GetValue(pp));
            }
            catch (Exception) { sb.Append(" used=?"); }
            Player p = Player.m_localPlayer;
            foreach (string n in new string[] { "m_godMode", "m_ghostMode", "m_debugFly", "m_noPlacementCost" })
            {
                try
                {
                    FieldInfo f = typeof(Player).GetField(n, AllF);
                    sb.Append(' ').Append(n.Substring(2)).Append('=');
                    if (f == null) sb.Append('?'); else sb.Append(f.IsStatic ? f.GetValue(null) : (p == null ? (object)"?" : f.GetValue(p)));
                }
                catch (Exception) { sb.Append('?'); }
            }
            return sb.ToString();
        }

        // ---- drawing: the always-on action bar (IMGUI), one row of 10 cells --------------------------------------------------------

        const float PanelScale = 0.70f;     // whole bar, banner and toast (was 1.0, then 0.85)
        const float CellW = 72f, CellH = 88f, CellGap = 6f, GroupGap = 20f;

        void MakeStyles()
        {
            if (sKey != null) return;
            sKey = new GUIStyle(GUI.skin.label); sKey.fontSize = 13; sKey.fontStyle = FontStyle.Bold; sKey.alignment = TextAnchor.UpperLeft; sKey.normal.textColor = new Color(1f, 0.85f, 0.45f);
            sName = new GUIStyle(GUI.skin.label); sName.fontSize = 12; sName.alignment = TextAnchor.UpperCenter; sName.wordWrap = true; sName.clipping = TextClipping.Clip; sName.normal.textColor = Color.white;
            sToast = new GUIStyle(GUI.skin.label); sToast.fontSize = 16; sToast.alignment = TextAnchor.MiddleCenter; sToast.wordWrap = true; sToast.normal.textColor = Color.white;
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

        // cell i: 0-4 = Shift+1..5 (labelled 1-5), 5-9 = F1-F5; a wider gap between the two groups of five
        static Rect CellRect(int i, float W, float H)
        {
            float total = 10 * CellW + 8 * CellGap + GroupGap;
            float x = (W - total) / 2f + i * (CellW + CellGap) + (i >= 5 ? GroupGap - CellGap : 0f);
            return new Rect(x, H - 36f - CellH, CellW, CellH);
        }

        // the spell's icon: the sprite of its group in the game's radial (spells have no icon of their own yet)
        Sprite IconOf(Spell s)
        {
            Sprite sp;
            if (iconCache.TryGetValue(s.Slot, out sp) && sp != null) return sp;
            sp = IconFor(SlotIcon(s.Slot));
            iconCache[s.Slot] = sp;
            return sp;
        }

        static void DrawSprite(Rect r, Sprite sp, float alpha)
        {
            if (sp == null) return;
            try
            {
                Texture2D t = sp.texture;
                if (t == null) return;
                Rect tr = sp.textureRect;
                Rect uv = new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height);
                Color o = GUI.color; GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.DrawTextureWithTexCoords(r, t, uv, true);
                GUI.color = o;
            }
            catch (Exception) { }
        }

        void OnGUI()
        {
            if (SpellsSettings.Current == null) return;
            bool asg = SpellsSettings.On && assigning != null;
            bool showToast = SpellsSettings.On && toast.Length > 0 && Time.unscaledTime < toastUntil;
            if (!(SpellsSettings.On && barVisible) && !showToast) return;
            if (Event.current.type != EventType.Repaint) return;
            MakeStyles();
            float sc = Screen.height / 1080f * PanelScale;
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(sc, sc, 1f));
            float W = Screen.width / sc, H = Screen.height / sc;
            Rect first = CellRect(0, W, H), last = CellRect(9, W, H);
            if (SpellsSettings.On && barVisible)
            {
                float a = (inputOk || asg || InRadial()) ? 1f : 0.45f;
                // only filled cells, packed and centred (empty cells are not shown)
                List<string> shown = new List<string>();
                foreach (string cell in SpellFile.PinOrder) if (file.Pins.ContainsKey(cell)) shown.Add(cell);
                float rowW = shown.Count * CellW + (shown.Count - 1) * CellGap;
                for (int i = 0; i < shown.Count; i++)
                {
                    string cell = shown[i];
                    Rect r = new Rect((W - rowW) / 2f + i * (CellW + CellGap), first.y, CellW, CellH);
                    DrawSlot(r, cell, cell[0] == 'S' ? cell.Substring(1) : cell, a, false, false);
                }
            }
            if (asg)
            {
                Rect br = new Rect(first.x - 40f, first.y - 46f, last.xMax - first.x + 80f, 34f);
                Fill(br, new Color(0.30f, 0.22f, 0.04f, 0.92f));
                GUI.Label(br, "Place \"" + assigning.Label + "\": click a cell in the radial Hotbar (closing the radial cancels)", sToast);
            }
            else if (showToast)
            {
                Rect tr = new Rect(first.x - 40f, first.y - 46f, last.xMax - first.x + 80f, 34f);
                Fill(tr, toastErr ? new Color(0.45f, 0.08f, 0.08f, 0.88f) : new Color(0.06f, 0.18f, 0.08f, 0.88f));
                GUI.Label(tr, toast, sToast);
            }
            GUI.matrix = old;
        }

        void DrawSlot(Rect r, string pinKey, string keyLabel, float alpha, bool picking, bool hover)
        {
            Spell s;
            bool has = file.Pins.TryGetValue(pinKey, out s);
            float fl;
            bool flashing = flash.TryGetValue(pinKey, out fl) && Time.unscaledTime < fl;
            Color bg = has ? new Color(0.08f, 0.08f, 0.10f, 0.82f * alpha) : new Color(0.08f, 0.08f, 0.10f, 0.35f * alpha);
            if (flashing) bg = new Color(0.75f, 0.58f, 0.2f, 0.95f * alpha);
            if (hover) bg = new Color(0.32f, 0.26f, 0.08f, 0.95f);
            Fill(r, bg);
            Fill(new Rect(r.x, r.y, r.width, 3f), has ? new Color(1f, 0.85f, 0.45f, 0.9f * alpha) : new Color(0.4f, 0.4f, 0.4f, 0.5f * alpha));
            if (picking) Frame(r, hover ? 3f : 1.5f, new Color(1f, 0.85f, 0.45f, hover ? 1f : 0.6f));
            GUI.Label(new Rect(r.x + 5f, r.y + 3f, 40f, 20f), keyLabel, sKey);
            if (!has) return;
            DrawSprite(new Rect(r.x + (r.width - 38f) / 2f, r.y + 18f, 38f, 38f), IconOf(s), alpha);
            GUI.Label(new Rect(r.x + 2f, r.y + 58f, r.width - 4f, r.height - 58f), s.Label, sName);
            float cd = s.Next - Time.unscaledTime;
            if (cd > 0f) Fill(new Rect(r.x, r.y, r.width, r.height * Mathf.Clamp01(cd / 0.3f)), new Color(0f, 0f, 0f, 0.5f));
        }
    }
}
