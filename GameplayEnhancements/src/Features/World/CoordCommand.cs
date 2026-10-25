using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // coord: stores named points (start, end, any other name) for commands that work between two points
    // (path from=coords, later cultivate / field / plant area=coords and dome). The points live in the core (Kit.Coords),
    // per world, in BepInEx/config/homecheat/coords.state, so they survive op=reload and game restarts. Only this PC knows them.
    // A short local marker shows each point (marker=none for none).
    public sealed class CoordCommand : CommandBase
    {
        const string DefaultMarker = "vfx_Potion_health_medium";
        const float MarkerSeconds = 10f;

        public CoordCommand()
        {
            SubAction("set", "set=start|end|<name> [aim=0 (1 = the crosshair instead of your feet)] [marker=" + DefaultMarker + "|none]",
                delegate (Args a) { return Set(a); });
            SubFlag("show", "show=1 [marker=...] (every point of this world, distance and rectangle of start-end)",
                delegate (Args a) { return Show(a); });
            SubFlag("get", "get=1 (machine-readable: start=x,y,z end=x,y,z, unset points left out; read-only)",
                delegate (Args a) { return Get(); });
            SubFlag("clear", "clear=1 [name=<point> (only that one; default all points of this world)]",
                delegate (Args a) { return Clear(a); });
        }

        public override string Name { get { return "coord"; } }
        protected override string Usage
        {
            get { return "coord set=start|end|<name> [aim=0] | show=1 | get=1 | clear=1 [name=] - points for other commands, kept per world (max " + PointStore.MaxPointsPerWorld + "); no parameter = show"; }
        }

        protected override string Execute(Args a) { return Show(a); }

        string Set(Args a)
        {
            Host.RequireWorld();
            Player p = Players.Local;
            string name = a.Str("set", Coords.Start).ToLowerInvariant();
            bool aimed = false;
            Vector3 pos = a.Flag("aim", false) ? Aim.Point(p, out aimed) : p.transform.position;
            Coords.Set(name, pos);
            Mark(a, pos);
            string where = a.Flag("aim", false) ? (aimed ? " (crosshair)" : " (no crosshair hit, 8 m ahead)") : " (your feet)";
            return "coord " + name + " = " + Fmt.Vec(pos) + where + PairLine(" ");
        }

        string Show(Args a)
        {
            string world = Host.RequireWorld();
            List<string> names = Coords.Names();
            if (names.Count == 0) return "no points in world " + world + ": op=coord&set=start, then op=coord&set=end";
            List<string> parts = new List<string>();
            foreach (string n in names)
            {
                Vector3 pos;
                if (!Coords.Get(n, out pos)) continue;
                parts.Add(n + "=" + Fmt.Vec(pos));
                Mark(a, pos);
            }
            return names.Count + " point(s) in world " + world + ": " + string.Join(" ", parts.ToArray()) + PairLine("; ");
        }

        string Get()
        {
            Host.RequireWorld();
            List<string> parts = new List<string>();
            foreach (string n in Coords.Names())
            {
                Vector3 pos;
                if (Coords.Get(n, out pos)) parts.Add(n + "=" + Fmt.Vec(pos));
            }
            return string.Join(" ", parts.ToArray());
        }

        string Clear(Args a)
        {
            string world = Host.RequireWorld();
            string name = a.Str("name", "").ToLowerInvariant();
            if (name.Length > 0)
                return Coords.Clear(name) ? "coord " + name + " cleared in world " + world : "coord " + name + " was not set in world " + world;
            return "cleared " + Coords.ClearAll() + " point(s) in world " + world;
        }

        // "start-end 20.3 m, rectangle 14.0 x 14.4 m (dx x dz)" once both are set, else ""
        static string PairLine(string sep)
        {
            Vector3 s, e;
            if (!Coords.Get(Coords.Start, out s) || !Coords.Get(Coords.End, out e)) return "";
            Vector3 flat = e - s; flat.y = 0f;
            return sep + "start-end " + Fmt.F(flat.magnitude, 1) + " m, rectangle " + Fmt.F(Mathf.Abs(flat.x), 1) + " x " + Fmt.F(Mathf.Abs(flat.z), 1) + " m (dx x dz)";
        }

        void Mark(Args a, Vector3 pos)
        {
            string marker = a.Str("marker", DefaultMarker);
            if (marker == "none" || marker == "0") return;
            if (Fx.Spawn(this, marker, pos, MarkerSeconds) == null) HomeApi.Log("coord: no marker prefab '" + marker + "'");
        }
    }
}
