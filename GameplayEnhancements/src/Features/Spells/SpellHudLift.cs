using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.Spells
{
    // HUD lift (card hud-above, Maksym 2026-11-03: "Lift game HUD"): while the spell bar shows, the game's stamina, eitr and
    // adrenaline bars and the build-piece requirements move up so their bottom edge sits above the top panel; back when the bar
    // hides, on unload and on world exit. The panels stay put. hudlift=0 in spells.state turns it off. This PC only, nothing saved.
    // Each target keeps the game's own position (orig) and the one we wrote; when the game or an animator moved it since, its new
    // position becomes orig, so the lift never stacks and a restore never undoes the game's own move.
    internal sealed class SpellHudLift
    {
        const BindingFlags AllF = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        public const float GapPx = 10f;     // screen pixels between the panels' top edge and a lifted element (at 1080p, scaled)

        // the groups that move together (one shared lift each, so their stacking stays as the game draws it)
        static readonly string[] BarFields = new string[] { "m_staminaBar2Root", "m_eitrBarRoot", "m_adrenalineBarRoot" };

        sealed class Target
        {
            public string Name;
            public RectTransform Rt;
            public Vector2 Orig, Written;
            public bool Lifted;
        }

        Hud hud;                                         // the Hud the targets belong to (a new world has a new Hud)
        List<Target> bars = new List<Target>();
        List<Target> build = new List<Target>();
        public string LastLine = "not run yet";

        // every frame from SpellBar.LateUpdate: lift while 'on' and panels are drawn (panelTopPx > 0, pixels from the screen bottom)
        public void Tick(bool on, float panelTopPx)
        {
            if (Hud.instance == null || Hud.instance != hud) { Forget(); if (Hud.instance == null) return; Resolve(Hud.instance); }
            if (!on || panelTopPx <= 0f) { Restore(); return; }
            float target = panelTopPx + GapPx * Screen.height / 1080f;
            float lb = LiftGroup(bars, target), lr = LiftGroup(build, target);
            LastLine = "lifted: bars " + Fmt.F(lb, 0) + " px, build " + Fmt.F(lr, 0) + " px (panel top " + Fmt.F(panelTopPx, 0) + " px)";
        }

        // puts every target back where the game had it; safe to call any time (also from OnDestroy)
        public void Restore()
        {
            RestoreGroup(bars); RestoreGroup(build);
            LastLine = "restored";
        }

        void Forget() { bars.Clear(); build.Clear(); hud = null; }

        void Resolve(Hud h)
        {
            hud = h;
            foreach (string f in BarFields) { RectTransform rt = RectOf(FieldValue(h, f)); if (rt != null) bars.Add(new Target { Name = f, Rt = rt }); }
            RectTransform req = RequirementsPanel(h);
            if (req != null) build.Add(new Target { Name = "requirements:" + req.name, Rt = req });
        }

        // the panel that holds the build-piece requirements: the parent of m_requirementItems[0]
        static RectTransform RequirementsPanel(Hud h)
        {
            GameObject[] items = FieldValue(h, "m_requirementItems") as GameObject[];
            if (items == null || items.Length == 0 || items[0] == null) return null;
            Transform p = items[0].transform.parent;
            return p == null ? null : p as RectTransform;
        }

        // one lift for the whole group: the most any of its elements needs, from where the game has them (orig)
        float LiftGroup(List<Target> g, float targetPx)
        {
            float need = 0f;
            foreach (Target t in g)
            {
                if (t.Rt == null) continue;
                if (t.Lifted && t.Rt.anchoredPosition != t.Written) { t.Orig = t.Rt.anchoredPosition; t.Lifted = false; }
                if (!t.Lifted) t.Orig = t.Rt.anchoredPosition;
                float ppu = PxPerUnit(t.Rt);
                if (ppu <= 0f) continue;
                float bottomNow = ScreenRect(t.Rt).yMin;
                float bottomOrig = bottomNow - (t.Rt.anchoredPosition.y - t.Orig.y) * ppu;
                need = Mathf.Max(need, targetPx - bottomOrig);
            }
            foreach (Target t in g)
            {
                if (t.Rt == null) continue;
                float ppu = PxPerUnit(t.Rt);
                Vector2 want = need > 0f && ppu > 0f ? t.Orig + new Vector2(0f, need / ppu) : t.Orig;
                if (t.Rt.anchoredPosition != want) t.Rt.anchoredPosition = want;
                t.Written = want;
                t.Lifted = true;
            }
            return need;
        }

        static void RestoreGroup(List<Target> g)
        {
            foreach (Target t in g)
            {
                if (!t.Lifted) continue;
                t.Lifted = false;
                if (t.Rt == null) continue;                                       // destroyed with its Hud
                if (t.Rt.anchoredPosition == t.Written) t.Rt.anchoredPosition = t.Orig;   // else the game moved it itself
            }
        }

        // ---- geometry: screen pixels (y from the bottom), whatever the canvas mode ----

        static Camera CamOf(RectTransform rt)
        {
            Canvas c = rt.GetComponentInParent<Canvas>();
            if (c == null) return null;
            c = c.rootCanvas;
            return c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera;
        }

        internal static Rect ScreenRect(RectTransform rt)
        {
            Vector3[] w = new Vector3[4];
            rt.GetWorldCorners(w);
            Camera cam = CamOf(rt);
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector2 s = RectTransformUtility.WorldToScreenPoint(cam, w[i]);
                x0 = Mathf.Min(x0, s.x); y0 = Mathf.Min(y0, s.y); x1 = Mathf.Max(x1, s.x); y1 = Mathf.Max(y1, s.y);
            }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        // screen pixels per unit of anchoredPosition.y (the parent's local space)
        static float PxPerUnit(RectTransform rt)
        {
            Transform p = rt.parent;
            if (p == null) return 0f;
            Camera cam = CamOf(rt);
            Vector3 w0 = p.position, w1 = w0 + p.TransformVector(Vector3.up * 100f);
            return (RectTransformUtility.WorldToScreenPoint(cam, w1).y - RectTransformUtility.WorldToScreenPoint(cam, w0).y) / 100f;
        }

        static object FieldValue(object o, string name)
        {
            try { FieldInfo f = o.GetType().GetField(name, AllF); return f == null ? null : f.GetValue(o); }
            catch (Exception) { return null; }
        }

        static RectTransform RectOf(object v)
        {
            GameObject go = v as GameObject;
            if (go != null) return go.transform as RectTransform;
            Component c = v as Component;
            return c != null ? c.transform as RectTransform : null;
        }

        // ---- probe=hud (read only): the Hud's RectTransforms in the bottom 40 % of the screen, in this game version ----

        public string Probe(float panelTopPx, bool on)
        {
            Hud h = Hud.instance;
            if (h == null) throw new CommandException("no Hud (load a world first)");
            StringBuilder sb = new StringBuilder();
            sb.Append("probe hud: screen ").Append(Screen.width).Append('x').Append(Screen.height)
              .Append(", panel top ").Append(panelTopPx > 0f ? Fmt.F(panelTopPx, 0) + " px" : "none (no row shown)")
              .Append(", hudlift ").Append(on ? "on" : "off").Append(", ").Append(LastLine).Append('\n');
            int n = 0;
            foreach (FieldInfo f in typeof(Hud).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                object v = FieldValue(h, f.Name);
                List<RectTransform> rts = new List<RectTransform>();
                GameObject[] arr = v as GameObject[];
                if (arr != null) { if (arr.Length > 0 && arr[0] != null) { rts.Add(arr[0].transform as RectTransform); if (arr[0].transform.parent != null) rts.Add(arr[0].transform.parent as RectTransform); } }
                else rts.Add(RectOf(v));
                for (int i = 0; i < rts.Count; i++)
                {
                    RectTransform rt = rts[i];
                    if (rt == null) continue;
                    Rect s = ScreenRect(rt);
                    if (s.yMin > Screen.height * 0.40f) continue;
                    n++;
                    sb.Append(f.Name).Append(arr != null ? (i == 0 ? "[0]" : "[0].parent") : "")
                      .Append(" '").Append(rt.name).Append("' active=").Append(rt.gameObject.activeInHierarchy ? 1 : 0)
                      .Append(" anchors=").Append(V(rt.anchorMin)).Append('-').Append(V(rt.anchorMax)).Append(" pivot=").Append(V(rt.pivot))
                      .Append(" pos=").Append(V(rt.anchoredPosition)).Append(" size=").Append(V(rt.sizeDelta))
                      .Append(" screen x ").Append(Fmt.F(s.xMin, 0)).Append("..").Append(Fmt.F(s.xMax, 0)).Append(" y ").Append(Fmt.F(s.yMin, 0)).Append("..").Append(Fmt.F(s.yMax, 0))
                      .Append(" ppu=").Append(Fmt.F(PxPerUnit(rt), 3)).Append('\n');
                }
            }
            Canvas c = h.GetComponentInParent<Canvas>();
            if (c == null) c = h.GetComponentInChildren<Canvas>();
            sb.Append(n).Append(" bottom elements; canvas ").Append(c == null ? "?" : c.rootCanvas.name + " " + c.rootCanvas.renderMode + " scale " + Fmt.F(c.rootCanvas.scaleFactor, 3));
            sb.Append("; targets: ");
            foreach (Target t in bars) sb.Append(t.Name).Append(t.Lifted ? "(lifted) " : " ");
            foreach (Target t in build) sb.Append(t.Name).Append(t.Lifted ? "(lifted) " : " ");
            return sb.ToString();
        }

        static string V(Vector2 v) { return Fmt.F(v.x, 1) + "," + Fmt.F(v.y, 1); }
    }
}
