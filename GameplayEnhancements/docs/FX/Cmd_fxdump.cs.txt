// op=fxdump: inventory of every fx_/vfx_/sfx_ prefab for the effect description files.
// Writes fxdump.csv (one row per effect prefab: components, particles, light, audio clips, lifetime, ZNetView, used-by)
// and fxdump-usedby.csv (effect, owner, field path) next to it. Read only: nothing is spawned, nothing is changed.
// C# 5 (csc 4.0). Build: bash build-cmd.sh fxdump ; load in game: op=reload ; run: op=fxdump [out=<folder>]
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace HomeCheat
{
    public class FxDumpCommand : IHomeCommand
    {
        public string Name { get { return "fxdump"; } }
        public string Help { get { return "fxdump [out=<folder>] : writes fxdump.csv and fxdump-usedby.csv (all fx_/vfx_/sfx_ prefabs, components, clips, used-by). Read only."; } }

        const string DefaultOut = @"D:\Claude\Personal\HomeOS\LocalOutputs\2026-10-24--00-48";
        const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        HashSet<string> effectNames;
        // effect -> list of "owner :: path"
        Dictionary<string, List<string>> usedBy;
        Assembly valheim = typeof(ZNetView).Assembly;
        int scannedRoots;

        static bool IsEffectName(string n)
        {
            return n.StartsWith("fx_", StringComparison.OrdinalIgnoreCase) || n.StartsWith("vfx_", StringComparison.OrdinalIgnoreCase)
                || n.StartsWith("sfx_", StringComparison.OrdinalIgnoreCase);
        }

        public string Run(Dictionary<string, string> a)
        {
            string outDir = HomeApi.S(a, "out", DefaultOut);
            FieldInfo nf = typeof(ZNetScene).GetField("m_namedPrefabs", F);
            Dictionary<int, GameObject> named = nf.GetValue(ZNetScene.instance) as Dictionary<int, GameObject>;
            if (named == null) return "ERROR: m_namedPrefabs not found";

            effectNames = new HashSet<string>();
            usedBy = new Dictionary<string, List<string>>();
            List<GameObject> effects = new List<GameObject>();
            List<GameObject> all = new List<GameObject>();
            HashSet<GameObject> seen = new HashSet<GameObject>();
            foreach (KeyValuePair<int, GameObject> kv in named)
            {
                GameObject g = kv.Value;
                if (g == null || !seen.Add(g)) continue;
                all.Add(g);
                if (IsEffectName(g.name)) { effects.Add(g); effectNames.Add(g.name); }
            }
            // items from the object database that are not in the scene table
            if (ObjectDB.instance != null)
                foreach (GameObject g in ObjectDB.instance.m_items)
                    if (g != null && seen.Add(g)) all.Add(g);

            // ---- used-by scan -----------------------------------------------------------
            int errors = 0;
            foreach (GameObject g in all)
            {
                try
                {
                    scannedRoots++;
                    HashSet<object> visited = new HashSet<object>();
                    foreach (MonoBehaviour mb in g.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (mb == null || mb.GetType().Assembly != valheim) continue;
                        { visited.Add(mb); ScanFields(mb, mb.GetType(), g.name, mb.GetType().Name, visited, 0); }
                    }
                }
                catch (Exception e) { errors++; HomeApi.Log("fxdump scan " + g.name + ": " + e.Message); }
            }
            if (ObjectDB.instance != null)
                foreach (StatusEffect se in ObjectDB.instance.m_StatusEffects)
                {
                    if (se == null) continue;
                    try { HashSet<object> vis = new HashSet<object>(); vis.Add(se); ScanFields(se, se.GetType(), "SE:" + se.name, se.GetType().Name, vis, 0); }
                    catch (Exception e) { errors++; HomeApi.Log("fxdump SE " + se.name + ": " + e.Message); }
                }

            // ---- per prefab rows -------------------------------------------------------
            effects.Sort(delegate (GameObject x, GameObject y) { return string.CompareOrdinal(x.name, y.name); });
            StringBuilder csv = new StringBuilder();
            csv.AppendLine("name,znetview,components,ps_count,ps_loop,ps_maxlife,ps_colors,materials,children,light,clips,audio_loop,fields,usedby_count,usedby,extent,maxsize");
            StringBuilder ub = new StringBuilder();
            ub.AppendLine("effect,owner,path");
            foreach (GameObject g in effects)
            {
                try { csv.AppendLine(Row(g)); }
                catch (Exception e) { errors++; csv.AppendLine(g.name + ",ERROR " + Q(e.Message)); }
                List<string> u;
                if (usedBy.TryGetValue(g.name, out u))
                    foreach (string s in u)
                    {
                        int i = s.IndexOf(" :: ", StringComparison.Ordinal);
                        ub.AppendLine(Q(g.name) + "," + Q(s.Substring(0, i)) + "," + Q(s.Substring(i + 4)));
                    }
            }
            Directory.CreateDirectory(outDir);
            File.WriteAllText(Path.Combine(outDir, "fxdump.csv"), csv.ToString(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(outDir, "fxdump-usedby.csv"), ub.ToString(), new UTF8Encoding(false));
            return "fxdump: " + effects.Count + " effect prefabs, " + scannedRoots + " owner prefabs scanned, " + usedBy.Count
                + " effects with a user, " + errors + " errors; written to " + outDir;
        }

        // ---- reflection scan for references to effect prefabs ---------------------------

        void Add(string effect, string owner, string path)
        {
            List<string> l;
            if (!usedBy.TryGetValue(effect, out l)) { l = new List<string>(); usedBy[effect] = l; }
            string entry = owner + " :: " + path;
            if (!l.Contains(entry)) l.Add(entry);
        }

        void ScanFields(object obj, Type t, string owner, string path, HashSet<object> visited, int depth)
        {
            if (depth > 5) return;
            foreach (FieldInfo f in t.GetFields(F))
            {
                if (f.IsStatic) continue;
                Type ft = f.FieldType;
                if (ft.IsPrimitive || ft.IsEnum || ft == typeof(string)) continue;
                object v;
                try { v = f.GetValue(obj); } catch (Exception) { continue; }
                if (v == null) continue;
                ScanValue(v, ft, owner, path + "." + f.Name, visited, depth);
            }
        }

        void ScanValue(object v, Type ft, string owner, string path, HashSet<object> visited, int depth)
        {
            if (v == null || depth > 5) return;
            GameObject go = v as GameObject;
            if (go != null)
            {
                if (effectNames.Contains(go.name)) Add(go.name, owner, path);
                return;
            }
            if (v is UnityEngine.Object) return; // components, assets, materials: not followed
            Type vt = v.GetType();
            if (vt.IsPrimitive || vt.IsEnum || vt == typeof(string)) return;
            if (vt.Name == "EffectList")
            {
                FieldInfo ef = vt.GetField("m_effectPrefabs", F);
                Array arr = ef == null ? null : ef.GetValue(v) as Array;
                if (arr == null) return;
                foreach (object d in arr)
                {
                    if (d == null) continue;
                    FieldInfo pf = d.GetType().GetField("m_prefab", F);
                    GameObject p = pf == null ? null : pf.GetValue(d) as GameObject;
                    if (p != null && effectNames.Contains(p.name)) Add(p.name, owner, path);
                }
                return;
            }
            IEnumerable en = v as IEnumerable;
            if (en != null)
            {
                int i = 0;
                foreach (object item in en)
                {
                    if (++i > 300) break;
                    if (item == null) continue;
                    ScanValue(item, item.GetType(), owner, path + "[]", visited, depth + 1);
                }
                return;
            }
            if (vt.Assembly != valheim) return; // framework structs (Vector3, Color...) hold no prefabs
            if (!vt.IsValueType && !visited.Add(v)) return;
            ScanFields(v, vt, owner, path, visited, depth + 1);
        }

        // ---- one CSV row per effect prefab -------------------------------------------------

        static string Hex(Color c)
        {
            Color32 k = c;
            return k.r.ToString("x2") + k.g.ToString("x2") + k.b.ToString("x2");
        }

        static string Q(string s)
        {
            if (s == null) s = "";
            return "\"" + s.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
        }

        static void AddDistinct(List<string> l, string s, int cap)
        {
            if (string.IsNullOrEmpty(s) || l.Contains(s) || l.Count >= cap) return;
            l.Add(s);
        }

        string Row(GameObject g)
        {
            Dictionary<string, int> comps = new Dictionary<string, int>();
            int psCount = 0; bool psLoop = false; float psMax = 0f; float extent = 0f, maxSize = 0f;
            List<string> colors = new List<string>(), mats = new List<string>(), kids = new List<string>(),
                lights = new List<string>(), clips = new List<string>(), fields = new List<string>();
            bool audioLoop = false;
            bool zn = g.GetComponent<ZNetView>() != null;

            foreach (Component c in g.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                string tn = c.GetType().Name;
                if (tn == "Transform") continue;
                int n; comps.TryGetValue(tn, out n); comps[tn] = n + 1;

                ParticleSystem ps = c as ParticleSystem;
                if (ps != null)
                {
                    psCount++;
                    ParticleSystem.MainModule m = ps.main;
                    if (m.loop) psLoop = true;
                    float life = m.duration + m.startLifetime.constantMax;
                    if (!m.loop && life > psMax) psMax = life;
                    float sc3 = Mathf.Max(ps.transform.lossyScale.x, Mathf.Max(ps.transform.lossyScale.y, ps.transform.lossyScale.z));
                    float reach = m.startSpeed.constantMax * m.startLifetime.constantMax * sc3;
                    float shp = ps.shape.enabled ? ps.shape.radius * sc3 : 0f;
                    if (reach > 60f) reach = 60f;
                    if (reach + shp > extent) extent = reach + shp;
                    float psz = m.startSize.constantMax * sc3;
                    if (psz > maxSize) maxSize = psz;
                    Color sc = m.startColor.color;
                    if (sc.a > 0.1f) AddDistinct(colors, Hex(sc), 5);
                    if (ps.transform != g.transform) AddDistinct(kids, ps.gameObject.name, 12);
                    continue;
                }
                ParticleSystemRenderer pr = c as ParticleSystemRenderer;
                if (pr != null)
                {
                    Material mt = pr.sharedMaterial;
                    if (mt != null) AddDistinct(mats, mt.name, 6);
                    continue;
                }
                Light l = c as Light;
                if (l != null) { AddDistinct(lights, l.type + " #" + Hex(l.color) + " r" + l.range.ToString("F0") + " i" + l.intensity.ToString("F1"), 4); continue; }
                AudioSource au = c as AudioSource;
                if (au != null)
                {
                    if (au.clip != null) AddDistinct(clips, au.clip.name, 10);
                    if (au.loop) audioLoop = true;
                    continue;
                }
                if (c.GetType().Assembly == typeof(ZNetView).Assembly)
                {
                    if (tn == "ZSFX")
                    {
                        FieldInfo cf = c.GetType().GetField("m_audioClips", F);
                        AudioClip[] ac = cf == null ? null : cf.GetValue(c) as AudioClip[];
                        if (ac != null) foreach (AudioClip k in ac) if (k != null) AddDistinct(clips, k.name, 12);
                        FieldInfo lf = c.GetType().GetField("m_loop", F);
                        if (lf != null && lf.GetValue(c) is bool && (bool)lf.GetValue(c)) audioLoop = true;
                    }
                    if (tn == "ZSFX" || tn == "CamShaker" || tn == "TimedDestruction" || tn == "Aoe" || tn == "Projectile"
                        || tn == "EffectFade" || tn == "LightFlicker" || tn == "LightLod" || tn == "Smoke")
                        fields.Add(tn + "{" + Prims(c) + "}");
                }
            }
            // direct child names (first level) as extra hints
            foreach (Transform ch in g.transform) AddDistinct(kids, ch.gameObject.name, 12);

            StringBuilder cs = new StringBuilder();
            foreach (KeyValuePair<string, int> kv in comps) cs.Append(kv.Key).Append('x').Append(kv.Value).Append(' ');

            List<string> u;
            usedBy.TryGetValue(g.name, out u);
            int uc = u == null ? 0 : u.Count;
            List<string> shown = new List<string>();
            if (u != null)
            {
                // compact: owner (first path segment after the type) grouped, max 15
                foreach (string s in u)
                {
                    int i = s.IndexOf(" :: ", StringComparison.Ordinal);
                    string own = s.Substring(0, i);
                    string p = s.Substring(i + 4);
                    int dot = p.LastIndexOf('.');
                    string fld = dot >= 0 ? p.Substring(dot + 1) : p;
                    AddDistinct(shown, own + "." + fld, 15);
                }
            }
            return Q(g.name) + "," + (zn ? "1" : "0") + "," + Q(cs.ToString().Trim()) + "," + psCount + "," + (psLoop ? "1" : "0") + ","
                + psMax.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "," + Q(string.Join(" ", colors.ToArray())) + ","
                + Q(string.Join(";", mats.ToArray())) + "," + Q(string.Join(";", kids.ToArray())) + "," + Q(string.Join(";", lights.ToArray())) + ","
                + Q(string.Join(";", clips.ToArray())) + "," + (audioLoop ? "1" : "0") + "," + Q(string.Join(" ", fields.ToArray())) + ","
                + uc + "," + Q(string.Join(" | ", shown.ToArray())) + "," + extent.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "," + maxSize.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        }

        // primitive public fields of a component (name=value), for the few component types that carry timing / strength
        static string Prims(Component c)
        {
            StringBuilder sb = new StringBuilder();
            foreach (FieldInfo f in c.GetType().GetFields(F))
            {
                if (f.IsStatic) continue;
                Type ft = f.FieldType;
                if (!(ft.IsPrimitive || ft.IsEnum)) continue;
                object v; try { v = f.GetValue(c); } catch (Exception) { continue; }
                if (v is float) sb.Append(f.Name.Replace("m_", "")).Append('=').Append(((float)v).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)).Append(' ');
                else if (v is bool) { if ((bool)v) sb.Append(f.Name.Replace("m_", "")).Append(' '); }
                else sb.Append(f.Name.Replace("m_", "")).Append('=').Append(v).Append(' ');
            }
            return sb.ToString().Trim();
        }
    }
}
