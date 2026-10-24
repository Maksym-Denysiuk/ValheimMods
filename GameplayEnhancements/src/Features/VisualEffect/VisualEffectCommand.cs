// op=visualeffect: spawn visual/sound effects, force weather or time of day (auto-reverting), add a light, shake the camera.
// C# 5 (csc 4.0). Build: bash build-cmd.sh visualeffect ; load in game: op=reload.
// Only the local client is affected (weather/time/light/shake are client side; prefab VFX are local instances).
using System;
using System.Collections.Generic;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.VisualEffect
{
    public class VisualEffectCommand : CommandBase
    {
        public override string Name { get { return "visualeffect"; } }
        protected override string Usage
        {
            get
            {
                return "visualeffect name=<effect> [at=aim|self|x,y,z] [scale=1] [duration=sec] [count=1] [spacing=2] [lift=0.2] [ground=1] [share=1: all players see it; share=0: local only, scale/duration work] | list=1 | name=reset | name=status"
                    + " | probe: name=_probe&prefab=<vfx prefab>&duration=8 | name=_envs";
            }
        }

        // kind: prefab (vfx/fx/sfx prefab), env (forced weather), time (forced day fraction), light, shake
        // arg: prefab name / environment name / day fraction / "r,g,b" colour. dur: default seconds (0 = prefab's own life)
        class Fx
        {
            public override string Name, Kind, Arg, Note;
            public float Scale, Dur;
            public Fx(string n, string k, string a, float scale, float dur, string note)
            { Name = n; Kind = k; Arg = a; Scale = scale; Dur = dur; Note = note; }
        }

        
        // verified in game 2026-10-24 (screenshots from Maksym), spawned on the ground at the crosshair
        static readonly List<Fx> Catalog = new List<Fx>
        {
            new Fx("lightning", "prefab", "fx_Lightning", 1f, 6f, "small blue lightning bolt with thunder"),
            new Fx("lightningred", "prefab", "fx_Lightning_red", 1f, 6f, "red sparks / red lightning"),
            new Fx("explosion", "prefab", "fx_dynamite_explosion", 1f, 0f, "fire burst"),
            new Fx("fireball", "prefab", "fx_fireball_staff_explosion", 1f, 0f, "fireball blast with smoke and camera shake"),
            new Fx("nova", "prefab", "fx_DvergerMage_Nova_ring", 1f, 6f, "big cyan expanding ring"),
            new Fx("thunderstomp", "prefab", "fx_eikthyr_stomp", 1f, 0f, "huge lightning shockwave with flying rocks, camera shake")
        };

        // revert tokens: a newer weather/time request cancels the pending revert of the older one
        static int envToken, timeToken;

        protected override string Execute(Args a)
        {
            if (a.Flag("list", false)) return ListCatalog();
            string name = a.Str("name", "");
            if (name.Length == 0) return "ERROR: name required (visualeffect list=1). " + Help;
            string lname = name.ToLowerInvariant();

            if (lname == "reset") return Reset();
            if (lname == "status") return State();
            if (lname == "_envs") return ListEnvs();
            if (lname == "_probe") return Probe(a);
            if (lname == "_count") return CountPrefabs(a);

            Fx fx = null;
            foreach (Fx f in Catalog) if (f.Name.ToLowerInvariant() == lname) { fx = f; break; }
            if (fx == null) return "ERROR: unknown effect '" + name + "' (visualeffect list=1)";

            float scale = a.Float("scale", fx.Scale);
            float dur = a.Float("duration", fx.Dur);
            switch (fx.Kind)
            {
                case "prefab": return SpawnPrefab(fx.Arg, a, scale, dur, fx.Name);
                case "env": return ForceEnv(fx.Arg, dur > 0f ? dur : 30f, fx.Name);
                case "time": return ForceTime(float.Parse(fx.Arg, System.Globalization.CultureInfo.InvariantCulture), dur > 0f ? dur : 30f, fx.Name);
                case "light": return AddLight(fx.Arg, a, scale, dur > 0f ? dur : 20f, fx.Name);
                case "shake": return Shake(scale, dur > 0f ? dur : 2f);
            }
            return "ERROR: bad catalog kind " + fx.Kind;
        }

        // ---- catalog -------------------------------------------------------------------

        static string ListCatalog()
        {
            if (Catalog.Count == 0) return "catalog empty: nothing verified yet. Use name=_probe&prefab=<vfx prefab> to test, name=_envs for weather.";
            StringBuilder sb = new StringBuilder();
            foreach (Fx f in Catalog)
                sb.Append(f.Name).Append(" [").Append(f.Kind).Append("] ").Append(f.Note).Append('\n');
            sb.Append("revert: name=reset");
            return sb.ToString();
        }

        // ---- prefab VFX / SFX ----------------------------------------------------------

        // target point, never below the terrain: y = max(point, ground) + lift (lift=0.2 default; ground=0 keeps the raw y)
        static Vector3 Where(Args a, out string error)
        {
            Vector3 p = RawWhere(a, out error);
            if (error != null) return p;
            if (a.Flag("ground", true))
                p.y = Mathf.Max(p.y, Ground.Height(p));
            p.y += a.Float("lift", 0.2f);
            return p;
        }

        // Valheim's string hash (same as GetStableHashCode); checked against the scene's prefab table before use
        static int StableHash(string s)
        {
            int h1 = 5381, h2 = h1;
            for (int i = 0; i < s.Length && s[i] != '\0'; i += 2)
            {
                h1 = ((h1 << 5) + h1) ^ s[i];
                if (i == s.Length - 1 || s[i + 1] == '\0') break;
                h2 = ((h2 << 5) + h2) ^ s[i + 1];
            }
            return h1 + h2 * 1566083941;
        }

        // share=1: ask every connected client (including this one) to spawn the prefab through the game's own
        // "SpawnObject" routed RPC, so other players see and hear it. Scale and duration are not carried by that RPC.
        static bool Share(string prefab, Vector3 pos)
        {
            if (ZRoutedRpc.instance == null) return false;
            int hash = StableHash(prefab);
            if (ZNetScene.instance.GetPrefab(hash) == null) return false;
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SpawnObject", pos, Quaternion.identity, hash);
            return true;
        }

        static Vector3 RawWhere(Args a, out string error)
        {
            error = null;
            Player p = Players.Local;
            string at = a.Str("at", "aim").ToLowerInvariant();
            if (at == "self") return p.transform.position;
            if (at == "aim") { bool aimed; return Aim.Point(p, out aimed); }
            if (at.StartsWith("xyz:")) at = at.Substring(4);
            string[] parts = at.Split(',');
            float x, y, z;
            if (parts.Length == 3
                && float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x)
                && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y)
                && float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out z))
                return new Vector3(x, y, z);
            error = "ERROR: at must be aim, self or x,y,z";
            return Vector3.zero;
        }

        // one effect at one point: networked prefab -> local Instantiate (replicates), plain vfx -> RPC to everybody (share=1) or local
        static bool Emit(string prefab, Vector3 pos, float scale, float dur, bool netted, bool share)
        {
            if (share) return Share(prefab, pos);
            GameObject go = Prefabs.Spawn(prefab, pos, Quaternion.identity);
            if (go == null) return false;
            if (Math.Abs(scale - 1f) > 0.001f) Trees.SetScale(go, scale);
            if (dur > 0f) Kill(go, dur);
            return true;
        }

        static string SpawnPrefab(string prefab, Args a, float scale, float dur, string label)
        {
            string err;
            Vector3 centre = Where(a, out err);
            if (err != null) return err;
            if (Prefabs.Get(prefab) == null) return "ERROR: no prefab '" + prefab + "' (op=list&filter=" + prefab.Substring(0, Math.Min(4, prefab.Length)) + ")";
            // prefabs with a ZNetView are networked objects: a plain local Instantiate already replicates to every player,
            // so the RPC (which would duplicate them on each client) is only for ZNetView-less prefabs (pure vfx/sfx)
            bool netted = Prefabs.Get(prefab).GetComponent<ZNetView>() != null;
            bool share = !netted && a.Flag("share", true);
            string where = netted ? " (networked object, other players see it)" : (share ? " (sent to all players)" : " locally");

            if (a.Str("pattern", "").ToLowerInvariant() == "outward")
            {
                // first strike at the centre, then rings moving outwards: rings, step (m between rings), itemsize (m between strikes
                // on a ring), delay (s between rings). Capped at 150 strikes.
                int rings = Math.Max(1, Math.Min(12, a.Int("rings", 4)));
                float step = a.Float("step", 4f);
                float itemSize = Math.Max(1f, a.Float("itemsize", 5f));
                float delay = a.Float("delay", 1f);
                int total = 0;
                for (int r = 0; r <= rings; r++)
                {
                    List<Vector3> pts;
                    if (r == 0) { pts = new List<Vector3>(); pts.Add(centre); }
                    else pts = HarmEffect.Ring(centre, r * step, itemSize, r * 17f);
                    if (total + pts.Count > 150) break;
                    total += pts.Count;
                    float lift = a.Float("lift", 0.2f);
                    List<Vector3> copy = pts;
                    Later(r * delay, delegate
                    {
                        foreach (Vector3 q in copy)
                        {
                            Vector3 w = q;
                            if (a.Flag("ground", true)) w.y = Mathf.Max(w.y, Ground.Height(w));
                            w.y += lift;
                            Emit(prefab, w, scale, dur, netted, share);
                        }
                    });
                }
                return label + ": outward pattern from the centre, " + total + " strikes over " + rings + " rings (" + step.ToString("F0") + " m apart, "
                    + delay.ToString("F1") + " s per ring)" + where + " at " + Fmt.Vec(centre);
            }

            int count = Math.Max(1, a.Int("count", 1));
            float spacing = a.Float("spacing", 2f);
            Vector3 right = Players.Local.transform.right;
            int made = 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = centre + right * ((i - (count - 1) / 2f) * spacing);
                if (Emit(prefab, pos, scale, dur, netted, share)) made++;
            }
            return label + ": " + made + "x " + prefab + where + " at " + Fmt.Vec(centre)
                + (share ? "" : (dur > 0f ? ", removed after " + dur.ToString("F0") + " s" : ", lives as long as the prefab itself"));
        }

        static void Kill(GameObject go, float seconds)
        {
            Later(seconds, delegate
            {
                if (go == null) return;
                ZNetView nv = go.GetComponent<ZNetView>();
                if (nv != null && nv.IsValid()) ZNetScene.instance.Destroy(go);
                else UnityEngine.Object.Destroy(go);
            });
        }

        // discovery helper: spawn any prefab, report what it is made of
        static string Probe(Args a)
        {
            string prefab = a.Str("prefab", "");
            if (prefab.Length == 0) return "ERROR: prefab required, e.g. name=_probe&prefab=fx_Lightning (op=list&filter=vfx_)";
            GameObject src = Prefabs.Get(prefab);
            if (src == null) return "ERROR: no prefab '" + prefab + "'";
            string err;
            Vector3 pos = Where(a, out err);
            if (err != null) return err;
            float dur = a.Float("duration", 8f);
            float scale = a.Float("scale", 1f);
            Dictionary<string, int> comps = new Dictionary<string, int>();
            foreach (Component c in src.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                string tn = c.GetType().Name;
                if (tn == "Transform") continue;
                int n; comps.TryGetValue(tn, out n); comps[tn] = n + 1;
            }
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, int> kv in comps) sb.Append(kv.Key).Append('x').Append(kv.Value).Append(' ');
            if (a.Flag("share", false))
                return Share(prefab, pos) ? "probe " + prefab + " sent to all players at " + Fmt.Vec(pos) + "; components: " + sb : "ERROR: no network";
            GameObject go = Prefabs.Spawn(prefab, pos, Quaternion.identity);
            if (go == null) return "ERROR: spawn failed";
            if (Math.Abs(scale - 1f) > 0.001f) Trees.SetScale(go, scale);
            Kill(go, dur);
            return "probe " + prefab + " at " + Fmt.Vec(pos) + " removed after " + dur.ToString("F0") + " s; components: " + sb;
        }

        // name=_count: how many prefabs start with vfx_ / fx_ / sfx_; with filter=<text> also lists every matching name
        static string CountPrefabs(Args a)
        {
            System.Reflection.FieldInfo f = typeof(ZNetScene).GetField("m_namedPrefabs", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            Dictionary<int, GameObject> d = f.GetValue(ZNetScene.instance) as Dictionary<int, GameObject>;
            string filter = a.Str("filter", "");
            int v = 0, fx = 0, s = 0;
            List<string> names = new List<string>();
            foreach (KeyValuePair<int, GameObject> kv in d)
            {
                if (kv.Value == null) continue;
                string n = kv.Value.name;
                if (n.StartsWith("vfx_", StringComparison.OrdinalIgnoreCase)) v++;
                else if (n.StartsWith("fx_", StringComparison.OrdinalIgnoreCase)) fx++;
                else if (n.StartsWith("sfx_", StringComparison.OrdinalIgnoreCase)) s++;
                if (filter.Length > 0 && n.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) names.Add(n);
            }
            names.Sort();
            return "vfx_=" + v + " fx_=" + fx + " sfx_=" + s + (filter.Length > 0 ? "\n" + string.Join("\n", names.ToArray()) : "");
        }

        // ---- weather -------------------------------------------------------------------

        static string ListEnvs()
        {
            if (EnvMan.instance == null) return "ERROR: EnvMan not ready (load a world)";
            StringBuilder sb = new StringBuilder();
            foreach (EnvSetup e in EnvMan.instance.m_environments)
                sb.Append(e.m_name).Append(" wet=").Append(e.m_isWet).Append(" cold=").Append(e.m_isCold).Append(" freezing=").Append(e.m_isFreezing)
                  .Append(" wind=").Append(e.m_windMin.ToString("F1")).Append('-').Append(e.m_windMax.ToString("F1")).Append('\n');
            sb.Append("current: ").Append(CurrentEnvName());
            return sb.ToString();
        }

        static string CurrentEnvName()
        {
            EnvMan em = EnvMan.instance;
            if (em == null) return "?";
            return (em.GetCurrentEnvironment() != null ? em.GetCurrentEnvironment().m_name : "?");
        }

        static string ForceEnv(string env, float seconds, string label)
        {
            EnvMan em = EnvMan.instance;
            if (em == null) return "ERROR: EnvMan not ready (load a world)";
            bool known = false;
            foreach (EnvSetup e in em.m_environments) if (e.m_name == env) { known = true; break; }
            if (!known) return "ERROR: unknown environment '" + env + "' (name=_envs)";
            string before = CurrentEnvName();
            em.SetForceEnvironment(env);
            int token = ++envToken;
            Later(seconds, delegate
            {
                if (token != envToken || EnvMan.instance == null) return;
                EnvMan.instance.SetForceEnvironment("");
            });
            return label + ": forced environment " + env + " (was " + before + "), reverts automatically in " + seconds.ToString("F0") + " s (name=reset to revert now)";
        }

        // ---- time of day ---------------------------------------------------------------

        static string ForceTime(float fraction, float seconds, string label)
        {
            EnvMan em = EnvMan.instance;
            if (em == null) return "ERROR: EnvMan not ready (load a world)";
            float before = em.GetDayFraction();
            em.m_debugTimeOfDay = true;
            em.m_debugTime = Mathf.Repeat(fraction, 1f);
            int token = ++timeToken;
            Later(seconds, delegate
            {
                if (token != timeToken || EnvMan.instance == null) return;
                EnvMan.instance.m_debugTimeOfDay = false;
            });
            return label + ": day fraction forced to " + fraction.ToString("F2") + " (was " + before.ToString("F2") + "), reverts automatically in " + seconds.ToString("F0") + " s (name=reset to revert now)";
        }

        // ---- light ---------------------------------------------------------------------

        static string AddLight(string rgb, Args a, float scale, float seconds, string label)
        {
            string err;
            Vector3 pos = Where(a, out err);
            if (err != null) return err;
            string[] c = rgb.Split(',');
            Color col = new Color(
                float.Parse(c[0], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(c[1], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(c[2], System.Globalization.CultureInfo.InvariantCulture));
            GameObject go = new GameObject("HomeCheat_light");
            go.transform.position = pos + Vector3.up * 2f;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = col;
            l.range = 15f * scale;
            l.intensity = 3f;
            Kill(go, seconds);
            return label + ": light at " + Fmt.Vec(pos) + " for " + seconds.ToString("F0") + " s";
        }

        // ---- camera --------------------------------------------------------------------

        static string Shake(float strength, float seconds)
        {
            if (GameCamera.instance == null) return "ERROR: no game camera";
            Vector3 p = GameCamera.instance.transform.position;
            GameCamera.instance.AddShake(p, 50f, strength, false);
            return "camera shake strength " + strength.ToString("F1");
        }

        // ---- reset / state ---------------------------------------------------------------

        static string Reset()
        {
            envToken++; timeToken++;
            EnvMan em = EnvMan.instance;
            if (em == null) return "nothing to reset (EnvMan not ready)";
            em.SetForceEnvironment("");
            em.m_debugTimeOfDay = false;
            return "reset: forced environment cleared, forced time cleared";
        }

        static string ForcedEnvName(EnvMan em)
        {
            System.Reflection.FieldInfo f = typeof(EnvMan).GetField("m_forceEnv", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            return f == null ? "?" : (string)f.GetValue(em);
        }

        static string State()
        {
            EnvMan em = EnvMan.instance;
            if (em == null) return "EnvMan not ready";
            return "environment=" + CurrentEnvName() + " forcedEnv='" + ForcedEnvName(em) + "' debugTimeOfDay=" + em.m_debugTimeOfDay
                + " dayFraction=" + em.GetDayFraction().ToString("F2") + " isDay=" + EnvMan.IsDay() + " isNight=" + EnvMan.IsNight();
        }
    }
}
