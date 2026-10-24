// earthquake: the stag hammer's ground slam repeated in expanding rings around a player, with a tilted stone circle.
// Own game / own world only. C# 5 (csc 4.0). Build: bash build-cmd.sh earthquake
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.Earthquake
{
    public class EarthquakeEffect : HarmEffect
    {
        public override string Name { get { return "earthquake"; } }
        const float SimpleForce = 2.1f;      // the Demolisher sledge push (210) against the slam prefab's 100
        const float StrongForce = 4.2f;      // simple x 2
        protected override string Usage
        {
            get
            {
                return "earthquake [target=<nick, other player only if this game is the host>] [stages=5] [hits=2] [delay=0.003 s between hits of one point] [stagegap=0.4 s between stages]"
                    + " [radius=<r m, default the slam radius>] [spacing=<m between points, default 1.4r so neighbouring slams overlap>] [aoe=<slam prefab>] [damage=0 (x prefab damage, 0 = push only)] [mode=simple|strong (push x2.1 = Demolisher sledge value, or x4.2)] [force=<own multiplier, overrides mode>] [players=1 (also pushes other players and friendly or tamed creatures, never yourself; 0 = enemies only)] [props=0]"
                    + " [stone=rock_7] [stonefrom=2] [stonestep=2 (stones on every 2nd ring from stonefrom: 1 = every ring)] [tilt=45 (deg, top leans away from the player)] [stoney=0 (m, lift)] [stonelife=60 (s from the command until the INNERMOST stone circle breaks, 0 = they stay)] [dropcount=<n or min-max> (stones dropped by each broken rock, default 1; game = as the game rolls it, Rock_7: 3-6)] [stonegap=10 (s each circle further out breaks earlier)] [stoneyaw=90 (deg, spin of each stone about its own axis; 90 puts the wide side along the circle)] [noStones=1]"
                    + " | fx=layered (set five as accepted: rings vfx_troll_groundslam, linger vfx_darkland_groundfog, stone break fx_tombstone_destroyed + sfx_rock_wall_crumble) | fx=earth (DEFAULT, accepted set: cast fx_goblinbrute_groundslam, rings vfx_troll_groundslam, stone-break fx_tombstone_destroyed + sfx_rock_wall_crumble, linger vfx_darkland_groundfog; fx=none = no effect layers; stonefx= adds an effect when stones rise) or castfx= ringfx= stonefx= breakfx= breaksfx= lingerfx= (prefab names) lingerdur=20 fxevery=4 | kill=<radius m> (kills all untamed creatures around you, no effect run) | clearitems=1 (removes all items lying on the ground, inventories untouched) | clear=1 (remove stones this command placed, also after a reload) | find=<text> | dump=<prefab> | item=<item prefab> (print the hammer's attacks)";
            }
        }

        const BindingFlags AllF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static bool HitPlayers = true;
        static string BreakFx = "", BreakSfx = "";
        static int BreakCount, BreakEvery = 4;

        // a visual or sound prefab at a point, removed again after 'life' seconds (most fx_ prefabs clean themselves up; looping ones do not)
        static void Fx(string prefab, Vector3 pos, float life)
        {
            GameObject go = Prefabs.Spawn(prefab, pos, Quaternion.identity);
            if (go == null) return;
            Later(life, delegate
            {
                if (go == null) return;
                ZNetView nv = go.GetComponent<ZNetView>();
                if (nv != null && nv.IsValid()) ZNetScene.instance.Destroy(go);
                else UnityEngine.Object.Destroy(go);
            });
        }
        const string StoneMark = "hc_earthquake";
        static readonly string[] HammerItems = new string[] { "SledgeStagbreaker", "Stagbreaker" };
        static readonly string[] StoneNames = new string[] { "rock_7", "Rock_7" };

        protected override string Execute(Args a)
        {
            string find = a.Str("find", "");
            if (find.Length > 0) return Find(find);
            string dump = a.Str("dump", "");
            if (dump.Length > 0) return Dump(dump);
            string item = a.Str("item", "");
            if (item.Length > 0) return DumpItem(item);
            if (a.Flag("clear", false)) return Clear();
            if (a.Flag("clearitems", false)) return ClearItems();
            if (a.Flag("weapons", false)) return Weapons(a.Int("top", 15));
            if (a.Flag("demo", false)) return Demo(a);
            float killR = a.Float("kill", 0f);
            if (killR > 0f) return KillAll(killR);

            string err;
            Player target = Target(a, out err);
            if (target == null) return err;
            if (target != Players.Local && !ZNet.instance.IsServer()) return "ERROR: effects on other players need this game to be the host";

            int stages = Mathf.Max(1, a.Int("stages", 5));
            int hits = Mathf.Max(1, a.Int("hits", 2));
            float delay = Mathf.Max(0f, a.Float("delay", 0.003f));
            float stageGap = Mathf.Max(0f, a.Float("stagegap", 0.4f));
            float damage = Mathf.Max(0f, a.Float("damage", 0f));
            HitPlayers = a.Flag("players", true);
            string mode = a.Str("mode", "simple").ToLowerInvariant();
            if (mode != "simple" && mode != "strong" && mode != "1" && mode != "2") return "ERROR: mode must be simple or strong (got '" + mode + "')";
            bool strong = mode == "strong" || mode == "2";
            float force = Mathf.Max(0f, a.Float("force", strong ? StrongForce : SimpleForce));
            bool props = a.Flag("props", false);
            int stoneFrom = Mathf.Max(1, a.Int("stonefrom", 2));
            int stoneStep = Mathf.Max(1, a.Int("stonestep", 2));
            float tilt = a.Float("tilt", 45f);
            float stoneY = a.Float("stoney", 0f);
            float stoneYaw = a.Float("stoneyaw", 90f);
            float stoneLife = a.Float("stonelife", 60f);
            float stoneGap = Mathf.Max(0f, a.Float("stonegap", 10f));
            // dropcount=<n> or <min>-<max>: stones per broken rock, rolled by the game's own drop table; default 1, "game" = untouched (Rock_7: 3-6)
            string dcs = a.Str("dropcount", "1").Trim().ToLowerInvariant();
            int dropMin = -1, dropMax = -1;
            if (dcs != "game")
            {
                string[] dp = dcs.Split('-');
                if (dp.Length == 1 && int.TryParse(dp[0], out dropMin)) dropMax = dropMin;
                else if (dp.Length == 2 && int.TryParse(dp[0], out dropMin) && int.TryParse(dp[1], out dropMax) && dropMin >= 0 && dropMax >= dropMin) { }
                else return "ERROR: dropcount must be a number, a range like 0-1, or 'game' (got '" + dcs + "')";
                if (dropMin < 0) return "ERROR: dropcount cannot be negative";
            }
            // visual layers: fx=earth is the ready-made "earth" set; castfx / ringfx / stonefx / breakfx / breaksfx / lingerfx pick any prefab (empty = none)
            string fxSet = a.Str("fx", "earth").ToLowerInvariant();   // default: the accepted "earth" set; fx=none switches all effect layers off
            string castFx = a.Str("castfx", fxSet == "earth" ? "fx_goblinbrute_groundslam" : "");
            string ringFx = a.Str("ringfx", (fxSet == "earth" || fxSet == "layered") ? "vfx_troll_groundslam" : "");
            string stoneFx = a.Str("stonefx", "");
            BreakFx = a.Str("breakfx", (fxSet == "earth" || fxSet == "layered") ? "fx_tombstone_destroyed" : "");
            BreakSfx = a.Str("breaksfx", (fxSet == "earth" || fxSet == "layered") ? "sfx_rock_wall_crumble" : "");
            string lingerFx = a.Str("lingerfx", (fxSet == "earth" || fxSet == "layered") ? "vfx_darkland_groundfog" : "");
            float lingerDur = a.Float("lingerdur", 20f);
            int ringEvery = Mathf.Max(1, a.Int("ringevery", 1));   // the ring layer fires on every n-th point of a ring
            int fxEvery = Mathf.Max(1, a.Int("fxevery", 4));   // the stone and sound layers fire on every n-th stone
            string[] fxAll = new string[] { castFx, ringFx, stoneFx, BreakFx, BreakSfx, lingerFx };
            foreach (string fxn in fxAll) if (fxn.Length > 0 && Prefabs.Get(fxn) == null) return "ERROR: effect prefab '" + fxn + "' not found";
            int stoneRings = 0;
            for (int kk = 1; kk <= stages; kk++) if (kk >= stoneFrom && (kk - stoneFrom) % stoneStep == 0) stoneRings++;
            int stoneIdx = 0;
            bool stones = !a.Flag("noStones", false);

            // the slam prefab: aoe= wins, else the hammer's secondary attack (ground slam), else its primary attack
            string slam = a.Str("aoe", "");
            string slamSrc = "aoe=";
            if (slam.Length == 0) { slam = HammerSlam(out slamSrc); if (slam.Length == 0 && Prefabs.Get("sledge_aoe") != null) { slamSrc = slamSrc + "; fallback sledge_aoe (the sledge family slam Aoe, radius 4, force 100)"; slam = "sledge_aoe"; } }
            if (slam.Length == 0) return "ERROR: could not find the hammer's slam prefab (" + slamSrc + "); try op=earthquake&item=SledgeStagbreaker, then aoe=<prefab>";
            GameObject pf = Prefabs.Get(slam);
            if (pf == null) return "ERROR: no prefab '" + slam + "' (op=earthquake&find=...)";
            Aoe pfAoe = pf.GetComponentInChildren<Aoe>(true);
            if (pfAoe == null) return "ERROR: prefab '" + slam + "' has no Aoe component";

            float slamR = pfAoe.m_radius * Mathf.Max(Mathf.Abs(pfAoe.transform.lossyScale.x), Mathf.Abs(pfAoe.transform.lossyScale.z));
            float r = a.Float("radius", -1f);
            if (r <= 0f) r = slamR;
            if (r <= 0.1f) return "ERROR: slam radius unreadable (" + slamR.ToString("F2") + "); pass radius=<metres>";
            float spacing = a.Float("spacing", 1.4f * r);

            GameObject stonePf = null;
            string stoneName = a.Str("stone", "");
            float stoneW = 0f;
            if (stones)
            {
                if (stoneName.Length > 0) stonePf = Prefabs.Get(stoneName);
                else foreach (string sn in StoneNames) { stonePf = Prefabs.Get(sn); if (stonePf != null) { stoneName = sn; break; } }
                if (stonePf == null) return "ERROR: stone prefab '" + (stoneName.Length > 0 ? stoneName : "rock_7") + "' not found (op=earthquake&find=rock); or noStones=1";
                stoneW = StoneWidth(stonePf);
            }

            Vector3 center = target.transform.position;
            Player owner = target;
            StringBuilder sb = new StringBuilder();
            int totalHits = 0, totalStones = 0;
            float t0 = 0f;

            // stage 0: 'hits' slams right under the player
            for (int j = 0; j < hits; j++) Schedule(t0 + j * delay, slam, center, owner, damage, props, force);
            totalHits += hits;
            sb.Append("stage 0: R=0.0 m, 1 point, hits=").Append(hits).Append('\n');
            BreakCount = 0;
            BreakEvery = fxEvery;
            if (castFx.Length > 0) Fx(castFx, center, 12f);
            if (lingerFx.Length > 0) { string lf = lingerFx; Later(stages * stageGap, delegate { Fx(lf, center, lingerDur); }); }

            for (int k = 1; k <= stages; k++)
            {
                float R = k * r;
                float when = k * stageGap;
                List<Vector3> pts = Ring(center, R, spacing, (k % 2 == 0) ? 0f : 180f / Mathf.Max(1, Mathf.FloorToInt(2f * Mathf.PI * R / spacing)));
                int pi = 0;
                foreach (Vector3 p in pts)
                {
                    Vector3 pos = p;
                    for (int j = 0; j < hits; j++) Schedule(when + j * delay, slam, pos, owner, damage, props, force);
                    if (ringFx.Length > 0 && pi++ % ringEvery == 0) { string rf = ringFx; Later(when, delegate { Fx(rf, pos, 10f); }); }
                }
                totalHits += pts.Count * hits;
                sb.Append("stage ").Append(k).Append(": R=").Append(R.ToString("F1")).Append(" m, ").Append(pts.Count).Append(" points, hits=").Append(hits).Append('\n');

                if (stones && k >= stoneFrom && (k - stoneFrom) % stoneStep == 0)
                {
                    // break time counted from the command: the innermost stone circle at stonelife, each circle further out stonegap earlier
                    float ringLife = stoneLife > 0f ? Mathf.Max(0.1f, stoneLife - stoneGap * stoneIdx - when) : 0f;
                    stoneIdx++;
                    List<Vector3> sp = Ring(center, R, stoneW, 0f);
                    int placed = 0;
                    foreach (Vector3 q in sp)
                    {
                        Vector3 pos = q;
                        Vector3 outward = pos - center; outward.y = 0f;
                        if (outward.sqrMagnitude < 0.01f) continue;
                        outward.Normalize();
                        string sName = stoneName;
                        Later(when, delegate { PlaceStone(sName, pos, outward, tilt, stoneY, stoneYaw, ringLife, dropMin, dropMax); });
                        if (stoneFx.Length > 0 && placed % fxEvery == 0) { string sf = stoneFx; Later(when, delegate { Fx(sf, pos, 15f); }); }
                        placed++;
                    }
                    totalStones += placed;
                    sb.Append("  stones: ").Append(placed).Append(" x ").Append(stoneName).Append(" (width ").Append(stoneW.ToString("F2")).Append(" m) on R=").Append(R.ToString("F1")).Append(" m, tilt ").Append(tilt.ToString("F0")).Append(" deg, breaks at ").Append(stoneLife > 0f ? (when + ringLife).ToString("F1") + " s" : "never").Append("\n");
                }
            }
            sb.Append(Probe(center, stages * r + r, stages * stageGap + 2.5f));
            sb.Append("earthquake: ").Append(totalHits).Append(" slams, ").Append(totalStones).Append(" stones; slam prefab ").Append(slam).Append(" (").Append(slamSrc)
              .Append("), slam r=").Append(slamR.ToString("F2")).Append(", ring step r=").Append(r.ToString("F2")).Append(", spacing ").Append(spacing.ToString("F2"))
              .Append(", mode ").Append(strong ? "strong" : "simple").Append(", force x").Append(force.ToString("F1")).Append(", damage x").Append(damage.ToString("F2")).Append(", props=").Append(props).Append(", delay ").Append(delay.ToString("F3")).Append(" s, centre ").Append(Fmt.Vec(center));
            return sb.ToString();
        }

        // ---- slam ---------------------------------------------------------------------------

        static void Schedule(float t, string prefab, Vector3 pos, Player owner, float damage, bool props, float force)
        {
            if (t <= 0f) Slam(prefab, pos, owner, damage, props, force);
            else Later(t, delegate { Slam(prefab, pos, owner, damage, props, force); });
        }

        static void Slam(string prefab, Vector3 pos, Player owner, float damage, bool props, float force)
        {
            GameObject go = Prefabs.Spawn(prefab, pos + Vector3.up * 0.1f, Quaternion.identity);
            if (go == null) return;
            foreach (Aoe aoe in go.GetComponentsInChildren<Aoe>(true))
            {
                try
                {
                    // push, do not hurt: the owner and friends are skipped, damage is scaled (default 0)
                    aoe.m_hitOwner = false;
                    // players and friendly or tamed creatures are pushed too (players=0 turns that off); the owner is still never hit
                    aoe.m_hitFriendly = HitPlayers;
                    aoe.m_ignorePVP = HitPlayers;
                    aoe.m_hitSame = HitPlayers;      // the slam otherwise skips targets of the owner's own kind, i.e. other players
                    aoe.m_hitProps = props && aoe.m_hitProps;
                    aoe.m_hitTerrain = false;
                    aoe.m_damage.Modify(damage);
                    aoe.m_damagePerLevel.Modify(damage);
                    aoe.m_damageSelf = 0f;
                    aoe.m_attackForce *= force;
                    aoe.m_knockBackForce *= force;
                    aoe.Setup(owner, Vector3.zero, 0f, null, null, null);
                }
                catch (Exception e) { HomeApi.Log("earthquake: slam setup failed: " + e.Message); }
            }
        }

        // hammer's secondary attack first (ground slam), else its primary; the prefab the attack spawns on trigger
        static string HammerSlam(out string src)
        {
            foreach (string h in HammerItems)
            {
                GameObject ip = ObjectDB.instance.GetItemPrefab(h);
                if (ip == null) continue;
                ItemDrop d = ip.GetComponent<ItemDrop>();
                if (d == null) continue;
                ItemDrop.ItemData.SharedData sh = d.m_itemData.m_shared;
                if (sh.m_secondaryAttack != null && sh.m_secondaryAttack.m_spawnOnTrigger != null)
                { src = h + " secondary attack"; return sh.m_secondaryAttack.m_spawnOnTrigger.name; }
                if (sh.m_attack != null && sh.m_attack.m_spawnOnTrigger != null)
                { src = h + " primary attack"; return sh.m_attack.m_spawnOnTrigger.name; }
                src = h + " has no spawnOnTrigger on either attack";
                return "";
            }
            src = "no hammer item among " + string.Join("/", HammerItems);
            return "";
        }


        // records where non-player characters stand now, and logs how far each moved after the effect has played out
        static string Probe(Vector3 center, float range, float after)
        {
            List<Character> cs = new List<Character>();
            List<Vector3> from = new List<Vector3>();
            foreach (Character c in Character.GetAllCharacters())
            {
                if (c == null || c == Players.Local) continue;
                if (Vector3.Distance(c.transform.position, center) > range) continue;
                cs.Add(c);
                from.Add(c.transform.position);
            }
            if (cs.Count == 0) return "probe: no creatures within " + range.ToString("F0") + " m\n";
            Later(after, delegate
            {
                for (int i = 0; i < cs.Count; i++)
                {
                    if (cs[i] == null) { HomeApi.Log("earthquake probe: " + i + " gone (dead or unloaded)"); continue; }
                    Vector3 to = cs[i].transform.position;
                    HomeApi.Log("earthquake probe: " + (cs[i] is Player ? "PLAYER " + ((Player)cs[i]).GetPlayerName() : cs[i].name) + " moved " + Vector3.Distance(from[i], to).ToString("F1") + " m (from " + Fmt.Vec(from[i]) + " to " + Fmt.Vec(to) + "), hp " + cs[i].GetHealth().ToString("F0") + "/" + cs[i].GetMaxHealth().ToString("F0"));
                }
            });
            return "probe: " + cs.Count + " creatures within " + range.ToString("F0") + " m, displacement logged to BepInEx/LogOutput.log after " + after.ToString("F1") + " s\n";
        }

        // ---- stones -------------------------------------------------------------------------

        static float StoneWidth(GameObject pf)
        {
            Renderer[] rs = pf.GetComponentsInChildren<Renderer>(true);
            bool any = false;
            Bounds b = new Bounds();
            foreach (Renderer rd in rs)
            {
                if (!any) { b = rd.bounds; any = true; } else b.Encapsulate(rd.bounds);
            }
            if (!any) return 2f;
            return Mathf.Max(0.5f, Mathf.Max(b.size.x, b.size.z));
        }

        static GameObject PlaceStone(string name, Vector3 pos, Vector3 outward, float tilt, float lift, float yaw, float life, int dropMin, int dropMax)
        {
            // up-axis leans 'tilt' degrees away from the player; a random spin keeps the circle from looking stamped
            Vector3 up = (Vector3.up * Mathf.Cos(tilt * Mathf.Deg2Rad) + outward * Mathf.Sin(tilt * Mathf.Deg2Rad)).normalized;
            Quaternion rot = Quaternion.AngleAxis(tilt, Vector3.Cross(Vector3.up, outward)) * Quaternion.LookRotation(outward) * Quaternion.Euler(0f, yaw, 0f);
            GameObject go = Prefabs.Spawn(name, pos + Vector3.up * lift, rot);
            if (go == null) return null;
            ZNetView nv = go.GetComponent<ZNetView>();
            if (nv != null && nv.IsValid()) nv.GetZDO().Set(StoneMark, 1);
            if (life > 0f)
                Later(life, delegate { Break(go, dropMin, dropMax); });
            return go;
        }

        // the stone is broken the game's own way (break effect, removal synced to everyone). With dropcount >= 0 its drop
        // table is emptied first; nothing is searched for or deleted afterwards (a scan of all items per stone made the game stutter)
        static void Break(GameObject go, int dropMin, int dropMax)
        {
            if (go == null) return;
            if (BreakFx.Length > 0) Fx(BreakFx, go.transform.position, 8f);
            if (BreakSfx.Length > 0 && BreakCount++ % BreakEvery == 0) Fx(BreakSfx, go.transform.position, 6f);
            DropOnDestroyed dd = go.GetComponent<DropOnDestroyed>();
            if (dropMin >= 0 && dd != null && dd.m_dropWhenDestroyed != null)
            {
                // a fixed number of draws from the table (the game rolls dropMin..dropMax); 0 empties it
                dd.m_dropWhenDestroyed.m_dropMin = dropMin;
                dd.m_dropWhenDestroyed.m_dropMax = dropMax;
                if (dropMax == 0) dd.m_dropWhenDestroyed.m_drops.Clear();
            }
            Destructible d = go.GetComponent<Destructible>();
            if (d != null) d.DestroyNow();
            else ZNetScene.instance.Destroy(go);
        }

        // kills every creature (not players, not tamed ones) within 'range' m of the local player; used to clear a test arena
        static string KillAll(float range)
        {
            Player p = Players.Local;
            if (p == null) return "ERROR: no local player";
            int n = 0, spared = 0;
            foreach (Character c in new List<Character>(Character.GetAllCharacters()))
            {
                if (c == null || c is Player || c.IsDead()) continue;
                if (Vector3.Distance(c.transform.position, p.transform.position) > range) continue;
                if (c.IsTamed()) { spared++; continue; }
                HitData hit = new HitData();
                hit.m_damage.m_damage = 100000f;
                hit.m_point = c.transform.position;
                c.Damage(hit);
                n++;
            }
            return "earthquake: killed " + n + " creatures within " + range.ToString("F0") + " m (" + spared + " tamed spared)";
        }

        // every weapon, strongest push first: SharedData.m_attackForce x the attack's m_forceMultiplier (primary and secondary)
        static string Weapons(int top)
        {
            List<KeyValuePair<float, string>> rows = new List<KeyValuePair<float, string>>();
            HashSet<string> crafted = new HashSet<string>();
            foreach (Recipe rc in ObjectDB.instance.m_recipes) if (rc != null && rc.m_item != null) crafted.Add(rc.m_item.name);
            foreach (GameObject go in ObjectDB.instance.m_items)
            {
                if (go == null || !crafted.Contains(go.name)) continue;
                ItemDrop d = go.GetComponent<ItemDrop>();
                if (d == null) continue;
                ItemDrop.ItemData.SharedData sh = d.m_itemData.m_shared;
                if (sh.m_attack == null || sh.m_attackForce <= 0f) continue;
                float f1 = sh.m_attackForce * sh.m_attack.m_forceMultiplier;
                float f2 = sh.m_secondaryAttack != null ? sh.m_attackForce * sh.m_secondaryAttack.m_forceMultiplier : 0f;
                float best = Mathf.Max(f1, f2);
                rows.Add(new KeyValuePair<float, string>(best, go.name + "  type=" + sh.m_itemType + "  weaponForce=" + sh.m_attackForce.ToString("F0")
                    + "  primary x" + sh.m_attack.m_forceMultiplier.ToString("F1") + " (" + sh.m_attack.m_attackType + ")"
                    + (sh.m_secondaryAttack != null ? "  secondary x" + sh.m_secondaryAttack.m_forceMultiplier.ToString("F1") + " (" + sh.m_secondaryAttack.m_attackType + ")" : "")
                    + "  => " + best.ToString("F0")));
            }
            rows.Sort(delegate(KeyValuePair<float, string> x, KeyValuePair<float, string> y) { return y.Key.CompareTo(x.Key); });
            StringBuilder sb = new StringBuilder();
            sb.Append(rows.Count).Append(" weapons with a push force; strongest ").Append(top).Append(":\n");
            for (int i = 0; i < rows.Count && i < top; i++) sb.Append(rows[i].Value).Append('\n');
            return sb.ToString();
        }

        // removes every item lying on the ground in the loaded world (ItemDrop objects). Inventories, chests, pickables and pieces are not touched.
        static string ClearItems()
        {
            int n = 0;
            Dictionary<string, int> kinds = new Dictionary<string, int>();
            SortedDictionary<int, int> hist = new SortedDictionary<int, int>();
            foreach (ItemDrop it in UnityEngine.Object.FindObjectsOfType<ItemDrop>())
            {
                if (it == null) continue;
                ZNetView nv = it.GetComponent<ZNetView>();
                if (nv == null || !nv.IsValid()) continue;
                string nm = it.name.Replace("(Clone)", "");
                if (nm == "Stone" && Players.Local != null) { int bk = (int)(Vector3.Distance(it.transform.position, Players.Local.transform.position) / 4f) * 4; int hc; hist.TryGetValue(bk, out hc); hist[bk] = hc + (it.m_itemData != null ? it.m_itemData.m_stack : 1); }
                int c; kinds.TryGetValue(nm, out c); kinds[nm] = c + 1;
                ZNetScene.instance.Destroy(it.gameObject);
                n++;
            }
            StringBuilder sb = new StringBuilder("earthquake: removed " + n + " items lying on the ground");
            foreach (KeyValuePair<string, int> kv in kinds) sb.Append(" ").Append(kv.Key).Append("x").Append(kv.Value);
            if (hist.Count > 0) { sb.Append("; stones (count in stacks) by distance from you, 4 m buckets:"); foreach (KeyValuePair<int, int> kv in hist) sb.Append(" ").Append(kv.Key).Append("m:").Append(kv.Value); }
            return sb.ToString();
        }

        // demo=1: five Rock_7 side by side in front of you, tilted away like in the spell; each one is broken in turn with the break layers of
        // one effect set (1 earth, 2 epic, 3 subtle, 4 frost, 5 layered), the first after 'demostart' s, then every 'demogap' s
        static string Demo(Args a)
        {
            Player p = Players.Local;
            if (p == null) return "ERROR: no local player";
            string[][] sets = new string[][]
            {
                new string[] { "earth",   "fx_tombstone_destroyed", "sfx_troll_rock_destroyed" },
                new string[] { "epic",    "",                       "sfx_gdking_rock_destroyed" },
                new string[] { "subtle",  "",                       "" },
                new string[] { "frost",   "fx_icicle_destruction",  "sfx_ice_destroyed" },
                new string[] { "layered", "fx_tombstone_destroyed", "sfx_rock_wall_crumble" }
            };
            foreach (string[] s in sets)
                for (int i = 1; i <= 2; i++)
                    if (s[i].Length > 0 && Prefabs.Get(s[i]) == null) return "ERROR: effect prefab '" + s[i] + "' not found";
            string stoneName = a.Str("stone", "");
            if (stoneName.Length == 0) foreach (string sn in StoneNames) if (Prefabs.Get(sn) != null) { stoneName = sn; break; }
            if (stoneName.Length == 0 || Prefabs.Get(stoneName) == null) return "ERROR: stone prefab not found";
            float width = StoneWidth(Prefabs.Get(stoneName));
            float tilt = a.Float("tilt", 45f);
            float yaw = a.Float("stoneyaw", 90f);
            float dist = a.Float("demodist", 6f);
            float start = a.Float("demostart", 5f);
            float gap = a.Float("demogap", 15f);

            Vector3 fwd = p.transform.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            Vector3 mid = p.transform.position + fwd * dist;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < sets.Length; i++)
            {
                Vector3 pos = mid + right * ((i - 2) * width);
                pos.y = Ground.Height(pos);
                GameObject go = PlaceStone(stoneName, pos, fwd, tilt, 0f, yaw, 0f, 0, 0);
                string[] set = sets[i];
                float t = start + i * gap;
                Later(t, delegate
                {
                    BreakFx = set[1]; BreakSfx = set[2]; BreakCount = 0; BreakEvery = 1;
                    Break(go, 0, 0);
                });
                sb.Append("stone ").Append(i + 1).Append(" (").Append(i < 2 ? "left" : i == 2 ? "centre" : "right").Append("): set ").Append(set[0]).Append(", breaks at ").Append(t.ToString("F0")).Append(" s with ")
                  .Append(set[1].Length > 0 ? set[1] : "no extra effect").Append(set[2].Length > 0 ? " + " + set[2] : "").Append('\n');
            }
            sb.Append("demo: 5 x ").Append(stoneName).Append(" ").Append(dist.ToString("F0")).Append(" m ahead of you, left to right, drops off");
            return sb.ToString();
        }

        static string Clear()
        {
            int n = 0;
            foreach (ZNetView nv in UnityEngine.Object.FindObjectsOfType<ZNetView>())
            {
                if (nv == null || !nv.IsValid()) continue;
                if (nv.GetZDO().GetInt(StoneMark, 0) != 1) continue;
                ZNetScene.instance.Destroy(nv.gameObject);
                n++;
            }
            return "earthquake: removed " + n + " stones placed by this command";
        }

        // ---- facts --------------------------------------------------------------------------

        static string Find(string text)
        {
            StringBuilder sb = new StringBuilder();
            FieldInfo f = typeof(ZNetScene).GetField("m_namedPrefabs", AllF);
            Dictionary<int, GameObject> d = f.GetValue(ZNetScene.instance) as Dictionary<int, GameObject>;
            List<string> names = new List<string>();
            foreach (KeyValuePair<int, GameObject> kv in d)
                if (kv.Value != null && kv.Value.name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                    names.Add(kv.Value.name + (kv.Value.GetComponentInChildren<Aoe>(true) != null ? "  [Aoe]" : ""));
            names.Sort();
            int n = 0;
            foreach (string s in names) { if (n++ >= 150) { sb.Append("... (more)"); break; } sb.Append(s).Append('\n'); }
            return names.Count == 0 ? "no prefab matches '" + text + "'" : sb.ToString();
        }

        static string Dump(string prefab)
        {
            GameObject pf = Prefabs.Get(prefab);
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
                SphereCollider sc = c as SphereCollider;
                if (sc != null) sb.Append(" radius=").Append(sc.radius.ToString("F2")).Append(" trigger=").Append(sc.isTrigger).Append(" lossyScale=").Append(Fmt.Vec(sc.transform.lossyScale));
                CapsuleCollider cc = c as CapsuleCollider;
                if (cc != null) sb.Append(" radius=").Append(cc.radius.ToString("F2")).Append(" height=").Append(cc.height.ToString("F2"));
                BoxCollider bc = c as BoxCollider;
                if (bc != null) sb.Append(" size=").Append(Fmt.Vec(bc.size)).Append(" centre=").Append(Fmt.Vec(bc.center));
                MeshCollider mc = c as MeshCollider;
                if (mc != null) sb.Append(" convex=").Append(mc.convex);
                Destructible dsr = c as Destructible;
                if (dsr != null)
                {
                    sb.Append(" health=").Append(dsr.m_health).Append(" destroyedEffect=[");
                    if (dsr.m_destroyedEffect != null && dsr.m_destroyedEffect.m_effectPrefabs != null)
                        foreach (EffectList.EffectData ed in dsr.m_destroyedEffect.m_effectPrefabs)
                            sb.Append(ed.m_prefab != null ? ed.m_prefab.name : "?").Append("; ");
                    sb.Append("] hitEffect=[");
                    if (dsr.m_hitEffect != null && dsr.m_hitEffect.m_effectPrefabs != null)
                        foreach (EffectList.EffectData ed2 in dsr.m_hitEffect.m_effectPrefabs)
                            sb.Append(ed2.m_prefab != null ? ed2.m_prefab.name : "?").Append("; ");
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
                if (tn == "Aoe")
                {
                    foreach (FieldInfo f in c.GetType().GetFields(AllF))
                    {
                        object v = f.GetValue(c);
                        if (v == null) continue;
                        if (f.FieldType.IsPrimitive || f.FieldType == typeof(string) || f.FieldType.IsEnum || f.FieldType == typeof(Vector2))
                            sb.Append(' ').Append(f.Name).Append('=').Append(v);
                        else if (f.Name == "m_damage") sb.Append(' ').Append(f.Name).Append('=').Append(v.ToString().Replace('\n', ' '));
                    }
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        static string DumpItem(string item)
        {
            GameObject ip = ObjectDB.instance.GetItemPrefab(item);
            if (ip == null) return "ERROR: no item prefab '" + item + "' (op=earthquake&find=sledge shows prefabs, items may be listed under op=list)";
            ItemDrop d = ip.GetComponent<ItemDrop>();
            if (d == null) return "ERROR: '" + item + "' has no ItemDrop";
            StringBuilder sb = new StringBuilder();
            ItemDrop.ItemData.SharedData sh = d.m_itemData.m_shared;
            sb.Append("item ").Append(item).Append(" name ").Append(sh.m_name).Append(" weaponForce=").Append(sh.m_attackForce).Append(" damage ").Append(sh.m_damages.ToString().Replace('\n', ' ')).Append('\n');
            AppendAttack(sb, "primary", sh.m_attack);
            AppendAttack(sb, "secondary", sh.m_secondaryAttack);
            return sb.ToString();
        }

        static void AppendAttack(StringBuilder sb, string label, Attack at)
        {
            if (at == null) { sb.Append(label).Append(": none\n"); return; }
            sb.Append(label).Append(": type=").Append(at.m_attackType).Append(" anim=").Append(at.m_attackAnimation)
              .Append(" spawnOnTrigger=").Append(at.m_spawnOnTrigger == null ? "-" : at.m_spawnOnTrigger.name)
              .Append(" spawnOnHit=").Append(at.m_spawnOnHit == null ? "-" : at.m_spawnOnHit.name)
              .Append(" projectile=").Append(at.m_attackProjectile == null ? "-" : at.m_attackProjectile.name)
              .Append(" range=").Append(at.m_attackRange).Append(" angle=").Append(at.m_attackAngle)
              .Append(" damageMult=").Append(at.m_damageMultiplier).Append(" forceMult=").Append(at.m_forceMultiplier)
              .Append(" staggerMult=").Append(at.m_staggerMultiplier).Append('\n');
        }
    }
}

