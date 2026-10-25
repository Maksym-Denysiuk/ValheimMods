using System;
using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.TransformCommands
{
    // One creature form (Maksym 2026-10-25: every form must inherit what other players see). TransformState holds what all forms
    // share: the hidden human, the networked stand-in that other players see (share) with the human body sent away (hide), stamina
    // never used up, the movement modes (Flies: Character flight, Swims: the game's swim code) and the revert. A form adds its
    // prefabs, numbers and creature details through the virtual methods below; a new form is a subclass plus a row in All.
    internal abstract class TransformForm
    {
        public abstract string Name { get; }
        public abstract string[] Prefabs { get; }      // tried in order, the first that exists is used
        public virtual float YOffset { get { return 0f; } }
        public virtual bool Flies { get { return false; } }
        public virtual bool Swims { get { return false; } }

        // other players see the form (host only): on for every form unless the cast says share=0 or hide=0
        public virtual bool Shareable { get { return true; } }

        // the networked copy on the host: it must neither think, move by itself, nor be usable; the base takes its colliders off
        public virtual void PrepareStandIn(GameObject net)
        {
            foreach (Collider c in net.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            Rigidbody rb = net.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
        }

        // what other players' copies should show (animator values through the stand-in's ZSyncAnimation / ZDO)
        public virtual void SyncStandIn(TransformState st, ZNetView nv, ZSyncAnimation anim) { }

        // movement numbers and creature setup when the form starts (TransformState saved the originals and restores them)
        public virtual void Begin(TransformState st, Player p, GameObject prefab) { }

        // each FixedUpdate, after the movement mode: block = the block key went down (2 s cooldown)
        public virtual void Step(TransformState st, Player p, float dt, bool wet, bool block) { }

        // each LateUpdate: the pose of the local model
        public virtual void Pose(TransformState st, Player p, GameObject model, float dt) { }

        // a new instance per cast: a form keeps its own state (tail bones, jump, flop) for that one transformation
        public static TransformForm Get(string name)
        {
            switch ((name ?? "").ToLowerInvariant())
            {
                case "raven": return new RavenForm();
                case "serpent": return new SerpentForm();
                case "fish": return new FishForm();
                default: return null;
            }
        }

        public static readonly string[] AllNames = new string[] { "raven", "serpent", "fish" };
        public static string Names { get { return string.Join("|", AllNames); } }

        protected static void Strip(GameObject go, params Type[] types)
        {
            foreach (Type tp in types)
            {
                Component cp = go.GetComponent(tp);
                if (cp == null) continue;
                try { UnityEngine.Object.DestroyImmediate(cp); } catch (Exception e) { HomeApi.Log("transform share strip " + tp.Name + ": " + e.Message); }
            }
        }
    }

    // flight with the Crow's own numbers (the flight itself is TransformState's Flies mode)
    internal sealed class RavenForm : TransformForm
    {
        public override string Name { get { return "raven"; } }
        public override string[] Prefabs { get { return new string[] { "Crow", "Seagal", "Raven", "Hugin" }; } }
        public override float YOffset { get { return 0.6f; } }   // pivot near the body centre so banking looks right
        public override bool Flies { get { return true; } }

        public override void PrepareStandIn(GameObject net)
        {
            Strip(net, typeof(RandomFlyingBird));   // no wandering: we place it
            base.PrepareStandIn(net);
        }

        public override void SyncStandIn(TransformState st, ZNetView nv, ZSyncAnimation anim)
        {
            nv.GetZDO().Set(TransformState.LandedHash, !st.Airborne);
            if (anim != null) anim.SetBool("flapping", st.Airborne && st.LastFlap);
        }
    }

    // the Serpent prefab's own swim numbers, fists hit like its bite, the taunt on block, tail above the ground on land
    internal sealed class SerpentForm : TransformForm
    {
        public override string Name { get { return "serpent"; } }
        public override string[] Prefabs { get { return new string[] { "Serpent" }; } }
        public override bool Swims { get { return true; } }

        readonly List<Transform> tailJoints = new List<Transform>();
        float baseY, landLift;
        int groundMask;

        public override void PrepareStandIn(GameObject net)
        {
            // a real Serpent object, but with no brain and no creature script on the host: AI, loot, then the Character itself
            // (its OnDestroy removes it from the world's creature list)
            Strip(net, typeof(MonsterAI), typeof(CharacterDrop), typeof(Humanoid));
            base.PrepareStandIn(net);
        }

        public override void SyncStandIn(TransformState st, ZNetView nv, ZSyncAnimation anim)
        {
            if (anim != null) anim.SetBool("inWater", st.LastWet);
        }

        public override void Begin(TransformState st, Player p, GameObject prefab)
        {
            baseY = st.Model.transform.localPosition.y;
            foreach (Tail tl in st.Model.GetComponentsInChildren<Tail>(true))
            {
                tl.m_groundCheck = true;
                if (tl.m_tailJoints != null) tailJoints.AddRange(tl.m_tailJoints);
            }
            Humanoid sh = prefab.GetComponent<Humanoid>();
            if (sh == null) return;
            p.m_swimSpeed = sh.m_swimSpeed * st.SpeedMul; p.m_swimTurnSpeed = sh.m_swimTurnSpeed;
            p.m_swimAcceleration = sh.m_swimAcceleration; p.m_swimDepth = sh.m_swimDepth;
            st.SetFistDamage(sh);
        }

        public override void Step(TransformState st, Player p, float dt, bool wet, bool block)
        {
            if (!block) return;
            st.Trigger("taunt");
            GameObject sfx = HomeCheat.Kit.Prefabs.Spawn("sfx_serpent_taunt", p.transform.position + Vector3.up, Quaternion.identity);
            if (sfx == null) HomeApi.Log("transform: sfx_serpent_taunt prefab missing");
        }

        public override void Pose(TransformState st, Player p, GameObject model, float dt)
        {
            // the body pivot sits low: on land lift the model so the head clears the ground, in water leave it sunk
            bool wetNow = p.InWater() || p.IsSwimming();
            landLift = Mathf.Lerp(landLift, wetNow ? 0f : 1.3f, 1f - Mathf.Exp(-6f * Mathf.Max(0.0001f, dt)));
            Vector3 lp = model.transform.localPosition;
            model.transform.localPosition = new Vector3(lp.x, baseY + landLift, lp.z);
            // on land lift every tail bone above whatever is under it (rocks, buildings, terrain): a ray down from 4 m above the bone
            if (wetNow || tailJoints.Count == 0) return;
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

    // A fish (Maksym 2026-10-25). The fish prefabs have no animator: the game moves them with physics (Fish: swim along the
    // way, jumps out of the water, flops on land). The local model copies that: in water it noses along its velocity, the block
    // key jumps out of the water with the fish's own jump effects, on land it lies on its side and flops in small hops.
    internal sealed class FishForm : TransformForm
    {
        public override string Name { get { return "fish"; } }
        public override string[] Prefabs { get { return new string[] { "Fish1", "Fish2", "Fish3", "Fish4_cave" }; } }
        public override bool Swims { get { return true; } }

        const float JumpUp = 7f, JumpForward = 6f;   // m/s: about 2.5 m out of the water, a few metres ahead
        Fish fish;                                     // the prefab's Fish: jump effects
        float pitch, roll, yawWiggle, flopT, flopPhase, nextFlop;
        bool jumping;

        public override void PrepareStandIn(GameObject net)
        {
            // a real Fish object that neither swims by itself nor can be picked up or killed on the host
            Strip(net, typeof(Fish), typeof(ItemDrop), typeof(Destructible));
            base.PrepareStandIn(net);
        }

        public override void Begin(TransformState st, Player p, GameObject prefab)
        {
            fish = prefab.GetComponent<Fish>();
            p.m_swimSpeed = st.OrigSwimSpeed * 2f * st.SpeedMul;
            // a fish swims without stamina (Maksym 2026-10-25); restored when human again
            p.m_swimStaminaDrainMinSkill = 0f; p.m_swimStaminaDrainMaxSkill = 0f;
        }

        public override void Step(TransformState st, Player p, float dt, bool wet, bool block)
        {
            Rigidbody body = st.Body;
            if (body == null) return;
            if (block && wet && !jumping)
            {
                // the jump out of the water: up and along the look direction, with the fish's own splash
                Vector3 f = p.GetLookDir(); f.y = 0f; f = f.sqrMagnitude > 0.001f ? f.normalized : p.transform.forward;
                body.linearVelocity = f * JumpForward + Vector3.up * JumpUp;
                jumping = true;
                if (fish != null && fish.m_jumpEffects != null)
                    fish.m_jumpEffects.Create(p.transform.position, Quaternion.identity, null, 1f, -1);
            }
            else if (jumping && body.linearVelocity.y < 0f && wet)
            {
                jumping = false;   // back in the water
                if (fish != null && fish.m_jumpEffects != null)
                    fish.m_jumpEffects.Create(p.transform.position, Quaternion.identity, null, 1f, -1);
            }
            if (jumping && p.IsOnGround()) jumping = false;
        }

        public override void Pose(TransformState st, Player p, GameObject model, float dt)
        {
            Rigidbody body = st.Body;
            Vector3 v = body != null ? body.linearVelocity : Vector3.zero;
            float hs = new Vector3(v.x, 0f, v.z).magnitude;
            bool wet = p.InWater() || p.IsSwimming();
            float k = 1f - Mathf.Exp(-8f * Mathf.Max(0.0001f, dt));
            float wantPitch, wantRoll, hop = 0f;
            if (wet || jumping)
            {
                // nose along the velocity (the jump arcs nose up, then nose down), a light tail wiggle while swimming
                wantPitch = hs + Mathf.Abs(v.y) > 0.3f ? Mathf.Clamp(Mathf.Atan2(v.y, Mathf.Max(hs, 0.5f)) * Mathf.Rad2Deg, -75f, 75f) : 0f;
                wantRoll = 0f;
                yawWiggle = hs > 0.3f ? Mathf.Sin(Time.time * 12f) * 8f : Mathf.Lerp(yawWiggle, 0f, k);
                flopT = 0f;
            }
            else
            {
                // on land: on its side, flopping (the game's land jumps: m_jumpOnLandChance every m_jumpFrequencySeconds)
                wantPitch = 0f;
                wantRoll = 85f;
                if (Time.time >= nextFlop)
                {
                    float chance = fish != null ? fish.m_jumpOnLandChance : 0.2f;
                    nextFlop = Time.time + (fish != null ? Mathf.Max(0.1f, fish.m_jumpFrequencySeconds) : 0.3f);
                    if (hs > 0.2f || UnityEngine.Random.value < chance * 2f) flopT = 0.45f;
                }
                if (flopT > 0f)
                {
                    flopT -= dt;
                    flopPhase = Mathf.Clamp01(1f - flopT / 0.45f);
                    hop = Mathf.Sin(flopPhase * Mathf.PI) * 0.35f;
                    yawWiggle = Mathf.Sin(flopPhase * Mathf.PI * 2f) * 25f;
                    wantRoll = 85f - Mathf.Sin(flopPhase * Mathf.PI) * 40f;
                }
                else yawWiggle = Mathf.Lerp(yawWiggle, 0f, k);
            }
            pitch = Mathf.Lerp(pitch, wantPitch, k);
            roll = Mathf.Lerp(roll, wantRoll, k);
            Vector3 lp = model.transform.localPosition;
            model.transform.localPosition = new Vector3(lp.x, st.ModelBaseY + hop + (wet ? 0f : 0.15f), lp.z);
            model.transform.localRotation = Quaternion.Euler(-pitch, yawWiggle, roll);
        }
    }
}
