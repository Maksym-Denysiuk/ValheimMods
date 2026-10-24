// Prefab lookup, spawning and the read-only discovery helpers several commands share (find=, dump=, list).
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace HomeCheat.Kit
{
    public static class Prefabs
    {
        // null when the name is unknown
        public static GameObject Get(string name) { return ZNetScene.instance.GetPrefab(name); }

        // the prefab, or a CommandException "no prefab '<name>' <hint>"
        public static GameObject Require(string name, string hint)
        {
            GameObject go = Get(name);
            if (go == null) throw new CommandException("no prefab '" + name + "'" + (hint == null || hint.Length == 0 ? "" : " " + hint));
            return go;
        }

        // an instance of a prefab by name; null when the name is unknown
        public static GameObject Spawn(string name, Vector3 pos, Quaternion rot)
        {
            GameObject go = Get(name);
            return go == null ? null : (GameObject)UnityEngine.Object.Instantiate(go, pos, rot);
        }

        public static string BaseName(GameObject g) { return g.name.Replace("(Clone)", "").Trim(); }

        // every named prefab of the scene (ZNetScene.m_namedPrefabs)
        public static IEnumerable<GameObject> All()
        {
            Dictionary<int, GameObject> d = Refl.Get(ZNetScene.instance, "m_namedPrefabs") as Dictionary<int, GameObject>;
            if (d == null) yield break;
            foreach (KeyValuePair<int, GameObject> kv in d) if (kv.Value != null) yield return kv.Value;
        }

        // names containing 'text', sorted; 'tag' (may be null) appends a marker such as "  [Aoe]"
        public static List<string> Matching(string text, Func<GameObject, string> tag)
        {
            List<string> names = new List<string>();
            foreach (GameObject g in All())
                if (g.name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) names.Add(g.name + (tag == null ? "" : tag(g)));
            names.Sort();
            return names;
        }

        // the find= answer: one name per line, at most 'max', or "no prefab matches '<text>'"
        public static string Find(string text, int max, Func<GameObject, string> tag)
        {
            List<string> names = Matching(text, tag);
            if (names.Count == 0) return "no prefab matches '" + text + "'";
            return Lines(names, max, "... (more)");
        }

        public static string Lines(List<string> lines, int max, string more)
        {
            StringBuilder sb = new StringBuilder();
            int n = 0;
            foreach (string s in lines) { if (n++ >= max) { sb.Append(more); break; } sb.Append(s).Append('\n'); }
            return sb.ToString();
        }

        // the dump= answer: components with collider sizes, destructible and drop facts, and the plain fields of the
        // component types named in 'fieldsOf' (e.g. "Aoe"). Spawns nothing.
        public static string Dump(string prefab, params string[] fieldsOf)
        {
            GameObject pf = Get(prefab);
            if (pf == null) return "ERROR: no prefab '" + prefab + "'";
            StringBuilder sb = new StringBuilder();
            sb.Append("prefab ").Append(prefab).Append(" root scale ").Append(Fmt.Vec(pf.transform.lossyScale));
            Renderer[] rs = pf.GetComponentsInChildren<Renderer>(true);
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                foreach (Renderer rd in rs) b.Encapsulate(rd.bounds);
                sb.Append("  render bounds size ").Append(Fmt.Vec(b.size)).Append(" centre offset ").Append(Fmt.Vec(b.center - pf.transform.position));
            }
            sb.Append('\n');
            foreach (Component c in pf.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                string tn = c.GetType().Name;
                if (tn == "Transform") continue;
                sb.Append("  ").Append(c.transform == pf.transform ? "root" : c.transform.name).Append(": ").Append(tn);
                AppendColliders(sb, c);
                AppendDestructible(sb, c);
                if (Array.IndexOf(fieldsOf, tn) >= 0) AppendFields(sb, c);
                sb.Append('\n');
            }
            return sb.ToString();
        }

        static void AppendColliders(StringBuilder sb, Component c)
        {
            SphereCollider sc = c as SphereCollider;
            if (sc != null) sb.Append(" radius=").Append(sc.radius.ToString("F2")).Append(" trigger=").Append(sc.isTrigger).Append(" lossyScale=").Append(Fmt.Vec(sc.transform.lossyScale));
            CapsuleCollider cc = c as CapsuleCollider;
            if (cc != null) sb.Append(" radius=").Append(cc.radius.ToString("F2")).Append(" height=").Append(cc.height.ToString("F2"));
            BoxCollider bc = c as BoxCollider;
            if (bc != null) sb.Append(" size=").Append(Fmt.Vec(bc.size)).Append(" centre=").Append(Fmt.Vec(bc.center));
            MeshCollider mc = c as MeshCollider;
            if (mc != null) sb.Append(" convex=").Append(mc.convex);
        }

        static void AppendDestructible(StringBuilder sb, Component c)
        {
            Destructible dsr = c as Destructible;
            if (dsr != null)
            {
                sb.Append(" health=").Append(dsr.m_health).Append(" destroyedEffect=[");
                AppendEffects(sb, dsr.m_destroyedEffect);
                sb.Append("] hitEffect=[");
                AppendEffects(sb, dsr.m_hitEffect);
                sb.Append("] spawnWhenDestroyed=").Append(dsr.m_spawnWhenDestroyed != null ? dsr.m_spawnWhenDestroyed.name : "-").Append(" fragments=").Append(dsr.m_autoCreateFragments);
            }
            DropOnDestroyed dod = c as DropOnDestroyed;
            if (dod != null && dod.m_dropWhenDestroyed != null)
            {
                DropTable dt = dod.m_dropWhenDestroyed;
                sb.Append(" dropMin=").Append(dt.m_dropMin).Append(" dropMax=").Append(dt.m_dropMax).Append(" chance=").Append(dt.m_dropChance).Append(" oneOfEach=").Append(dt.m_oneOfEach).Append(" items=[");
                foreach (DropTable.DropData dr in dt.m_drops)
                    sb.Append(dr.m_item != null ? dr.m_item.name : "?").Append(" stack ").Append(dr.m_stackMin).Append("-").Append(dr.m_stackMax).Append(" w").Append(dr.m_weight).Append("; ");
                sb.Append("]");
            }
        }

        static void AppendEffects(StringBuilder sb, EffectList list)
        {
            if (list == null || list.m_effectPrefabs == null) return;
            foreach (EffectList.EffectData ed in list.m_effectPrefabs) sb.Append(ed.m_prefab != null ? ed.m_prefab.name : "?").Append("; ");
        }

        // primitive, string, enum and Vector2 fields; m_damage on one line
        static void AppendFields(StringBuilder sb, Component c)
        {
            foreach (FieldInfo f in c.GetType().GetFields(Refl.InstanceAll))
            {
                object v = f.GetValue(c);
                if (v == null) continue;
                if (f.FieldType.IsPrimitive || f.FieldType == typeof(string) || f.FieldType.IsEnum || f.FieldType == typeof(Vector2))
                    sb.Append(' ').Append(f.Name).Append('=').Append(v);
                else if (f.Name == "m_damage") sb.Append(' ').Append(f.Name).Append('=').Append(v.ToString().Replace('\n', ' '));
            }
        }
    }
}
