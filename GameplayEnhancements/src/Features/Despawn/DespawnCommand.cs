// op=despawn: removes the one networked object under the crosshair through the game's own destroy path
// (ZNetView.ClaimOwnership + ZNetScene.Destroy: the ZDO is deleted for every player, nothing drops). Host only.
// Every allowed object goes at once (no confirm step: Maksym 2026-10-25; look=1 previews). Never a player, a tombstone, a location's own object (trader, altar,
// runestone, vegvisir, dungeon), or anything without a ZNetView. Tamed animals and bosses only with tamed=1 / boss=1,
// pieces another player built only with others=1 (Maksym's decisions 2026-10-25, page: docs/HomeCheat-plugin/despawn.md).
// inloc=1 (Maksym 2026-11-03, spell 6.8): the location rule shrinks to the start temple's and the trader camps' own objects
// (a ZNetView child of that location's prefab, same prefab name as Utils.GetPrefabName gives, within 0.5 m of where the game spawned it) and the traders;
// everything else inside a location radius may go (your items at the spawn, other locations' runestones and altars).
// A boss stone in a kept location, or an altar, runestone or vegvisir of a name its prefab holds, stays wherever it stands: an older world
// can hold them away from today's prefab position (2026-11-04: BossStone_Fader and BossStone_TheQueen passed the 0.5 m check and went).
// restore=<prefab>&pos=x,y,z[&euler=x,y,z]: puts one object back at an exact spot read from a world backup; remove=1 takes one away again
// (a wrong restore).
// undo=<n> (Maksym 2026-11-04): every despawn first writes the object's whole ZDO to a journal per world; undo brings them back newest first,
// with their saved state (a chest's items, a stand's trophy, health). File: BepInEx/config/homecheat/despawn-journal.state, last 500 objects.
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx;
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

        const int JournalMax = 500;   // objects kept in the journal, all worlds together
        const int UndoMax = 50;       // objects per undo cast
        static string JournalPath { get { return Path.Combine(Path.Combine(Paths.ConfigPath, "homecheat"), "despawn-journal.state"); } }

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
                    + " [undo=<n> (brings back the last n despawned objects, newest first, with their saved state; default 1, cap " + UndoMax + ")]"
                    + " [restore=<prefab> pos=x,y,z [euler=x,y,z] (puts one object back at that exact spot, refused when the same prefab already stands within 0.5 m; remove=1 takes one such object away again)]"
                    + " - removes the object under the crosshair for everyone (creature, item, piece, tree, rock, ship); never players, tombstones, traders or location objects; no drops, no undo; host only";
            }
        }

        protected override string Execute(Args a)
        {
            if (a.Has("undo")) return Undo(a);
            if (a.Has("restore")) return Restore(a);
            Found f = Find(a);
            if (f.Refusal != null) throw new CommandException("refused " + f.Name + " at " + Fmt.F(f.Distance, 1) + " m: " + f.Refusal);
            Remember(f.View);   // before the destroy: no record, no despawn
            f.View.ClaimOwnership();
            ZNetScene.instance.Destroy(f.View.gameObject);
            return "despawned " + Describe(f);
        }

        // one object back at an exact spot (position and rotation read from a world backup); a new ZDO, so saved state such as a trophy is not back
        string Restore(Args a)
        {
            Host.RequireServer();
            string name = a.Str("restore", "");
            GameObject prefab = Prefabs.Get(name);
            if (prefab == null) throw new CommandException("restore: no prefab " + name);
            if (prefab.GetComponent<ZNetView>() == null) throw new CommandException("restore: " + name + " is not a networked object");
            if (prefab.GetComponent<Character>() != null) throw new CommandException("restore: creatures are not restored");
            Vector3 pos = ParseVec(a.Str("pos", ""), "pos");
            Vector3 euler = a.Has("euler") ? ParseVec(a.Str("euler", ""), "euler") : Vector3.zero;
            List<ZDO> same = SameAt(prefab, pos);
            if (a.Flag("remove", false))
            {
                if (same.Count == 0) throw new CommandException("restore: no " + name + " within " + Fmt.F(OwnMatch, 1) + " m of " + Fmt.Vec(pos));
                RemoveZdo(same[same.Count - 1]);
                HomeApi.Log("despawn: restore removed one " + name + " at " + Fmt.Vec(pos) + ", " + (same.Count - 1) + " left");
                return "removed one " + name + " at " + Fmt.Vec(pos) + ", " + (same.Count - 1) + " left there";
            }
            if (same.Count > 0) throw new CommandException("restore: " + name + " already stands at " + Fmt.Vec(same[0].GetPosition()));
            GameObject made = Object.Instantiate(prefab, pos, Quaternion.Euler(euler));
            HomeApi.Log("despawn: restored " + name + " at " + Fmt.Vec(pos) + " euler " + Fmt.Vec(euler));
            return "restored " + Prefabs.BaseName(made) + " at " + Fmt.Vec(made.transform.position);
        }

        // the saved objects of this prefab at that spot, loaded on this PC or not (a collider search misses what is out of range)
        static List<ZDO> SameAt(GameObject prefab, Vector3 pos)
        {
            List<ZDO> all = new List<ZDO>();
            int index = 0;
            while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(prefab.name, all, ref index)) { }
            List<ZDO> same = new List<ZDO>();
            foreach (ZDO z in all) if (z != null && z.IsValid() && Vector3.Distance(z.GetPosition(), pos) <= OwnMatch) same.Add(z);
            return same;
        }

        static void RemoveZdo(ZDO z)
        {
            ZNetView v = ZNetScene.instance.FindInstance(z);
            if (v != null) { v.ClaimOwnership(); ZNetScene.instance.Destroy(v.gameObject); return; }
            z.SetOwner(ZDOMan.GetSessionID());
            ZDOMan.instance.DestroyZDO(z);
        }

        static Vector3 ParseVec(string s, string key)
        {
            string[] p = s.Split(',');
            float x, y, z;
            if (p.Length != 3
                || !float.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                || !float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y)
                || !float.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
                throw new CommandException(key + " must be x,y,z");
            return new Vector3(x, y, z);
        }

        // ---- the journal: one line per object: world <tab> prefab name <tab> x,y,z <tab> the ZDO's data in base64 -------------------

        static void Remember(ZNetView view)
        {
            try
            {
                ZDO z = view.GetZDO();
                ZPackage pkg = new ZPackage();
                z.Serialize(pkg);
                Vector3 p = z.GetPosition();
                CultureInfo inv = CultureInfo.InvariantCulture;
                string line = ZNet.instance.GetWorldName() + "	" + Prefabs.BaseName(view.gameObject) + "	"
                    + p.x.ToString("R", inv) + "," + p.y.ToString("R", inv) + "," + p.z.ToString("R", inv) + "	" + System.Convert.ToBase64String(pkg.GetArray());
                List<string> lines = ReadJournal();
                lines.Add(line);
                if (lines.Count > JournalMax) lines.RemoveRange(0, lines.Count - JournalMax);
                WriteJournal(lines);
            }
            catch (System.Exception e) { throw new CommandException("no undo record could be written, nothing despawned: " + e.Message); }
        }

        static List<string> ReadJournal()
        {
            return File.Exists(JournalPath) ? new List<string>(File.ReadAllLines(JournalPath, Encoding.UTF8)) : new List<string>();
        }

        static void WriteJournal(List<string> lines)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(JournalPath));
            File.WriteAllLines(JournalPath, lines.ToArray(), new UTF8Encoding(false));
        }

        string Undo(Args a)
        {
            Host.RequireServer();
            string raw = a.Str("undo", "1");
            int want;
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out want) || want < 1) want = 1;   // the flag form undo=1, or a word
            if (want > UndoMax) want = UndoMax;
            string world = ZNet.instance.GetWorldName();
            List<string> lines = ReadJournal();
            StringBuilder said = new StringBuilder();
            int done = 0, left = 0;
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                string[] c = lines[i].Split('	');
                if (c.Length != 4 || c[0] != world) continue;
                if (done >= want) { left++; continue; }
                GameObject prefab = Prefabs.Get(c[1]);
                if (prefab == null) { HomeApi.LogError("despawn: undo: no prefab " + c[1] + " in this game version, kept in the journal"); left++; continue; }
                Vector3 pos = ParseVec(c[2], "journal position");
                ZDO z = ZDOMan.instance.CreateNewZDO(pos, prefab.name.GetStableHashCode());
                z.Deserialize(new ZPackage(System.Convert.FromBase64String(c[3])));
                z.DataRevision = z.DataRevision + 1u;
                lines.RemoveAt(i);
                done++;
                if (said.Length > 0) said.Append(", ");
                said.Append(c[1]).Append(" at ").Append(Fmt.Vec(pos));
            }
            if (done == 0) throw new CommandException("undo: nothing in the despawn journal of this world");
            WriteJournal(lines);
            HomeApi.Log("despawn: undo restored " + done + ": " + said + "; " + left + " left");
            return "restored " + done + " (newest first): " + said + "; " + left + " left to undo";
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
            // a boss stone, or a story object of a name the prefab holds, stays wherever this world has it
            if (go.GetComponentInParent<BossStone>() != null || (best != float.MaxValue && LocationPart(go) != null))
            {
                f.Note = "location: " + locName + " (kept), own object by kind, " + (best == float.MaxValue ? "no " + key + " in its prefab" : Fmt.F(best, 2) + " m from the prefab's position");
                return locName;
            }
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
