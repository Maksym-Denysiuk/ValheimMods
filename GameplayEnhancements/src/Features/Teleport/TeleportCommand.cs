// op=teleport: moves you through the game's own portal path. Player.TeleportTo(pos, rot, distantTeleport=true) waits 2 s, waits for
// the area, then puts you on the ground (y = ZoneSystem.GetSolidHeight + 0.5; Player.UpdateTeleport, IL 1.0.20).
// to=death: the death point of this world in your profile; no effect of ours (the game's teleport fade stays: it loads the area).
// to=map: opens the large map and arms one left click (TeleportMapPatch); the click's world point is Minimap.ScreenToWorldPoint.
// to=player: a random spot 2 m to radius m from that player (host only: a player outside your area is read from his character ZDO).
// map and player: a smoke puff and a little pop at your feet once you have arrived, networked (card docs/Backlog/teleport.md).
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
        const float WatchStep = 0.2f;
        const string SmokeFx = "vfx_Place_HildirClothesRack";   // smoke_poof
        const string PopFx = "sfx_cooking_station_take";        // Poof_CloudDisappear

        const BindingFlags All = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly MethodInfo mScreenToWorld = typeof(Minimap).GetMethod("ScreenToWorldPoint", All);
        static readonly MethodInfo mSetMapMode = typeof(Minimap).GetMethod("SetMapMode", All);
        static readonly PropertyInfo pPointer = typeof(ZInput).GetProperty("pointerPosition", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        internal static TeleportCommand Current;

        bool mapArmed;
        bool mapFx = true;

        public override string Name { get { return "teleport"; } }
        protected override string Usage
        {
            get
            {
                return "teleport to=death|map|player [player=<nick> (to=player; host only)] [radius=5 (to=player: you land 2 m to radius m from them, cap 50)]"
                    + " [fx=1 (map, player: smoke and a little pop where you appear; 0 = none)]"
                    + " - moves you to your last death point in this world, to a spot you left-click on the large map (closing the map cancels), or next to another player";
            }
        }

        internal bool MapArmed { get { return mapArmed; } }

        protected override void OnLoad() { Current = this; }
        protected override void OnUnload() { mapArmed = false; if (Current == this) Current = null; }
        protected override void OnWorldExit() { mapArmed = false; }

        protected override string Execute(Args a)
        {
            string to = a.Required("to", "death, map or player").ToLowerInvariant();
            bool fx = a.Flag("fx", true);
            if (to == "death") return Death();
            if (to == "map") return Map(fx);
            if (to == "player") return ToPlayer(a, fx);
            throw new CommandException("to=" + to + " unknown: death, map or player");
        }

        string Death()
        {
            Host.RequireWorld();
            Player p = Players.Local;
            PlayerProfile prof = Game.instance.GetPlayerProfile();
            if (prof == null || !prof.HaveDeathPoint()) throw new CommandException("no death point in this world");
            Vector3 at = prof.GetDeathPoint();
            Go(p, at, p.transform.rotation, false);
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
            if (fx) WatchArrival(p, 0f);
        }

        // the appear effect waits until the game has put you down
        void WatchArrival(Player p, float waited)
        {
            Later(WatchStep, delegate
            {
                if (p == null) return;
                if (p.IsTeleporting() && waited < ArrivalTimeout) { WatchArrival(p, waited + WatchStep); return; }
                Vector3 at = p.transform.position;
                if (Fx.Spawn(this, SmokeFx, at, 8f) == null) HomeApi.Log("teleport: " + SmokeFx + " prefab missing");
                if (Fx.Spawn(this, PopFx, at + Vector3.up, 4f) == null) HomeApi.Log("teleport: " + PopFx + " prefab missing");
                HomeApi.Log("teleport: arrived at " + Fmt.Vec(at) + " after " + Fmt.F(waited, 1) + " s");
            });
        }
    }
}
