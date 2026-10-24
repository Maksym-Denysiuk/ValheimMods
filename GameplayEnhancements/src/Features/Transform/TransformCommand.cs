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

namespace HomeCheat.Features.Transform
{
    public class TransformCommand : CommandBase
    {
        public override string Name { get { return "transform"; } }

        protected override string Usage
        {
            get
            {
                return "op=transform&type=human|raven|serpent|fish[&duration=<s, 0=until human>][&speed=<x, default 1>][&hover=0 (raven keeps gliding forward)][&lookdive=0 (altitude only with Space/Ctrl)][&share=1 (raven, host only: a real networked Crow follows you so other players see it)][&scale=<x>][&yoff=<m>][&model=<prefab>]"
                    + " - raven flies (look up/down to climb/dive, shift = fast), serpent/fish swim fast and crawl on land; local player only (other players' clients would not see it); type=human always restores; type=models lists candidate prefabs";
            }
        }

        // prefab names tried in order; the first that exists in ZNetScene is used
        static string[] Candidates(string type)
        {
            switch (type)
            {
                case "raven": return new string[] { "Crow", "Seagal", "Raven", "Hugin" };
                case "serpent": return new string[] { "Serpent" };
                case "fish": return new string[] { "Fish1", "Fish2", "Fish3", "Fish4_cave" };
                default: return new string[0];
            }
        }

        // visual scale when the caller gives none: natural size (scale=<x> overrides)
        static float DefaultScale(string type)
        {
            switch (type)
            {
                default: return 1f; // natural size of the creature
            }
        }

        protected override string Execute(Args a)
        {
            Player p = Players.Local;
            if (p == null) return "ERROR: no local player (load a world first)";
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

            string[] names = Candidates(type);
            string model = a.Str("model", "");
            if (model.Length > 0) names = new string[] { model };
            if (names.Length == 0) return "ERROR: unknown type '" + type + "' (human|raven|serpent|fish, or model=<prefab>)";

            GameObject prefab = null;
            string used = null;
            foreach (string n in names)
            {
                prefab = Prefabs.Get(n);
                if (prefab != null) { used = n; break; }
            }
            if (prefab == null) return "ERROR: no prefab among [" + string.Join(", ", names) + "] (try op=transform&type=models)";

            float scale = a.Float("scale", DefaultScale(type));
            float yoff = a.Float("yoff", type == "raven" ? 0.6f : 0f); // raven: pivot near body centre so banking looks right
            float duration = a.Float("duration", 0f);

            GameObject clone = MakeVisualClone(prefab, p.transform, scale, yoff);
            TransformState st = p.gameObject.AddComponent<TransformState>();
            st.Init(p, clone, duration > 0f ? Time.time + duration : 0f, type, a.Float("speed", 1f), prefab, a.Flag("hover", true), a.Flag("lookdive", true), a.Flag("share", false), a.Float("humanoffset", 0f));

            StringBuilder sb = new StringBuilder();
            sb.Append("transformed into ").Append(type).Append(" (prefab ").Append(used).Append(", scale ").Append(scale.ToString("F2")).Append(")");
            if (duration > 0f) sb.Append(", back to human in ").Append(duration.ToString("F0")).Append(" s");
            if (a.Flag("share", false))
                sb.Append(st.Shared ? "\nshared: a networked " + used + " follows you for other players (your human body stays visible to them)"
                                    : "\nshare NOT active (raven and serpent only, and only when this game is the host)");
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
            foreach (string t in new string[] { "raven", "serpent", "fish" })
                foreach (string n in Candidates(t))
                    sb.Append(t).Append(": ").Append(n).Append(Prefabs.Get(n) != null ? " found" : " missing").Append('\n');
            sb.Append("other: op=list&filter=<text> lists every prefab name");
            return sb.ToString();
        }
    }

    // Lives on the player while transformed. Hides the human renderers every frame (equipment can re-create them),
    // drives the movement mode, reverts on duration end, on op=transform&type=human and when the player object goes away.
    // Order 1000: runs after the game's own FixedUpdate, so the values set here are the last write before physics.
    //   raven   : Character flight (m_flying) with the Crow's own numbers (RandomFlyingBird: speed, flap/sail durations),
    //             momentum, bank into turns, pitch with climb, flap/sail switching, landed model when standing on the ground
    //   serpent : the Serpent prefab's own swim numbers (speed, turn, acceleration, depth), fists hit like Serpent_attack at 1 star
    //   fish    : swim x2
    //   all     : stamina is never used up
    [DefaultExecutionOrder(1000)]
    public class TransformState : MonoBehaviour
    {
        const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly FieldInfo fMoveDir = typeof(Character).GetField("m_moveDir", BF);
        static readonly FieldInfo fRun = typeof(Character).GetField("m_run", BF);
        static readonly int landedHash = (int)typeof(Player).Assembly.GetType("ZDOVars").GetField("s_landed", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
        float humanOffset;
        Vector3 visOrigin;
        public bool Shared { get { return share; } }

        // diagnostics for type=probe
        public string ShareInfo()
        {
            if (!share) return "share: off";
            if (netCrow == null || netView == null) return "share: on, no networked crow yet";
            ZDO z = netView.IsValid() ? netView.GetZDO() : null;
            StringBuilder sb = new StringBuilder("share: networked " + sharePrefab + " valid=" + netView.IsValid());
            if (z != null)
            {
                sb.Append(" owner=").Append(z.GetOwner()).Append(" isOwner=").Append(netView.IsOwner())
                  .Append(" persistent=").Append(z.Persistent).Append(" distant=").Append(z.Distant)
                  .Append(" sector=").Append(z.GetSector()).Append(" pos=").Append(Fmt.Vec(z.GetPosition()));
            }
            if (z != null)
            {
                // does the host believe each connected peer has received this object? (ZDOMan.m_peers[i].m_zdos)
                try
                {
                    object peers = typeof(ZDOMan).GetField("m_peers", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(ZDOMan.instance);
                    foreach (object pe in (System.Collections.IEnumerable)peers)
                    {
                        Type pt = pe.GetType();
                        object zdos = pt.GetField("m_zdos").GetValue(pe);
                        object inv = pt.GetField("m_invalidSector").GetValue(pe);
                        object forced = pt.GetField("m_forceSend").GetValue(pe);
                        object zp = pt.GetField("m_peer").GetValue(pe);
                        string who = zp.GetType().GetField("m_playerName") != null ? Convert.ToString(zp.GetType().GetField("m_playerName").GetValue(zp)) : "?";
                        bool has = ((System.Collections.IDictionary)zdos).Contains(z.m_uid);
                        bool bad = (bool)inv.GetType().GetMethod("Contains").Invoke(inv, new object[] { z.m_uid });
                        bool frc = (bool)forced.GetType().GetMethod("Contains").Invoke(forced, new object[] { z.m_uid });
                        sb.Append("\npeer '").Append(who).Append("': has this crow=").Append(has).Append(" invalidSector=").Append(bad).Append(" forceSend=").Append(frc)
                          .Append(" (peer knows ").Append(((System.Collections.IDictionary)zdos).Count).Append(" objects)");
                    }
                }
                catch (Exception e) { sb.Append("\npeer check failed: ").Append(e.Message); }
            }
            sb.Append("\nplayers online: ");
            foreach (Player pl in Player.GetAllPlayers()) sb.Append(pl.GetPlayerName()).Append(" (").Append(Fmt.Vec(pl.transform.position)).Append("); ");
            return sb.ToString();
        }
        bool share, lastFlapState, lastWet;
        string sharePrefab;
        GameObject netCrow;
        ZNetView netView;
        ZSyncAnimation netAnim;
        float nextClaim, nextHide;
        readonly List<Renderer> netRenderers = new List<Renderer>();
        static readonly MethodInfo mTakeInput =typeof(Player).GetMethod("TakeInput", BF);
        static readonly FieldInfo fCrouch = typeof(Player).GetField("m_crouchToggled", BF);
        static readonly FieldInfo fCamDist =typeof(GameCamera).GetField("m_distance", BF);
        static readonly FieldInfo fBlocking =typeof(Character).GetField("m_blocking", BF);
        static readonly FieldInfo fStamina =typeof(Player).GetField("m_stamina", BF);

        Player player;
        GameObject model;
        Rigidbody body;
        float until;
        float mul;
        bool flyForm, swimForm, flyingBefore, damageSet;
        float oFlySlow, oFlyFast, oFlyTurn, oWalk, oRun, oSwim, oSwimTurn, oSwimAcc, oSwimDepth, oAccel;
        HitData.DamageTypes oFist;
        readonly List<Renderer> hidden = new List<Renderer>();
        readonly List<Renderer> scratch = new List<Renderer>();
        readonly List<Animator> anims = new List<Animator>();

        // raven
        float flapDur = 2f, sailDur = 1.5f, modeTimer;
        bool cycleFlap = true;
        GameObject flyModel, landModel;
        float pitch, roll, lastYaw;
        RandomFlyingBird bird;
        float cruise = 15f, energy = 1f, oFov = 65f, oCamMax, oCamDist;
        bool hover = true, lookDive = true, camSet, airborne, lastUp;
        float groundTime, takeoffT, curY, hopPhase;
        // serpent / fish
        float baseDepth, diveOff, baseY, landLift;
        int groundMask;
        readonly List<Transform> tailJoints = new List<Transform>();
        bool lastAttack, lastBlock;
        float nextRoar;
        string kind;

        public void Init(Player p, GameObject m, float untilTime, string k, float speedMul, GameObject prefab, bool allowHover, bool allowLookDive, bool shareWithOthers, float humanOffsetY)
        {
            humanOffset = humanOffsetY;
            GameObject vis0 = p.GetVisual();
            if (vis0 != null) visOrigin = vis0.transform.localPosition;
            player = p; model = m; until = untilTime; kind = k; mul = Mathf.Max(0.1f, speedMul); hover = allowHover; lookDive = allowLookDive;
            // share (raven, host only): a real networked Crow follows the local model, so other players see it without any mod
            share = shareWithOthers && (k == "raven" || k == "serpent") && ZNet.instance != null && ZNet.instance.IsServer();
            sharePrefab = prefab.name;
            body = p.GetComponent<Rigidbody>();
            anims.AddRange(m.GetComponentsInChildren<Animator>(true));
            oFlySlow = p.m_flySlowSpeed; oFlyFast = p.m_flyFastSpeed; oFlyTurn = p.m_flyTurnSpeed; oAccel = p.m_acceleration;
            oWalk = p.m_walkSpeed; oRun = p.m_runSpeed;
            oSwim = p.m_swimSpeed; oSwimTurn = p.m_swimTurnSpeed; oSwimAcc = p.m_swimAcceleration; oSwimDepth = p.m_swimDepth;
            flyingBefore = p.m_flying;
            flyForm = k == "raven";
            swimForm = k == "serpent" || k == "fish";
            lastYaw = p.transform.eulerAngles.y;
            baseY = m.transform.localPosition.y;
            curY = baseY;

            if (flyForm)
            {
                bird = prefab.GetComponent<RandomFlyingBird>();
                cruise = bird != null ? bird.m_speed : 15f;
                if (bird != null)
                {
                    flapDur = bird.m_flapDuration; sailDur = bird.m_sailDuration;
                    if (bird.m_flyingModel != null) flyModel = FindChild(m.transform, bird.m_flyingModel.name);
                    if (bird.m_landedModel != null) landModel = FindChild(m.transform, bird.m_landedModel.name);
                }
                p.m_flySlowSpeed = cruise * mul;
                p.m_flyFastSpeed = cruise * 1.6f * mul;
                p.m_flyTurnSpeed = 120f;
                p.m_acceleration = 0.12f; // momentum: speed builds and bleeds off over about a second
                p.m_flying = false; // on the ground until the first jump (Space) takes off
                // camera: 4 m further back (and room to zoom), field of view widens with speed (see FixedUpdate)
                GameCamera cam = GameCamera.instance;
                if (cam != null && fCamDist != null)
                {
                    oFov = cam.m_fov; oCamMax = cam.m_maxDistance; oCamDist = (float)fCamDist.GetValue(cam);
                    cam.m_maxDistance = oCamMax + 5f;
                    fCamDist.SetValue(cam, Mathf.Min(oCamDist + 4f, cam.m_maxDistance));
                    camSet = true;
                }
            }
            baseDepth = k == "serpent" ? (prefab.GetComponent<Humanoid>() != null ? prefab.GetComponent<Humanoid>().m_swimDepth : p.m_swimDepth) : p.m_swimDepth;
            if (k == "serpent")
            {
                // tail segments stay above the terrain on land (terrain only: rocks and buildings are not checked)
                foreach (Tail tl in m.GetComponentsInChildren<Tail>(true))
                {
                    tl.m_groundCheck = true;
                    if (tl.m_tailJoints != null) tailJoints.AddRange(tl.m_tailJoints);
                }
                Humanoid sh = prefab.GetComponent<Humanoid>();
                if (sh != null)
                {
                    p.m_swimSpeed = sh.m_swimSpeed * mul; p.m_swimTurnSpeed = sh.m_swimTurnSpeed;
                    p.m_swimAcceleration = sh.m_swimAcceleration; p.m_swimDepth = sh.m_swimDepth;
                    SetFistDamage(sh);
                }
            }
            else if (k == "fish") p.m_swimSpeed = oSwim * 2f * mul;
            HideHuman();
        }

        // fists hit like the serpent's bite as a 1-star creature (Attack.GetLevelDamageFactor: 1 + 0.5 per star)
        void SetFistDamage(Humanoid serpent)
        {
            try
            {
                if (player.m_unarmedWeapon == null || serpent.m_defaultItems == null) return;
                ItemDrop.ItemData.SharedData fist = player.m_unarmedWeapon.m_itemData.m_shared;
                foreach (GameObject it in serpent.m_defaultItems)
                {
                    ItemDrop id = it == null ? null : it.GetComponent<ItemDrop>();
                    if (id == null || id.m_itemData.m_shared.m_attack == null || id.m_itemData.m_shared.m_attack.m_attackAnimation != "attack") continue;
                    HitData.DamageTypes d = id.m_itemData.m_shared.m_damages;
                    float skillAvg = 1f;
                    Skills sk = player.GetSkills();
                    if (sk != null)
                    {
                        float lo, hi;
                        sk.GetRandomSkillRange(out lo, out hi, Skills.SkillType.Unarmed);
                        skillAvg = Mathf.Max(0.2f, (lo + hi) * 0.5f);
                    }
                    // the game multiplies fist damage by the unarmed skill roll; divide by its average so the expected hit matches
                    d.Modify(1.5f / skillAvg);
                    oFist = fist.m_damages;
                    fist.m_damages = d;
                    damageSet = true;
                    HomeApi.Log("transform: fist damage set from " + it.name + " x1.5 (skill avg " + skillAvg.ToString("F2") + ")");
                    return;
                }
            }
            catch (Exception e) { HomeApi.Log("transform fist damage: " + e.Message); }
        }

        static GameObject FindChild(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
            return null;
        }

        // destroys every TransformState on the player, whichever build of the DLL created it
        public static int RemoveAll(Player p)
        {
            int n = 0;
            foreach (MonoBehaviour mb in p.GetComponents<MonoBehaviour>())
                if (mb != null && mb.GetType().Name == "TransformState") { UnityEngine.Object.DestroyImmediate(mb); n++; }
            return n;
        }

        void HideHuman()
        {
            if (player == null) return;
            GameObject vis = player.GetVisual();
            if (vis == null) return;
            scratch.Clear();
            vis.GetComponentsInChildren<Renderer>(true, scratch);
            foreach (Renderer r in scratch)
                if (r != null && r.enabled) { r.enabled = false; hidden.Add(r); }
        }

        void SetAnim(string param, bool v)
        {
            foreach (Animator an in anims)
            {
                if (an == null) continue;
                foreach (AnimatorControllerParameter pr in an.parameters)
                    if (pr.name == param && pr.type == AnimatorControllerParameterType.Bool) an.SetBool(param, v);
            }
        }

        void SetTrigger(string param)
        {
            if (netAnim != null && netView != null && netView.IsValid() && netView.IsOwner()) netAnim.SetTrigger(param);
            foreach (Animator an in anims)
            {
                if (an == null) continue;
                foreach (AnimatorControllerParameter pr in an.parameters)
                    if (pr.name == param && pr.type == AnimatorControllerParameterType.Trigger) an.SetTrigger(param);
            }
        }

        void FixedUpdate()
        {
            if (player == null) return;
            // no stamina use in any form: refill after whatever the game just spent (run, jump, swim, attack)
            if (fStamina != null) fStamina.SetValue(player, player.GetMaxStamina());

            Vector3 md = fMoveDir != null ? (Vector3)fMoveDir.GetValue(player) : Vector3.zero;
            bool moving = md.magnitude > 0.1f;
            float dt = Time.fixedDeltaTime;
            // Space (jump) = up, Ctrl (crouch) = down, read through the game's own bindings; ignored while a menu or chat has the input
            bool up = false, down = false;
            if (mTakeInput == null || (bool)mTakeInput.Invoke(player, null))
            {
                up = ZInput.GetButton("Jump");
                down = ZInput.GetButton("Crouch");
            }
            if (fCrouch != null) fCrouch.SetValue(player, false); // Ctrl must not leave the (hidden) human crouching
            if (up && down) { up = false; down = false; }
            bool jumpEdge = up && !lastUp;
            lastUp = up;
            // block key: the creature's call (serpent roar, crow caw), 2 s cooldown, on the key going down
            bool blk = player.IsBlocking() || (fBlocking != null && (bool)fBlocking.GetValue(player));
            bool callNow = blk && !lastBlock && Time.time >= nextRoar;
            lastBlock = blk;
            if (callNow) nextRoar = Time.time + 2f;
            if (flyForm)
            {
                // walking until a jump takes off; landing (standing still on the ground for 0.5 s) goes back to walking
                if (!airborne && jumpEdge) { airborne = true; groundTime = 0f; takeoffT = 0.8f; }
                player.m_flying = airborne;
                bool run = fRun != null && (bool)fRun.GetValue(player);
                float ly = player.GetLookDir().y;
                if (!airborne)
                {
                    if (flyModel != null) flyModel.SetActive(false);
                    if (landModel != null) landModel.SetActive(true);
                    SetAnim("flapping", false);
                    if (GameCamera.instance != null) GameCamera.instance.ResetTempFOV();
                    if (callNow && bird != null && bird.m_randomNoise != null)
                        bird.m_randomNoise.Create(player.transform.position + Vector3.up, Quaternion.identity, player.transform, 1f, -1, default(ZDOID));
                }
                else
                {
                // energy: a dive builds speed (up to x1.5), a climb costs it (down to x0.7)
                if (!lookDive && !up && !down) ly = 0f; // lookdive=0: only Space / Ctrl change altitude
                float tf = down ? 1.4f : up ? 0.8f : !moving ? 1f : (ly < 0f ? 1f + 0.5f * Mathf.Clamp01(-ly) : 1f - 0.3f * Mathf.Clamp01(ly));
                energy = Mathf.Lerp(energy, tf, 1f - Mathf.Exp(-2f * dt));
                player.m_flySlowSpeed = cruise * mul * energy;
                player.m_flyFastSpeed = cruise * 1.6f * mul * energy;
                // the bird's own flap / sail rhythm, overridden by what the flight needs
                modeTimer += dt;
                if (modeTimer > (cycleFlap ? flapDur : sailDur)) { cycleFlap = !cycleFlap; modeTimer = 0f; }
                bool flap = cycleFlap;
                if (!moving || run || ly > 0.15f) flap = true;
                else if (ly < -0.25f) flap = false;
                if (body != null)
                {
                    Vector3 v = body.linearVelocity;
                    if (takeoffT > 0f && !down)
                    {
                        // the jump that starts the flight: a sustained climb for 0.8 s
                        takeoffT -= dt;
                        body.linearVelocity = new Vector3(v.x, cruise * mul * 0.6f, v.z);
                    }
                    else if (up || down)
                    {
                        // Space climbs, Ctrl dives (faster than the climb); works standing still too
                        float vk = (down ? -1f : 1f) * cruise * mul * (down ? 1.1f : 0.7f) * (run ? 1.3f : 1f);
                        body.linearVelocity = new Vector3(v.x, vk, v.z);
                    }
                    else if (moving)
                    {
                        float spd = run ? player.m_flyFastSpeed : player.m_flySlowSpeed;
                        float vy = Mathf.Abs(ly) < 0.05f ? 0f : ly * spd;
                        if (!flap && Mathf.Abs(ly) < 0.15f) vy = -1f; // sailing in level flight sinks slowly
                        body.linearVelocity = new Vector3(v.x, vy, v.z);
                    }
                    else if (!player.IsOnGround())
                    {
                        if (hover) body.linearVelocity = new Vector3(v.x, -1.4f, v.z); // nothing touched: lose altitude
                        else
                        {
                            // hover=0: a crow cannot hover, it keeps gliding forward at stall speed and sinks a little
                            Vector3 f = player.transform.forward; f.y = 0f; f.Normalize();
                            body.linearVelocity = f * cruise * mul * 0.35f + Vector3.down * 1.4f;
                        }
                    }
                }
                if (player.IsOnGround() && !moving && !up) groundTime += dt; else groundTime = 0f;
                if (groundTime > 0.5f) { airborne = false; groundTime = 0f; player.m_flying = false; }
                bool landed = player.IsOnGround() && !moving;
                if (flyModel != null) flyModel.SetActive(!landed);
                if (landModel != null) landModel.SetActive(landed);
                SetAnim("flapping", flap);
                lastFlapState = flap;
                if (callNow && bird != null && bird.m_randomNoise != null)
                    bird.m_randomNoise.Create(player.transform.position + Vector3.up, Quaternion.identity, player.transform, 1f, -1, default(ZDOID));
                // wider view with speed
                GameCamera cam = GameCamera.instance;
                if (cam != null)
                {
                    float spdNow = body != null ? body.linearVelocity.magnitude : 0f;
                    cam.SetTempFOV(oFov + 18f * Mathf.Clamp01((spdNow - cruise * 0.6f) / (cruise * 0.9f)), 0.5f);
                }
                }
            }
            else if (swimForm)
            {
                bool wet = player.InWater() || player.IsSwimming();
                lastWet = wet;
                // out of the water the body is nearly helpless: 20 % walk and run speed
                player.m_walkSpeed = wet ? oWalk : oWalk * 0.2f;
                player.m_runSpeed = wet ? oRun : oRun * 0.2f;
                SetAnim("inWater", wet);
                bool atk = player.InAttack();
                if (atk && !lastAttack) SetTrigger("attack");
                lastAttack = atk;
                if (kind == "serpent" && callNow)
                {
                    SetTrigger("taunt");
                    GameObject sfx = Prefabs.Spawn("sfx_serpent_taunt", player.transform.position + Vector3.up, Quaternion.identity);
                    if (sfx == null) HomeApi.Log("transform: sfx_serpent_taunt prefab missing");
                }
                // diving: the game's swim code holds the body at (water level - m_swimDepth), so a deeper m_swimDepth sinks it.
                // Looking down while swimming moves the depth down (3 m/s), looking up brings it back; never closer than 1.5 m to the floor.
                if (wet)
                {
                    float surf = player.GetLiquidLevel();
                    float floor = Ground.Height(player.transform.position);
                    float maxOff = Mathf.Max(0f, surf - floor - 1.5f - baseDepth);
                    if (down) diveOff += 3f * dt;
                    else if (up) diveOff -= 3f * dt;
                    else if (moving && lookDive)
                    {
                        float ly = player.GetLookDir().y;
                        if (ly < -0.2f) diveOff += 3f * dt * Mathf.Clamp01((-ly - 0.2f) / 0.4f);
                        else if (ly > 0.2f) diveOff -= 3f * dt * Mathf.Clamp01((ly - 0.2f) / 0.4f);
                    }
                    diveOff = Mathf.Clamp(diveOff, 0f, maxOff);
                }
                else diveOff = 0f;
                player.m_swimDepth = baseDepth + diveOff;
            }
        }

        // the networked crow: a real Crow object owned by this (host) game, AI removed, hidden on our own screen, placed on our local model
        void UpdateShared()
        {
            if (netCrow == null || netView == null || !netView.IsValid())
            {
                if (netCrow != null && ZNetScene.instance != null) ZNetScene.instance.Destroy(netCrow);
                netCrow = Prefabs.Spawn(sharePrefab, model.transform.position, model.transform.rotation);
                if (netCrow == null) { share = false; HomeApi.Log("transform share: prefab " + sharePrefab + " missing"); return; }
                netView = netCrow.GetComponent<ZNetView>();
                netAnim = netCrow.GetComponent<ZSyncAnimation>();
                RandomFlyingBird b = netCrow.GetComponent<RandomFlyingBird>();
                if (b != null) UnityEngine.Object.DestroyImmediate(b); // no wandering: we place it
                if (kind == "serpent")
                {
                    // a real Serpent object, but with no brain and no creature script on the host: AI, loot, then the Character itself
                    // (its OnDestroy removes it from the world's creature list); the body stays kinematic so physics does not fight us
                    foreach (Type tp in new Type[] { typeof(MonsterAI), typeof(CharacterDrop), typeof(Humanoid) })
                    {
                        Component cp = netCrow.GetComponent(tp);
                        if (cp != null) { try { UnityEngine.Object.DestroyImmediate(cp); } catch (Exception e) { HomeApi.Log("transform share strip " + tp.Name + ": " + e.Message); } }
                    }
                    Rigidbody nb = netCrow.GetComponent<Rigidbody>();
                    if (nb != null) { nb.isKinematic = true; nb.useGravity = false; }
                }
                foreach (Collider c in netCrow.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                netRenderers.Clear();
                netRenderers.AddRange(netCrow.GetComponentsInChildren<Renderer>(true));
                HomeApi.Log("transform share: spawned networked " + sharePrefab);
            }
            netCrow.transform.position = model.transform.position;
            netCrow.transform.rotation = model.transform.rotation;
            if (Time.time >= nextHide) { nextHide = Time.time + 0.5f; foreach (Renderer r in netRenderers) if (r != null) r.enabled = false; }
            if (netView != null && netView.IsValid())
            {
                if (Time.time >= nextClaim) { nextClaim = Time.time + 2f; if (!netView.IsOwner()) netView.ClaimOwnership(); }
                if (netView.IsOwner())
                {
                    if (kind == "raven")
                    {
                        netView.GetZDO().Set(landedHash, !airborne);
                        if (netAnim != null) netAnim.SetBool("flapping", airborne && lastFlapState);
                    }
                    else if (netAnim != null) netAnim.SetBool("inWater", lastWet);
                }
            }
        }

        void LateUpdate()
        {
            if (player == null) { Destroy(this); return; }
            if (until > 0f && Time.time >= until) { Destroy(this); return; }
            HideHuman();
            // optional: the human model is moved humanoffset metres up (default 0 = off; it does not hide the body from other players)
            if (humanOffset != 0f)
            {
                GameObject vis = player.GetVisual();
                if (vis != null) vis.transform.localPosition = visOrigin + Vector3.up * humanOffset;
            }
            if (share && model != null) UpdateShared();
            if (kind == "serpent" && model != null)
            {
                // the body pivot sits low: on land lift the model so the head clears the ground, in water leave it sunk
                bool wetNow = player.InWater() || player.IsSwimming();
                landLift = Mathf.Lerp(landLift, wetNow ? 0f : 1.3f, 1f - Mathf.Exp(-6f * Mathf.Max(0.0001f, Time.deltaTime)));
                Vector3 lp = model.transform.localPosition;
                model.transform.localPosition = new Vector3(lp.x, baseY + landLift, lp.z);
                // on land lift every tail bone above whatever is under it (rocks, buildings, terrain): a ray down from 4 m above the bone
                if (!wetNow && tailJoints.Count > 0)
                {
                    if (groundMask == 0) groundMask = LayerMask.GetMask("Default", "static_solid", "terrain", "piece", "Default_small");
                    foreach (Transform j in tailJoints)
                    {
                        if (j == null) continue;
                        RaycastHit hit;
                        Vector3 o = j.position + Vector3.up * 4f;
                        if (Physics.Raycast(o, Vector3.down, out hit, 8f, groundMask, QueryTriggerInteraction.Ignore))
                        {
                            float want = hit.point.y + 0.7f;
                            if (j.position.y < want) j.position = new Vector3(j.position.x, want, j.position.z);
                        }
                    }
                }
            }
            if (flyForm && model != null && body != null)
            {
                float dt = Mathf.Max(0.0001f, Time.deltaTime);
                float yaw = player.transform.eulerAngles.y;
                float yawRate = Mathf.DeltaAngle(lastYaw, yaw) / dt;
                lastYaw = yaw;
                Vector3 v = body.linearVelocity;
                float hs = new Vector3(v.x, 0f, v.z).magnitude;
                // bank into the turn (about 45 degrees at the bird's own 60 deg/s), nose follows the climb angle
                float wantRoll = -Mathf.Clamp(yawRate * 0.75f, -45f, 45f);
                float wantPitch = hs + Mathf.Abs(v.y) > 0.5f ? Mathf.Clamp(Mathf.Atan2(v.y, Mathf.Max(hs, 1f)) * Mathf.Rad2Deg, -60f, 60f) : 0f;
                float k = 1f - Mathf.Exp(-6f * dt);
                // walking on the ground: the sitting crow has no animation of its own, so it hops (up to 5 hops/s, 0.2 m high) and rocks
                // with each hop; it also drops to the ground instead of hanging at the flight pivot height
                bool walkNow = !airborne;
                bool hopping = walkNow && hs > 0.3f;
                curY = Mathf.Lerp(curY, walkNow ? 0f : baseY, k);
                if (hopping) hopPhase += Mathf.Clamp(hs * 1.2f, 0f, 5f) * 6.2832f * dt;
                float hop = hopping ? Mathf.Abs(Mathf.Sin(hopPhase)) * 0.2f : 0f;
                Vector3 lp2 = model.transform.localPosition;
                model.transform.localPosition = new Vector3(lp2.x, curY + hop, lp2.z);
                if (walkNow) { wantRoll = 0f; wantPitch = hopping ? Mathf.Sin(hopPhase * 2f) * 8f : 0f; }
                roll = Mathf.Lerp(roll, wantRoll, k);
                pitch = Mathf.Lerp(pitch, wantPitch, k);
                model.transform.localRotation = Quaternion.Euler(-pitch, 0f, roll);
            }
        }

        void OnDestroy()
        {
            foreach (Renderer r in hidden) if (r != null) r.enabled = true;
            hidden.Clear();
            if (model != null) UnityEngine.Object.Destroy(model);
            if (netCrow != null && ZNetScene.instance != null) ZNetScene.instance.Destroy(netCrow);
            if (player == null) return;
            if (humanOffset != 0f)
            {
                GameObject vis = player.GetVisual();
                if (vis != null) vis.transform.localPosition = visOrigin;
            }
            try
            {
                player.m_flySlowSpeed = oFlySlow; player.m_flyFastSpeed = oFlyFast; player.m_flyTurnSpeed = oFlyTurn; player.m_acceleration = oAccel;
                player.m_walkSpeed = oWalk; player.m_runSpeed = oRun;
                player.m_swimSpeed = oSwim; player.m_swimTurnSpeed = oSwimTurn; player.m_swimAcceleration = oSwimAcc; player.m_swimDepth = oSwimDepth;
                player.m_flying = flyingBefore;
                if (camSet && GameCamera.instance != null)
                {
                    GameCamera cam = GameCamera.instance;
                    cam.ResetTempFOV();
                    cam.m_maxDistance = oCamMax;
                    fCamDist.SetValue(cam, Mathf.Min((float)fCamDist.GetValue(cam), oCamMax));
                }
                if (damageSet && player.m_unarmedWeapon != null) player.m_unarmedWeapon.m_itemData.m_shared.m_damages = oFist;
                if (flyForm)
                {
                    if (body != null) body.useGravity = true;
                    // back in the air as a human: fall without damage (m_maxAirAltitude is kept at the current height until landing)
                    if (!player.IsOnGround() && player.gameObject.activeInHierarchy) player.gameObject.AddComponent<TransformLanding>();
                }
            }
            catch (Exception e) { HomeApi.Log("transform restore: " + e.Message); }
        }
    }

    // after a flight ends in mid-air: keeps the fall-damage reference at the current height until the player lands or swims
    [DefaultExecutionOrder(1000)]
    public class TransformLanding : MonoBehaviour
    {
        static readonly FieldInfo fMaxAir = typeof(Character).GetField("m_maxAirAltitude", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Player player;
        float start;

        void Start() { player = GetComponent<Player>(); start = Time.time; }

        void FixedUpdate()
        {
            if (player == null || player.IsOnGround() || player.InWater() || Time.time - start > 60f) { Destroy(this); return; }
            if (fMaxAir != null) fMaxAir.SetValue(player, player.transform.position.y);
        }
    }

    // Lives on ANOTHER player's object in the host game. Spawns one networked Crow (AI removed) that follows that player's real position
    // and shows the sitting model on the ground / the flapping one in the air, hides the player's human renderers on this screen,
    // and removes everything on revert, on duration end, or when the player leaves.
    [DefaultExecutionOrder(1000)]
    public class TransformRemote : MonoBehaviour
    {
        static readonly int landedHash = (int)typeof(Player).Assembly.GetType("ZDOVars").GetField("s_landed", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
        Player player;
        string prefabName;
        float until, nextClaim, nextHide;
        GameObject netCrow;
        ZNetView netView;
        ZSyncAnimation netAnim;
        readonly List<Renderer> netRenderers = new List<Renderer>();
        readonly List<Renderer> hidden = new List<Renderer>();
        readonly List<Renderer> scratch = new List<Renderer>();

        public void Init(Player p, string prefab, float duration)
        {
            player = p; prefabName = prefab; until = duration > 0f ? Time.time + duration : 0f;
        }

        public static int RemoveAll(Player p)
        {
            int n = 0;
            foreach (MonoBehaviour mb in p.GetComponents<MonoBehaviour>())
                if (mb != null && mb.GetType().Name == "TransformRemote") { UnityEngine.Object.DestroyImmediate(mb); n++; }
            return n;
        }

        void LateUpdate()
        {
            if (player == null) { Destroy(this); return; }
            if (until > 0f && Time.time >= until) { Destroy(this); return; }
            // hide their body on this screen
            GameObject vis = player.GetVisual();
            if (vis != null)
            {
                scratch.Clear();
                vis.GetComponentsInChildren<Renderer>(true, scratch);
                foreach (Renderer r in scratch) if (r != null && r.enabled) { r.enabled = false; hidden.Add(r); }
            }
            if (netCrow == null || netView == null || !netView.IsValid())
            {
                if (netCrow != null && ZNetScene.instance != null) ZNetScene.instance.Destroy(netCrow);
                netCrow = Prefabs.Spawn(prefabName, player.transform.position, player.transform.rotation);
                if (netCrow == null) { Destroy(this); return; }
                netView = netCrow.GetComponent<ZNetView>();
                netAnim = netCrow.GetComponent<ZSyncAnimation>();
                RandomFlyingBird b = netCrow.GetComponent<RandomFlyingBird>();
                if (b != null) UnityEngine.Object.DestroyImmediate(b);
                foreach (Collider c in netCrow.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                netRenderers.Clear();
                netRenderers.AddRange(netCrow.GetComponentsInChildren<Renderer>(true));
                HomeApi.Log("transform: networked " + prefabName + " follows " + player.GetPlayerName());
            }
            netCrow.transform.position = player.transform.position;
            netCrow.transform.rotation = player.transform.rotation;
            // unlike our own form, this crow stays visible on the host screen: it is the only thing standing in for the hidden body
            if (netView != null && netView.IsValid())
            {
                if (Time.time >= nextClaim) { nextClaim = Time.time + 2f; if (!netView.IsOwner()) netView.ClaimOwnership(); }
                if (netView.IsOwner())
                {
                    bool air = !player.IsOnGround();
                    netView.GetZDO().Set(landedHash, !air);
                    if (netAnim != null) netAnim.SetBool("flapping", air);
                }
            }
        }

        void OnDestroy()
        {
            foreach (Renderer r in hidden) if (r != null) r.enabled = true;
            hidden.Clear();
            if (netCrow != null && ZNetScene.instance != null) ZNetScene.instance.Destroy(netCrow);
        }
    }
}
