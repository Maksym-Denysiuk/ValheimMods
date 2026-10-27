// op=teleport: moves you through the game's own portal path. Player.TeleportTo(pos, rot, distantTeleport=true) waits 2 s, waits for
// the area, then puts you on the ground (y = ZoneSystem.GetSolidHeight + 0.5; Player.UpdateTeleport, IL 1.0.20).
// to=death: the death point of this world in your profile (the game's teleport fade stays: it loads the area).
// to=map: opens the large map and arms one left click (TeleportMapPatch); the click's world point is Minimap.ScreenToWorldPoint.
// to=player: a random spot 2 m to radius m from that player (host only: a player outside your area is read from his character ZDO).
// every mode: the ward dome (fx_guardstone_activate, its sound the only one) and a slow sphere of smoke once the screen clears, networked (to=death too: Maksym 2026-10-27) (card: docs/Spells/teleport.md, Cards).
using System;
using System.Reflection;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.TeleportCommands
{
    public sealed class TeleportCommand : CommandBase
    {
        const float DefaultRadius = 5f;
        const float MinRadius = 2f;
        const float MaxRadius = 50f;
        const float ArrivalTimeout = 30f;
        const float WatchStep = 0.1f;
        const float ArriveRadius = 2f;   // metres from the target (flat) that count as landed
        const string SmokeFx = "vfx_Place_HildirClothesRack";   // smoke_poof, full size (a small puff; a quarter was not visible, Maksym 2026-10-27)
        const int SmokePoints = 18;       // puffs on the sphere
        const float SmokeRadius = 2.6f;   // metres from its centre (Maksym 2026-10-27: twice the first 1.3)
        const float SmokeCentre = 1f;     // the centre, metres above your feet
        const int SmokeWaves = 1;         // one sphere (Maksym 2026-10-27: "play it once and make it last longer") ...
        const float SmokeTime = 3f;       // ... over this many seconds (Maksym 2026-10-27: "make it last 3 seconds")
        const float SmokeLife = 6f;       // each puff is removed after this
        const float SmokeSlow = 0.33f;    // particle speed: a third, so each puff lasts about three times as long (on this PC; others see the normal speed)
        const string DomeFx = "fx_guardstone_activate";   // the ward's blue dome pulse with its "dome shield on" sound, the one sound of the appear (Maksym 2026-10-27, from 22 sounds heard)

        const BindingFlags All = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly MethodInfo mScreenToWorld = typeof(Minimap).GetMethod("ScreenToWorldPoint", All);
        static readonly MethodInfo mSetMapMode = typeof(Minimap).GetMethod("SetMapMode", All);
        static readonly PropertyInfo pPointer = typeof(ZInput).GetProperty("pointerPosition", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        internal static TeleportCommand Current;

        bool mapArmed;
        bool mapFx = true;

        public TeleportCommand()
        {
            SubFlag("preview", "preview=1 (the appear effect where you stand, no teleport: a test aid for the look)",
                delegate (Args a)
                {
                    Host.RequireWorld();
                    Vector3 at = Players.Local.transform.position;
                    Appear(Players.Local, 0f, true);
                    return "appear effect at " + Fmt.Vec(at);
                });
        }

        public override string Name { get { return "teleport"; } }
        protected override string Usage
        {
            get
            {
                return "teleport to=death|map|player|point|aim [player=<nick> (to=player; host only)] [at=x,y,z or x,z (to=point)] [radius=5 (to=player: you land 2 m to radius m from them, cap 50)]"
                    + " [fx=1 (the ward dome and a sphere of smoke where you appear; 0 = none)]"
                    + " - moves you to your last death point in this world, to a spot you left-click on the large map (closing the map cancels), or next to another player";
            }
        }

        internal bool MapArmed { get { return mapArmed; } }

        protected override void OnLoad() { Current = this; }
        protected override void OnUnload() { mapArmed = false; if (Current == this) Current = null; }
        protected override void OnWorldExit() { mapArmed = false; }

        protected override string Execute(Args a)
        {
            string to = a.Required("to", "death, map, player, point or aim").ToLowerInvariant();
            bool fx = a.Flag("fx", true);
            if (to == "death") return Death(fx);
            if (to == "map") return Map(fx);
            if (to == "player") return ToPlayer(a, fx);
            if (to == "point") return ToPoint(a, fx);
            if (to == "aim") return ToAim(fx);
            throw new CommandException("to=" + to + " unknown: death, map, player, point or aim");
        }

        string Death(bool fx)
        {
            Host.RequireWorld();
            Player p = Players.Local;
            PlayerProfile prof = Game.instance.GetPlayerProfile();
            if (prof == null || !prof.HaveDeathPoint()) throw new CommandException("no death point in this world");
            Vector3 at = prof.GetDeathPoint();
            Go(p, at, p.transform.rotation, fx);
            return "teleporting to your death point at " + Fmt.Vec(at) + " (" + Fmt.F(Vector3.Distance(p.transform.position, at), 0) + " m)";
        }

        string Map(bool fx)
        {
            Host.RequireWorld();
            Players.Local.GetPlayerName();   // throws without a local player
            if (Minimap.instance == null) throw new CommandException("no map: load a world first");
            if (mScreenToWorld == null || mSetMapMode == null || pPointer == null) throw new CommandException("map methods not found (game update?)");
            EnsurePatched();
            mapArmed = true;
            mapFx = fx;
            SetMapMode("Large");
            return "map armed: left-click a spot to teleport there (closing the map cancels)";
        }

        // the map patches go in only once a world is loaded and the map is used (UI patch rule, spells)
        void EnsurePatched()
        {
            if (Patches.IsActive) return;
            MethodInfo click = typeof(Minimap).GetMethod("OnMapLeftClick", All);
            if (click == null) throw new CommandException("Minimap.OnMapLeftClick not found (game update?)");
            Patches.Patch(click, typeof(TeleportMapPatch).GetMethod("ClickPrefix"), null);
            Patches.Patch(mSetMapMode, null, typeof(TeleportMapPatch).GetMethod("ModePostfix"));
        }

        // the map closed (any mode but Large): the click is no longer ours
        internal void MapModeChanged()
        {
            if (mapArmed && !Minimap.IsOpen()) { mapArmed = false; HomeApi.Log("teleport: map closed, click disarmed"); }
        }

        // the armed left click: read its world point, close the map, go
        internal void MapClicked(Minimap map)
        {
            mapArmed = false;
            object ptr = pPointer.GetValue(null, null);
            Vector3 screen = ptr is Vector2 ? (Vector3)(Vector2)ptr : (Vector3)ptr;
            Vector3 at = (Vector3)mScreenToWorld.Invoke(map, new object[] { screen });
            SetMapMode("Small");
            if (at == Vector3.zero) throw new CommandException("click inside the map to teleport");
            Player p = Players.Local;
            Go(p, at, p.transform.rotation, mapFx);
            HomeApi.Log("teleport: map point " + Fmt.Vec(at) + " (" + Fmt.F(Vector3.Distance(p.transform.position, at), 0) + " m)");
        }

        static void SetMapMode(string mode)
        {
            Type t = mSetMapMode.GetParameters()[0].ParameterType;
            mSetMapMode.Invoke(Minimap.instance, new object[] { Enum.Parse(t, mode) });
        }

        // to=point&at=x,y,z (or x,z): a point you type (Maksym 2026-10-27, "teleport me to that spot"); the game puts you on the ground there
        string ToPoint(Args a, bool fx)
        {
            Host.RequireWorld();
            Player p = Players.Local;
            string raw = a.Required("at", "x,y,z or x,z");
            string[] parts = raw.Split(',');
            float x = 0f, y = 0f, z = 0f;
            System.Globalization.CultureInfo inv = System.Globalization.CultureInfo.InvariantCulture;
            System.Globalization.NumberStyles num = System.Globalization.NumberStyles.Float;
            bool ok = parts.Length == 3
                ? float.TryParse(parts[0], num, inv, out x) & float.TryParse(parts[1], num, inv, out y) & float.TryParse(parts[2], num, inv, out z)
                : parts.Length == 2 && float.TryParse(parts[0], num, inv, out x) & float.TryParse(parts[1], num, inv, out z);
            if (!ok) throw new CommandException("at=" + raw + ": write x,y,z or x,z");
            Vector3 at = new Vector3(x, y, z);
            if (new Vector2(x, z).magnitude > 10400f) throw new CommandException("at=" + raw + " is outside the world");
            float far = Vector3.Distance(p.transform.position, at);
            Go(p, at, p.transform.rotation, fx);
            return "teleporting to " + Fmt.Vec(at) + " (" + Fmt.F(far, 0) + " m)";
        }

        // to=aim: the spot under your crosshair (Kit.Aim: 60 m, solid objects and terrain; nothing hit = refused, not 8 m ahead)
        string ToAim(bool fx)
        {
            Host.RequireWorld();
            Player p = Players.Local;
            bool aimed;
            Vector3 at = Aim.Point(p, out aimed);
            if (!aimed) throw new CommandException("nothing under the crosshair within 60 m");
            float far = Vector3.Distance(p.transform.position, at);
            Go(p, at, p.transform.rotation, fx);
            return "teleporting to the crosshair at " + Fmt.Vec(at) + " (" + Fmt.F(far, 0) + " m)";
        }

        string ToPlayer(Args a, bool fx)
        {
            Host.RequireServer();
            Player p = Players.Local;
            string nick = a.Required("player", "the nickname of an online player");
            float radius = Caps.Check("radius", a.Float("radius", DefaultRadius), MaxRadius);
            if (radius < MinRadius) throw new CommandException("radius must be at least " + Fmt.F(MinRadius, 0));
            Vector3 at;
            string name;
            if (!FindPlayer(nick, out at, out name)) throw new CommandException("player '" + nick + "' is not online");
            if (string.Equals(name, p.GetPlayerName(), StringComparison.OrdinalIgnoreCase)) throw new CommandException("that is you: pick another player");
            float ang = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = UnityEngine.Random.Range(MinRadius, radius);
            Vector3 land = at + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * dist;
            Vector3 look = at - land;
            look.y = 0f;
            Go(p, land, Quaternion.LookRotation(look.sqrMagnitude > 0.01f ? look : p.transform.forward), fx);
            return "teleporting to " + name + ": " + Fmt.F(dist, 1) + " m from them at " + Fmt.Vec(land);
        }

        // a player loaded here, else the host's copy of his character ZDO
        static bool FindPlayer(string nick, out Vector3 pos, out string name)
        {
            Player near = Players.Find(nick);
            if (near != null) { pos = near.transform.position; name = near.GetPlayerName(); return true; }
            foreach (ZNet.PlayerInfo pi in ZNet.instance.GetPlayerList())
            {
                if (!string.Equals(pi.m_name, nick, StringComparison.OrdinalIgnoreCase)) continue;
                ZDO z = ZDOMan.instance.GetZDO(pi.m_characterID);
                if (z == null) break;
                pos = z.GetPosition();
                name = pi.m_name;
                return true;
            }
            pos = Vector3.zero;
            name = nick;
            return false;
        }

        void Go(Player p, Vector3 at, Quaternion rot, bool fx)
        {
            if (!p.TeleportTo(at, rot, true)) throw new CommandException("the game refused the teleport (already teleporting?)");
            WatchArrival(p, at, 0f, fx, false);
        }

        // UpdateTeleport (IL 1.0.20) moves you to the target after 2 s and then holds you there until the area is ready (8 s at least
        // for a distant teleport) before it clears IsTeleporting; the loading screen is black while IsTeleporting and then fades
        // (Hud.UpdateBlackScreen). So the fall guard starts the first moment you stand within ArriveRadius of the target, and again at
        // the end; the pop and the smoke wait for the end, when you can see (Maksym 2026-10-27: the smoke was over before the screen cleared).
        void WatchArrival(Player p, Vector3 target, float waited, bool fx, bool landed)
        {
            Later(WatchStep, delegate
            {
                if (p == null) return;
                bool teleporting = p.IsTeleporting() && waited < ArrivalTimeout;
                if (!landed && teleporting && Flat(p.transform.position - target) < ArriveRadius && p.IsOnGround())
                {
                    landed = true;
                    TeleportFx.FallGuard(this, p, TeleportFx.FallGuardTime);
                    HomeApi.Log("teleport: landed at " + Fmt.Vec(p.transform.position) + " after " + Fmt.F(waited, 1) + " s (screen still black)");
                }
                if (teleporting) { WatchArrival(p, target, waited + WatchStep, fx, landed); return; }
                Appear(p, waited, fx);
            });
        }

        // the screen clears: fall guard, the dome with its sound, and the smoke sphere
        void Appear(Player p, float waited, bool fx)
        {
            Vector3 at = p.transform.position;
            TeleportFx.FallGuard(this, p, TeleportFx.FallGuardTime);
            HomeApi.Log("teleport: appeared at " + Fmt.Vec(at) + " after " + Fmt.F(waited, 1) + " s");
            if (!fx) return;
            if (Fx.Spawn(this, DomeFx, at, 6f) == null) HomeApi.Log("teleport: " + DomeFx + " prefab missing");
            SmokeWave(at, 0);
        }

        // a sphere of smoke around you (Maksym 2026-10-27): SmokePoints puffs spread evenly over a sphere of SmokeRadius m round your
        // chest (a Fibonacci sphere), none below the ground; made again every SmokeTime / (SmokeWaves - 1) s, each wave turned, so the
        // cloud lasts SmokeTime s. Each puff carries the game's "poof" sound; all are muted here, the dome is the sound
        // (other players' copies still play theirs: a limit, check on Claude2)
        void SmokeWave(Vector3 at, int wave)
        {
            Vector3 centre = at + Vector3.up * SmokeCentre;
            float golden = Mathf.PI * (3f - Mathf.Sqrt(5f));
            float turn = wave * 0.7f;
            for (int i = 0; i < SmokePoints; i++)
            {
                float y = 1f - 2f * (i + 0.5f) / SmokePoints;
                float r = Mathf.Sqrt(1f - y * y);
                float ang = golden * i + turn;
                Vector3 pos = centre + new Vector3(Mathf.Cos(ang) * r, y, Mathf.Sin(ang) * r) * SmokeRadius;
                pos.y = Mathf.Max(pos.y, Ground.Height(pos) + 0.1f);
                GameObject puff = Fx.Spawn(this, SmokeFx, pos, SmokeLife);
                if (puff == null) { HomeApi.Log("teleport: " + SmokeFx + " prefab missing"); return; }
                Slow(puff, SmokeSlow);
                Mute(puff);
            }
            if (wave + 1 < SmokeWaves) Later(SmokeTime / Mathf.Max(1, SmokeWaves - 1), delegate { SmokeWave(at, wave + 1); });
        }

        static void Slow(GameObject go, float speed)
        {
            foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true)) { ParticleSystem.MainModule m = ps.main; m.simulationSpeed = m.simulationSpeed * speed; }
        }

        static void Mute(GameObject go)
        {
            foreach (ZSFX z in go.GetComponentsInChildren<ZSFX>(true)) z.enabled = false;
            foreach (AudioSource s in go.GetComponentsInChildren<AudioSource>(true)) { s.Stop(); s.mute = true; }
        }

        static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }
    }
}
