// op=despawn: removes the one networked object under the crosshair through the game's own destroy path
// (ZNetView.ClaimOwnership + ZNetScene.Destroy: the ZDO is deleted for every player, nothing drops). Host only.
// Creatures, dropped items, trees, rocks and bushes go at once; player-built pieces, ships and carts need confirm=1
// (without it the answer says what would go). Never a player, a tombstone, a location's own object (trader, altar,
// runestone, vegvisir, dungeon), or anything without a ZNetView. Tamed animals and bosses only with tamed=1 / boss=1,
// pieces another player built only with others=1 (Maksym's decisions 2026-10-25, card docs/Backlog/despawn.md).
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.Despawn
{
    public sealed class DespawnCommand : CommandBase
    {
        const float DefaultRange = 60f;
        const float MaxRange = 100f;

        static int mask = -1;

        // what the ray sees: the solid layers, terrain (stops the ray), creatures and loose items
        static int Mask
        {
            get
            {
                if (mask == -1) mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle",
                    "character", "character_net", "character_ghost", "character_noenv", "hitbox", "item");
                return mask;
            }
        }

        // the object found under the crosshair and what decides whether it may go
        sealed class Found
        {
            public ZNetView View;
            public string Name;
            public string Kind;
            public float Distance;
            public string Refusal;   // null = may go
            public bool NeedsConfirm;
        }

        public DespawnCommand()
        {
            SubFlag("look", "look=1 [range=60] (says what is under the crosshair and whether it would go, changes nothing)",
                delegate (Args a) { return Look(a); });
        }

        public override string Name { get { return "despawn"; } }
        protected override string Usage
        {
            get
            {
                return "despawn [range=60 (cap 100)] [confirm=1 (player-built pieces, ships, carts)] [tamed=1] [boss=1] [others=1 (pieces another player built)]"
                    + " - removes the object under the crosshair for everyone (creature, item, piece, tree, rock, ship); never players, tombstones or location objects; no drops, no undo; host only";
            }
        }

        protected override string Execute(Args a)
        {
            Found f = Find(a);
            if (f.Refusal != null) throw new CommandException("refused " + f.Name + " at " + Fmt.F(f.Distance, 1) + " m: " + f.Refusal);
            if (f.NeedsConfirm && !a.Flag("confirm", false))
                return "would despawn " + Describe(f) + ": add confirm=1 (player-built pieces, ships and carts)";
            f.View.ClaimOwnership();
            ZNetScene.instance.Destroy(f.View.gameObject);
            return "despawned " + Describe(f);
        }

        string Look(Args a)
        {
            Found f = Find(a);
            if (f.Refusal != null) return "would refuse " + f.Name + " at " + Fmt.F(f.Distance, 1) + " m: " + f.Refusal;
            return "would despawn " + Describe(f) + (f.NeedsConfirm ? " (needs confirm=1)" : "");
        }

        static string Describe(Found f) { return f.Name + " at " + Fmt.F(f.Distance, 1) + " m (" + f.Kind + ")"; }

        static Found Find(Args a)
        {
            Host.RequireServer();
            Player me = Players.Local;
            float range = Caps.Check("range", a.Float("range", DefaultRange), MaxRange);
            if (range <= 0f) throw new CommandException("range must be > 0");
            Camera cam = Camera.main;
            if (cam == null) throw new CommandException("no camera");

            RaycastHit[] hits = Physics.RaycastAll(cam.transform.position, cam.transform.forward, range, Mask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, delegate (RaycastHit x, RaycastHit y) { return x.distance.CompareTo(y.distance); });
            foreach (RaycastHit h in hits)
            {
                if (h.collider == null) continue;
                Player pl = h.collider.GetComponentInParent<Player>();
                if (pl == me) continue;   // the camera ray can pass through your own body
                float d = Vector3.Distance(me.transform.position, h.point);
                if (pl != null) return Refused(Prefabs.BaseName(pl.gameObject) + " " + pl.GetPlayerName(), d, "players are never despawned");
                if (h.collider.GetComponentInParent<Heightmap>() != null) break;   // the ground: nothing in front of it
                ZNetView nv = h.collider.GetComponentInParent<ZNetView>();
                if (nv == null || !nv.IsValid()) return Refused(Prefabs.BaseName(h.collider.transform.root.gameObject), d, "not a networked object (part of the world itself)");
                return Judge(nv, d, me, a);
            }
            throw new CommandException("nothing under the crosshair within " + Fmt.F(range, 0) + " m");
        }

        static Found Judge(ZNetView nv, float d, Player me, Args a)
        {
            GameObject go = nv.gameObject;
            Found f = new Found();
            f.View = nv;
            f.Name = Prefabs.BaseName(go);
            f.Distance = d;

            if (go.GetComponentInParent<TombStone>() != null) { f.Kind = "tombstone"; f.Refusal = "tombstones hold a player's items: never despawned"; return f; }
            string protectedKind = LocationPart(go);
            if (protectedKind != null) { f.Kind = protectedKind; f.Refusal = protectedKind + " belongs to a location: never despawned"; return f; }

            Character c = go.GetComponent<Character>();
            if (c != null)
            {
                f.Kind = c.IsBoss() ? "boss" : c.IsTamed() ? "tamed" : "creature";
                if (c.IsBoss() && !a.Flag("boss", false)) f.Refusal = "a boss: add boss=1";
                else if (c.IsTamed() && !a.Flag("tamed", false)) f.Refusal = "a tamed animal: add tamed=1";
                return f;
            }
            if (go.GetComponent<ItemDrop>() != null) { f.Kind = "item"; return f; }

            Piece piece = go.GetComponent<Piece>();
            bool built = piece != null && piece.IsPlacedByPlayer();
            if (go.GetComponent<Ship>() != null) { f.Kind = "ship"; f.NeedsConfirm = true; }
            else if (go.GetComponent<Vagon>() != null) { f.Kind = "cart"; f.NeedsConfirm = true; }
            else if (built) { f.Kind = "piece"; f.NeedsConfirm = true; }
            else if (go.GetComponent<TreeBase>() != null || go.GetComponent<TreeLog>() != null) f.Kind = "tree";
            else if (go.GetComponent<MineRock>() != null || go.GetComponent<MineRock5>() != null || go.GetComponent<Destructible>() != null) f.Kind = "rock or bush";
            else f.Kind = "object";

            if (built)
            {
                long creator = piece.GetCreator();
                if (creator != 0L && creator != me.GetPlayerID() && !a.Flag("others", false)) f.Refusal = "built by another player: add others=1";
            }
            else if (f.Kind != "ship" && f.Kind != "cart")
            {
                Location loc = Location.GetLocation(go.transform.position);
                if (loc != null) f.Refusal = "inside the location " + Prefabs.BaseName(loc.gameObject) + ": never despawned";
            }
            return f;
        }

        // the kind of a location's own interactive object, or null
        static string LocationPart(GameObject go)
        {
            if (go.GetComponentInParent<Trader>() != null) return "trader";
            if (go.GetComponentInParent<OfferingBowl>() != null) return "altar";
            if (go.GetComponentInParent<RuneStone>() != null) return "runestone";
            if (go.GetComponentInParent<Vegvisir>() != null) return "vegvisir";
            if (go.GetComponentInParent<Location>() != null || go.GetComponentInParent<DungeonGenerator>() != null) return "location";
            return null;
        }

        static Found Refused(string name, float d, string why)
        {
            Found f = new Found();
            f.Name = name;
            f.Kind = "refused";
            f.Distance = d;
            f.Refusal = why;
            return f;
        }
    }
}
