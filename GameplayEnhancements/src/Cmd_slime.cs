// slime: even ring of slime (blob/ooze) area clouds around a player, with a free gap in the middle.
// Own game / own world only. C# 5 (csc 4.0). Build: bash build-cmd.sh slime
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace HomeCheat
{
    public class SlimeEffect : HarmEffect
    {
        public override string Name { get { return "slime"; } }
        public override string Help
        {
            get
            {
                return "slime [target=<nick, other player only if this game is the host>] [gap=2] [rings=2 (circle layers)] [delay=0] [cloud=<prefab>] [cloudr=<radius m>] [lifetime=<s>] [lift=1 (m above the ground, so the cloud is visible)] [damage=3 (x Blob attack damage 90 = Poison 270, kills Fulings; 1 = Blob original, 0 = none)] [fx=1 (cast effects, set 1..5 of the spell-fx list, 0 = off)] [stonefx=0 (1..5: demo of that set's stone-break effects on ring 2 after 3 s)] [hurtself=1 (default: the clouds hit every player and NPC outside the gap, the target too if it leaves the gap; 0 = the target is immune, so moving is safe)] | test aids, not part of the effect: keephealed=<nick>&seconds=N (keeps a player at full health), heal=1 (full health for the local player) | near=<r> (list characters and hp) | weapon=<character prefab> | dump=<prefab> (print Aoe/collider facts, spawns nothing) | find=<text> (prefabs that carry an Aoe)";
            }
        }

        // candidates for the slime cloud, tried in order when no cloud= is given (first that exists in this game)
        static readonly string[] Candidates = new string[] { "blob_aoe", "Blob_aoe", "oozer_aoe", "BlobElite_attack_aoe", "blob_attack_aoe", "bloblava_aoe" };

        const BindingFlags AllF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public override string Run(Dictionary<string, string> a)
        {
            string find = HomeApi.S(a, "find", "");
            if (find.Length > 0) return Find(find);
            string weapon = HomeApi.S(a, "weapon", "");
            if (weapon.Length > 0) return WeaponDump(weapon);
            if (HomeApi.Flag(a, "healstats", false)) return "heal ticks that found a player below full health: " + HealCount + ", lowest hp seen " + HealMin.ToString("F1");
            string keep = HomeApi.S(a, "keephealed", "");
            if (keep.Length > 0) return KeepHealed(keep, HomeApi.F(a, "seconds", 30f));
            if (HomeApi.Flag(a, "probe", false)) return Probe();
            if (HomeApi.Flag(a, "onself", false))
            {
                // test aid: ONE cloud on the local player (use a small damage=, e.g. 0.05) to see whether the player is hit
                string dn;
                object d1 = BlobDamage(HomeApi.F(a, "damage", 0.05f), out dn);
                Player me = HomeApi.P();
                CloudOwner = HomeApi.Flag(a, "owner", false) ? me : null;
                SpawnOne("blob_aoe", me.transform.position + Vector3.up * 1f, -1f, d1);
                return "one blob_aoe on the player, " + dn + "; hp now " + me.GetHealth().ToString("F1") + "/" + me.GetMaxHealth().ToString("F0");
            }
            if (HomeApi.Flag(a, "heal", false))
            {
                Player me = HomeApi.P();
                me.Heal(me.GetMaxHealth(), true);
                return "healed: player hp " + me.GetHealth().ToString("F1") + "/" + me.GetMaxHealth().ToString("F0");
            }
            float near = HomeApi.F(a, "near", 0f);
            if (near > 0f) return Near(near);
            string dump = HomeApi.S(a, "dump", "");
            if (dump.Length > 0) return Dump(dump);

            string err;
            Player target = Target(a, out err);
            if (target == null) return err;
            if (target != HomeApi.P() && !ZNet.instance.IsServer()) return "ERROR: effects on other players need this game to be the host";

            float gap = HomeApi.F(a, "gap", 2f);
            if (gap < 2f) return "ERROR: gap must be at least 2 m (got " + gap.ToString("F1") + ")";
            int rings = Mathf.Max(1, HomeApi.I(a, "rings", 2));
            float delay = Mathf.Max(0f, HomeApi.F(a, "delay", 0f));
            float life = HomeApi.F(a, "lifetime", -1f);
            float lift = HomeApi.F(a, "lift", 1f);
            CloudOwner = HomeApi.Flag(a, "hurtself", true) ? null : target;
            // Blob's own attack damage (Poison from its blob_attack_aoe item) x 'damage'; the poison damage type brings the game's
            // own Poison status effect. damage=0 leaves the prefab as it is (blob_aoe alone does no damage).
            float dmgMul = HomeApi.F(a, "damage", 3f);
            object dmg = null;
            string dmgNote = "prefab damage";
            if (dmgMul > 0f && HomeApi.S(a, "cloud", "").Length == 0)
            {
                dmg = BlobDamage(dmgMul, out dmgNote);
                if (dmg == null) return "ERROR: could not read Blob attack damage (" + dmgNote + "); use damage=0 for a visual cloud";
            }

            string prefab = HomeApi.S(a, "cloud", "");
            if (prefab.Length == 0)
            {
                foreach (string c in Candidates) if (HomeApi.Prefab(c) != null) { prefab = c; break; }
                if (prefab.Length == 0) return "ERROR: no slime cloud prefab found among known names; use op=slime&find=blob or &find=ooze, then cloud=<name>";
            }
            GameObject pf = HomeApi.Prefab(prefab);
            if (pf == null) return "ERROR: no prefab '" + prefab + "' (op=slime&find=...)";

            float radius = HomeApi.F(a, "cloudr", -1f);
            string src = "cloudr";
            if (radius <= 0f) { radius = CloudRadius(pf, out src); }
            if (radius <= 0f) return "ERROR: could not read the cloud radius of '" + prefab + "' (" + src + "); pass cloudr=<metres> (op=slime&dump=" + prefab + ")";
            float diameter = 2f * radius;

            Vector3 center = target.transform.position;
            // cast effects (fx=1..5 = the five combinations of the spell-fx list, fx=0 off). All prefabs besides the checked ones are
            // inferred from the description files, see references/ValheimCustomMods/FX/.
            int fxSet = HomeApi.I(a, "fx", 1);
            bool castFx = fxSet >= 1 && fxSet <= 5;
            string ringFx = null;
            float ringFxT = 0.2f;
            if (castFx)
            {
                switch (fxSet)
                {
                    case 1: FxAt("fx_Bonemass_aoe_start", center, 0f); FxAt("sfx_blob_attack", center, 0f); ringFx = "vfx_blob_attack"; break;
                    case 2: FxAt("fx_fader_meteor_hit", center, 0f); FxAt("fx_Abomination_attack2", center, 0f); FxAt("fx_Fader_Roar", center, 0.3f); break;
                    case 3: FxAt("fx_Fader_Roar", center, 0f); ringFx = "vfx_blob_hit"; break;
                    case 4: FxAt("fx_fenring_icenova", center, 0f); ringFx = "vfx_blob_frost_attack"; break;
                    case 5: FxAt("fx_Bonemass_aoe_start", center, 0f); FxAt("sfx_blob_attack", center, 0f); ringFx = "vfx_blob_attack"; ringFxT = 0.25f;
                            FxAt("fx_Fader_Roar", center, 0.5f); break;
                }
            }
            StringBuilder sb = new StringBuilder();
            int total = 0;
            float spawnT = 0f;
            for (int k = 0; k < rings; k++)
            {
                float R = gap + radius + k * diameter;
                // safety: the nearest edge of a cloud must stay outside the gap
                if (R - radius < gap - 0.001f) return "ERROR: ring " + k + " would put a cloud edge inside the gap";
                List<Vector3> pts = Ring(center, R, diameter, k % 2 == 0 ? 0f : 180f / Mathf.Max(1, Mathf.FloorToInt(2f * Mathf.PI * R / diameter)));
                foreach (Vector3 p in pts)
                {
                    Vector3 pos = p;
                    float t = spawnT;
                    if (delay > 0f) spawnT += delay;
                    if (ringFx != null && k == 0) FxAt(ringFx, pos + Vector3.up * lift, ringFxT);
                    if (t <= 0f) SpawnOne(prefab, pos + Vector3.up * lift, life, dmg);
                    else HomeApi.Later(t, delegate { SpawnOne(prefab, pos + Vector3.up * lift, life, dmg); });
                }
                total += pts.Count;
                sb.Append(pts.Count).Append(" clouds on ring R=").Append(R.ToString("F1")).Append(" m; ");
            }
            // demo of the stone-break effects of the same set (stonefx=<set>): 4 points on ring 2, 3 s after the cast
            int stoneSet = HomeApi.I(a, "stonefx", 0);
            if (stoneSet >= 1 && stoneSet <= 5)
            {
                List<Vector3> demo = Ring(center, gap + radius + diameter, diameter, 90f);
                for (int i = 0; i < demo.Count; i += Mathf.Max(1, demo.Count / 4))
                    StoneFx(stoneSet, demo[i], 3f);
            }
            return "slime: " + total + " clouds (" + sb.ToString().TrimEnd(' ', ';') + "), prefab " + prefab + ", cloud r=" + radius.ToString("F2")
                + " (from " + src + "), damage " + dmgNote + ", gap " + gap.ToString("F1") + ", centre " + HomeApi.V(center);
        }

        // effects of a broken stone, per set (to be fired by the earthquake command when a Rock_7 breaks)
        static void StoneFx(int set, Vector3 pos, float wait)
        {
            switch (set)
            {
                case 1: FxAt("vfx_BombBlob_explode_poison", pos, wait); FxAt("sfx_oozebomb_explode", pos, wait); break;
                case 2: break; // set 2 has no stone-break effect (fx_Abomination_attack2 moved to the cast, at the caster)
                case 3: FxAt("vfx_GuckSackSmall_Destroyed", pos, wait); FxAt("sfx_GuckSackDestroyed", pos, wait); break;
                case 4: FxAt("vfx_ice_destroyed", pos, wait); FxAt("sfx_ice_destroyed", pos, wait); break;
                case 5: FxAt("vfx_GuckSackDestroyed", pos, wait); FxAt("vfx_BombBlob_explode_poison", pos, wait); FxAt("sfx_gdking_rock_destroyed", pos, wait); break;
            }
        }

        // spawn an effect prefab after 'wait' seconds and remove it after 8 s (effects of unknown length, loops)
        static void FxAt(string prefab, Vector3 pos, float wait)
        {
            HomeApi.Later(wait, delegate
            {
                GameObject go = HomeApi.Spawn(prefab, pos, Quaternion.identity);
                if (go == null) { HomeApi.Log("slime fx: no prefab " + prefab); return; }
                HomeApi.Later(8f, delegate { if (go != null) UnityEngine.Object.Destroy(go); });
            });
        }

        static void SpawnOne(string prefab, Vector3 pos, float life, object dmg)
        {
            GameObject go = HomeApi.Spawn(prefab, pos, Quaternion.identity);
            if (go == null) return;
            Last.Add(go);
            if (life > 0f) SetField(go, "Aoe", "m_ttl", life);
            if (CloudOwner != null)
            {
                Component ao = AoeOf(go);
                FieldInfo fo = ao == null ? null : ao.GetType().GetField("m_owner", AllF);
                if (fo != null) fo.SetValue(ao, CloudOwner);
            }
            if (dmg != null)
            {
                Component aoe = AoeOf(go);
                if (aoe == null) return;
                FieldInfo fu = aoe.GetType().GetField("m_useAttackSettings", AllF);
                if (fu != null) fu.SetValue(aoe, false);
                FieldInfo fd = aoe.GetType().GetField("m_damage", AllF);
                if (fd != null) fd.SetValue(aoe, dmg);
            }
        }

        // DamageTypes (boxed) of the Blob attack item, scaled by mul
        static object BlobDamage(float mul, out string note)
        {
            note = "Blob default item";
            GameObject item = null;
            if (ObjectDB.instance != null) item = ObjectDB.instance.GetItemPrefab("blob_attack_aoe");
            if (item == null) item = HomeApi.Prefab("blob_attack_aoe");
            if (item == null) { note = "no item prefab blob_attack_aoe"; return null; }
            ItemDrop id = item.GetComponent<ItemDrop>();
            if (id == null) { note = "blob_attack_aoe has no ItemDrop"; return null; }
            object d = id.m_itemData.m_shared.m_damages;
            if (Mathf.Abs(mul - 1f) > 0.001f)
            {
                MethodInfo m = d.GetType().GetMethod("Modify", new Type[] { typeof(float) });
                if (m == null) { note = "DamageTypes.Modify(float) missing"; return null; }
                object r = m.Invoke(d, new object[] { mul });
                if (r != null) d = r; // a void Modify mutates the boxed struct in place
            }
            note = "blob_attack_aoe " + d.ToString().Trim() + " x" + mul.ToString("F1");
            return d;
        }

        // ---- facts --------------------------------------------------------------------------

        // Aoe component of a prefab or a child (the "cloud" may hang below the root)
        static Component AoeOf(GameObject go)
        {
            foreach (Component c in go.GetComponentsInChildren<Component>(true))
                if (c != null && c.GetType().Name == "Aoe") return c;
            return null;
        }

        static object GetField(Component c, string name)
        {
            FieldInfo f = c.GetType().GetField(name, AllF);
            return f == null ? null : f.GetValue(c);
        }

        static void SetField(GameObject go, string typeName, string field, float v)
        {
            Component c = AoeOf(go);
            if (c == null) return;
            FieldInfo f = c.GetType().GetField(field, AllF);
            if (f != null && f.FieldType == typeof(float)) f.SetValue(c, v);
        }

        // effective cloud radius in metres: Aoe.m_radius times the scale of the object that carries it
        // (not verified in game until dump has been compared with a spawned cloud; see plan-slime.md)
        static float CloudRadius(GameObject pf, out string src)
        {
            Component aoe = AoeOf(pf);
            if (aoe != null)
            {
                object r = GetField(aoe, "m_radius");
                if (r is float && (float)r > 0f)
                {
                    Vector3 s = aoe.transform.lossyScale;
                    float sc = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
                    src = "Aoe.m_radius " + ((float)r).ToString("F2") + " x scale " + sc.ToString("F2");
                    return (float)r * sc;
                }
            }
            SphereCollider sph = pf.GetComponentInChildren<SphereCollider>(true);
            if (sph != null)
            {
                Vector3 s2 = sph.transform.lossyScale;
                src = "SphereCollider.radius " + sph.radius.ToString("F2");
                return sph.radius * Mathf.Max(Mathf.Abs(s2.x), Mathf.Abs(s2.z));
            }
            src = "no Aoe or SphereCollider";
            return -1f;
        }

        // test aid, not part of the effect: top a player up to full health every 0.2 s for N seconds (Character.Heal reaches
        // remote players through the Heal rpc of the game), so a poison test cannot kill them
        static string KeepHealed(string nick, float seconds)
        {
            Player p = HomeApi.FindPlayer(nick);
            if (p == null) return "ERROR: no player named '" + nick + "' is online";
            float end = Time.time + seconds;
            HealCount = 0; HealMin = 999f;
            HealTick(nick, end);
            return "keeping " + nick + " healed for " + seconds.ToString("F0") + " s; hp now " + p.GetHealth().ToString("F1") + "/" + p.GetMaxHealth().ToString("F0");
        }

        static int HealCount;
        static float HealMin = 999f;

        static void HealTick(string nick, float end)
        {
            Player p = HomeApi.FindPlayer(nick);
            if (p == null || Time.time > end) return;
            if (p.GetHealth() < p.GetMaxHealth())
            {
                HealCount++;
                if (p.GetHealth() < HealMin) HealMin = p.GetHealth();
                p.Heal(p.GetMaxHealth(), false);
            }
            HomeApi.Later(0.2f, delegate { HealTick(nick, end); });
        }

        static readonly List<GameObject> Last = new List<GameObject>();
        static Character CloudOwner; // owner of the clouds: Aoe does not hit its owner (m_hitOwner=False)

        // test aid: state of the particle systems of the clouds still alive (is anything actually emitting?)
        static string Probe()
        {
            StringBuilder sb = new StringBuilder();
            int alive = 0;
            foreach (GameObject go in Last)
            {
                if (go == null) continue;
                alive++;
                if (alive > 2) continue;
                sb.Append(go.name).Append(" at ").Append(HomeApi.V(go.transform.position)).Append(" active=").Append(go.activeInHierarchy).Append('\n');
                foreach (Component c in go.GetComponentsInChildren<Component>(true))
                {
                    if (c == null || c.GetType().Name != "ParticleSystem") continue;
                    Type t = c.GetType();
                    object playing = t.GetProperty("isPlaying").GetValue(c, null);
                    object count = t.GetProperty("particleCount").GetValue(c, null);
                    object main = t.GetProperty("main").GetValue(c, null);
                    object ssize = main.GetType().GetProperty("startSizeMultiplier").GetValue(main, null);
                    object sscale = main.GetType().GetProperty("scalingMode").GetValue(main, null);
                    sb.Append("  ").Append(c.name).Append(" playing=").Append(playing).Append(" particles=").Append(count)
                      .Append(" startSize=").Append(ssize).Append(" scaling=").Append(sscale).Append(" enabledRenderer=")
                      .Append(RendererOn(c.gameObject)).Append('\n');
                }
            }
            if (alive == 0) { Last.Clear(); return "no slime clouds alive (they live 3 s)"; }
            return alive + " clouds alive (first 2 shown)\n" + sb.ToString();
        }

        static string RendererOn(GameObject g)
        {
            Renderer r = g.GetComponent<Renderer>();
            return r == null ? "none" : (r.enabled + " layer=" + g.layer);
        }

        static string Flat(object v)
        {
            if (v == null) return "null";
            if (v is GameObject) return "GO:" + ((GameObject)v).name;
            if (v is UnityEngine.Object) return "obj:" + ((UnityEngine.Object)v).name;
            if (v is Array) return "array[" + ((Array)v).Length + "]";
            return v.ToString().Replace('\n', ' ');
        }

        // what a character attacks with: default items of its Humanoid, with damage, status effect and attack settings
        static string WeaponDump(string charPrefab)
        {
            GameObject pf = HomeApi.Prefab(charPrefab);
            if (pf == null) return "ERROR: no prefab '" + charPrefab + "'";
            Component hum = null;
            foreach (Component c in pf.GetComponents<Component>()) if (c != null && c.GetType().Name == "Humanoid") hum = c;
            if (hum == null) return "ERROR: '" + charPrefab + "' has no Humanoid";
            StringBuilder sb = new StringBuilder();
            foreach (string fname in new string[] { "m_defaultItems", "m_randomWeapon", "m_randomArmor", "m_randomShield" })
            {
                FieldInfo hf = hum.GetType().GetField(fname, AllF);
                GameObject[] items = hf == null ? null : hf.GetValue(hum) as GameObject[];
                if (items == null) continue;
                foreach (GameObject it in items)
                {
                    sb.Append(fname).Append(": ").Append(it == null ? "null" : it.name).Append('\n');
                    if (it == null) continue;
                    ItemDrop id = it.GetComponent<ItemDrop>();
                    if (id == null) continue;
                    object shared = id.m_itemData.m_shared;
                    foreach (FieldInfo f in shared.GetType().GetFields(AllF))
                    {
                        if (f.Name == "m_damages" || f.Name == "m_attackStatusEffect" || f.Name == "m_name" || f.Name.StartsWith("m_attack") || f.Name == "m_toolTier" || f.Name == "m_aiAttackRange")
                            sb.Append("  ").Append(f.Name).Append('=').Append(Flat(f.GetValue(shared))).Append('\n');
                        if (f.Name == "m_attack")
                        {
                            object at = f.GetValue(shared);
                            foreach (FieldInfo af in at.GetType().GetFields(AllF))
                            {
                                object v = af.GetValue(at);
                                if (v == null) continue;
                                if (af.FieldType.IsPrimitive || af.FieldType.IsEnum || af.FieldType == typeof(string) || v is GameObject)
                                    sb.Append("    attack.").Append(af.Name).Append('=').Append(Flat(v)).Append('\n');
                            }
                        }
                    }
                }
            }
            return sb.Length == 0 ? "no default items found" : sb.ToString();
        }

        // test aid: every character within r m of the local player with distance, health, and the player's own health
        static string Near(float r)
        {
            Player me = HomeApi.P();
            StringBuilder sb = new StringBuilder();
            sb.Append("player hp ").Append(me.GetHealth().ToString("F1")).Append('/').Append(me.GetMaxHealth().ToString("F0")).Append('\n');
            foreach (Character c in Character.GetAllCharacters())
            {
                if (c == null || c == me) continue;
                float d = Vector3.Distance(c.transform.position, me.transform.position);
                if (d > r) continue;
                sb.Append(c.name.Replace("(Clone)", "")).Append(" dist ").Append(d.ToString("F1")).Append(" hp ")
                  .Append(c.GetHealth().ToString("F1")).Append('/').Append(c.GetMaxHealth().ToString("F0")).Append('\n');
            }
            return sb.ToString();
        }

        static string Find(string text)
        {
            StringBuilder sb = new StringBuilder();
            FieldInfo f = typeof(ZNetScene).GetField("m_namedPrefabs", AllF);
            Dictionary<int, GameObject> d = f.GetValue(ZNetScene.instance) as Dictionary<int, GameObject>;
            List<string> names = new List<string>();
            foreach (KeyValuePair<int, GameObject> kv in d)
                if (kv.Value != null && kv.Value.name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                    names.Add(kv.Value.name + (AoeOf(kv.Value) != null ? "  [Aoe]" : ""));
            names.Sort();
            int n = 0;
            foreach (string s in names) { if (n++ >= 150) { sb.Append("... (more)"); break; } sb.Append(s).Append('\n'); }
            return names.Count == 0 ? "no prefab matches '" + text + "'" : sb.ToString();
        }

        static string Dump(string prefab)
        {
            GameObject pf = HomeApi.Prefab(prefab);
            if (pf == null) return "ERROR: no prefab '" + prefab + "'";
            StringBuilder sb = new StringBuilder();
            sb.Append("prefab ").Append(prefab).Append(" root scale ").Append(HomeApi.V(pf.transform.lossyScale)).Append('\n');
            foreach (Component c in pf.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                string tn = c.GetType().Name;
                if (tn == "Transform") continue;
                sb.Append("  ").Append(c.transform == pf.transform ? "root" : c.transform.name).Append(": ").Append(tn);
                SphereCollider sc = c as SphereCollider;
                if (sc != null) sb.Append(" radius=").Append(sc.radius.ToString("F2")).Append(" trigger=").Append(sc.isTrigger).Append(" lossyScale=").Append(HomeApi.V(sc.transform.lossyScale));
                CapsuleCollider cc = c as CapsuleCollider;
                if (cc != null) sb.Append(" radius=").Append(cc.radius.ToString("F2")).Append(" height=").Append(cc.height.ToString("F2"));
                if (tn == "Aoe" || tn == "TimedDestruction")
                {
                    foreach (FieldInfo f in c.GetType().GetFields(AllF))
                    {
                        object v = f.GetValue(c);
                        if (v == null) continue;
                        if (f.FieldType.IsPrimitive || f.FieldType == typeof(string) || f.FieldType.IsEnum) sb.Append(' ').Append(f.Name).Append('=').Append(v);
                        else if (f.Name == "m_damage") sb.Append(' ').Append(f.Name).Append('=').Append(v.ToString().Replace('\n', ' '));
                    }
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}
