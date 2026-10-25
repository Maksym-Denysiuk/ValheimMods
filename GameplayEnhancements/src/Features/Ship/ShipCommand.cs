// op=ship: sail force multiplier for the ship the local player is on (Ship.GetLocalShip).
// Multiplies the instance's m_sailForceFactor (sail push, used in Ship.GetSailForce) and m_backwardForce (paddling back,
// used in Ship.CustomFixedUpdate) from the values it had before the first change; speed=1 puts them back.
// Ship physics run on the ship's owner (the one steering), so a change only works while this PC owns the ship.
// Nothing is saved: the fields live on this PC's ship instance; OnUnload restores every changed ship.
using System.Collections.Generic;
using System.Text;
using HomeCheat.Kit;

namespace HomeCheat.Features.ShipCommands
{
    public sealed class ShipCommand : CommandBase
    {
        const float MaxSpeed = 10f;

        // original values of every ship this build changed, keyed by the ship instance
        sealed class Original
        {
            public float Sail;
            public float Backward;
            public float Factor;
        }

        readonly Dictionary<Ship, Original> changed = new Dictionary<Ship, Original>();

        public ShipCommand()
        {
            SubFlag("status", "status=1 (the ship you are on: owner, factor, speed; changes nothing)",
                delegate (Args a) { return Status(); });
        }

        public override string Name { get { return "ship"; } }

        protected override string Usage
        {
            get
            {
                return "ship speed=<1..10, cap 10> - multiplies the sail force and the backward paddle force of the ship you are on; speed=1 restores;"
                    + " only while this PC owns the ship (you steer it) and hosts the world; nothing is saved, op=reload restores every changed ship";
            }
        }

        protected override string Execute(Args a)
        {
            Host.RequireServer();
            a.Required("speed", "speed=<1..10> (1 = normal)");
            float speed = a.Float("speed", -1f);
            if (speed < 1f) throw new CommandException("speed must be 1..10 (1 = normal), got " + Fmt.F(speed, 1));
            Caps.Check("speed", speed, MaxSpeed);

            Ship ship = Ship.GetLocalShip();
            if (ship == null) throw new CommandException("not on a ship: board one first");
            string name = Prefabs.BaseName(ship.gameObject);

            Original orig;
            bool known = changed.TryGetValue(ship, out orig);
            if (speed == 1f)
            {
                if (!known) return name + ": already normal (speed=1)";
                Restore(ship, orig);
                changed.Remove(ship);
                return name + ": restored to normal (sail force factor " + Fmt.F(orig.Sail, 3) + ")";
            }
            if (!ship.IsOwner()) throw new CommandException(name + ": another player owns this ship; take the helm first (physics run on the owner)");

            if (!known)
            {
                orig = new Original();
                orig.Sail = ship.m_sailForceFactor;
                orig.Backward = ship.m_backwardForce;
                changed[ship] = orig;
            }
            orig.Factor = speed;
            ship.m_sailForceFactor = orig.Sail * speed;
            ship.m_backwardForce = orig.Backward * speed;
            return name + ": speed x" + Fmt.F(speed, 1) + " (sail force factor " + Fmt.F(orig.Sail, 3) + " -> " + Fmt.F(ship.m_sailForceFactor, 3)
                + ", backward force " + Fmt.F(orig.Backward, 1) + " -> " + Fmt.F(ship.m_backwardForce, 1) + ")";
        }

        string Status()
        {
            Host.RequireWorld();
            Ship ship = Ship.GetLocalShip();
            StringBuilder sb = new StringBuilder();
            if (ship == null) sb.Append("not on a ship");
            else
            {
                Original orig;
                bool known = changed.TryGetValue(ship, out orig);
                sb.Append(Prefabs.BaseName(ship.gameObject)).Append(": speed x").Append(known ? Fmt.F(orig.Factor, 1) : "1.0")
                    .Append(", owner ").Append(ship.IsOwner() ? "this PC" : "another player")
                    .Append(", moving ").Append(Fmt.F(ship.GetSpeed(), 1)).Append(" m/s")
                    .Append("\n  sail force factor ").Append(Fmt.F(ship.m_sailForceFactor, 3))
                    .Append(", backward force ").Append(Fmt.F(ship.m_backwardForce, 1));
                if (known) sb.Append(" (normal ").Append(Fmt.F(orig.Sail, 3)).Append(", ").Append(Fmt.F(orig.Backward, 1)).Append(")");
            }
            int others = 0;
            foreach (Ship s in changed.Keys) if (s != null && s != ship) others++;
            if (others > 0) sb.Append("\n  ").Append(others).Append(" other ship(s) still changed (op=reload restores them)");
            return sb.ToString();
        }

        static void Restore(Ship ship, Original orig)
        {
            if (ship == null) return;
            ship.m_sailForceFactor = orig.Sail;
            ship.m_backwardForce = orig.Backward;
        }

        protected override void OnUnload()
        {
            int n = 0;
            foreach (KeyValuePair<Ship, Original> kv in changed)
                if (kv.Key != null) { Restore(kv.Key, kv.Value); n++; }
            changed.Clear();
            if (n > 0) HomeApi.Log("ship: restored " + n + " ship(s) on unload");
        }

        // the ships go with the world; nothing to restore
        protected override void OnWorldExit() { changed.Clear(); }
    }
}
