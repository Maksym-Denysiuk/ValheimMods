// trap: traps laid as a matrix on the ground at the crosshair (the spellshot model: one placement, a kind per type=).
// Kind mine today, the electric fence later. Host only: mines hurt other players and stay in shared world data.
// op=trap&type=mine [perrow=1] [rows=1] [spacing=2] [at=aim|self|front] [dist=3] [life=300] [trigger=1.5] [blast=3] ...
// op=trap&remove=1 [radius=0]   op=trap&detonate=1 [radius=100]   op=trap&list=1   op=trap&type=probe&filter=trap
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
    public sealed class TrapCommand : AttackCommand
    {
        internal static TrapCommand Current;

        const int MaxSide = 9;            // perrow and rows
        const int MaxLive = 6000;         // live traps in the loaded area (was 50, 400, 1200; Maksym 2026-10-25 "raise the cap")
        const int MaxField = 5000;        // mines of one minefield (field=1, a 10..100 m ring field is ~4950); more is refused with the count
        const int PlaceBatch = 250;       // mines laid per batch, one batch every PlaceGap s (thousands in one frame would stall the game)
        const float PlaceGap = 0.1f;
        const float RingGap = 1f;         // m between the trigger squares of neighbours on a ring or row and between rings or rows (Maksym 2026-10-25: 0.5, then 1)
        const float FrontDist = 5f;        // at=front: m from you to the first row (a 3 m blast stays clear of you)
        const float MaxFieldRadius = 100f;
        const float MaxLife = 3600f;
        const float MaxTrigger = 5f;
        const float MaxBlast = 10f;
        const float MaxMult = 10f;
        const float MaxRemoveRadius = 100f;
        const int DetonateWave = 25;          // detonate=1: traps set off per wave, nearest first
        const float DetonateGap = 0.1f;       // s between waves (234 blasts in one frame would stall the game)
        // per mine instead of its blast set (Maksym 2026-10-25, picked from 10 looks): the witch's violet bolt and the spawner's
        // spirit sparks, with the blood-thunder cast and a flame-burst sound
        const string DetonateFx = "fx_JotunWitch_LightningBolt_Explosion,vfx_spawn_small,sfx_staffthunderblood_cast,sfx_FireAddFuel";
        const float DetonateFxScale = 2f;     // x the look's size (the size Maksym saw and picked)
        const float DetonateFxLife = 3f;      // s, then removed (the sounds play out; prefabs without a timeout of their own, such as fx_Lightning, still go)
        const float RemoveAimRange = 2f;  // remove=1 without radius: the trap this close to the crosshair point
        const float TickSeconds = 0.2f;   // watcher step while traps are live
        const float IdleSeconds = 3f;     // watcher step with none
        const float RescanSeconds = 3f;   // find traps loaded since (areas, reload, world load)
        const float HeightReach = 2.5f;   // a character this far above or below still counts

        const string KeyKind = "hc_trap";
        const string KeyEnd = "hc_trap_end";       // world time in ms when it disappears
        const string KeyArm = "hc_trap_arm";       // world time in ms from which it can go off
        const string KeyTrigger = "hc_trap_trigger";   // a radius, or with hc_trap_square the half size along the piece's x
        const string KeyTriggerZ = "hc_trap_trigger_z"; // with hc_trap_square: the half size along the piece's z
        const string KeyShape = "hc_trap_square";       // 1 = square trigger in the piece's own axes (a floor plate)
        const float TriggerMargin = 0.3f;              // a character this far past the plate's edge still steps on it
        const float TriggerScale = 1.25f;              // the square is 25 % wider than plate + margin (Maksym 2026-10-25: 1 x 1 tile -> 2.0 m, was 1.6 m)
        const string KeyBlast = "hc_trap_blast";
        const string KeyWeapon = "hc_trap_weapon";
        const string KeyQuality = "hc_trap_quality";
        const string KeyMult = "hc_trap_mult";
        const string KeyFxBoom = "hc_trap_fxboom";
        const string KeyFxEnd = "hc_trap_fxend";
        const string KeyPush = "hc_trap_push";     // knockback force of the blast
        const string KeySE = "hc_trap_se";         // status effect put on everyone in the blast ("" = none)
        const string SetPrefix = "set:";           // hc_trap_fxboom = "set:<TrapFxSet name>" or a comma list of prefabs
        const float DefaultPush = 150f;            // SledgeStagbreaker's attack force, when the item is missing
        const string KeyPvp = "hc_trap_pvp";       // 0 = no pvp=1 (hits every player), 1 = placer's PvP was on (hits players), 2 = it was off (spares other players)
        const string KeyPlacer = "hc_trap_placer"; // the placer's player id (with pvp=1 the placer is still hit by his own mine)

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
            SubFlag("detonate", "detonate=1 [radius=100 (cap 100)] [fx=" + DetonateFx + " (the look of each blast, comma list of prefabs; own = the mine's own blast set)] [fxscale=" + Fmt.F(DetonateFxScale, 0) + " (x its size, cap 10)] (every live trap within radius m of you goes off as if stepped on, nearest first, " + DetonateWave + " every " + Fmt.F(DetonateGap, 1) + " s; host only)",
                delegate (Args a) { return Detonate(a.Float("radius", MaxRemoveRadius), a.Str("fx", DetonateFx), a.Float("fxscale", DetonateFxScale)); });
        }

        public override string Name { get { return "trap"; } }
        protected override string Usage
        {
            get
            {
                return "trap [type=mine (default; also explosive)|tar|random (a kind of the mine pool per trap: mine, tar)] [perrow=1] [rows=1] (1..9 each, the matrix is centred on the point; rows run along your look direction) [spacing=2] [scatter=0 (1 = spacing 2 x the trigger radius + 1 m, every second row shifted by half a step, a chess board: no straight lane)] [at=aim|self|front] [dist=3 (with at=self: metres ahead; with at=front, default 5: the first row that far ahead, the rows run away from you)] "
                    + "[life=300 (s, cap 3600, then it vanishes)] [arm=3 (s before it can go off)] [trigger=<m, cap 5; default the square of the plate, its footprint + 0.3 m; a number = a circle of that radius>] [blast=3 (m, cap 10)] [weapon=BowAshlands] [quality=max] [mult=2 (x the weapon damage, cap 10)] "
                    + "[prefab=<visual with a vanilla Trap; default piece_trap_troll, else fuling_trap>] [fxplace=, fxend= (comma lists of fx prefabs, none = no effect)] [fxboom=random (a blast set at random per mine: " + TrapFxSet.Names + "; or one set name, or a comma list of prefabs)] [push=<force> (default one hit of SledgeStagbreaker, 150; 0 = none)] [field=0 (1 = a minefield around you: no mines within safe=<m> (default 10), mines out to radius=<m> (default 30, cap " + MaxFieldRadius + ", above safe), on rings 2 x the trigger radius + " + Fmt.F(RingGap, 1) + " m apart, each ring circumference / that step mines, every second ring shifted by half a step (a chess board), facing the centre, cap " + MaxField + ", none on water; perrow, rows, at, dist ignored)] [pvp=0 (1 = the AttackCommand PvP rule, taken when the mine is laid: your PvP on = other players are hit whatever their own setting, off = they are spared; you, creatures and tamed animals are hit either way)]; "
                    + "mine = goes off once when any player or creature (you, other players, tamed, bosses) comes within trigger, hits everyone within blast with no player attacker (like a monster's hit, no PvP needed), then is gone; "
                    + "host only; cap 50 live traps in the loaded area; only the host's loaded area is watched. type=probe&filter=trap lists prefabs with a Trap (read-only).";
            }
        }

        protected override string Execute(Args a)
        {
            // no type = the explosive mine, the default kind (Maksym 2026-10-25: explosive is the default, Place mine = random, Shift = tar)
            string type = a.Str("type", "mine").ToLowerInvariant();
            if (type == "probe") { Host.RequireWorld(); return Probe(a.Str("filter", "trap")); }
            if (type == "fence") throw new CommandException("type=fence is planned, not built yet; built: " + TrapKind.Names);
            bool randomKind = type == "random";   // a kind of RandomPool per trap (Maksym 2026-10-25: Place mine = any kind, any look)
            TrapKind kind = randomKind ? TrapKind.Get("mine") : TrapKind.Get(type);
            if (kind == null) throw new CommandException("unknown type '" + type + "': " + TrapKind.Names + "|explosive|random (or probe)");
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
            bool square = trigger <= 0f;   // no trigger= and the kind's default 0: the plate's own square
            float blast = Caps.Check("blast", a.Float("blast", kind.BlastRadius), MaxBlast);
            float mult = Caps.Check("mult", a.Float("mult", kind.Mult), MaxMult);
            if (trigger < 0f || blast <= 0f || mult < 0f) throw new CommandException("trigger must be >= 0, blast > 0, mult >= 0");
            string weaponName = a.Str("weapon", kind.Weapon);
            string quality = a.Str("quality", "max");
            TrapWeapon w = kind.Damages || randomKind ? Weapon(weaponName, quality) : null;
            if ((kind.Damages || randomKind) && w == null) throw new CommandException("weapon '" + weaponName + "' not found or has no damage (op=list&filter=Bow)");
            string fxPlace = a.Str("fxplace", kind.FxPlace), fxBoom = a.Str("fxboom", kind.FxBoom), fxEnd = a.Str("fxend", kind.FxEnd);
            bool fxRandom = fxBoom.Equals("random", StringComparison.OrdinalIgnoreCase);
            TrapFxSet fxSet = fxRandom ? null : TrapFxSet.Get(fxBoom);
            float push = a.Float("push", HammerPush(kind));
            if (push < 0f) throw new CommandException("push must be >= 0");
            Dictionary<string, int> setsUsed = new Dictionary<string, int>();
            Dictionary<string, int> kindsUsed = new Dictionary<string, int>();

            GameObject visual = PickVisual(a.Str("prefab", ""), kind);
            Vector2 half = square ? Footprint(visual) : Vector2.zero;
            int pvpMode = PvpAsked(a) ? (HitsPlayers(a) ? 1 : 2) : 0;   // fixed when laid: the placer may be gone when it goes off

            Vector3 fwd = FlatForward(p);
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            bool aimed = true;
            bool field = a.Flag("field", false);
            float safe = 0f, outer = 0f;
            Vector3 centre;
            List<Vector3> spots = new List<Vector3>();
            if (field)
            {
                // a minefield (Maksym 2026-10-25): a ring around you, nothing inside the safe radius
                safe = Mathf.Max(0f, a.Float("safe", 10f));
                outer = Caps.Check("radius", a.Float("radius", 30f), MaxFieldRadius);
                if (outer <= safe) throw new CommandException("the minefield radius (" + Fmt.F(outer, 0) + " m) must be larger than the safe radius (" + Fmt.F(safe, 0) + " m)");
                centre = p.transform.position;
                // Maksym's rule: on every ring, count = circumference / (2 x activation radius + RingGap); the rings are that step apart
                float act = square ? Mathf.Max(half.x, half.y) : trigger;
                spacing = 2f * act + RingGap;
                spots = Ring(centre, safe, outer, act, spacing);
            }
            else
            {
                string at = a.Str("at", "aim").ToLowerInvariant();
                bool front = at == "front";   // Maksym 2026-10-25 (Rows): the block starts dist ahead of you and runs away from you
                centre = at == "self" ? p.transform.position + fwd * Mathf.Max(0f, a.Float("dist", 3f))
                    : front ? p.transform.position + fwd * Mathf.Max(0f, a.Float("dist", FrontDist))
                    : Aim.Point(p, out aimed);
                // scatter=1 (Maksym 2026-10-25, the mine field form's Rows): the ring rule, step = 2 x activation radius + RingGap along
                // a row and between rows; a shift flag flips with every new row and a shifted row starts half a step in (a chess board),
                // so there is no straight lane through the rows
                bool scatter = a.Flag("scatter", false);
                if (scatter) spacing = 2f * (square ? Mathf.Max(half.x, half.y) : trigger) + RingGap;
                bool shift = true;
                for (int i = 0; i < rows; i++)
                {
                    shift = !shift;   // row 0 unshifted, row 1 shifted, ...
                    for (int j = 0; j < perrow; j++)
                    {
                        float col = j - (perrow - 1) / 2f, row = front ? i : i - (rows - 1) / 2f;
                        if (scatter && shift) col += 0.5f;
                        Vector3 pos = centre + right * (col * spacing) + fwd * (row * spacing);
                        pos.y = GroundAt(pos);
                        spots.Add(pos);
                    }
                }
            }

            Rescan();
            int count = spots.Count;
            if (live.Count + count > MaxLive)
                throw new CommandException("live traps " + live.Count + " + " + count + " new is over the cap " + MaxLive + " (op=trap&remove=1&radius=50 or wait for them to expire)");

            long now = NowMs();
            // what each mine is, chosen now so the answer can name the kinds and looks; laid in batches below
            int count0 = spots.Count;
            TrapKind[] kinds = new TrapKind[count0];
            TrapFxSet[] sets = new TrapFxSet[count0];
            for (int i = 0; i < count0; i++)
            {
                TrapKind k = randomKind ? TrapKind.Random() : kind;
                kinds[i] = k;
                int kc; kindsUsed.TryGetValue(k.Name, out kc); kindsUsed[k.Name] = kc + 1;
                // the blast look is fixed when laid: a kind with its own look keeps it (tar); else a set (random per mine,
                // or the named one) or the given prefab list
                TrapFxSet own = k.Damages ? null : TrapFxSet.Get(k.FxBoom);
                TrapFxSet set = own != null ? own : fxRandom ? TrapFxSet.Random() : fxSet;
                sets[i] = set;
                if (set != null) { int c; setsUsed.TryGetValue(set.Name, out c); setsUsed[set.Name] = c + 1; }
            }
            int placed = 0, batches = (count0 + PlaceBatch - 1) / PlaceBatch;
            string firstErr = null;
            for (int b = 0; b < batches; b++)
            {
                int from = b * PlaceBatch, to = Mathf.Min(count0, from + PlaceBatch);
                bool lastBatch = b == batches - 1;
                Action lay = delegate
                {
                    for (int i = from; i < to; i++)
                    {
                        Vector3 pos = spots[i];
                        // a field mine faces the centre, so its square lines up with its ring neighbours (the 0.5 m gap holds)
                        Vector3 toC = new Vector3(centre.x - pos.x, 0f, centre.z - pos.z);
                        Quaternion rot = field && toC.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toC) : Quaternion.LookRotation(fwd);
                        try
                        {
                            GameObject go = (GameObject)UnityEngine.Object.Instantiate(visual, pos, rot);
                            ZNetView nv = go.GetComponent<ZNetView>();
                            if (nv == null || !nv.IsValid()) { Fx.Destroy(go); throw new CommandException("visual has no valid ZNetView"); }
                            ZDO z = nv.GetZDO();
                            TrapKind k = kinds[i];
                            TrapFxSet set = sets[i];
                            z.Set(KeyKind, k.Name);
                            z.Set(KeySE, k.StatusEffect);
                            z.Set(KeyEnd, now + (long)(life * 1000f));
                            z.Set(KeyArm, now + (long)(arm * 1000f));
                            z.Set(KeyTrigger, square ? half.x : trigger);
                            z.Set(KeyTriggerZ, square ? half.y : trigger);
                            z.Set(KeyShape, square ? 1 : 0);
                            z.Set(KeyBlast, blast);
                            z.Set(KeyWeapon, k.Damages ? weaponName : "");
                            z.Set(KeyQuality, quality);
                            z.Set(KeyMult, k.Damages ? mult : 0f);
                            z.Set(KeyFxBoom, set != null ? SetPrefix + set.Name : fxBoom);
                            z.Set(KeyPush, k.PushItem != null ? push : 0f);
                            z.Set(KeyFxEnd, fxEnd);
                            z.Set(KeyPvp, pvpMode);
                            z.Set(KeyPlacer, p.GetPlayerID());
                            Inert(go, z);
                            NoGrowth(go);
                            Effects(fxPlace, pos);
                            live.Add(nv);
                            placed++;
                        }
                        catch (Exception e)
                        {
                            if (firstErr == null) firstErr = e.Message;
                        }
                    }
                    if (lastBatch && batches > 1) HomeApi.Log("trap: laid " + placed + "/" + count0 + " in " + batches + " batches" + (firstErr != null ? "; first error: " + firstErr : ""));
                };
                if (b == 0) lay(); else Later(b * PlaceGap, lay);
            }
            EnsureWatch();

            HitData.DamageTypes dmg = w.Damage;
            dmg.Modify(mult);
            string laying = batches > 1 ? "laying " + count0 + " in " + batches + " batches over " + Fmt.F((batches - 1) * PlaceGap, 1) + " s (log: trap: laid), first " : "placed ";
            return laying + placed + " " + (randomKind ? "trap" : kind.Name) + (placed == 1 ? "" : "s") + (randomKind ? " (" + Looks(kindsUsed, "") + ")" : "")
                + (field ? " (minefield around you: safe " + Fmt.F(safe, 0) + " m, out to " + Fmt.F(outer, 0) + " m, spacing about " + Fmt.F(spacing, 1) + " m)"
                         : " (" + perrow + "x" + rows + ", spacing " + Fmt.F(spacing, 1) + " m)") + " at " + Fmt.Vec(centre)
                + (aimed ? "" : " (nothing aimed at: 8 m ahead)") + ", visual " + visual.name + ", life " + Fmt.F(life, 0) + " s, arms after " + Fmt.F(arm, 1) + " s, trigger "
                + (square ? Fmt.F(half.x * 2f, 1) + " x " + Fmt.F(half.y * 2f, 1) + " m square" : Fmt.F(trigger, 1) + " m") + ", blast " + Fmt.F(blast, 1) + " m, damage " + Fmt.F(dmg.GetTotalDamage(), 0) + " (" + Fmt.F(mult, 1) + " x " + w.Label + "), push " + Fmt.F(push, 0) + ", blast look " + Looks(setsUsed, fxBoom) + "; live traps "
                + live.Count + "/" + MaxLive + (pvpMode == 0 ? "; hits players and creatures alike" : pvpMode == 1 ? "; other players hit (your PvP on)" : "; other players spared (your PvP off)")
                + (firstErr != null ? " ; first error: " + firstErr : "");
        }

        // ---- placement helpers ----------------------------------------------------------------------

        // the spots of a minefield (Maksym 2026-10-25): rings from safe + act out to outer - act, 'step' apart; on a ring of radius
        // r, floor(2 pi r / step) mines evenly spaced from one random start angle for the field; a shift flag flips with every new
        // ring and a shifted ring starts half a step further round (a chess board, no straight lane through the rings);
        // on the ground, never on water. Over MaxField is refused with the count.
        static List<Vector3> Ring(Vector3 c, float safe, float outer, float act, float step)
        {
            List<Vector3> r = new List<Vector3>();
            float water = ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f;
            int need = 0;
            for (float rr = safe + act; rr <= outer - act + 0.01f; rr += step) need += Mathf.FloorToInt(2f * Mathf.PI * rr / step);
            if (need > MaxField)
                throw new CommandException("the minefield " + Fmt.F(safe, 0) + ".." + Fmt.F(outer, 0) + " m needs " + need + " mines (" + Fmt.F(step, 1) + " m apart), over the cap " + MaxField + ": a smaller radius or a larger safe radius");
            float start = UnityEngine.Random.Range(0f, 2f * Mathf.PI);
            bool shift = true;
            for (float rr = safe + act; rr <= outer - act + 0.01f; rr += step)
            {
                int n = Mathf.FloorToInt(2f * Mathf.PI * rr / step);
                shift = !shift;   // ring 0 unshifted, ring 1 shifted, ...
                float a0 = start + (shift ? 0.5f * 2f * Mathf.PI / n : 0f);
                for (int k = 0; k < n; k++)
                {
                    float ang = a0 + k * 2f * Mathf.PI / n;
                    Vector3 pos = new Vector3(c.x + Mathf.Cos(ang) * rr, 0f, c.z + Mathf.Sin(ang) * rr);
                    pos.y = GroundAt(pos);
                    if (pos.y < water) continue;
                    r.Add(pos);
                }
            }
            return r;
        }

        // a sapling as the look must never grow into a tree (the tree would carry no mine mark): its Plant goes on the host,
        // which is the only game that grows plants; done again on every rescan, since a reload brings the Plant back
        static void NoGrowth(GameObject go)
        {
            if (go == null) return;
            Plant pl = go.GetComponent<Plant>();
            if (pl != null) UnityEngine.Object.Destroy(pl);
        }

        GameObject PickVisual(string given, TrapKind kind)
        {
            List<string> names = new List<string>();
            if (given.Length > 0) names.Add(given); else names.AddRange(kind.Visuals);
            foreach (string n in names)
            {
                GameObject g = Prefabs.Get(n);
                if (g == null) continue;
                if (g.GetComponent<ZNetView>() == null)
                    throw new CommandException("prefab '" + n + "' is not networked (no ZNetView); the watcher finds traps again by their ZDO mark");
                return g;
            }
            throw new CommandException("no trap look found (tried " + string.Join(", ", names.ToArray()) + "); prefab=<a networked look, e.g. wood_floor, piece_trap_troll or Beech_Sapling>");
        }

        // half size of the plate's footprint (its widest BoxCollider, x and z) plus TriggerMargin, x TriggerScale; 0.5 m without a box
        static Vector2 Footprint(GameObject visual)
        {
            float hx = 0.5f, hz = 0.5f, best = 0f;
            foreach (BoxCollider b in visual.GetComponentsInChildren<BoxCollider>(true))
            {
                Vector3 sz = Vector3.Scale(b.size, b.transform.lossyScale);
                if (sz.x * sz.z <= best) continue;
                best = sz.x * sz.z; hx = sz.x / 2f; hz = sz.z / 2f;
            }
            return new Vector2((hx + TriggerMargin) * TriggerScale, (hz + TriggerMargin) * TriggerScale);
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
            if (t == null) return;   // a plain piece (the floor plate) has nothing to switch off
            foreach (MethodInfo m in typeof(Trap).GetMethods(Refl.InstanceAll))
                if (m.Name == "UpdateState" && m.GetParameters().Length == 0)
                {
                    try { m.Invoke(t, null); } catch (Exception) { }   // only refreshes the look
                    break;
                }
            OpenLook(t);
        }

        // the armed (open) model of the vanilla trap, while its ZDO state stays 0 (unarmed, so the vanilla Trap never fires):
        // Trap.UpdateState(int) only switches m_visualArmed / m_visualUnarmed (Maksym 2026-10-25: the mine looks open).
        // Local look: a client without HomeCheat sees it closed; a state change RPC would reset it (a mine never gets one).
        static void OpenLook(Trap t)
        {
            if (t == null) return;
            foreach (MethodInfo m in typeof(Trap).GetMethods(Refl.InstanceAll))
            {
                if (m.Name != "UpdateState") continue;
                ParameterInfo[] ps = m.GetParameters();
                if (ps.Length != 1 || ps[0].ParameterType != typeof(int)) continue;
                try { m.Invoke(t, new object[] { 1 }); } catch (Exception) { }
                break;
            }
        }

        // "glacier, dynamite x2" for the sets drawn, else the prefab list given
        static string Looks(Dictionary<string, int> used, string fxBoom)
        {
            if (used.Count == 0) return fxBoom.Length == 0 ? "none" : fxBoom;
            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, int> kv in used) parts.Add(kv.Value > 1 ? kv.Key + " x" + kv.Value : kv.Key);
            return string.Join(", ", parts.ToArray());
        }

        // one hit of the kind's push item (its attack force), else DefaultPush
        static float HammerPush(TrapKind kind)
        {
            if (kind.PushItem == null) return DefaultPush;
            TrapWeapon w = TrapWeapon.Pick(kind.PushItem, "1");
            return w != null && w.Push > 0f ? w.Push : DefaultPush;
        }

        void Effects(string list, Vector3 pos)
        {
            if (list.Length == 0 || list == "none") return;
            if (list.StartsWith(SetPrefix))
            {
                TrapFxSet set = TrapFxSet.Get(list.Substring(SetPrefix.Length));
                if (set == null) { HomeApi.Log("trap: blast set '" + list + "' not known"); return; }
                foreach (string n in set.Prefabs)
                {
                    GameObject go = Fx.Spawn(this, n, pos, 12f);
                    if (go == null) HomeApi.Log("trap: effect prefab '" + n + "' not found");
                    else TrapFxSet.Tune(go, set.Scale, set.Speed);
                }
                return;
            }
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

        // every loaded object with the hc_trap mark, every 3 s
        void Rescan()
        {
            nextRescan = Time.time + RescanSeconds;
            live.Clear();
            // every loaded networked object with the mark (a look may be a piece, a trap or a sapling, which has no Piece)
            foreach (ZNetView nv in UnityEngine.Object.FindObjectsOfType<ZNetView>())
            {
                if (nv == null || !nv.IsValid() || nv.GetZDO().GetString(KeyKind, "").Length == 0) continue;
                live.Add(nv);
                OpenLook(nv.GetComponent<Trap>());
                NoGrowth(nv.gameObject);
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
                Character by = Triggered(nv.transform, z, chars, z.GetInt(KeyPvp, 0) == 2, z.GetLong(KeyPlacer, 0L));
                if (by != null) Boom(nv, z, pos, by, chars);
            }
        }

        // the first living character within the trigger radius (a point trap; the fence will check a segment here)
        // (a spared player, pvp=1 laid with PvP off, walks over it safely)
        static Character Triggered(Transform tr, ZDO z, List<Character> chars, bool spareOthers, long placer)
        {
            Vector3 pos = tr.position;
            float radius = z.GetFloat(KeyTrigger, 1.5f), radiusZ = z.GetFloat(KeyTriggerZ, radius);
            bool square = z.GetInt(KeyShape, 0) == 1;
            foreach (Character c in chars)
            {
                if (c == null || c.IsDead()) continue;
                Player pl = c as Player;
                if (spareOthers && pl != null && pl.GetPlayerID() != placer) continue;
                Vector3 d = c.transform.position - pos;
                if (Mathf.Abs(d.y) > HeightReach) continue;
                d.y = 0f;
                if (square)
                {
                    Vector3 l = Quaternion.Inverse(tr.rotation) * d;
                    if (Mathf.Abs(l.x) <= radius && Mathf.Abs(l.z) <= radiusZ) return c;
                }
                else if (d.magnitude <= radius) return c;
            }
            return null;
        }

        // every living character within the blast radius takes the weapon's damage x mult, with no attacker (like a
        // monster's hit, so players are hit without PvP); the effect plays, the trap is gone
        void Boom(ZNetView nv, ZDO z, Vector3 pos, Character by, List<Character> chars) { Boom(nv, z, pos, by, chars, null, 1f); }

        // look: null = the mine's own blast (stored when laid); else these prefabs, x lookScale, for DetonateFxLife s (detonate)
        void Boom(ZNetView nv, ZDO z, Vector3 pos, Character by, List<Character> chars, string look, float lookScale)
        {
            float blast = z.GetFloat(KeyBlast, 3f);
            string se = z.GetString(KeySE, "");
            TrapWeapon w = se.Length > 0 ? null : Weapon(z.GetString(KeyWeapon, "BowAshlands"), z.GetString(KeyQuality, "max"));
            float mult = z.GetFloat(KeyMult, 2f);
            bool spareOthers = z.GetInt(KeyPvp, 0) == 2;
            long placer = z.GetLong(KeyPlacer, 0L);
            int hit = 0;
            if (w != null || se.Length > 0)
            {
                foreach (Character c in chars)
                {
                    if (c == null || c.IsDead()) continue;
                    Vector3 d = c.transform.position - pos;
                    if (Mathf.Abs(d.y) > HeightReach) continue;
                    d.y = 0f;
                    if (d.magnitude > blast) continue;
                    Player pl = c as Player;
                    if (spareOthers && pl != null && pl.GetPlayerID() != placer) continue;   // pvp=1 laid with PvP off
                    HitData h = new HitData();
                    if (w != null) { h.m_damage = w.Damage; h.m_damage.Modify(mult); }
                    if (se.Length > 0) h.m_statusEffectHash = se.GetStableHashCode();   // applied by the character's owner with the hit
                    h.m_point = c.GetCenterPoint();
                    Vector3 dir = d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward;
                    h.m_dir = (dir + Vector3.up * 0.3f).normalized;
                    h.m_pushForce = z.GetFloat(KeyPush, se.Length > 0 ? 0f : DefaultPush);
                    h.m_hitType = HitData.HitType.EnemyHit;
                    c.Damage(h);
                    hit++;
                }
            }
            else HomeApi.LogError("trap: weapon '" + z.GetString(KeyWeapon, "") + "' not found: blast without damage");
            string own = z.GetString(KeyFxBoom, "");
            HomeApi.Log("trap: " + z.GetString(KeyKind, "trap") + " at " + Fmt.Vec(pos) + " set off by " + Who(by) + ", " + hit + " hit"
                + (look != null ? ", look " + look : own.StartsWith(SetPrefix) ? ", blast " + own.Substring(SetPrefix.Length) : ""));
            if (look == null) { Vanish(nv, own); return; }
            Vanish(nv, "none");
            foreach (string n in look.Split(','))
            {
                if (n.Trim().Length == 0) continue;
                GameObject g = Fx.Spawn(this, n.Trim(), pos, DetonateFxLife);
                if (g == null) HomeApi.Log("trap: effect prefab '" + n.Trim() + "' not found");
                else if (lookScale != 1f) TrapFxSet.Tune(g, lookScale, 1f);
            }
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

        // detonate (Maksym 2026-10-25): the traps within radius of you go off as if stepped on (Boom, the same hits and pvp rule),
        // set off by you, nearest first in waves
        string Detonate(float radius, string fx, float fxScale)
        {
            Caps.Check("fxscale", fxScale, 10f);
            if (fxScale <= 0f) throw new CommandException("fxscale must be > 0");
            string look = fx.Equals("own", StringComparison.OrdinalIgnoreCase) ? null : fx;
            Host.RequireServer();
            Player p = Players.Local;
            Caps.Check("radius", radius, MaxRemoveRadius);
            if (radius <= 0f) throw new CommandException("radius must be > 0");
            Rescan();
            Vector3 me = p.transform.position;
            List<ZNetView> todo = new List<ZNetView>();
            foreach (ZNetView nv in live) if (Vector3.Distance(nv.transform.position, me) <= radius) todo.Add(nv);
            if (todo.Count == 0) return "no live traps within " + Fmt.F(radius, 0) + " m of you";
            todo.Sort(delegate (ZNetView x, ZNetView y) { return Vector3.Distance(x.transform.position, me).CompareTo(Vector3.Distance(y.transform.position, me)); });
            int waves = (todo.Count + DetonateWave - 1) / DetonateWave;
            for (int b = 0; b < waves; b++)
            {
                int from = b * DetonateWave, to = Mathf.Min(todo.Count, from + DetonateWave);
                Action wave = delegate
                {
                    Player by = Player.m_localPlayer;
                    if (by == null) return;
                    List<Character> chars = Character.GetAllCharacters();
                    for (int i = from; i < to; i++)
                    {
                        ZNetView nv = todo[i];
                        if (nv == null || !nv.IsValid()) continue;   // already gone (stepped on, expired, removed)
                        Boom(nv, nv.GetZDO(), nv.transform.position, by, chars, look, fxScale);
                    }
                };
                if (b == 0) wave(); else Later(b * DetonateGap, wave);
            }
            return "detonating " + todo.Count + " trap" + (todo.Count == 1 ? "" : "s") + " within " + Fmt.F(radius, 0) + " m of you"
                + (waves > 1 ? ", nearest first, " + waves + " waves over " + Fmt.F((waves - 1) * DetonateGap, 1) + " s" : "")
                + ", look " + (look != null ? look + " x" + Fmt.F(fxScale, 1) : "each mine's own blast");
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
