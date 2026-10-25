// trap: traps laid as a matrix on the ground at the crosshair (the spellshot model: one placement, a kind per type=).
// Kind mine today, the electric fence later. Host only: mines hurt other players and stay in shared world data.
// op=trap&type=mine [perrow=1] [rows=1] [spacing=2] [at=aim|self] [dist=3] [life=300] [trigger=1.5] [blast=3] ...
// op=trap&remove=1 [radius=0]   op=trap&list=1   op=trap&type=probe&filter=trap
//
// The vanilla Trap component of the visual is left inert on every PC (state unarmed, cooldown far in the future, so
// nobody can arm it by hand, HomeCheat installed or not); the host's watcher does the triggering, the blast and the expiry.
// Each trap carries its settings in its ZDO, so it keeps working the same after op=reload or the next world load.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.Traps
{
    public sealed class TrapCommand : CommandBase
    {
        internal static TrapCommand Current;

        const int MaxSide = 9;            // perrow and rows
        const int MaxLive = 50;           // live traps in the loaded area
        const float MaxLife = 3600f;
        const float MaxTrigger = 5f;
        const float MaxBlast = 10f;
        const float MaxMult = 10f;
        const float MaxRemoveRadius = 100f;
        const float RemoveAimRange = 2f;  // remove=1 without radius: the trap this close to the crosshair point
        const float TickSeconds = 0.2f;   // watcher step while traps are live
        const float IdleSeconds = 3f;     // watcher step with none
        const float RescanSeconds = 3f;   // find traps loaded since (areas, reload, world load)
        const float HeightReach = 2.5f;   // a character this far above or below still counts

        const string KeyKind = "hc_trap";
        const string KeyEnd = "hc_trap_end";       // world time in ms when it disappears
        const string KeyArm = "hc_trap_arm";       // world time in ms from which it can go off
        const string KeyTrigger = "hc_trap_trigger";
        const string KeyBlast = "hc_trap_blast";
        const string KeyWeapon = "hc_trap_weapon";
        const string KeyQuality = "hc_trap_quality";
        const string KeyMult = "hc_trap_mult";
        const string KeyFxBoom = "hc_trap_fxboom";
        const string KeyFxEnd = "hc_trap_fxend";

        readonly List<ZNetView> live = new List<ZNetView>();
        readonly Dictionary<string, TrapWeapon> weapons = new Dictionary<string, TrapWeapon>();
        bool watching;
        float nextRescan;

        public TrapCommand()
        {
            SubFlag("list", "list=1 (live traps in the loaded area: kind, position, distance, seconds left; read-only)",
                delegate (Args a) { return List(); });
            SubFlag("remove", "remove=1 [radius=0 (0 = the trap within 2 m of the crosshair point; > 0 = every trap within radius m of you, cap 100)] (with the vanishing effect, nothing dropped)",
                delegate (Args a) { return Remove(a.Float("radius", 0f)); });
        }

        public override string Name { get { return "trap"; } }
        protected override string Usage
        {
            get
            {
                return "trap type=" + TrapKind.Names + " [perrow=1] [rows=1] (1..9 each, the matrix is centred on the point; rows run along your look direction) [spacing=2] [at=aim|self] [dist=3 (with at=self: metres ahead)] "
                    + "[life=300 (s, cap 3600, then it vanishes)] [arm=3 (s before it can go off)] [trigger=1.5 (m, cap 5)] [blast=3 (m, cap 10)] [weapon=BowAshlands] [quality=max] [mult=2 (x the weapon damage, cap 10)] "
                    + "[prefab=<visual with a vanilla Trap; default piece_trap_troll, else fuling_trap>] [fxplace=, fxboom=, fxend= (comma lists of fx prefabs, none = no effect)]; "
                    + "mine = goes off once when any player or creature (you, other players, tamed, bosses) comes within trigger, hits everyone within blast with no player attacker (like a monster's hit, no PvP needed), then is gone; "
                    + "host only; cap 50 live traps in the loaded area; only the host's loaded area is watched. type=probe&filter=trap lists prefabs with a Trap (read-only).";
            }
        }

        protected override string Execute(Args a)
        {
            string type = a.Str("type", "").ToLowerInvariant();
            if (type == "probe") { Host.RequireWorld(); return Probe(a.Str("filter", "trap")); }
            if (type == "fence") throw new CommandException("type=fence is planned, not built yet; built: " + TrapKind.Names);
            TrapKind kind = TrapKind.Get(type);
            if (kind == null) throw new CommandException("type required: " + TrapKind.Names + " (or probe)");
            Host.RequireServer();
            Player p = Players.Local;

            int perrow = Caps.Check("perrow", a.Int("perrow", 1), MaxSide);
            int rows = Caps.Check("rows", a.Int("rows", 1), MaxSide);
            if (perrow < 1 || rows < 1) throw new CommandException("perrow and rows must be 1 to 9");
            float spacing = Mathf.Max(0.5f, a.Float("spacing", kind.Spacing));
            float life = Caps.Check("life", a.Float("life", kind.Life), MaxLife);
            if (life <= 0f) throw new CommandException("life must be > 0");
            float arm = Mathf.Clamp(a.Float("arm", kind.ArmDelay), 0f, life);
            float trigger = Caps.Check("trigger", a.Float("trigger", kind.TriggerRadius), MaxTrigger);
            float blast = Caps.Check("blast", a.Float("blast", kind.BlastRadius), MaxBlast);
            float mult = Caps.Check("mult", a.Float("mult", kind.Mult), MaxMult);
            if (trigger <= 0f || blast <= 0f || mult < 0f) throw new CommandException("trigger and blast must be > 0, mult >= 0");
            string weaponName = a.Str("weapon", kind.Weapon);
            string quality = a.Str("quality", "max");
            TrapWeapon w = Weapon(weaponName, quality);
            if (w == null) throw new CommandException("weapon '" + weaponName + "' not found or has no damage (op=list&filter=Bow)");
            string fxPlace = a.Str("fxplace", kind.FxPlace), fxBoom = a.Str("fxboom", kind.FxBoom), fxEnd = a.Str("fxend", kind.FxEnd);

            GameObject visual = PickVisual(a.Str("prefab", ""), kind);

            Rescan();
            int count = perrow * rows;
            if (live.Count + count > MaxLive)
                throw new CommandException("live traps " + live.Count + " + " + count + " new is over the cap " + MaxLive + " (op=trap&remove=1&radius=50 or wait for them to expire)");

            Vector3 fwd = FlatForward(p);
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            bool aimed = true;
            Vector3 centre = a.Str("at", "aim") == "self"
                ? p.transform.position + fwd * Mathf.Max(0f, a.Float("dist", 3f))
                : Aim.Point(p, out aimed);

            long now = NowMs();
            int placed = 0;
            string firstErr = null;
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < perrow; j++)
                {
                    Vector3 pos = centre + right * ((j - (perrow - 1) / 2f) * spacing) + fwd * ((i - (rows - 1) / 2f) * spacing);
                    pos.y = GroundAt(pos);
                    try
                    {
                        GameObject go = (GameObject)UnityEngine.Object.Instantiate(visual, pos, Quaternion.LookRotation(fwd));
                        ZNetView nv = go.GetComponent<ZNetView>();
                        if (nv == null || !nv.IsValid()) { Fx.Destroy(go); throw new CommandException("visual has no valid ZNetView"); }
                        ZDO z = nv.GetZDO();
                        z.Set(KeyKind, kind.Name);
                        z.Set(KeyEnd, now + (long)(life * 1000f));
                        z.Set(KeyArm, now + (long)(arm * 1000f));
                        z.Set(KeyTrigger, trigger);
                        z.Set(KeyBlast, blast);
                        z.Set(KeyWeapon, weaponName);
                        z.Set(KeyQuality, quality);
                        z.Set(KeyMult, mult);
                        z.Set(KeyFxBoom, fxBoom);
                        z.Set(KeyFxEnd, fxEnd);
                        Inert(go, z);
                        Effects(fxPlace, pos);
                        live.Add(nv);
                        placed++;
                    }
                    catch (Exception e)
                    {
                        if (firstErr == null) firstErr = e.Message;
                    }
                }
            EnsureWatch();

            HitData.DamageTypes dmg = w.Damage;
            dmg.Modify(mult);
            return "placed " + placed + " " + kind.Name + (placed == 1 ? "" : "s") + " (" + perrow + "x" + rows + ", spacing " + Fmt.F(spacing, 1) + " m) at " + Fmt.Vec(centre)
                + (aimed ? "" : " (nothing aimed at: 8 m ahead)") + ", visual " + visual.name + ", life " + Fmt.F(life, 0) + " s, arms after " + Fmt.F(arm, 1) + " s, trigger "
                + Fmt.F(trigger, 1) + " m, blast " + Fmt.F(blast, 1) + " m, damage " + Fmt.F(dmg.GetTotalDamage(), 0) + " (" + Fmt.F(mult, 1) + " x " + w.Label + "); live traps "
                + live.Count + "/" + MaxLive + "; hits players and creatures alike"
                + (firstErr != null ? " ; first error: " + firstErr : "");
        }

        // ---- placement helpers ----------------------------------------------------------------------

        GameObject PickVisual(string given, TrapKind kind)
        {
            List<string> names = new List<string>();
            if (given.Length > 0) names.Add(given); else names.AddRange(kind.Visuals);
            foreach (string n in names)
            {
                GameObject g = Prefabs.Get(n);
                if (g == null) continue;
                if (g.GetComponent<Trap>() == null || g.GetComponent<ZNetView>() == null)
                    throw new CommandException("prefab '" + n + "' has no vanilla Trap or no ZNetView; the watcher finds traps again by their Trap (type=probe&filter=trap)");
                return g;
            }
            throw new CommandException("no trap prefab found (tried " + string.Join(", ", names.ToArray()) + "); type=probe&filter=trap, then prefab=<name>");
        }

        static Vector3 FlatForward(Player p)
        {
            Camera cam = Camera.main;
            Vector3 f = cam != null ? cam.transform.forward : p.transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 0.0001f) { f = p.transform.forward; f.y = 0f; }
            return f.normalized;
        }

        // the floor or ground under a cell (a mine on a wooden floor sits on the floor, not on the terrain under it)
        static float GroundAt(Vector3 pos)
        {
            RaycastHit hit;
            if (Physics.Raycast(pos + Vector3.up * 3f, Vector3.down, out hit, 10f, Masks.SolidAndTerrain)) return hit.point.y;
            return Ground.Height(pos);
        }

        // the vanilla trap never arms: unarmed, and its re-arm cooldown runs until world time 1e9 s (Trap.Interact refuses)
        static void Inert(GameObject go, ZDO z)
        {
            z.Set(ZDOVars.s_state, 0);
            z.Set(ZDOVars.s_triggered, 1e9f);
            Trap t = go.GetComponent<Trap>();
            if (t == null) return;
            foreach (MethodInfo m in typeof(Trap).GetMethods(Refl.InstanceAll))
                if (m.Name == "UpdateState" && m.GetParameters().Length == 0)
                {
                    try { m.Invoke(t, null); } catch (Exception) { }   // only refreshes the look
                    break;
                }
        }

        void Effects(string list, Vector3 pos)
        {
            if (list.Length == 0 || list == "none") return;
            foreach (string n in list.Split(','))
            {
                string name = n.Trim();
                if (name.Length == 0) continue;
                if (Fx.Spawn(this, name, pos, 12f) == null) HomeApi.Log("trap: effect prefab '" + name + "' not found");
            }
        }

        TrapWeapon Weapon(string item, string quality)
        {
            string key = item.ToLowerInvariant() + "|" + quality;
            TrapWeapon w;
            if (weapons.TryGetValue(key, out w)) return w;
            w = TrapWeapon.Pick(item, quality);
            if (w != null) weapons[key] = w;
            return w;
        }

        static long NowMs() { return (long)(ZNet.instance.GetTimeSeconds() * 1000.0); }

        // ---- watcher (host) -------------------------------------------------------------------------

        internal void EnsureWatch()
        {
            if (watching || !Host.IsServer || ZNet.instance == null) return;
            watching = true;
            nextRescan = 0f;
            Later(TickSeconds, Tick);
        }

        void Tick()
        {
            if (!Host.IsServer || ZNet.instance == null || ZNetScene.instance == null) { watching = false; return; }
            try
            {
                if (Time.time >= nextRescan) Rescan();
                Step();
            }
            catch (Exception e) { HomeApi.LogError("trap: watch: " + e); }
            Later(live.Count > 0 ? TickSeconds : IdleSeconds, Tick);
        }

        // every loaded object with a vanilla Trap and the hc_trap mark (traps are few, so this is cheap)
        void Rescan()
        {
            nextRescan = Time.time + RescanSeconds;
            live.Clear();
            foreach (Trap t in UnityEngine.Object.FindObjectsOfType<Trap>())
            {
                if (t == null) continue;
                ZNetView nv = t.GetComponent<ZNetView>();
                if (nv != null && nv.IsValid() && nv.GetZDO().GetString(KeyKind, "").Length > 0) live.Add(nv);
            }
        }

        void Step()
        {
            if (live.Count == 0) return;
            long now = NowMs();
            List<Character> chars = Character.GetAllCharacters();
            foreach (ZNetView nv in live.ToArray())
            {
                if (nv == null || !nv.IsValid()) { live.Remove(nv); continue; }
                ZDO z = nv.GetZDO();
                Vector3 pos = nv.transform.position;
                if (now >= z.GetLong(KeyEnd, 0L)) { Vanish(nv, z.GetString(KeyFxEnd, "")); continue; }
                if (now < z.GetLong(KeyArm, 0L)) continue;
                Character by = Triggered(pos, z.GetFloat(KeyTrigger, 1.5f), chars);
                if (by != null) Boom(nv, z, pos, by, chars);
            }
        }

        // the first living character within the trigger radius (a point trap; the fence will check a segment here)
        static Character Triggered(Vector3 pos, float radius, List<Character> chars)
        {
            foreach (Character c in chars)
            {
                if (c == null || c.IsDead()) continue;
                Vector3 d = c.transform.position - pos;
                if (Mathf.Abs(d.y) > HeightReach) continue;
                d.y = 0f;
                if (d.magnitude <= radius) return c;
            }
            return null;
        }

        // every living character within the blast radius takes the weapon's damage x mult, with no attacker (like a
        // monster's hit, so players are hit without PvP); the effect plays, the trap is gone
        void Boom(ZNetView nv, ZDO z, Vector3 pos, Character by, List<Character> chars)
        {
            float blast = z.GetFloat(KeyBlast, 3f);
            TrapWeapon w = Weapon(z.GetString(KeyWeapon, "BowAshlands"), z.GetString(KeyQuality, "max"));
            float mult = z.GetFloat(KeyMult, 2f);
            int hit = 0;
            if (w != null)
            {
                foreach (Character c in chars)
                {
                    if (c == null || c.IsDead()) continue;
                    Vector3 d = c.transform.position - pos;
                    if (Mathf.Abs(d.y) > HeightReach) continue;
                    d.y = 0f;
                    if (d.magnitude > blast) continue;
                    HitData h = new HitData();
                    h.m_damage = w.Damage;
                    h.m_damage.Modify(mult);
                    h.m_point = c.GetCenterPoint();
                    Vector3 dir = d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward;
                    h.m_dir = (dir + Vector3.up * 0.3f).normalized;
                    h.m_pushForce = w.Push * mult;
                    h.m_hitType = HitData.HitType.EnemyHit;
                    c.Damage(h);
                    hit++;
                }
            }
            else HomeApi.LogError("trap: weapon '" + z.GetString(KeyWeapon, "") + "' not found: blast without damage");
            HomeApi.Log("trap: " + z.GetString(KeyKind, "trap") + " at " + Fmt.Vec(pos) + " set off by " + Who(by) + ", " + hit + " hit");
            Vanish(nv, z.GetString(KeyFxBoom, ""));
        }

        void Vanish(ZNetView nv, string fx)
        {
            Vector3 pos = nv.transform.position;
            live.Remove(nv);
            Effects(fx, pos);
            nv.ClaimOwnership();
            ZNetScene.instance.Destroy(nv.gameObject);
        }

        static string Who(Character c)
        {
            Player pl = c as Player;
            return pl != null ? pl.GetPlayerName() : Prefabs.BaseName(c.gameObject);
        }

        // ---- read-only parts and remove -------------------------------------------------------------

        string List()
        {
            Host.RequireWorld();
            Rescan();
            if (live.Count == 0) return "no live traps in the loaded area" + (Host.IsServer ? "" : " (traps work only in a world this PC hosts)");
            Player p = Player.m_localPlayer;
            long now = NowMs();
            StringBuilder sb = new StringBuilder();
            sb.Append(live.Count).Append(" live trap").Append(live.Count == 1 ? "" : "s").Append(" (cap ").Append(MaxLive).Append(")").Append(watching ? "" : ", watcher not running").Append('\n');
            foreach (ZNetView nv in live)
            {
                ZDO z = nv.GetZDO();
                Vector3 pos = nv.transform.position;
                long left = z.GetLong(KeyEnd, 0L) - now;
                sb.Append(z.GetString(KeyKind, "?")).Append(" at ").Append(Fmt.Vec(pos));
                if (p != null) sb.Append(", ").Append(Fmt.F(Vector3.Distance(p.transform.position, pos), 1)).Append(" m from you");
                sb.Append(", ").Append(Fmt.F(Mathf.Max(0f, left / 1000f), 0)).Append(" s left")
                  .Append(now < z.GetLong(KeyArm, 0L) ? ", arming" : "").Append('\n');
            }
            return sb.ToString();
        }

        string Remove(float radius)
        {
            Host.RequireServer();
            Player p = Players.Local;
            Caps.Check("radius", radius, MaxRemoveRadius);
            Rescan();
            List<ZNetView> gone = new List<ZNetView>();
            string where;
            if (radius <= 0f)
            {
                bool aimed;
                Vector3 at = Aim.Point(p, out aimed);
                ZNetView best = null; float bestD = RemoveAimRange;
                foreach (ZNetView nv in live)
                {
                    float d = Vector3.Distance(nv.transform.position, at);
                    if (d <= bestD) { best = nv; bestD = d; }
                }
                if (best != null) gone.Add(best);
                where = "within " + Fmt.F(RemoveAimRange, 0) + " m of the crosshair " + Fmt.Vec(at);
            }
            else
            {
                foreach (ZNetView nv in live)
                    if (Vector3.Distance(nv.transform.position, p.transform.position) <= radius) gone.Add(nv);
                where = "within " + Fmt.F(radius, 0) + " m of you";
            }
            foreach (ZNetView nv in gone) Vanish(nv, nv.GetZDO().GetString(KeyFxEnd, ""));
            return "removed " + gone.Count + " trap" + (gone.Count == 1 ? "" : "s") + " " + where + "; " + live.Count + " live left";
        }

        static string Probe(string filter)
        {
            List<string> lines = new List<string>();
            foreach (GameObject g in Prefabs.All())
            {
                if (filter.Length > 0 && g.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                Trap t = g.GetComponent<Trap>();
                if (t == null) continue;
                Aoe aoe = t.m_AOE != null ? t.m_AOE.GetComponent<Aoe>() : null;
                lines.Add("TRAP " + g.name + " piece=" + (g.GetComponent<Piece>() != null ? "yes" : "no")
                    + " triggeredByPlayers=" + t.m_triggeredByPlayers + " triggeredByEnemies=" + t.m_triggeredByEnemies
                    + " rearm=" + Fmt.F(t.m_rearmCooldown, 0) + " s aoe=" + (aoe != null ? Fmt.F(aoe.m_radius, 1) + " m, damage " + Fmt.F(aoe.m_damage.GetTotalDamage(), 0) : "-"));
            }
            lines.Sort();
            if (lines.Count == 0) return "no prefab with a Trap matches '" + filter + "'";
            return Prefabs.Lines(lines, 60, "... (more)");
        }

        // ---- lifecycle ------------------------------------------------------------------------------

        protected override void OnLoad()
        {
            Current = this;
            TrapPatches.Apply(this);
            if (Player.m_localPlayer != null) EnsureWatch();
        }

        protected override void OnUnload() { Current = null; }

        protected override void OnWorldExit()
        {
            watching = false;
            live.Clear();
            weapons.Clear();
        }

        internal PatchSet PatchesForTraps { get { return Patches; } }
    }
}
