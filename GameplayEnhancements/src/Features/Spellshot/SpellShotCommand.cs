// spellshot: a matrix of projectiles along the crosshair (own game / own world only).
// op=spellshot&type=arrow|balistashot|thunderbolt|harpoon&perrow=1&rows=1&dist=1&spacing=0.6&speed=&projectile=&damage=1
// op=spellshot&type=probe&filter=needle   -> discovery: prefabs with a Projectile, items whose attack fires one
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.Spellshot
{
    public sealed class SpellShotCommand : AttackCommand
    {
        public override string Name { get { return "spellshot"; } }
        protected override string Usage
        {
            get
            {
                return "spellshot type=arrow|balistashot|thunderbolt|harpoon [perrow=1] [rows=1] [dist=1] [spacing=0.6] [speed=] [projectile=<prefab>] [damage=1] [scale=1 (strike size)] [strikedamage=40 (lightning damage of a strike, 0 = visual only)] [at=aim|self] [push=1 (x strike push force; enemies are pushed, you are not hit at all)] " + PvpUsage + "; thunderbolt = Eikthyr-style lightning strikes on the ground at the aim point, spacing default 1.5; "
                    + "harpoon = the Abyssal Harpoon's projectile (SpearChitin) carrying its vanilla pull status effect (Harpooned): a hit enemy is reeled in like with the harpoon, costs your stamina by the target's weight, block or attack releases it, bosses break free; [se=<status effect name> (default: the harpoon item's own)] [speed= (default: the harpoon's throw speed)]; "
                    + "perrow/rows are 1..9 (even counts centre the grid between items); formation is centred on the crosshair, all shots fly along it; "
                    + "local player only (owner = you). type=probe&filter=<text> lists projectile prefabs.";
            }
        }

        const int MaxCount = 81;
        float lastShot = -100f;   // volley cooldown
        bool pvpHit;              // pvp=1 and the caster's PvP on (set per volley)
        string pvpNote = "";      // the pvp part of the first answer line

        // type -> candidate prefab names (a Projectile prefab, or an ammo item whose attack fires one), first existing wins
        static string[] Candidates(string type)
        {
            // verified in game 2026-10-24 with type=probe: ArrowNeedle -> bow_projectile_needle; TurretBoltFlametal (the siege ballista's
            // Flametal bolt) -> Turret_projectile; Eikthyr's own attacks are melee/AoE, the Projectile that carries his lightning is
            // FrozenKing_P2_Projectile_Eikthyr, but that one spawns a stag on hit (wrong): thunderbolt now = the Lightning Staff shot
            if (type == "arrow") return new string[] { "ArrowNeedle", "bow_projectile_needle" };
            if (type == "balistashot") return new string[] { "TurretBoltFlametal" };
            // thunderbolt = lightningAOE: the Lightning-damage strike (bolt from the sky, rod + ground ring) at the aim point. The staff shot (projectile=StaffLightning) stays available.
            if (type == "thunderbolt") return new string[] { "fx_eikthyr_forwardshockwave" };
            // harpoon = the Abyssal Harpoon item; its attack fires the projectile, its m_attackStatusEffect (SE_Harpooned) does the pull
            if (type == "harpoon") return new string[] { "SpearChitin" };
            return new string[0];
        }

        static float DefaultSpeed(string type)
        {
            if (type == "arrow") return 60f;
            if (type == "balistashot") return 90f;
            return 120f;
        }

        protected override string Execute(Args a)
        {
            string type = a.Str("type", "").ToLowerInvariant();
            if (type == "probe") return Probe(a.Str("filter", ""));
            if (type == "clear") return Clear(a.Str("fx", "fx_Lightning"));
            if (type == "pos") { Player pp = Players.Local; return pp == null ? "ERROR: no player" : "pos " + pp.transform.position.x.ToString("F2") + " " + pp.transform.position.y.ToString("F2") + " " + pp.transform.position.z.ToString("F2") + " vel " + pp.GetVelocity().magnitude.ToString("F2"); }
            if (type != "arrow" && type != "balistashot" && type != "thunderbolt" && type != "harpoon")
                return "ERROR: type must be arrow, balistashot, thunderbolt or harpoon (or probe or clear)";
            if (type == "harpoon") Host.RequireWorld();

            int perrow = a.Int("perrow", 1);
            int rows = a.Int("rows", 1);
            if (!Odd(perrow) || !Odd(rows)) return "ERROR: perrow and rows must be 1 to 9";
            float dist = Mathf.Max(0.5f, a.Float("dist", 1f));
            float spacing = Mathf.Max(0.1f, a.Float("spacing", 0.6f));
            bool spacingGiven = a.Has("spacing");
            float scale = a.Float("scale", 1f);
            float strikeDamage = a.Float("strikedamage", type == "thunderbolt" ? 40f : 0f);
            float dmgMul = a.Float("damage", 1f);
            float speed = a.Float("speed", DefaultSpeed(type));

            Player p = Players.Local;
            Camera cam = Camera.main;
            if (p == null || cam == null) return "ERROR: no local player or camera (load a world first)";
            // pvp=1: the PvP rule of AttackCommand (own PvP on = shots and strikes hit other players, whatever their own setting)
            pvpHit = HitsPlayers(a);
            pvpNote = PvpNote(a);
            if (Time.time - lastShot < 0.4f) return "ERROR: cooldown, wait a moment between volleys";

            // resolve projectile prefab and (optionally) the ammo item
            GameObject projPrefab = null;
            GameObject ammoPrefab = null;
            string given = a.Str("projectile", "");
            List<string> names = new List<string>();
            if (given.Length > 0) names.Add(given); else names.AddRange(Candidates(type));
            string used = null;
            foreach (string n in names)
            {
                GameObject g = Prefabs.Get(n);
                if (g == null) continue;
                used = n;
                if (g.GetComponent<Projectile>() != null) projPrefab = g;
                else
                {
                    ItemDrop id = g.GetComponent<ItemDrop>();
                    if (id != null && id.m_itemData != null && id.m_itemData.m_shared != null
                        && id.m_itemData.m_shared.m_attack != null && id.m_itemData.m_shared.m_attack.m_attackProjectile != null)
                    {
                        ammoPrefab = g;
                        projPrefab = id.m_itemData.m_shared.m_attack.m_attackProjectile;
                    }
                    else projPrefab = g; // no Projectile: spawned as a strike at the aim point
                }
                break;
            }
            if (projPrefab == null)
                return "ERROR: no prefab found for '" + type + "' (tried " + string.Join(", ", names.ToArray()) + "); use type=probe&filter=... and projectile=<name>";

            Vector3 origin = p.GetEyePoint(); // from the head, not the camera: in third person the camera sits behind the player
            Vector3 D = cam.transform.forward.normalized;
            Vector3 Rt = Vector3.Cross(Vector3.up, D);
            if (Rt.sqrMagnitude < 0.0001f) Rt = cam.transform.right;
            Rt.Normalize();
            Vector3 Up = Vector3.Cross(D, Rt).normalized;
            Vector3 C = origin + D * dist;
            bool isProj = projPrefab.GetComponent<Projectile>() != null;
            if (!isProj && !spacingGiven) spacing = 1.5f; // strikes have a 2.4 m radius, so a wider default grid than arrows

            MethodInfo setup = FindSetup();
            if (isProj && setup == null) return "ERROR: Projectile.Setup not found in this game version";

            // harpoon: the pull is the item's attack status effect, which the game's Attack puts into HitData.m_statusEffectHash;
            // Projectile.Setup copies it and Character.RPC_Damage adds it and calls SetAttacker(owner) (IL read 2026-10-25)
            int seHash = 0;
            string seName = null;
            if (type == "harpoon")
            {
                if (!isProj) return "ERROR: '" + projPrefab.name + "' fires no projectile; harpoon needs the harpoon item or its projectile (type=probe&filter=chitin)";
                StatusEffect se = HarpoonEffect(a.Str("se", ""), ammoPrefab);
                if (se == null) return "ERROR: no pull status effect found" + (a.Has("se") ? " named '" + a.Str("se", "") + "'" : " on " + (ammoPrefab != null ? ammoPrefab.name : projPrefab.name)) + "; list status effects with op=bless&list=1 and pass se=<name>";
                seHash = se.NameHash();
                seName = se.name;
                if (!a.Has("speed") && ammoPrefab != null)
                {
                    Attack at = ammoPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_attack;
                    if (at.m_projectileVel > 0f) speed = at.m_projectileVel;
                }
            }

            lastShot = Time.time;
            if (type == "thunderbolt" && given.Length == 0 && a.Flag("beam", true))
                return Beam(a, p, cam, perrow, rows, a.Has("dist") ? dist : 6f, spacingGiven ? spacing : 1.5f, scale);
            int ok = 0;
            string firstErr = null;
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < perrow; j++)
                {
                    float dx = (j - (perrow - 1) / 2f) * spacing;
                    float dy = (i - (rows - 1) / 2f) * spacing;
                    try
                    {
                        if (isProj)
                        {
                            Vector3 pos = C + Rt * dx + Up * dy;
                            Fire(p, setup, projPrefab, ammoPrefab, pos, D, speed, dmgMul, seHash, pvpHit);
                        }
                        else
                        {
                            // no Projectile: strike at the aim point, offsets on the ground plane
                            bool aimed;
                            Vector3 ap = AimWithCharacters(p, cam, out aimed);
                            if (a.Str("at", "aim") == "self") ap = p.transform.position + new Vector3(D.x, 0f, D.z).normalized * dist;
                            Vector3 fwd = new Vector3(D.x, 0f, D.z).normalized;
                            Vector3 pos = ap + new Vector3(Rt.x, 0f, Rt.z).normalized * dx + fwd * dy;
                            pos.y = Ground.Height(pos);
                            GameObject st = (GameObject)UnityEngine.Object.Instantiate(projPrefab, pos, Quaternion.LookRotation(fwd));
                            if (scale != 1f) st.transform.localScale = st.transform.localScale * scale;
                            if (strikeDamage > 0f)
                            {
                                // the Eikthyr effects are visual only; the damage comes from lightningAOE's Aoe, with its looks switched off
                                GameObject carrier = st.GetComponentInChildren<Aoe>() != null ? st : Prefabs.Spawn("lightningAOE", pos, Quaternion.LookRotation(fwd));
                                if (carrier != null)
                                {
                                    if (carrier != st)
                                    {
                                        if (scale != 1f) carrier.transform.localScale = carrier.transform.localScale * scale;
                                        foreach (Renderer r in carrier.GetComponentsInChildren<Renderer>()) r.enabled = false;
                                        foreach (Light l in carrier.GetComponentsInChildren<Light>()) l.enabled = false;
                                        foreach (ZSFX au in carrier.GetComponentsInChildren<ZSFX>()) au.enabled = false;
                                        foreach (CamShaker cs in carrier.GetComponentsInChildren<CamShaker>()) cs.enabled = false;
                                    }
                                    ArmAoe(p, carrier, strikeDamage, a.Float("push", 1f), pvpHit);
                                }
                            }
                        }
                        ok++;
                    }
                    catch (Exception e)
                    {
                        if (firstErr == null) firstErr = e.GetType().Name + ": " + (e.InnerException != null ? e.InnerException.Message : e.Message);
                    }
                }
            return "spellshot " + type + " " + perrow + "x" + rows + " = " + ok + " projectiles (" + (isProj ? "prefab " + projPrefab.name : "strike prefab " + projPrefab.name)
                + (used != null && used != projPrefab.name ? " via " + used : "") + (seName != null ? ", status effect " + seName : "") + ") from " + Fmt.Vec(C) + " along " + Fmt.Vec(D)
                + pvpNote + (firstErr != null ? " ; first error: " + firstErr : "");
        }

        // gives the biggest Aoe of 'go' lightning damage and hands it the player as owner (so it never hits the player)
        static void ArmAoe(Player p, GameObject go, float dmg, float push, bool hitPlayers)
        {
            Aoe big = null;
            foreach (Aoe ao in go.GetComponentsInChildren<Aoe>())
                if (big == null || ao.m_radius > big.m_radius) big = ao;
            foreach (Aoe ao in go.GetComponentsInChildren<Aoe>())
            {
                ao.m_damage.m_lightning = ao == big ? dmg : 0f;
                PvpHits.ArmAoe(ao, hitPlayers);   // never the owner; other players only with pvp=1 and your PvP on
                ao.m_attackForce *= push;
                ao.m_knockBackForce *= push;
            }
            if (big == null) return;
            BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (MethodInfo m in typeof(Aoe).GetMethods(all))
            {
                if (m.Name != "Setup" && !m.Name.EndsWith(".Setup")) continue;
                // every Aoe gets the player as owner: lightningAOE also has AOE_ROD (a 100 m column, push 100) next to AOE_AREA,
                // and an ownerless rod pushed the caster (in-game test 2026-10-25)
                foreach (Aoe ao in go.GetComponentsInChildren<Aoe>())
                {
                    HitData hit = new HitData();
                    hit.m_damage.m_lightning = ao == big ? dmg : 0f;
                    hit.m_pushForce = 100f * push;
                    PvpHits.ArmHit(hit, hitPlayers);
                    ParameterInfo[] ps = m.GetParameters();
                    object[] args = new object[ps.Length];
                    for (int k = 0; k < ps.Length; k++)
                    {
                        Type t = ps[k].ParameterType;
                        if (typeof(Character).IsAssignableFrom(t)) args[k] = p;
                        else if (t == typeof(Vector3)) args[k] = Vector3.zero;
                        else if (t == typeof(float)) args[k] = 0f;
                        else if (t == typeof(HitData)) args[k] = hit;
                        else args[k] = null;
                    }
                    m.Invoke(ao, args);
                }
                break;
            }
        }

        // crosshair point like Aim.Point, but creatures count as hit surfaces (AimPoint's ray passes through them, so a
        // strike aimed at a troll landed on the ground behind it); the player's own colliders are skipped
        static Vector3 AimWithCharacters(Player p, Camera cam, out bool aimed)
        {
            int mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle", "character", "character_net", "character_noenv");
            RaycastHit[] hits = Physics.RaycastAll(cam.transform.position, cam.transform.forward, 100f, mask);
            Array.Sort(hits, delegate(RaycastHit x, RaycastHit y) { return x.distance.CompareTo(y.distance); });
            foreach (RaycastHit h in hits)
            {
                if (h.collider == null || h.collider.transform.root == p.transform.root) continue;
                aimed = true;
                return h.point;
            }
            return Aim.Point(p, out aimed);
        }

        // thunderbolt as a beam: from the head towards the crosshair point and on, until a wall/terrain/piece or 'range'.
        // Every 'step' metres a lightning visual (fx) plus an invisible lightningAOE damage carrier appear, one after another
        // (0.03 s apart) so the bolt sweeps forward. Creatures on the line are not blockers: everything on it is hit.
        string Beam(Args a, Player p, Camera cam, int perrow, int rows, float dist, float spacing, float scale)
        {
            string fx = a.Str("fx", "fx_Lightning");
            if (Prefabs.Get("lightningAOE") == null) return "ERROR: prefab lightningAOE not found";
            if (Prefabs.Get(fx) == null) return "ERROR: visual prefab '" + fx + "' not found (fx=<prefab>)";
            float range = Mathf.Max(2f, a.Float("range", 40f));
            float step = Mathf.Max(0.5f, a.Float("step", 2f));
            float dmg = a.Float("strikedamage", 20f) * a.Float("damage", 1f);
            float fxScale = a.Float("fxscale", 1f);
            float push = a.Float("push", 1f); // x the strike push force (100); 0 = nothing is pushed
            bool fxAlong = a.Flag("along", true); // lay the visual (a vertical bolt) along the beam instead of standing it up
            float life = a.Float("life", 1.2f); // seconds the visual stays (fx_Lightning has no timeout of its own); 0 = stays

            Vector3 origin = p.GetEyePoint();
            bool aimed;
            Vector3 ap = AimWithCharacters(p, cam, out aimed);
            Vector3 D = aimed ? (ap - origin).normalized : cam.transform.forward.normalized;
            Vector3 Rt = Vector3.Cross(Vector3.up, D);
            if (Rt.sqrMagnitude < 0.0001f) Rt = cam.transform.right;
            Rt.Normalize();
            Vector3 Up = Vector3.Cross(D, Rt).normalized;
            int solid = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain");
            int beams = perrow * rows;
            int maxPer = Mathf.Max(2, 300 / beams); // total cap 300 points per volley

            int points = 0;
            float totalLen = 0f;
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < perrow; j++)
                {
                    Vector3 start = origin + D * dist + Rt * ((j - (perrow - 1) / 2f) * spacing) + Up * ((i - (rows - 1) / 2f) * spacing);
                    RaycastHit hit;
                    float len = Physics.Raycast(start, D, out hit, range, solid) ? hit.distance : range;
                    totalLen += len;
                    int n = Mathf.Min(maxPer, Mathf.Max(1, Mathf.CeilToInt(len / step)));
                    for (int k = 0; k < n; k++)
                    {
                        Vector3 pos = start + D * (len * (k + 1) / n);
                        float delay = k * 0.03f;
                        string fxName = fx;
                        Quaternion rot = Quaternion.LookRotation(D);
                        Quaternion visRot = fxAlong ? Quaternion.FromToRotation(Vector3.up, D) : rot;
                        Vector3 dirB = D;
                        float segLen = len / n;
                        Player owner = p;
                        float dd = dmg, sc = scale, fs = fxScale, lifeS = life, pushF = push;
                        bool pvpB = pvpHit;
                        Later(delay, delegate
                        {
                            try
                            {
                                GameObject v = Prefabs.Spawn(fxName, pos - dirB * (segLen * 0.5f), visRot);
                                if (v != null && fs != 1f) v.transform.localScale = v.transform.localScale * fs;
                                if (v != null && lifeS > 0f) { GameObject vv = v; Later(lifeS, delegate { Fx.Destroy(vv); }); }
                                GameObject c = Prefabs.Spawn("lightningAOE", pos, rot);
                                if (c == null) return;
                                if (sc != 1f) c.transform.localScale = c.transform.localScale * sc;
                                foreach (Renderer r in c.GetComponentsInChildren<Renderer>()) r.enabled = false;
                                foreach (Light l in c.GetComponentsInChildren<Light>()) l.enabled = false;
                                foreach (ZSFX z in c.GetComponentsInChildren<ZSFX>()) z.enabled = false;
                                foreach (CamShaker cs in c.GetComponentsInChildren<CamShaker>()) cs.enabled = false;
                                ArmAoe(owner, c, dd, pushF, pvpB);
                            }
                            catch (Exception e) { HomeApi.Log("spellshot beam point: " + e.Message); }
                        });
                        points++;
                    }
                }
            return "spellshot thunderbolt beam " + perrow + "x" + rows + " = " + beams + " beams, " + points + " strikes, avg length "
                + (totalLen / beams).ToString("F1") + " m, damage " + dmg + " per strike, from " + Fmt.Vec(origin) + " along " + Fmt.Vec(D)
                + (aimed ? " (crosshair " + Fmt.Vec(ap) + ")" : " (nothing aimed at)") + pvpNote;
        }

        static bool Odd(int n) { return n >= 1 && n <= 9; } // any count 1..9; even counts have no centre item, the grid is symmetric around the crosshair
        // removes leftover visuals of a prefab (default fx_Lightning) that were spawned without a timeout
        static string Clear(string fx)
        {
            int n = 0;
            foreach (ZNetView nv in UnityEngine.Object.FindObjectsOfType<ZNetView>())
                if (nv != null && nv.gameObject.name == fx + "(Clone)") { ZNetScene.instance.Destroy(nv.gameObject); n++; }
            return "cleared " + n + " x " + fx;
        }


        static MethodInfo FindSetup()
        {
            BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (MethodInfo m in typeof(Projectile).GetMethods(all))
                if (m.Name == "Setup" || m.Name.EndsWith(".Setup")) return m;
            return null;
        }

        // the harpoon's pull: se=<name> from ObjectDB, else the item's own attack status effect, else ObjectDB "Harpooned"
        static StatusEffect HarpoonEffect(string name, GameObject item)
        {
            if (name.Length == 0 && item != null)
            {
                ItemDrop id = item.GetComponent<ItemDrop>();
                if (id != null && id.m_itemData != null && id.m_itemData.m_shared != null && id.m_itemData.m_shared.m_attackStatusEffect != null)
                    return id.m_itemData.m_shared.m_attackStatusEffect;
            }
            if (ObjectDB.instance == null) return null;
            string want = name.Length > 0 ? name : "Harpooned";
            foreach (StatusEffect se in ObjectDB.instance.m_StatusEffects)
                if (se != null && string.Equals(se.name, want, StringComparison.OrdinalIgnoreCase)) return se;
            return null;
        }

        // builds the Setup argument list from the parameter types, so a changed signature still works
        static void Fire(Player p, MethodInfo setup, GameObject projPrefab, GameObject ammoPrefab, Vector3 pos, Vector3 dir, float speed, float dmgMul, int seHash, bool hitPlayers)
        {
            GameObject go = (GameObject)UnityEngine.Object.Instantiate(projPrefab, pos, Quaternion.LookRotation(dir));
            Projectile pr = go.GetComponent<Projectile>();

            HitData hit = new HitData();
            hit.m_damage = pr.m_damage;
            ItemDrop.ItemData ammo = null;
            if (ammoPrefab != null)
            {
                ItemDrop id = ammoPrefab.GetComponent<ItemDrop>();
                if (id != null && id.m_itemData != null)
                {
                    ammo = id.m_itemData;
                    if (ammo.m_shared != null)
                    {
                        hit.m_damage = ammo.m_shared.m_damages;
                        hit.m_pushForce = ammo.m_shared.m_attackForce;
                    }
                }
            }
            if (dmgMul != 1f) hit.m_damage.Modify(dmgMul);
            hit.m_skill = pr.m_skill;
            PvpHits.ArmHit(hit, hitPlayers);
            if (seHash != 0) hit.m_statusEffectHash = seHash; // harpoon: applied on hit, the owner becomes the attacker

            ParameterInfo[] ps = setup.GetParameters();
            object[] args = new object[ps.Length];
            bool seenItem = false;
            for (int k = 0; k < ps.Length; k++)
            {
                Type t = ps[k].ParameterType;
                if (typeof(Character).IsAssignableFrom(t)) args[k] = p;
                else if (t == typeof(Vector3)) args[k] = dir * speed;
                else if (t == typeof(float)) args[k] = pr.m_hitNoise;
                else if (t == typeof(HitData)) args[k] = hit;
                else if (t == typeof(ItemDrop.ItemData))
                {
                    // order in the game's signature: weapon first, then ammo
                    args[k] = seenItem ? ammo : null;
                    seenItem = true;
                }
                else args[k] = null;
            }
            setup.Invoke(pr, args);
            if (hitPlayers) PvpHits.MarkProjectile(pr);   // the game's Projectile drops m_ignorePVP on the way to the hit
        }

        // discovery helper: projectile prefabs and ammo items matching 'filter'
        static string Probe(string filter)
        {
            StringBuilder sb = new StringBuilder();
            List<string> lines = new List<string>();
            foreach (GameObject g in Prefabs.All())
            {
                if (filter.Length > 0 && g.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                Projectile pr = g.GetComponent<Projectile>();
                if (pr != null)
                {
                    lines.Add("PROJECTILE " + g.name + " ttl=" + pr.m_ttl + " grav=" + pr.m_gravity + " aoe=" + pr.m_aoe
                        + " dmg=" + pr.m_damage.GetTotalDamage() + " onHit=" + (pr.m_spawnOnHit != null ? pr.m_spawnOnHit.name : "-"));
                    continue;
                }
                Humanoid hu = g.GetComponent<Humanoid>();
                if (hu != null && filter.Length > 0)
                {
                    List<GameObject> its = new List<GameObject>();
                    if (hu.m_defaultItems != null) its.AddRange(hu.m_defaultItems);
                    if (hu.m_randomWeapon != null) its.AddRange(hu.m_randomWeapon);
                    foreach (GameObject it in its)
                    {
                        if (it == null) continue;
                        ItemDrop wid = it.GetComponent<ItemDrop>();
                        if (wid == null || wid.m_itemData == null || wid.m_itemData.m_shared == null) continue;
                        Attack wa = wid.m_itemData.m_shared.m_attack;
                        lines.Add("CREATURE " + g.name + " item " + it.name + " attackType=" + (wa != null ? wa.m_attackType.ToString() : "-")
                            + " proj=" + (wa != null && wa.m_attackProjectile != null ? wa.m_attackProjectile.name : "-")
                            + " spawnOnTrigger=" + (wa != null && wa.m_spawnOnTrigger != null ? wa.m_spawnOnTrigger.name : "-")
                            + " secondary=" + (wid.m_itemData.m_shared.m_secondaryAttack != null && wid.m_itemData.m_shared.m_secondaryAttack.m_attackProjectile != null
                                ? wid.m_itemData.m_shared.m_secondaryAttack.m_attackProjectile.name : "-"));
                    }
                }
                ItemDrop id = g.GetComponent<ItemDrop>();
                if (id != null && id.m_itemData != null && id.m_itemData.m_shared != null && id.m_itemData.m_shared.m_attack != null
                    && id.m_itemData.m_shared.m_attack.m_attackProjectile != null)
                {
                    Attack at = id.m_itemData.m_shared.m_attack;
                    StatusEffect ase = id.m_itemData.m_shared.m_attackStatusEffect;
                    lines.Add("ITEM " + g.name + " -> " + at.m_attackProjectile.name + " vel=" + at.m_projectileVel
                        + " dmg=" + id.m_itemData.m_shared.m_damages.GetTotalDamage()
                        + " se=" + (ase != null ? ase.name + " (" + id.m_itemData.m_shared.m_attackStatusEffectChance + ")" : "-"));
                }
            }
            lines.Sort();
            int n = 0;
            foreach (string s in lines) { if (n++ >= 120) { sb.Append("... (more)\n"); break; } sb.Append(s).Append('\n'); }
            if (lines.Count == 0) sb.Append("nothing with a Projectile matches '" + filter + "'");
            return sb.ToString();
        }
    }
}
