using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.TransformCommands
{
    // Lives on the player while transformed: what every form shares (TransformForm holds what differs). Hides the human renderers
    // every frame (equipment can re-create them), shows the form to other players (share: a networked stand-in of the prefab, host
    // only; hide: the human body sent away for them), keeps stamina full, runs the movement mode (Flies: Character flight with the
    // Crow's numbers, momentum, banking, flap/sail, landed model; Swims: the game's swim code with diving), calls the form's
    // Begin / Step / Pose / PrepareStandIn / SyncStandIn, and reverts on duration end, on op=transform&type=human and when the
    // player object goes away. Order 1000: runs after the game's own FixedUpdate, so the values set here are the last write before physics.
    [DefaultExecutionOrder(1000)]
    public sealed class TransformState : MonoBehaviour
    {
        const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly FieldInfo fMoveDir = typeof(Character).GetField("m_moveDir", BF);
        static readonly FieldInfo fRun = typeof(Character).GetField("m_run", BF);
        internal static readonly int LandedHash = (int)typeof(Player).Assembly.GetType("ZDOVars").GetField("s_landed", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
        float humanOffset;
        Vector3 visOrigin;
        public bool Shared { get { return share; } }

        // for the forms (TransformForm)
        internal TransformForm Form { get { return form; } }
        internal GameObject Model { get { return model; } }
        internal Rigidbody Body { get { return body; } }
        internal float SpeedMul { get { return mul; } }
        internal float OrigSwimSpeed { get { return oSwim; } }
        internal float ModelBaseY { get { return baseY; } }
        internal bool Airborne { get { return airborne; } }
        internal bool LastFlap { get { return lastFlapState; } }
        internal bool LastWet { get { return lastWet; } }
        internal void Trigger(string param) { SetTrigger(param); }

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
        float oSwimDrainMin, oSwimDrainMax;   // swim stamina drain (Player.OnSwimming), 0 while a fish
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
        // swimming
        float baseDepth, diveOff, baseY;
        bool lastAttack, lastBlock;
        float nextRoar;
        TransformForm form;

        internal void Init(Player p, GameObject m, float untilTime, TransformForm f, float speedMul, GameObject prefab, bool allowHover, bool allowLookDive, bool shareWithOthers, bool hideBody, float humanOffsetY)
        {
            form = f;
            string k = f.Name;
            humanOffset = humanOffsetY;
            GameObject vis0 = p.GetVisual();
            if (vis0 != null) visOrigin = vis0.transform.localPosition;
            player = p; model = m; until = untilTime; mul = Mathf.Max(0.1f, speedMul); hover = allowHover; lookDive = allowLookDive;
            // share (host only): a real networked copy of the prefab follows the local model, so other players see it without any mod
            share = shareWithOthers && f.Shareable && ZNet.instance != null && ZNet.instance.IsServer();
            sharePrefab = prefab.name;
            body = p.GetComponent<Rigidbody>();
            anims.AddRange(m.GetComponentsInChildren<Animator>(true));
            oFlySlow = p.m_flySlowSpeed; oFlyFast = p.m_flyFastSpeed; oFlyTurn = p.m_flyTurnSpeed; oAccel = p.m_acceleration;
            oWalk = p.m_walkSpeed; oRun = p.m_runSpeed;
            oSwim = p.m_swimSpeed; oSwimTurn = p.m_swimTurnSpeed; oSwimAcc = p.m_swimAcceleration; oSwimDepth = p.m_swimDepth;
            oSwimDrainMin = p.m_swimStaminaDrainMinSkill; oSwimDrainMax = p.m_swimStaminaDrainMaxSkill;
            flyingBefore = p.m_flying;
            flyForm = f.Flies;
            swimForm = f.Swims;
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
            f.Begin(this, p, prefab);
            baseDepth = p.m_swimDepth;   // after Begin: a form may set its own swim depth
            HideHuman();
            // hide (with share): the human body goes away for other players (op=hidebody) until human again
            if (share && hideBody)
            {
                string hb = HomeApi.Exec("op=hidebody&on=1");
                HidBody = !hb.StartsWith("ERROR");
                if (!HidBody) HomeApi.Log("transform hide: " + hb);
            }
        }

        // fists hit like the serpent's bite as a 1-star creature (Attack.GetLevelDamageFactor: 1 + 0.5 per star)
        internal void SetFistDamage(Humanoid serpent)
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
            form.Step(this, player, dt, player.InWater() || player.IsSwimming(), callNow);
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
                form.PrepareStandIn(netCrow);   // no AI, no own movement, no colliders, not usable
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
                if (netView.IsOwner()) form.SyncStandIn(this, netView, netAnim);
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
            if (model != null) form.Pose(this, player, model, Time.deltaTime);
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

        // hide=1 with share: the human body was sent away for other players (op=hidebody); brought back when the transform ends
        public bool HidBody;

        void OnDestroy()
        {
            if (HidBody) { try { HomeApi.Exec("op=hidebody&on=0"); } catch (Exception) { } }
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
                player.m_swimStaminaDrainMinSkill = oSwimDrainMin; player.m_swimStaminaDrainMaxSkill = oSwimDrainMax;
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
}
