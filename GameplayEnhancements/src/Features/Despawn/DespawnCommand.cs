// op=despawn: removes the one networked object under the crosshair through the game's own destroy path
// (ZNetView.ClaimOwnership + ZNetScene.Destroy: the ZDO is deleted for every player, nothing drops). Host only.
// Every allowed object goes at once (no confirm step: Maksym 2026-10-25; look=1 previews). Never a player, a tombstone, a location's own object (trader, altar,
// runestone, vegvisir, dungeon), or anything without a ZNetView. Tamed animals and bosses only with tamed=1 / boss=1,
// pieces another player built only with others=1 (Maksym's decisions 2026-10-25, page: docs/HomeCheat-plugin/despawn.md).
// inloc=1 (Maksym 2026-11-03, spell 6.8): the location rule shrinks to the start temple's and the trader camps' own objects
// (a ZNetView child of that location's prefab, same prefab name as Utils.GetPrefabName gives, within 0.5 m of where the game spawned it) and the traders;
// everything else inside a location radius may go (your items at the spawn, other locations' runestones and altars).
using System.Collections.Generic;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.Despawn
{
    public sealed class DespawnCommand : CommandBase
    {
        const float DefaultRange = 60f;
        const float MaxRange = 100f;
        const float OwnMatch = 0.5f;   // m: how far a location's own object may be from where the game spawned it

        // locations whose own objects stay with inloc=1 (prefab names read from the game's asset data 2026-11-03)
        static readonly string[] KeptLocations = new string[] { "StartTemple", "Vendor_BlackForest", "Hildir_camp", "BogWitch_Camp" };

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
            public string Note;      // inloc=1: which location rule applied (look=1 shows it)
        }

        // a ZNetView child of a location prefab: its prefab name and position in the prefab's own frame
        sealed class OwnObject
        {
            public string Name;
            public Vector3 Local;
        }

        // per location prefab name, read once per game start (prefabs do not change while the game runs)
        static readonly Dictionary<string, List<OwnObject>> ownCache = new Dictionary<string, List<OwnObject>>();

        public DespawnCommand()
        {
            SubFlag("look", "look=1 [range=60] [inloc=1] (says what is under the crosshair and whether it would go, changes nothing)",
                delegate (Args a) { return Look(a); });
        }

        public override string Name { get { return "despawn"; } }
        protected override string Usage
        {
            get
            {
                return "despawn [range=60 (cap 100)] [tamed=1] [boss=1] [others=1 (pieces another player built)]"
                    + " [inloc=1 (inside a location radius only the start temple's and trader camps' own objects and the traders stay; default 0: nothing in a location goes)]"
                    + " - removes the object under the crosshair for everyone (creature, item, piece, tree, rock, ship); never players, tombstones, traders or location objects; no drops, no undo; host only";
            }
        }

        protected override string Execute(Args a)
        {
            Found f = Find(a);
            if (f.Refusal != null) throw new CommandException("refused " + f.Name + " at " + Fmt.F(f.Distance, 1) + " m: " + f.Refusal);
            f.View.ClaimOwnership();
            ZNetScene.instance.Destroy(f.View.gameObject);
            return "despawned " + Describe(f);
        }

        string Look(Args a)
        {
            Found f = Find(a);
            string note = f.Note != null ? "\n" + f.Note : "";
            if (f.Refusal != null) return "would refuse " + f.Name + " at " + Fmt.F(f.Distance, 1) + " m: " + f.Refusal + note;
            return "would despawn " + Describe(f) + note;
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
            bool inloc = a.Flag("inloc", false);

            if (go.GetComponentInParent<TombStone>() != null) { f.Kind = "tombstone"; f.Refusal = "tombstones hold a player's items: never despawned"; return f; }
            if (go.GetComponentInParent<Trader>() != null) { f.Kind = "trader"; f.Refusal = "trader belongs to a location: never despawned"; return f; }

            // inloc=1: one location rule for everything below; a kept location's own object stays, the rest may go
            if (inloc)
            {
                string kept = KeptOwner(go, f);
                if (kept != null) { f.Kind = LocationPart(go) ?? "location object"; f.Refusal = "own object of " + kept + ": kept with inloc=1"; return f; }
            }
            else
            {
                string protectedKind = LocationPart(go);
                if (protectedKind != null) { f.Kind = protectedKind; f.Refusal = protectedKind + " belongs to a location: never despawned"; return f; }
            }

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
            string part = LocationPart(go);
            if (go.GetComponent<Ship>() != null) f.Kind = "ship";
            else if (go.GetComponent<Vagon>() != null) f.Kind = "cart";
            else if (built) f.Kind = "piece";
            else if (part != null) f.Kind = part;   // inloc=1 only: another location's altar, runestone, vegvisir
            else if (go.GetComponent<TreeBase>() != null || go.GetComponent<TreeLog>() != null) f.Kind = "tree";
            else if (go.GetComponent<MineRock>() != null || go.GetComponent<MineRock5>() != null || go.GetComponent<Destructible>() != null) f.Kind = "rock or bush";
            else f.Kind = "object";

            if (built)
            {
                long creator = piece.GetCreator();
                if (creator != 0L && creator != me.GetPlayerID() && !a.Flag("others", false)) f.Refusal = "built by another player: add others=1";
            }
            else if (!inloc && f.Kind != "ship" && f.Kind != "cart")
            {
                Location loc = Location.GetLocation(go.transform.position);
                if (loc != null) f.Refusal = "inside the location " + Prefabs.BaseName(loc.gameObject) + ": never despawned (inloc=1 allows it)";
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

        // inloc=1: the name of the kept location (start temple, trader camp) this object is an own object of, or null.
        // Writes what was checked into f.Note for look=1.
        static string KeptOwner(GameObject go, Found f)
        {
            Vector3 pos = go.transform.position;
            string key = Utils.GetPrefabName(go);   // the name the game hashes into the ZDO, on both sides
            Location loc = Location.GetLocation(pos);
            if (loc == null) { f.Note = "location: none here"; return null; }
            string locName = Prefabs.BaseName(loc.gameObject);
            if (System.Array.IndexOf(KeptLocations, locName) < 0) { f.Note = "location: " + locName + " (not kept: its objects may go)"; return null; }

            List<OwnObject> own = OwnObjects(locName);
            if (own == null) { f.Note = "location: " + locName + " (kept; its prefab could not be read, so nothing in it goes)"; return locName; }
            Transform lt = loc.transform;
            float best = float.MaxValue;
            foreach (OwnObject o in own)
            {
                if (o.Name != key) continue;
                float dist = Vector3.Distance(lt.position + lt.rotation * o.Local, pos);
                if (dist < best) best = dist;
            }
            if (best <= OwnMatch) { f.Note = "location: " + locName + " (kept), own object, " + Fmt.F(best, 2) + " m from its spawn point"; return locName; }
            f.Note = "location: " + locName + " (kept), not its own object ("
                + (best == float.MaxValue ? "no " + key + " in its prefab" : "nearest " + key + " of the prefab " + Fmt.F(best, 2) + " m away") + ")";
            return null;
        }

        // the ZNetView children of a location prefab in the prefab's own frame, the frame ZoneSystem.SpawnLocation places them by
        // (each child at location position + location rotation * its position under the prefab root)
        static List<OwnObject> OwnObjects(string locName)
        {
            List<OwnObject> list;
            if (ownCache.TryGetValue(locName, out list)) return list;
            list = null;
            ZoneSystem zs = ZoneSystem.instance;
            ZoneSystem.ZoneLocation zl = null;
            if (zs != null) foreach (ZoneSystem.ZoneLocation z in zs.m_locations) if (z != null && z.m_prefab.IsValid && z.m_prefab.Name == locName) { zl = z; break; }
            if (zl != null)
            {
                bool loaded = false;
                try
                {
                    zl.m_prefab.Load();
                    loaded = true;
                    GameObject root = zl.m_prefab.Asset;
                    if (root != null)
                    {
                        list = new List<OwnObject>();
                        Quaternion inv = Quaternion.Inverse(root.transform.rotation);
                        foreach (ZNetView v in root.GetComponentsInChildren<ZNetView>(true))
                        {
                            if (v.gameObject == root) continue;
                            OwnObject o = new OwnObject();
                            o.Name = Utils.GetPrefabName(v.gameObject);
                            o.Local = inv * (v.transform.position - root.transform.position);
                            list.Add(o);
                        }
                    }
                }
                catch (System.Exception e) { HomeApi.Log("despawn: location prefab " + locName + ": " + e.Message); list = null; }
                finally { if (loaded) zl.m_prefab.Release(); }
            }
            if (list != null) HomeApi.Log("despawn: location " + locName + " has " + list.Count + " own networked objects");
            ownCache[locName] = list;
            return list;
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
