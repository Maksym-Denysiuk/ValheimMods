// op=transform: show another creature's model on the local player and give it a matching movement mode.
//   raven   -> flight (Character.m_flying, the game's own flight code, NOT the debug-fly cheat), vertical speed follows the look pitch
//   serpent -> fast in water, crawling on land;  fish -> same, smaller
// C# 5 only. Everything the command adds lives in one TransformState component on the player, so revert = destroy it.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.TransformCommands
{
    public sealed class TransformCommand : CommandBase
    {
        public override string Name { get { return "transform"; } }

        protected override string Usage
        {
            get
            {
                return "op=transform&type=human|raven|serpent|fish[&duration=<s, 0=until human>][&speed=<x, default 1>][&hover=0 (raven keeps gliding forward)][&lookdive=0 (altitude only with Space/Ctrl)][&share=1 (default, host only: a networked copy of the creature follows you so other players see it; 0 = only you see the form)][&hide=1 (default, with share: your human body is hidden from other players, op=hidebody, until you are human again)][&scale=<x>][&yoff=<m>][&model=<prefab>]"
                    + " - raven flies (look up/down to climb/dive, shift = fast), serpent/fish swim fast and crawl on land; other players see the form through share (default on, host only); type=human always restores; type=models lists candidate prefabs";
            }
        }

        // prefab names tried in order (the form's own list); the first that exists in ZNetScene is used
        static string[] Candidates(string type)
        {
            TransformForm f = TransformForm.Get(type);
            return f != null ? f.Prefabs : new string[0];
        }

        // visual scale when the caller gives none: natural size (scale=<x> overrides)
        static float DefaultScale(string type)
        {
            switch (type)
            {
                default: return 1f; // natural size of the creature
            }
        }

        // a replaced build takes its components off the players, so the new build starts from the human form
        protected override void OnUnload()
        {
            if (Player.m_localPlayer != null) TransformState.RemoveAll(Player.m_localPlayer);
            foreach (Player pl in Player.GetAllPlayers()) TransformRemote.RemoveAll(pl);
        }

        protected override string Execute(Args a)
        {
            Player p = Players.Local;
            string type = a.Str("type", "").ToLowerInvariant();
            string target = a.Str("target", "");
            if (target.Length > 0 && !string.Equals(target, p.GetPlayerName(), StringComparison.OrdinalIgnoreCase))
                return RunOther(target, type, a);

            if (type == "models") return Models();
            if (type == "components") return Components(Prefabs.Get(a.Str("model", "Serpent")));
            if (type == "probe") return Probe(p);
            if (type == "effects") return Effects(p, a.Flag("play", false));
            if (type == "values") return Values(Prefabs.Get(a.Str("model", "Crow")), a.Str("comp", ""));
            if (type.Length == 0) return "ERROR: type required: human|raven|serpent|fish. " + Help;

            // always start clean, so repeated calls never stack models
            int removed = TransformState.RemoveAll(p);
            if (type == "human") return "human: restored (removed " + removed + " transform state)";

            TransformForm form = TransformForm.Get(type);
            if (form == null) return "ERROR: unknown type '" + type + "' (human|" + TransformForm.Names + ")";
            string[] names = form.Prefabs;
            string model = a.Str("model", "");
            if (model.Length > 0) names = new string[] { model };

            GameObject prefab = null;
            string used = null;
            foreach (string n in names)
            {
                prefab = Prefabs.Get(n);
                if (prefab != null) { used = n; break; }
            }
            if (prefab == null) return "ERROR: no prefab among [" + string.Join(", ", names) + "] (try op=transform&type=models)";

            float scale = a.Float("scale", DefaultScale(type));
            float yoff = a.Float("yoff", form.YOffset);
            float duration = a.Float("duration", 0f);

            GameObject clone = MakeVisualClone(prefab, p.transform, scale, yoff);
            TransformState st = p.gameObject.AddComponent<TransformState>();
            // share and hide are on for every form (Maksym 2026-10-25: other players must see the form, not the human); share=0 / hide=0 opt out
            bool share = a.Flag("share", true), hide = a.Flag("hide", true);
            st.Init(p, clone, duration > 0f ? Time.time + duration : 0f, form, a.Float("speed", 1f), prefab, a.Flag("hover", true), a.Flag("lookdive", true), share, hide, a.Float("humanoffset", 0f));

            StringBuilder sb = new StringBuilder();
            sb.Append("transformed into ").Append(type).Append(" (prefab ").Append(used).Append(", scale ").Append(scale.ToString("F2")).Append(")");
            if (duration > 0f) sb.Append(", back to human in ").Append(duration.ToString("F0")).Append(" s");
            if (share)
                sb.Append(st.Shared ? "\nother players see: a networked " + used + (st.HidBody ? ", your human body is hidden from them" : ", and your human body (hide=0)")
                                    : "\nother players see your human body: share works only when this game is the host");
            else sb.Append("\nother players see your human body (share=0)");
            sb.Append("\nrevert: op=transform&type=human");
            sb.Append("\n").Append(Describe(clone));
            return sb.ToString();
        }

        // another player (host only): the raven is a host-spawned networked Crow that follows them, their body is hidden on THIS
        // screen only. Their own game is not touched: no flight, and they still see themselves.
        static string RunOther(string target, string type, Args a)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer())
                return "REFUSED: another player can only be transformed from the host game; here the change would stay on this screen only. Use your own name or leave target out.";
            Player other = Players.Find(target);
            if (other == null) return "ERROR: no player named '" + target + "' is online";
            if (other == Players.Local) return "ERROR: that is the local player; leave target out";
            int removed = TransformRemote.RemoveAll(other);
            if (type == "human") return target + ": restored (removed " + removed + " remote form)";
            if (type != "raven") return "REFUSED: other players can only be turned into a raven (and back with type=human) for now";
            GameObject prefab = Prefabs.Get("Crow");
            if (prefab == null) return "ERROR: no Crow prefab";
            TransformRemote tr = other.gameObject.AddComponent<TransformRemote>();
            tr.Init(other, "Crow", a.Float("duration", 0f));
            return target + " is shown as a raven: a networked Crow follows them for everyone, their body is hidden on this screen only. They still see their own body and keep walking (no flight). Revert: op=transform&type=human&target=" + target;
        }

        // Instantiate under an INACTIVE parent so no Awake runs (no ZNetView, no Character in the world list, no AI),
        // strip everything but the visuals, then attach to the player.
        static GameObject MakeVisualClone(GameObject prefab, Transform player, float scale, float yoff)
        {
            GameObject holder = new GameObject("hc_transform_holder");
            holder.SetActive(false);
            GameObject clone = (GameObject)UnityEngine.Object.Instantiate(prefab, holder.transform, false);
            clone.name = "hc_transform_model";
            Strip(clone);
            clone.transform.SetParent(player, false);
            clone.transform.localPosition = new Vector3(0f, yoff, 0f);
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one * scale;
            clone.SetActive(true);
            UnityEngine.Object.Destroy(holder);
            foreach (Animator an in clone.GetComponentsInChildren<Animator>(true))
            {
                an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (AnimatorControllerParameter pr in an.parameters)
                    if (pr.name == "inWater" && pr.type == AnimatorControllerParameterType.Bool) an.SetBool("inWater", true);
            }
            return clone;
        }

        static void Strip(GameObject root)
        {
            // order matters: colliders and joints before rigidbodies, scripts last
            foreach (Collider c in root.GetComponentsInChildren<Collider>(true)) Kill(c);
            foreach (Joint j in root.GetComponentsInChildren<Joint>(true)) Kill(j);
            foreach (Rigidbody r in root.GetComponentsInChildren<Rigidbody>(true)) Kill(r);
            foreach (AudioSource s in root.GetComponentsInChildren<AudioSource>(true)) Kill(s);
            // Character last: CharacterDrop and friends depend on it
            for (int pass = 0; pass < 3; pass++)
                foreach (MonoBehaviour m in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (m != null && !KeepScript(m) && (pass > 0 || !(m is Character))) Kill(m);
        }

        // scripts that only animate the model and need nothing from Character/ZNetView
        // (Tail = serpent tail follow-through, driven by MonoUpdaters; its IL never touches m_character)
        static bool KeepScript(MonoBehaviour m)
        {
            return m.GetType().Name == "Tail";
        }

        static void Kill(Component c)
        {
            try { UnityEngine.Object.DestroyImmediate(c); } catch (Exception e) { HomeApi.Log("transform strip: " + e.Message); }
        }

        static string Describe(GameObject clone)
        {
            StringBuilder sb = new StringBuilder();
            Bounds b = new Bounds(clone.transform.position, Vector3.zero);
            int n = 0;
            foreach (Renderer r in clone.GetComponentsInChildren<Renderer>(true))
            {
                if (n++ == 0) b = r.bounds; else b.Encapsulate(r.bounds);
            }
            sb.Append("model: ").Append(n).Append(" renderers, size ").Append(Fmt.Vec(b.size));
            foreach (Animator an in clone.GetComponentsInChildren<Animator>(true))
            {
                sb.Append("\nanimator ").Append(an.name).Append(" controller=").Append(an.runtimeAnimatorController == null ? "none" : an.runtimeAnimatorController.name).Append(" params:");
                foreach (AnimatorControllerParameter pr in an.parameters) sb.Append(' ').Append(pr.name).Append(':').Append(pr.type);
            }
            return sb.ToString();
        }

        // diagnostics: public simple field values of the prefab's components (comp=<type name filter>), item damages for default items
        static string Values(GameObject prefab, string comp)
        {
            if (prefab == null) return "ERROR: no such prefab";
            StringBuilder sb = new StringBuilder(prefab.name + "\n");
            foreach (Component c in prefab.GetComponents<Component>())
            {
                if (c == null) continue;
                string tn = c.GetType().Name;
                if (comp.Length > 0 && tn.IndexOf(comp, StringComparison.OrdinalIgnoreCase) < 0) continue;
                sb.Append('[').Append(tn).Append("]\n");
                DumpFields(sb, c, "  ", 0);
                Humanoid h = c as Humanoid;
                if (h != null && h.m_defaultItems != null)
                    foreach (GameObject it in h.m_defaultItems)
                    {
                        ItemDrop id = it == null ? null : it.GetComponent<ItemDrop>();
                        if (id == null) continue;
                        sb.Append("  defaultItem ").Append(it.name).Append(" type=").Append(id.m_itemData.m_shared.m_itemType).Append('\n');
                        DumpFields(sb, id.m_itemData.m_shared.m_damages, "    dmg ", 0);
                        Attack at = id.m_itemData.m_shared.m_attack;
                        if (at != null) { sb.Append("    attack: "); DumpFields(sb, at, "      ", 0); }
                    }
            }
            return sb.ToString();
        }

        static void DumpFields(StringBuilder sb, object o, string ind, int depth)
        {
            foreach (FieldInfo f in o.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                object v;
                try { v = f.GetValue(o); } catch { continue; }
                if (v == null) continue;
                Type t = f.FieldType;
                if (t == typeof(float) || t == typeof(int) || t == typeof(bool) || t == typeof(string) || t.IsEnum)
                { if (!(v is float) || (float)v != 0f) sb.Append(ind).Append(f.Name).Append(" = ").Append(v).Append('\n'); }
                else if (t.IsValueType && !t.IsPrimitive && depth < 2 && t.Namespace != "UnityEngine") DumpFields(sb, v, ind + f.Name + ".", depth + 1);
            }
        }

        // the effects the forms use: the serpent roar (sfx_serpent_taunt) and the crow's own call (RandomFlyingBird.m_randomNoise); play=1 fires both here
        static string Effects(Player p, bool play)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("serpent roar: prefab sfx_serpent_taunt (block key as serpent), found=").Append(Prefabs.Get("sfx_serpent_taunt") != null).Append('\n');
            GameObject crow = Prefabs.Get("Crow");
            RandomFlyingBird bird = crow == null ? null : crow.GetComponent<RandomFlyingBird>();
            if (bird == null || bird.m_randomNoise == null) { sb.Append("crow call: none found"); return sb.ToString(); }
            sb.Append("crow call (RandomFlyingBird.m_randomNoise), prefabs:");
            foreach (EffectList.EffectData ed in bird.m_randomNoise.m_effectPrefabs)
                sb.Append(' ').Append(ed.m_prefab == null ? "(null)" : ed.m_prefab.name);
            sb.Append('\n');
            if (play)
            {
                Prefabs.Spawn("sfx_serpent_taunt", p.transform.position + Vector3.up, Quaternion.identity);
                bird.m_randomNoise.Create(p.transform.position + Vector3.up, Quaternion.identity, p.transform, 1f, -1, default(ZDOID));
                sb.Append("played: serpent roar, then crow call");
            }
            return sb.ToString();
        }

        // diagnostics: every component in a prefab hierarchy
        static string Components(GameObject prefab)
        {
            if (prefab == null) return "ERROR: no such prefab";
            StringBuilder sb = new StringBuilder(prefab.name + "\n");
            foreach (Component c in prefab.GetComponentsInChildren<Component>(true))
                if (c != null) sb.Append(c.transform == prefab.transform ? "" : "  ").Append(c.transform.name).Append(" : ").Append(c.GetType().Name).Append('\n');
            return sb.ToString();
        }

        // diagnostics: live state of the model on the player (animator state, tail joints)
        static string Probe(Player p)
        {
            Transform m = p.transform.Find("hc_transform_model");
            if (m == null) return "no transform model on the player";
            StringBuilder sb = new StringBuilder();
            foreach (MonoBehaviour tsb in p.GetComponents<MonoBehaviour>())
                if (tsb != null && tsb.GetType().Name == "TransformState") sb.Append(((TransformState)tsb).ShareInfo()).Append('\n');
            foreach (Animator an in m.GetComponentsInChildren<Animator>(true))
            {
                AnimatorClipInfo[] ci = an.GetCurrentAnimatorClipInfo(0);
                AnimatorStateInfo si = an.GetCurrentAnimatorStateInfo(0);
                sb.Append("animator ").Append(an.name).Append(" enabled=").Append(an.enabled).Append(" speed=").Append(an.speed)
                  .Append(" layers=").Append(an.layerCount).Append(" clip=").Append(ci.Length > 0 ? ci[0].clip.name : "none")
                  .Append(" t=").Append(si.normalizedTime.ToString("F2")).Append(" loop=").Append(ci.Length > 0 && ci[0].clip.isLooping).Append('\n');
            }
            sb.Append("model active=").Append(m.gameObject.activeInHierarchy).Append(" pos=").Append(Fmt.Vec(m.position)).Append(" localPos=").Append(Fmt.Vec(m.localPosition))
              .Append(" localScale=").Append(Fmt.Vec(m.localScale)).Append(" player=").Append(Fmt.Vec(p.transform.position)).Append('\n');
            foreach (Renderer rr in m.GetComponentsInChildren<Renderer>(true))
                sb.Append("renderer ").Append(rr.name).Append(" enabled=").Append(rr.enabled).Append(" visible=").Append(rr.isVisible).Append(" layer=").Append(rr.gameObject.layer)
                  .Append(" center=").Append(Fmt.Vec(rr.bounds.center)).Append(" size=").Append(Fmt.Vec(rr.bounds.size)).Append('\n');
            if (Camera.main != null) sb.Append("camera pos=").Append(Fmt.Vec(Camera.main.transform.position)).Append('\n');
            foreach (MonoBehaviour mb in m.GetComponentsInChildren<MonoBehaviour>(true))
                sb.Append("script ").Append(mb.name).Append(" : ").Append(mb.GetType().Name).Append(" enabled=").Append(mb.enabled).Append('\n');
            return sb.ToString();
        }

        static string Models()
        {
            StringBuilder sb = new StringBuilder("candidate prefabs (found = exists in this world):\n");
            foreach (string t in TransformForm.AllNames)
                foreach (string n in Candidates(t))
                    sb.Append(t).Append(": ").Append(n).Append(Prefabs.Get(n) != null ? " found" : " missing").Append('\n');
            sb.Append("other: op=list&filter=<text> lists every prefab name");
            return sb.ToString();
        }
    }
}
