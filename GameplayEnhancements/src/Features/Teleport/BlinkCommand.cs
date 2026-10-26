// op=blink: a dash of dist m along your flat look direction. Walls, pieces and creatures are ignored; the ground lifts you onto a
// slope ahead (never below the terrain), a drop is left to fall as usual, the world edge refuses. The move copies Player.UpdateTeleport
// (position, body, velocity zero, m_maxAirAltitude) without TeleportTo, so there is no fade, no wait and no portal.
// Every character within width m of the line you cross is shoved to the side away from it and gets Tared (the tar slow), no damage:
// one HitData each, built as TrapCommand.Boom builds its tar hit (two users: a Kit candidate). An attack: the PvP rule of AttackCommand.
// Look: Hugin, the advice raven (Tutorial.m_ravenPrefab), poofs where you leave and where you appear (his despawn effect and the
// animator trigger "poff", Raven.FlyAway 1.0.20); his model is local, so a networked smoke and crow call are added when his despawn
// effect is not networked (probe=1 says which). Card: docs/Spells/teleport.md, Cards.
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.TeleportCommands
{
    public sealed class BlinkCommand : AttackCommand
    {
        const float DefaultDist = 5f;
        const float MaxDist = 20f;
        const float DefaultWidth = 2f;
        const float MaxWidth = 5f;
        const float DefaultPush = 150f;   // the trap's default push (SledgeStagbreaker's attack force)
        const float MaxPush = 500f;
        const float HeightReach = 3f;     // characters this far above or below the line are left alone
        const float WorldEdge = 10400f;   // metres from the world centre
        const float HuginLife = 2f;
        const string TarEffect = "Tared";
        const string FallbackSmoke = "vfx_odin_despawn";
        const string FallbackCall = "sfx_fallenvalkyrie_screech";

        static readonly FieldInfo fMaxAir = typeof(Character).GetField("m_maxAirAltitude", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public BlinkCommand()
        {
            SubFlag("probe", "probe=1 (read-only: Hugin's prefab, whether his model and his despawn effect are networked, the fallback prefabs)",
                delegate (Args a) { return Probe(); });
        }

        public override string Name { get { return "blink"; } }
        protected override string Usage
        {
            get
            {
                return "blink [dist=5 (metres along your flat look direction, cap 20)] [width=2 (metres to each side of the line, cap 5)] [push=150 (cap 500)]"
                    + " [tamed=0] [boss=0] " + PvpUsage + " [fx=1 (Hugin poofs where you leave and appear; 0 = none)]"
                    + " - dash through walls and creatures; everyone on the line is shoved aside and tarred, no damage; the ground and the world edge are the limit";
            }
        }

        protected override string Execute(Args a)
        {
            Host.RequireWorld();
            Player p = Players.Local;
            float dist = Caps.Check("dist", a.Float("dist", DefaultDist), MaxDist);
            float width = Caps.Check("width", a.Float("width", DefaultWidth), MaxWidth);
            float push = Caps.Check("push", a.Float("push", DefaultPush), MaxPush);
            if (dist <= 0f) throw new CommandException("dist must be > 0");
            if (width < 0f || push < 0f) throw new CommandException("width and push must be >= 0");
            bool fx = a.Flag("fx", true);

            Vector3 dir = p.GetLookDir();
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) { dir = p.transform.forward; dir.y = 0f; }
            dir.Normalize();
            Vector3 from = p.transform.position;
            Vector3 to = from + dir * dist;
            if (new Vector2(to.x, to.z).magnitude > WorldEdge) throw new CommandException("the world ends there: blink refused");
            to.y = Mathf.Max(from.y, Ground.Height(to) + 0.1f);

            Dictionary<string, int> shoved = new Dictionary<string, int>();
            int n = Shove(p, from, to, dir, dist, width, push, a.Flag("tamed", false), a.Flag("boss", false), HitsPlayers(a), shoved);
            if (fx) Poof(p, from, dir);
            Move(p, to);
            if (fx) Poof(p, to, -dir);
            HomeApi.Log("blink: " + Fmt.Vec(from) + " -> " + Fmt.Vec(to) + ", " + n + " shoved");
            return "blinked " + Fmt.F(dist, 1) + " m to " + Fmt.Vec(to) + ", shoved and tarred " + n + Counts(shoved) + PvpNote(a);
        }

        static int Shove(Player p, Vector3 from, Vector3 to, Vector3 dir, float dist, float width, float push,
            bool tamed, bool boss, bool hitPlayers, Dictionary<string, int> shoved)
        {
            float low = Mathf.Min(from.y, to.y) - HeightReach, high = Mathf.Max(from.y, to.y) + HeightReach;
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            int tar = TarEffect.GetStableHashCode();
            int n = 0;
            foreach (Character c in new List<Character>(Character.GetAllCharacters()))
            {
                if (c == null || c == p || c.IsDead()) continue;
                bool player = c is Player;
                if (player && !hitPlayers) continue;
                if (!player && !tamed && c.IsTamed()) continue;
                if (!boss && c.IsBoss()) continue;
                Vector3 pos = c.transform.position;
                if (pos.y < low || pos.y > high) continue;
                Vector3 rel = pos - from;
                rel.y = 0f;
                Vector3 side = rel - dir * Mathf.Clamp(Vector3.Dot(rel, dir), 0f, dist);
                if (side.magnitude > width) continue;
                Vector3 away = side.sqrMagnitude > 0.01f ? side.normalized : right * (Random.value < 0.5f ? 1f : -1f);
                HitData h = new HitData();
                h.m_point = c.GetCenterPoint();
                h.m_dir = (away + Vector3.up * 0.3f).normalized;
                h.m_pushForce = push;
                h.m_statusEffectHash = tar;   // applied by the character's owner with the hit
                h.m_hitType = HitData.HitType.PlayerHit;
                h.SetAttacker(p);
                PvpHits.ArmHit(h, hitPlayers);
                c.Damage(h);
                string key = player ? "player" : Prefabs.BaseName(c.gameObject);
                int k;
                shoved.TryGetValue(key, out k);
                shoved[key] = k + 1;
                n++;
            }
            return n;
        }

        static void Move(Player p, Vector3 to)
        {
            p.transform.position = to;
            Rigidbody body = p.GetComponent<Rigidbody>();
            if (body != null) { body.position = to; body.linearVelocity = Vector3.zero; }
            if (fMaxAir != null) fMaxAir.SetValue(p, to.y);   // no fall damage from the blink itself
        }

        // Hugin at 'at', turned towards 'face', plays his vanish; removed after HuginLife s
        void Poof(Player p, Vector3 at, Vector3 face)
        {
            GameObject prefab = HuginPrefab();
            bool everyoneSees = false;
            Quaternion rot = Quaternion.LookRotation(face);
            if (prefab != null)
            {
                Raven raven = prefab.GetComponent<Raven>();
                if (raven != null && raven.m_despawnEffect != null)
                {
                    everyoneSees = Networked(raven.m_despawnEffect);
                    GameObject[] made = raven.m_despawnEffect.Create(at + Vector3.up, rot, null, 1f, -1);
                    if (made != null) foreach (GameObject go in made) { GameObject g = go; Later(6f, delegate { Fx.Destroy(g); }); }
                }
                GameObject model = HuginModel(prefab, at, rot);
                if (model != null) Later(HuginLife, delegate { if (model != null) Object.Destroy(model); });
            }
            else HomeApi.Log("blink: Hugin's prefab not found (Tutorial.m_ravenPrefab)");
            if (!everyoneSees)
            {
                if (Fx.Spawn(this, FallbackSmoke, at + Vector3.up, 6f) == null) HomeApi.Log("blink: " + FallbackSmoke + " prefab missing");
                if (Fx.Spawn(this, FallbackCall, at + Vector3.up, 4f) == null) HomeApi.Log("blink: " + FallbackCall + " prefab missing");
            }
        }

        // a local copy of Hugin with his brain taken out (no texts, no chat), visual on, colliders off, "poff" fired
        GameObject HuginModel(GameObject prefab, Vector3 at, Quaternion rot)
        {
            bool was = prefab.activeSelf;
            prefab.SetActive(false);
            GameObject go;
            try { go = Object.Instantiate(prefab, at, rot); }
            finally { prefab.SetActive(was); }
            Raven raven = go.GetComponent<Raven>();
            GameObject visual = raven != null ? raven.m_visual : null;
            if (raven != null) Object.DestroyImmediate(raven);
            ZNetView nv = go.GetComponent<ZNetView>();
            if (nv != null) Object.DestroyImmediate(nv);   // never a second networked Hugin
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            OwnInWorld(go);
            go.SetActive(true);
            if (visual != null) visual.SetActive(true);
            Animator an = go.GetComponentInChildren<Animator>();
            if (an != null) an.SetTrigger("poff");
            else HomeApi.Log("blink: Hugin has no Animator");
            return go;
        }

        static GameObject HuginPrefab()
        {
            if (Tutorial.instance != null)
            {
                GameObject g = Refl.Get(Tutorial.instance, "m_ravenPrefab") as GameObject;
                if (g != null) return g;
            }
            GameObject byName = Prefabs.Get("Hugin");
            return byName != null ? byName : Prefabs.Get("Raven");
        }

        static bool Networked(EffectList list)
        {
            if (list.m_effectPrefabs == null || list.m_effectPrefabs.Length == 0) return false;
            foreach (EffectList.EffectData ed in list.m_effectPrefabs)
                if (ed.m_prefab == null || ed.m_prefab.GetComponent<ZNetView>() == null) return false;
            return true;
        }

        static string Probe()
        {
            Host.RequireWorld();
            StringBuilder sb = new StringBuilder();
            GameObject prefab = HuginPrefab();
            if (prefab == null) return "Hugin: prefab not found (Tutorial.m_ravenPrefab, Hugin, Raven)";
            Raven raven = prefab.GetComponent<Raven>();
            sb.Append("Hugin: prefab ").Append(prefab.name).Append(", ZNetView ").Append(prefab.GetComponent<ZNetView>() != null)
                .Append(", Animator ").Append(prefab.GetComponentInChildren<Animator>(true) != null).Append(", Raven ").Append(raven != null);
            if (raven != null && raven.m_despawnEffect != null && raven.m_despawnEffect.m_effectPrefabs != null)
            {
                sb.Append("; despawn effect:");
                foreach (EffectList.EffectData ed in raven.m_despawnEffect.m_effectPrefabs)
                    sb.Append(' ').Append(ed.m_prefab != null ? ed.m_prefab.name + (ed.m_prefab.GetComponent<ZNetView>() != null ? " (networked)" : " (local)") : "null");
                sb.Append(Networked(raven.m_despawnEffect) ? "; everyone sees it" : "; local only: fallback added");
            }
            sb.Append("; fallback ").Append(FallbackSmoke).Append(Prefabs.Get(FallbackSmoke) != null ? " found" : " missing")
                .Append(", ").Append(FallbackCall).Append(Prefabs.Get(FallbackCall) != null ? " found" : " missing");
            return sb.ToString();
        }

        static string Counts(Dictionary<string, int> shoved)
        {
            if (shoved.Count == 0) return "";
            StringBuilder sb = new StringBuilder(" (");
            bool first = true;
            foreach (KeyValuePair<string, int> kv in shoved)
            {
                if (!first) sb.Append(", ");
                sb.Append(kv.Key).Append('=').Append(kv.Value);
                first = false;
            }
            return sb.Append(')').ToString();
        }
    }
}
