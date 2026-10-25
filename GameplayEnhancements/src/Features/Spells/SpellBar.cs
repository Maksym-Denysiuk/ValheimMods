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
        internal SpellNode RootNode() { return file.Root; }
        internal SpellNode NodeAt(string id) { return file.NodeAt(id); }

        // first number of a dotted id: the root section, which picks the icon
        internal static int TopOf(string id)
        {
            int dot = id.IndexOf('.'), n;
            return int.TryParse(dot < 0 ? id : id.Substring(0, dot), out n) ? n : 0;
        }

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

        // one entry of a level: a spell is a leaf, a title is a GroupElement that opens its sub-section (owner = the level it sits on,
        // so the game's Back returns there)
        internal RadialMenuElement MakeNodeElement(SpellNode n, IRadialConfig owner, RadialBase radial)
        {
            if (n.Spell != null) return MakeLeaf(n.Spell, owner, radial, IconFor(SlotIcon(n.Spell.Top)), SubtitleFor(n.Spell, n.Id));
            GroupElement ge = UnityEngine.Object.Instantiate<GroupElement>(RadialData.SO.GroupElement);
            ge.Init(new SpellGroupConfig(this, n.Id), owner, radial);
            SetSubTitle(ge, n.Id + (n.Sticky ? "   sticky" : ""));
            return ge;
        }

        // A leaf is the game's GroupElement prefab with a name and an icon; its Interact and CloseOnInteract are replaced, so selecting it
        // casts the spell (CloseOnInteract false = sticky: the radial stays open), or opens its first question (answer chain).
        // Shift+click does not cast: it picks the spell up and opens the Hotbar section to choose a cell (see BeginPlace).
        internal RadialMenuElement MakeLeaf(Spell s, IRadialConfig owner, RadialBase radial, Sprite icon, string subtitle)
        {
            GroupElement ge = UnityEngine.Object.Instantiate<GroupElement>(RadialData.SO.GroupElement);
            ge.Init(new SpellLeafConfig(s.Label, icon), owner, radial);
            SetSubTitle(ge, subtitle);
            ge.Interact = delegate()
            {
                if (SpellsSettings.ShiftHeld()) BeginPlace(s, radial);
                else if (s.Asks.Count > 0) OpenLevel(radial, new SpellAskConfig(this, new SpellAskChain(s, false, null), 0, null, null), owner);
                else CastSpell(s, false, null, null);
                return true;
            };
            ge.CloseOnInteract = delegate() { return SpellsSettings.ShiftHeld() || s.Asks.Count > 0 ? false : !s.Sticky; };
            return ge;
        }

        // a leaf that only shows a text (an empty section, a question with no answers); a click does nothing
        internal RadialMenuElement MakeNote(string label, string subtitle, IRadialConfig owner, RadialBase radial)
        {
            GroupElement ge = UnityEngine.Object.Instantiate<GroupElement>(RadialData.SO.GroupElement);
            ge.Init(new SpellLeafConfig(label, null), owner, radial);
            SetSubTitle(ge, subtitle);
            ge.Interact = delegate() { Toast(subtitle, false); return true; };
            ge.CloseOnInteract = delegate() { return false; };
            return ge;
        }

        // opens a level from inside an Interact, the way the game opens a group: queued, with 'back' on the Back stack
        internal void OpenLevel(RadialBase radial, IRadialConfig cfg, IRadialConfig back)
        {
            if (mQueuedOpen == null) mQueuedOpen = typeof(RadialBase).GetMethod("QueuedOpen", AllF);
            if (mQueuedOpen != null) mQueuedOpen.Invoke(radial, new object[] { cfg, back });
            else radial.Open(cfg, back);
        }

        // the last answer of a chain was picked: substitute {key} and cast
        internal void CastChain(SpellAskChain c) { CastSpell(c.Spell, c.Shift, c.PinKey, c.Answers); }

        // a Hotbar section: the spell of one bar cell (or an empty slot). Click casts it; while a spell waits to be placed a click puts it here.
        internal RadialMenuElement MakeCellElement(string cell, IRadialConfig back, RadialBase radial)
        {
            Spell s;
            bool has = file.Pins.TryGetValue(cell, out s);
            bool placing = assigning != null;
            string key = SpellParse.KeyName(cell);
            GroupElement ge = UnityEngine.Object.Instantiate<GroupElement>(RadialData.SO.GroupElement);
            ge.Init(new SpellLeafConfig(has ? s.Label : "(empty)", has ? IconOf(s) : null), back, radial);
            SetSubTitle(ge, placing ? key + "   click: place \"" + assigning.Label + "\" here" : key + (has ? "   Shift+RMB: empty this cell" : "   Shift+click a spell to place it"));
            ge.Interact = delegate()
            {
                if (assigning != null) AssignTo(cell);
                else if (has && s.Asks.Count > 0) OpenLevel(radial, new SpellAskConfig(this, new SpellAskChain(s, false, cell), 0, null, null), new SpellHotbarConfig(this, SpellParse.PanelOf(cell)));
                else if (has) CastSpell(s, false, cell, null);
                return true;
            };
            ge.CloseOnInteract = delegate() { return placing || !has || (!s.Sticky && s.Asks.Count == 0); };
            cellOf[ge] = cell;
            return ge;
        }

        internal void ClearCellMap() { cellOf.Clear(); }

        // text under the name in the radial centre: id, the cell the spell sits in, how to move it
        internal string SubtitleFor(Spell s, string id)
        {
            string k = file.PinOf(s);
            string q = "";
            foreach (SpellAsk a in s.Asks) q += (q.Length == 0 ? "   asks: " : ", ") + a.Key;
            return id + (k.Length > 0 ? "  " + SpellFile.PinLabel(k) : "") + q + "   Shift+click: to the bar";
        }

        // ---- placing a spell on the bar ---------------------------------------------------------------------------------------

        // (re)opens the Hotbar section so it shows the current cells
        void OpenHotbar(RadialBase radial) { OpenLevel(radial, new SpellPanelsConfig(this), Root()); }

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
            string msg = s.Label + " placed on " + SpellParse.KeyName(cell);
            HomeApi.Log("spells: " + msg);
            assigning = null;
            Toast(msg, false);
        }

        void ClearCell(string cell)
        {
            if (!file.Pins.Remove(cell)) return;
            file.SaveOverrides();
            Toast("cell " + SpellParse.KeyName(cell) + " emptied", false);
        }

        // SubTitle has a non-public setter in the game: set it by reflection (text under the name in the radial centre)
        internal static void SetSubTitle(RadialMenuElement e, string text)
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
                + ", patches " + PatchInfo() + ", last cast: " + lastCast + ", flags: " + Snap() + "\n" + file.CountSections() + " sections, " + file.Pins.Count + " pins. file " + SpellsSettings.ActiveFile
                + "\nradial: MaxElementsRange=" + LayerRange() + " (elements per layer before the spiral starts a new layer)";
        }

        // RadialDataSO.MaxElementsRange: the asset's per-layer thresholds (int[]), read by RadialBase.SetElementsPerLayer
        static string LayerRange()
        {
            try
            {
                object v = HomeCheat.Kit.Refl.Get(RadialData.SO, "MaxElementsRange");
                int[] arr = v as int[];
                if (arr == null) return v == null ? "?" : v.ToString();
                string[] s = new string[arr.Length];
                for (int i = 0; i < arr.Length; i++) s[i] = arr[i].ToString();
                return "[" + string.Join(",", s) + "]";
            }
            catch (Exception e) { return "? (" + e.Message + ")"; }
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
                else hit = shift && ShiftCellDown(SpellParse.CellNumber(k));
                if (hit)
                {
                    // Shift is part of the key for cells 1-5, so it does not select the spell's shift= variant there
                    if (k[0] == 'S') SpellsSettings.MarkConsumed();
                    if (ps.Asks.Count > 0) OpenChainRadial(new SpellAskChain(ps, k[0] == 'F' && shift, k));
                    else CastSpell(ps, k[0] == 'F' && shift, k, null);
                    return;
                }
            }
        }

        static KeyCode FKey(string k)
        {
            int n = SpellParse.CellNumber(k);
            return n >= 1 && n <= 12 ? (KeyCode)((int)KeyCode.F1 + n - 1) : KeyCode.None;
        }

        // Shift cells 1-8 use the game's Hotbar1..8 buttons (rebindable); 9, 0, - and = have no game button: their keys
        static bool ShiftCellDown(int n)
        {
            if (n >= 1 && n <= 8) return ZInput.GetButtonDown("Hotbar" + n);
            KeyCode kc = n == 9 ? KeyCode.Alpha9 : n == 10 ? KeyCode.Alpha0 : n == 11 ? KeyCode.Minus : n == 12 ? KeyCode.Equals : KeyCode.None;
            return kc != KeyCode.None && ZInput.GetKeyDown(kc, false);
        }

        // ---- casting -----------------------------------------------------------------------------------------------------------

        // a pinned spell with questions: open the radial on its first question (the same Open the G key makes; ours is not swapped)
        void OpenChainRadial(SpellAskChain c)
        {
            try
            {
                RadialBase radial = (RadialBase)typeof(Hud).GetField("m_radialMenu", AllF).GetValue(Hud.instance);
                if (radial == null) { Toast(c.Spell.Label + ": radial menu not available, pick it with G", true); return; }
                PropertyInfo can = radial.GetType().GetProperty("CanOpen", AllF);
                if (can != null && can.CanWrite) can.SetValue(radial, true, null);
                flash[c.PinKey] = Time.unscaledTime + 0.25f;
                radial.Open(new SpellAskConfig(this, c, 0, null, null), null);
            }
            catch (Exception e)
            {
                HomeApi.Log("spells: cannot open the questions of " + c.Spell.Label + ": " + (e.InnerException != null ? e.InnerException.Message : e.Message));
                Toast(c.Spell.Label + " asks questions: pick it with G", true);
            }
        }

        // pinKey: the bar slot that fired (for the flash), null when the spell was picked in the radial; answers: of its questions, or null
        void CastSpell(Spell s, bool shift, string pinKey, Dictionary<string, string> answers)
        {
            if (Time.unscaledTime < s.Next) return;
            s.Next = Time.unscaledTime + 0.3f;
            if (pinKey != null) flash[pinKey] = Time.unscaledTime + 0.25f;
            string given = "";
            if (answers != null) foreach (KeyValuePair<string, string> kv in answers) given += " " + kv.Key + "=" + kv.Value;
            // placeholder mode (or a @dummy line): show a toast and log it, never call a command, never touch the game
            if (SpellsSettings.IsPlaceholder || s.Cmd.Trim() == "@dummy")
            {
                string pm = "placeholder: " + s.Label + " (" + s.Id + ")" + (shift && s.Shift.Length > 0 ? " +shift=" + s.Shift : "") + (given.Length > 0 ? " answers:" + given : "");
                lastCast = pm;
                HomeApi.Log("spells: " + pm);
                Toast(pm, false);
                return;
            }
            string form = s.Cmd;
            if (shift && s.Shift.Length > 0) form = form + "&" + s.Shift;
            form = SpellParse.Substitute(form, answers);
            if (form.IndexOf('{') >= 0 && form.IndexOf('}') > form.IndexOf('{'))
            {
                Toast(s.Label + ": a question is unanswered (" + form + ")", true);
                return;
            }
            string before = Snap();
            StringBuilder res = new StringBuilder();
            foreach (string part in form.Split(new string[] { ";;" }, StringSplitOptions.RemoveEmptyEntries))
            {
                string r = Exec(part.Trim());
                if (res.Length > 0) res.Append(" | ");
                res.Append(FirstLine(r));
            }
            string after = Snap();
            lastCast = s.Label + " (" + form + ")";   // answers are already in the form
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

        // ---- drawing: the always-on action bar (IMGUI), two rows: F1-F12, Shift+1..Shift+= --------------------------------------------------------

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

        // two rows (Maksym 2026-10-25): row 0 = F1-F12 on top, row 1 = Shift+1..Shift+= below; cell i of 12, a wider gap after the 5th
        static Rect CellRect(int i, int row, float W, float H)
        {
            float total = 12 * CellW + 10 * CellGap + GroupGap;
            float x = (W - total) / 2f + i * (CellW + CellGap) + (i >= 5 ? GroupGap - CellGap : 0f);
            float y = H - 36f - CellH - (row == 0 ? CellH + CellGap : 0f);
            return new Rect(x, y, CellW, CellH);
        }

        // the spell's icon: the sprite of its group in the game's radial (spells have no icon of their own yet)
        Sprite IconOf(Spell s)
        {
            Sprite sp;
            if (iconCache.TryGetValue(s.Top, out sp) && sp != null) return sp;
            sp = IconFor(SlotIcon(s.Top));
            iconCache[s.Top] = sp;
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
            Rect first = CellRect(0, 1, W, H), last = CellRect(11, 1, W, H);
            if (SpellsSettings.On && barVisible)
            {
                float a = (inputOk || asg || InRadial()) ? 1f : 0.45f;
                // two rows, F keys on top, Shift keys below; only filled cells, packed and centred per row (empty cells are not shown)
                for (int row = 0; row < 2; row++)
                {
                    char kind = row == 0 ? 'F' : 'S';
                    List<string> shown = new List<string>();
                    foreach (string cell in SpellFile.PinOrder) if (cell[0] == kind && file.Pins.ContainsKey(cell)) shown.Add(cell);
                    if (shown.Count == 0) continue;
                    float y = CellRect(0, row, W, H).y;
                    if (y < first.y) first = new Rect(first.x, y, first.width, first.height);   // banner and toast above the top row in use
                    float rowW = shown.Count * CellW + (shown.Count - 1) * CellGap;
                    for (int i = 0; i < shown.Count; i++)
                    {
                        string cell = shown[i];
                        Rect r = new Rect((W - rowW) / 2f + i * (CellW + CellGap), y, CellW, CellH);
                        DrawSlot(r, cell, SpellParse.KeyOf(cell), a, false, false);
                    }
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
