// op=dome&step=show[&alpha=0.1][&on=0]: a TEST AID (Maksym 2026-10-25, "just for now, to test"). A see-through half-sphere of the ward radius
// over every active dome ward (none over a deactivated one, Maksym 2026-10-25), one colour per group, 10 % visible by default (Maksym 2026-10-25), so the coverage and the overlaps
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
        static readonly Color[] Colours =
        {
            new Color(0.95f, 0.75f, 0.2f), new Color(0.3f, 0.6f, 1f), new Color(0.4f, 0.85f, 0.4f),
            new Color(0.9f, 0.35f, 0.35f), new Color(0.75f, 0.45f, 0.95f), new Color(0.3f, 0.85f, 0.85f)
        };

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
                int no;
                if (!numbers.TryGetValue(kv.Key, out no)) no = 0;
                Color c = Colours[(no <= 0 ? 0 : no - 1) % Colours.Length];
                c.a = alpha;
                foreach (ZDO z in kv.Value)
                {
                    if (!z.GetBool(ZDOVars.s_enabled, false)) continue;
                    GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    s.name = "HomeCheat.DomeShow";
                    Collider col = s.GetComponent<Collider>();
                    if (col != null) Object.Destroy(col);
                    s.transform.position = z.GetPosition();
                    s.transform.localScale = Vector3.one * wardRadius * 2f;
                    Renderer r = s.GetComponent<Renderer>();
                    if (r != null)
                    {
                        r.sharedMaterial = Material();
                        r.material.color = c;
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
                    sb.Append(z.m_uid).Append(z.GetBool(ZDOVars.s_enabled, false) ? "+" : "-").Append(';');
            return sb.ToString();
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
