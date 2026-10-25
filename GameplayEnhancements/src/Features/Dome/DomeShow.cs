// op=dome&step=show[&alpha=0.1][&on=0]: a TEST AID (Maksym 2026-10-25, "just for now, to test"). A see-through half-sphere of the ward radius
// over every active dome ward (none over a deactivated one), centred on the ward with its own area radius, a colour per ward
// (Maksym 2026-10-25), 10 % visible by default (Maksym 2026-10-25), so the coverage and the overlaps
// can be seen; a transparent shader without culling, so it shows from inside too. Drawn on this PC only: it breaks
// R0 (the same picture for everyone) on purpose and is never on by itself; on=0 or leaving the world removes it.
// No collider: nothing in the world hits it or is blocked by it. While on, the host's sync tick redraws it when a ward
// appears, goes or is switched on or off (Refresh).
using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.Dome
{
    internal static class DomeShow
    {
        static readonly List<GameObject> shown = new List<GameObject>();
        static Material material;
        static bool on;
        static float alpha = 0.1f;
        static string drawn = "";   // the wards and states last drawn

        public static bool On { get { return on; } }

        public static void Off() { on = false; Clear(); }

        public static void Clear()
        {
            drawn = "";
            foreach (GameObject g in shown) if (g != null) Object.Destroy(g);
            shown.Clear();
        }

        // one sphere per ward; the lower half sits under the ground, so it reads as a dome
        public static int Show(Dictionary<string, List<ZDO>> groups, Dictionary<string, int> numbers, float wardRadius, float a)
        {
            on = true;
            alpha = a;
            Clear();
            drawn = Key(groups);
            foreach (KeyValuePair<string, List<ZDO>> kv in groups)
            {
                foreach (ZDO z in kv.Value)
                {
                    if (!z.GetBool(ZDOVars.s_enabled, false)) continue;
                    GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    s.name = "HomeCheat.DomeShow";
                    Collider col = s.GetComponent<Collider>();
                    if (col != null) Object.Destroy(col);
                    // the ward itself when it is loaded here (its own area radius and position), else its saved position
                    PrivateArea pa = DomeWards.Loaded(z);
                    Vector3 at = pa != null ? pa.transform.position : z.GetPosition();
                    float radius = pa != null ? pa.m_radius : wardRadius;
                    s.transform.position = at;
                    s.transform.localScale = Vector3.one * radius * 2f;
                    Color c = WardColour(z);
                    Renderer r = s.GetComponent<Renderer>();
                    if (r != null)
                    {
                        r.sharedMaterial = Material();
                        r.material.color = new Color(c.r, c.g, c.b, alpha);
                        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        r.receiveShadows = false;
                    }
                    shown.Add(s);
                }
            }
            return shown.Count;
        }

        // redraws when the wards or their on/off differ from what is drawn; called by the sync tick while on
        public static void Refresh(Dictionary<string, List<ZDO>> groups, Dictionary<string, int> numbers, float wardRadius)
        {
            if (on && Key(groups) != drawn) Show(groups, numbers, wardRadius, alpha);
        }

        static string Key(Dictionary<string, List<ZDO>> groups)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (KeyValuePair<string, List<ZDO>> kv in groups)
                foreach (ZDO z in kv.Value)
                    sb.Append(z.m_uid).Append(z.GetBool(ZDOVars.s_enabled, false) ? "+" : "-").Append(DomeWards.Loaded(z) != null ? "L" : "").Append(';');
            return sb.ToString();
        }

        // a colour of its own per ward, the same on every redraw: hues spread by the golden ratio over the ward's ZDO id
        static Color WardColour(ZDO z)
        {
            double h = (z.m_uid.ID * 0.6180339887) % 1.0;
            return Color.HSVToRGB((float)h, 0.75f, 1f);
        }

        static Material Material()
        {
            if (material != null) return material;
            foreach (string name in new string[] { "Sprites/Default", "Legacy Shaders/Transparent/Diffuse", "Transparent/Diffuse" })
            {
                Shader sh = Shader.Find(name);
                if (sh == null) continue;
                material = new Material(sh);
                HomeApi.Log("dome show: shader " + name);
                return material;
            }
            material = new Material(Shader.Find("Hidden/Internal-Colored"));
            return material;
        }
    }
}
