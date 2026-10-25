// op=dome: a protected zone made of vanilla wards (guard_stone), placed as if Maksym built them with the hammer, so every
// player on the server sees and gets the same: the standard ward look and the vanilla ward rules, with no mod on their side.
// Build order steps 2-3 of the dome requirements: two points -> one ward at the centre, or a group of wards covering a
// larger zone; the group acts as one (on/off and the permitted list are copied to every ward by the host).
// Not here yet: the server monster guard (step 4), the owner panel and hover text (step 5), extra effects (step 6).
// All logic is host only: the wards are shared world data.
using System;
using System.Collections.Generic;
using System.Text;
using HomeCheat.Kit;
using Splatform;
using UnityEngine;

namespace HomeCheat.Features.Dome
{
    public sealed class DomeCommand : CommandBase
    {
        const float MaxRadius = 150f;      // zone radius cap (m)
        const int MaxWards = 50;           // wards per dome cap
        const float BaseRadius = 0.6f;     // half the ward's base: where its edges are probed for slope and buildings
        const float MaxBaseDrop = 0.5f;    // largest height difference under the base edges before the spot is refused
        const float Sink = 0.05f;          // how far the ward sits below its lowest base edge, so no part floats
        const float BuildingMargin = 1.5f; // pieces are measured to their pivot (Kit.Constructions): this much extra
        const float SyncSeconds = 2f;      // how often the host copies a changed ward's state to its group
        const float RescanSeconds = 30f;   // how often the host looks for dome wards again (built or removed elsewhere)
        const int MaxListed = 8;           // spots named in an answer

        internal static DomeCommand Current;

        Dictionary<string, List<ZDO>> groups = new Dictionary<string, List<ZDO>>();
        bool syncRunning;
        float nextRescan;
        bool nextMarkIsEnd;
        Dictionary<string, int> groupNos = new Dictionary<string, int>();   // group id -> N of Group-N
        DomeMenu menu;

        public DomeCommand()
        {
            SubAction("step", "step=mark [point=start|end] (stand on a point: first the start, then the end; uses the coord points)"
                + " | step=build from=coords|x,z [to=x,z] [cost=0] [partial=0] [preview=0] (as without step)"
                + " | step=list (every dome: centre, radius, wards, on/off, permitted)"
                + " | step=remove [confirm=0] (the dome at the crosshair with all its wards; without confirm=1 only says what it would remove)"
                + " | step=menu (the ward menu: New or one of your groups, Allowed players, Status; Apply)"
                + " | step=apply group=new|<n> [allow=<name>,...] [status=active|deactivated|deleted] (the menu's Apply as a command)",
                delegate (Args a) { return Step(a); });
        }

        public override string Name { get { return "dome"; } }

        protected override string Usage
        {
            get
            {
                return "dome step=menu (the ward menu of the spell bar: New or one of your groups, Allowed players, Status) | dome from=coords | from=x,z&to=x,z [cost=0 (1 = take the ward materials from your inventory)] [partial=0 (1 = skip spots that"
                    + " cannot take a ward, leaving gaps)] [preview=0 (1 = only say where the wards would go)] - a protected zone of vanilla wards between two"
                    + " points on opposite edges (centre = midpoint, radius = half the distance + 10 %, cap " + Fmt.F(MaxRadius, 0) + " m): one ward while the zone"
                    + " fits in one ward's radius, else a group of up to " + MaxWards + " wards without gaps; you are their creator, as with the hammer;"
                    + " refuses water, buildings, slopes over " + Fmt.F(MaxBaseDrop, 1) + " m under the base and other players' ward areas;"
                    + " spots your own wards already cover are left out; new wards that meet one of your groups join it (Group-N);"
                    + " a group acts as one (on/off and the permitted list copied to every ward by the host); host only";
            }
        }

        protected override string Execute(Args a)
        {
            if (!a.Has("from")) throw new CommandException("from required: from=coords (the coord start and end points) or from=x,z&to=x,z; or step=mark|list|remove");
            return Build(a);
        }

        string Step(Args a)
        {
            string step = a.Str("step", "").ToLowerInvariant();
            if (step == "mark") return Mark(a);
            if (step == "build") return Build(a);
            if (step == "list") return List();
            if (step == "remove") return Remove(a);
            if (step == "menu") return OpenMenu();
            if (step == "apply") return ApplyStep(a);
            throw new CommandException("step=" + step + " is unknown: menu, apply, mark, build, list or remove");
        }

        // ---- mark -----------------------------------------------------------------------------------

        string Mark(Args a)
        {
            Host.RequireWorld();
            Player p = Players.Local;
            string point = a.Str("point", nextMarkIsEnd ? Coords.End : Coords.Start).ToLowerInvariant();
            if (point != Coords.Start && point != Coords.End) throw new CommandException("point=" + point + ": start or end");
            Vector3 pos = p.transform.position;
            Coords.Set(point, pos);
            nextMarkIsEnd = point == Coords.Start;
            string line = "dome mark " + point + " = " + Fmt.Vec(pos) + " (your feet)";
            Vector3 s, e;
            if (point == Coords.Start || !Coords.Get(Coords.Start, out s) || !Coords.Get(Coords.End, out e))
                return line + "; next: op=dome&step=mark (the end point, on the opposite edge)";
            float cx, cz, r;
            DomeLayout.Zone(s.x, s.z, e.x, e.z, out cx, out cz, out r);
            r *= DomeLayout.Margin;
            string wards = "";
            PrivateArea area = WardArea(false);
            if (area != null) wards = " -> " + DomeLayout.Wards(cx, cz, r, area.m_radius).Count + " ward(s) of radius " + Fmt.F(area.m_radius, 1) + " m";
            return line + "; zone centre " + Fmt.F(cx, 1) + "," + Fmt.F(cz, 1) + ", radius " + Fmt.F(r, 1) + " m (marked + 10 %)" + wards
                + "; place: the ward menu (op=dome&step=menu, New) or op=dome&step=build&from=coords";
        }

        // ---- build ----------------------------------------------------------------------------------

        sealed class Spot
        {
            public DomePoint Lattice;
            public Vector3 Pos;
            public bool Ok;
            public string Why;
        }

        string Build(Args a)
        {
            Host.RequireServer();
            Vector3 s, e;
            string from = a.Str("from", "coords").ToLowerInvariant();
            if (from == "coords")
            {
                s = Coords.Require(Coords.Start);
                e = Coords.Require(Coords.End);
            }
            else
            {
                s = ParseXZ("from", from);
                e = ParseXZ("to", a.Required("to", "to=x,z (the point on the opposite edge)"));
            }
            return PlaceZone(s, e, a.Flag("cost", false), a.Flag("partial", false), a.Flag("preview", false), null);
        }

        // covers the circle between s and e plus 10 % (DomeLayout.Margin) with wards of the caller. Lattice spots whose cell your
        // own dome wards already cover are left out (overlaps minimal); new wards whose circle meets a ward of one of your groups
        // join that group (several touched: they merge into the lowest number), else they form a new group.
        // wanted: the state of a new group (the menu's Allowed and Status); null = the first new ward's own state.
        // Joining a group keeps the group's on/off; its permitted list gains the wanted players.
        internal string PlaceZone(Vector3 s, Vector3 e, bool cost, bool partial, bool preview, WardState wanted)
        {
            Host.RequireServer();
            Player p = Players.Local;
            long me = p.GetPlayerID();
            float cx, cz, radius;
            DomeLayout.Zone(s.x, s.z, e.x, e.z, out cx, out cz, out radius);
            if (radius < 1f) throw new CommandException("the two points are " + Fmt.F(radius * 2f, 1) + " m apart: mark two points on opposite edges of the zone");
            radius *= DomeLayout.Margin;
            Caps.Check("radius", radius, MaxRadius);

            GameObject prefab = Prefabs.Require(DomeWards.WardPrefab, "(the vanilla ward)");
            PrivateArea wardArea = WardArea(true);
            float wr = wardArea.m_radius;

            Rescan();
            List<DomePoint> mine = new List<DomePoint>();
            foreach (KeyValuePair<string, List<ZDO>> kv in groups)
                foreach (ZDO z in kv.Value)
                    if (DomeWards.Creator(z) == me) mine.Add(new DomePoint(z.GetPosition().x, z.GetPosition().z));
            List<DomePoint> lattice = new List<DomePoint>();
            int covered = 0;
            foreach (DomePoint lp in DomeLayout.Wards(cx, cz, radius, wr))
            {
                if (DomeLayout.Covered(lp, wr, mine)) { covered++; continue; }
                lattice.Add(lp);
            }
            string zone = "centre " + Fmt.F(cx, 1) + "," + Fmt.F(cz, 1) + ", radius " + Fmt.F(radius, 1) + " m (marked + 10 %), ward radius " + Fmt.F(wr, 1) + " m";
            if (lattice.Count == 0) throw new CommandException("the zone is already covered by your wards (" + zone + "): nothing to place");
            Caps.Check("wards", lattice.Count, MaxWards);
            float slack = lattice.Count == 1 && covered == 0 ? Mathf.Max(0f, wr - radius) : DomeLayout.Nudge(wr);

            List<ZDO> allWards = DomeWards.AllWards();
            List<Spot> spots = new List<Spot>();
            int bad = 0;
            foreach (DomePoint lp in lattice)
            {
                Spot sp = FindSpot(lp, slack, me, wr, allWards);
                if (!sp.Ok) bad++;
                spots.Add(sp);
            }
            int good = spots.Count - bad;
            if (bad > 0 && (!partial || good == 0))
                throw new CommandException(bad + " of " + spots.Count + " ward spot(s) cannot take a ward, nothing placed (" + zone + ")"
                    + (good > 0 ? "; partial=1 places the other " + good + " and leaves gaps" : "") + ": " + Refusals(spots));

            // the groups of yours the new wards meet (circles intersect: centres closer than two radii)
            List<string> touched = new List<string>();
            foreach (KeyValuePair<string, List<ZDO>> kv in groups)
            {
                if (DomeWards.Creator(kv.Value[0]) != me) continue;
                bool meets = false;
                foreach (Spot sp in spots)
                {
                    if (!sp.Ok) continue;
                    foreach (ZDO z in kv.Value) if (DomeWards.DistanceXZ(z.GetPosition(), sp.Pos) < wr * 2f) { meets = true; break; }
                    if (meets) break;
                }
                if (meets) touched.Add(kv.Key);
            }
            touched.Sort(delegate (string x, string y) { return GroupNo(x).CompareTo(GroupNo(y)); });
            string skippedCovered = covered > 0 ? ", " + covered + " spot(s) already covered by your wards left out" : "";
            string joins = touched.Count == 0 ? "" : "; joins " + GroupName(touched[0])
                + (touched.Count > 1 ? " and merges " + string.Join(", ", touched.GetRange(1, touched.Count - 1).ConvertAll<string>(GroupName).ToArray()) + " into it" : "");

            Piece piece = prefab.GetComponent<Piece>();
            string paid = "";
            if (cost && !preview) paid = Pay(p, piece, good);

            if (preview)
                return "preview: " + good + " ward(s) would be placed" + (bad > 0 ? ", " + bad + " spot(s) skipped (gaps)" : "") + skippedCovered + " (" + zone + ")" + joins
                    + Lines(spots, true) + (bad > 0 ? "\n  skipped: " + Refusals(spots) : "");

            string id;
            int no;
            if (touched.Count > 0) { id = touched[0]; no = GroupNo(id); }
            else
            {
                id = "d" + DateTime.UtcNow.Ticks.ToString("x");
                if (id.Length > 9) id = "d" + id.Substring(id.Length - 8);
                no = NextGroupNo();
            }
            Quaternion rot = Quaternion.Euler(0f, p.transform.eulerAngles.y, 0f);
            string profileName = Game.instance.GetPlayerProfile().GetName();
            List<ZDO> placed = new List<ZDO>();
            foreach (Spot sp in spots)
            {
                if (!sp.Ok) continue;
                ZDO z = Place(prefab, sp.Pos, rot, me, profileName);
                if (z == null) continue;
                z.Set(DomeWards.KeyDome, id);
                z.Set(DomeWards.KeyNo, no);
                z.Set(DomeWards.KeyCx, cx);
                z.Set(DomeWards.KeyCz, cz);
                z.Set(DomeWards.KeyRadius, radius);
                placed.Add(z);
            }
            if (placed.Count == 0) throw new CommandException("no ward could be created (the guard_stone prefab has no network view?)");

            // the whole group after this build: the new wards and every ward of the touched groups
            List<ZDO> all = new List<ZDO>(placed);
            foreach (string t in touched)
                foreach (ZDO z in groups[t])
                {
                    if (!DomeWards.Exists(z)) continue;
                    if (t != id) { DomeWards.Claim(z); z.Set(DomeWards.KeyDome, id); z.Set(DomeWards.KeyNo, no); }
                    all.Add(z);
                }
            WardState st;
            if (touched.Count > 0)
            {
                st = DomeWards.Read(groups[id][0]);
                if (wanted != null) foreach (KeyValuePair<long, string> kv in wanted.Permitted) AddPermitted(st, kv.Key, kv.Value);
            }
            else st = wanted ?? DomeWards.Read(placed[0]);
            WriteGroup(all, st);
            if (touched.Count == 0) groupNos[id] = no;
            Rescan();

            return "dome Group-" + no + " (" + id + "): placed " + placed.Count + " ward(s) (" + zone + ")" + (bad > 0 ? ", " + bad + " spot(s) skipped (gaps)" : "")
                + skippedCovered + joins + "; group of " + all.Count + " ward(s), " + (st.Enabled ? "active" : "deactivated") + ", " + st.Permitted.Count + " allowed" + paid
                + Lines(spots, false) + (bad > 0 ? "\n  skipped: " + Refusals(spots) : "");
        }

        // one state on every ward of a group, and the group size and the copied state recorded on each
        static void WriteGroup(List<ZDO> wards, WardState st)
        {
            string sig = st.Signature();
            foreach (ZDO z in wards)
            {
                if (!DomeWards.Exists(z)) continue;
                if (DomeWards.Read(z).Signature() != sig) DomeWards.Write(z, st);
                DomeWards.Claim(z);
                z.Set(DomeWards.KeyCount, wards.Count);
                z.Set(DomeWards.KeySig, sig);
            }
        }

        static void AddPermitted(WardState st, long id, string name)
        {
            foreach (KeyValuePair<long, string> kv in st.Permitted) if (kv.Key == id) return;
            st.Permitted.Add(new KeyValuePair<long, string>(id, name));
        }

        // ---- groups: numbers, menu data, apply ---------------------------------------------------

        internal sealed class GroupView
        {
            public string Id;
            public int No;
            public int Wards;
            public float Cx, Cz, Radius;
            public WardState State;
        }

        int GroupNo(string id) { int n; return groupNos.TryGetValue(id, out n) ? n : 0; }

        string GroupName(string id) { return "Group-" + GroupNo(id); }

        int NextGroupNo()
        {
            int max = 0;
            foreach (int n in groupNos.Values) if (n > max) max = n;
            return max + 1;
        }

        // Group-N: the number saved on the wards (hc_dome_no); a group without one (built before numbers) gets the next free
        // number in the order the groups were built (ids are time stamps), written to all its wards
        void NumberGroups()
        {
            groupNos.Clear();
            List<string> unnumbered = new List<string>();
            foreach (KeyValuePair<string, List<ZDO>> kv in groups)
            {
                int no = 0;
                foreach (ZDO z in kv.Value) no = Math.Max(no, z.GetInt(DomeWards.KeyNo, 0));
                if (no == 0) unnumbered.Add(kv.Key);
                else groupNos[kv.Key] = no;
            }
            unnumbered.Sort(delegate (string x, string y) { return x.Length != y.Length ? x.Length.CompareTo(y.Length) : string.CompareOrdinal(x, y); });
            foreach (string id in unnumbered) groupNos[id] = NextGroupNo();
            foreach (KeyValuePair<string, List<ZDO>> kv in groups)
            {
                int no = groupNos[kv.Key];
                foreach (ZDO z in kv.Value)
                    if (z.GetInt(DomeWards.KeyNo, 0) != no && DomeWards.Exists(z)) { DomeWards.Claim(z); z.Set(DomeWards.KeyNo, no); }
            }
        }

        // the caller's groups, by number
        internal List<GroupView> MyGroups()
        {
            long me = Players.Local.GetPlayerID();
            List<GroupView> r = new List<GroupView>();
            foreach (KeyValuePair<string, List<ZDO>> kv in groups)
            {
                ZDO z0 = kv.Value[0];
                if (DomeWards.Creator(z0) != me) continue;
                GroupView g = new GroupView();
                g.Id = kv.Key; g.No = GroupNo(kv.Key); g.Wards = kv.Value.Count;
                g.Cx = z0.GetFloat(DomeWards.KeyCx, z0.GetPosition().x); g.Cz = z0.GetFloat(DomeWards.KeyCz, z0.GetPosition().z);
                g.Radius = z0.GetFloat(DomeWards.KeyRadius, 0f);
                g.State = DomeWards.Read(z0);
                r.Add(g);
            }
            r.Sort(delegate (GroupView x, GroupView y) { return x.No.CompareTo(y.No); });
            return r;
        }

        // the menu's New line: what the coord points would give
        internal string NewInfo(out bool ready)
        {
            ready = false;
            Vector3 s, e;
            if (!Coords.Get(Coords.Start, out s) || !Coords.Get(Coords.End, out e))
                return "set the start and end points first (Build > Coordinates): they mark opposite edges of the zone";
            float cx, cz, r;
            DomeLayout.Zone(s.x, s.z, e.x, e.z, out cx, out cz, out r);
            if (r < 1f) return "the start and end points are " + Fmt.F(r * 2f, 1) + " m apart: mark opposite edges of the zone";
            r *= DomeLayout.Margin;
            PrivateArea area = WardArea(false);
            if (r > MaxRadius) return "radius " + Fmt.F(r, 0) + " m (marked + 10 %) is over the cap " + Fmt.F(MaxRadius, 0) + " m";
            ready = true;
            return "zone centre " + Fmt.F(cx, 0) + "," + Fmt.F(cz, 0) + ", radius " + Fmt.F(r, 1) + " m (marked + 10 %)"
                + (area == null ? "" : ", up to " + DomeLayout.Wards(cx, cz, r, area.m_radius).Count + " ward(s) of radius " + Fmt.F(area.m_radius, 0) + " m")
                + "; " + Fmt.F(DomeWards.DistanceXZ(Players.Local.transform.position, new Vector3(cx, 0f, cz)), 0) + " m from you";
        }

        // the menu's Apply (and step=apply): group 0 = New (a zone from the coord points), else Group-N of the caller.
        // status: active, deactivated or deleted (all wards removed, no materials back). The caller is the wards' creator and
        // always has access, so the permitted list holds only the others.
        internal string Apply(int no, List<KeyValuePair<long, string>> allowed, string status)
        {
            Host.RequireServer();
            long me = Players.Local.GetPlayerID();
            status = (status ?? "").ToLowerInvariant();
            if (status != "active" && status != "deactivated" && status != "deleted")
                throw new CommandException("status=" + status + ": active, deactivated or deleted");
            WardState st = new WardState();
            st.Enabled = status == "active";
            foreach (KeyValuePair<long, string> kv in allowed) if (kv.Key != me) AddPermitted(st, kv.Key, kv.Value);
            if (no == 0)
            {
                if (status == "deleted") throw new CommandException("New cannot be Deleted: pick Active or Deactivated");
                return PlaceZone(Coords.Require(Coords.Start), Coords.Require(Coords.End), false, false, false, st);
            }
            Rescan();
            string id = null;
            foreach (KeyValuePair<string, int> kv in groupNos) if (kv.Value == no) id = kv.Key;
            if (id == null || DomeWards.Creator(groups[id][0]) != me) throw new CommandException("Group-" + no + " is not one of your ward groups: op=dome&step=list");
            List<ZDO> wards = groups[id];
            if (status == "deleted")
            {
                int n = 0;
                foreach (ZDO z in wards) { if (!DomeWards.Exists(z)) continue; DomeWards.Remove(z); n++; }
                groups.Remove(id);
                groupNos.Remove(id);
                return "removed Group-" + no + ": " + n + " ward(s)";
            }
            WriteGroup(wards, st);
            List<string> names = new List<string>();
            foreach (KeyValuePair<long, string> kv in st.Permitted) names.Add(kv.Value);
            return "Group-" + no + ": " + wards.Count + " ward(s) " + status + ", allowed: you" + (names.Count > 0 ? ", " + string.Join(", ", names.ToArray()) : "");
        }

        // step=apply group=new|<n>|Group-<n> [allow=<name>,<name>] [status=active|deactivated|deleted]: the menu's Apply typed
        // as a command (tests, scripts). Without allow= a group keeps its list (New: only you); without status= it keeps its state
        // (New: active).
        string ApplyStep(Args a)
        {
            Host.RequireServer();
            Rescan();
            string g = a.Required("group", "group=new or group=<n> (Group-<n>)").ToLowerInvariant();
            if (g.StartsWith("group-")) g = g.Substring(6);
            int no = 0;
            if (g != "new" && (!int.TryParse(g, out no) || no < 1)) throw new CommandException("group=" + g + ": new or a group number");
            GroupView cur = null;
            foreach (GroupView v in MyGroups()) if (v.No == no) cur = v;
            List<KeyValuePair<long, string>> allowed = new List<KeyValuePair<long, string>>();
            if (a.Has("allow"))
            {
                foreach (string raw in a.Str("allow", "").Split(','))
                {
                    string n = raw.Trim();
                    if (n.Length == 0) continue;
                    long pid = DomePlayers.IdOf(n);
                    if (pid == 0L) throw new CommandException("allow: '" + n + "' has not joined world " + ZNet.instance.GetWorldName() + " yet (known: " + KnownNames() + ")");
                    allowed.Add(new KeyValuePair<long, string>(pid, n));
                }
            }
            else if (cur != null) allowed.AddRange(cur.State.Permitted);
            string status = a.Str("status", cur == null ? "active" : (cur.State.Enabled ? "active" : "deactivated"));
            return Apply(no, allowed, status);
        }

        static string KnownNames()
        {
            List<string> n = new List<string>();
            foreach (KeyValuePair<long, string> kv in DomePlayers.Known()) n.Add(kv.Value);
            return n.Count == 0 ? "nobody" : string.Join(", ", n.ToArray());
        }

        string OpenMenu()
        {
            Host.RequireServer();
            Rescan();
            if (menu == null)
            {
                GameObject go = OwnInWorld(new GameObject("HomeCheat.DomeMenu"));
                menu = go.AddComponent<DomeMenu>();
                menu.Cmd = this;
            }
            menu.Open();
            return "dome menu opened: " + MyGroups().Count + " group(s) of yours, " + DomePlayers.Known().Count + " known player(s)";
        }

        static Vector3 ParseXZ(string key, string text)
        {
            string[] parts = text.Split(',');
            float x, z;
            if (parts.Length != 2
                || !float.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x)
                || !float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out z))
                throw new CommandException(key + "=" + text + ": expected x,z (two numbers, a point as decimal mark)");
            return new Vector3(x, 0f, z);
        }

        // the vanilla ward's PrivateArea on its prefab (its radius is the asset's value, read live)
        static PrivateArea WardArea(bool require)
        {
            GameObject prefab = require ? Prefabs.Require(DomeWards.WardPrefab, "(the vanilla ward)") : Prefabs.Get(DomeWards.WardPrefab);
            PrivateArea area = prefab == null ? null : prefab.GetComponent<PrivateArea>();
            if (area == null && require) throw new CommandException(DomeWards.WardPrefab + " has no PrivateArea (game changed?)");
            return area;
        }

        // the lattice point itself, then up to 'slack' metres around it in 8 directions
        Spot FindSpot(DomePoint lp, float slack, long me, float wr, List<ZDO> allWards)
        {
            Spot sp = new Spot();
            sp.Lattice = lp;
            Vector3 pos;
            string why;
            if (CheckSpot(lp.X, lp.Z, me, wr, allWards, out pos, out why)) { sp.Ok = true; sp.Pos = pos; return sp; }
            sp.Why = why;
            float[] rings = { slack * 0.5f, slack };
            foreach (float ring in rings)
            {
                if (ring < 0.25f) continue;
                for (int i = 0; i < 8; i++)
                {
                    float ang = i * Mathf.PI * 0.25f;
                    string other;
                    if (CheckSpot(lp.X + Mathf.Cos(ang) * ring, lp.Z + Mathf.Sin(ang) * ring, me, wr, allWards, out pos, out other))
                    {
                        sp.Ok = true; sp.Pos = pos; sp.Why = null;
                        return sp;
                    }
                }
            }
            return sp;
        }

        bool CheckSpot(float x, float z, long me, float wr, List<ZDO> allWards, out Vector3 pos, out string why)
        {
            pos = new Vector3(x, 0f, z);
            why = null;
            if (Heightmap.FindHeightmap(pos) == null) { why = "terrain not loaded here (stand nearer the zone)"; return false; }
            float water = ZoneSystem.instance.m_waterLevel;
            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i <= 8; i++)
            {
                Vector3 probe = pos;
                if (i > 0)
                {
                    float ang = (i - 1) * Mathf.PI * 0.25f;
                    probe.x += Mathf.Cos(ang) * BaseRadius;
                    probe.z += Mathf.Sin(ang) * BaseRadius;
                }
                float h = Ground.Height(probe);
                if (h < water) { why = "on water (ground " + Fmt.F(water - h, 1) + " m below the water line)"; return false; }
                if (h < min) min = h;
                if (h > max) max = h;
            }
            if (max - min > MaxBaseDrop) { why = "slope too steep: the base edges differ by " + Fmt.F(max - min, 2) + " m, " + Fmt.F(max - min - MaxBaseDrop, 2) + " m over the limit " + Fmt.F(MaxBaseDrop, 1) + " m"; return false; }
            pos.y = min - Sink;

            Piece nearest;
            if (Constructions.Near(pos, BaseRadius + BuildingMargin, out nearest) > 0) { why = "on a building: " + Constructions.Describe(nearest, pos); return false; }
            if (Ground.IsBlocked(new Vector3(pos.x, max, pos.z), BaseRadius)) { why = "blocked by a tree, rock or object"; return false; }

            foreach (ZDO w in allWards)
            {
                long creator = DomeWards.Creator(w);
                if (creator == me || creator == 0L) continue;
                float d = DomeWards.DistanceXZ(w.GetPosition(), pos);
                if (d < wr * 2f) { why = "overlaps another player's ward area (ward at " + Fmt.F(w.GetPosition().x, 1) + "," + Fmt.F(w.GetPosition().z, 1) + ", " + Fmt.F(d, 1) + " m)"; return false; }
            }
            return true;
        }

        // creates the ward the way Player.PlacePiece does: creator, creator name, OnPlaced, place effect
        static ZDO Place(GameObject prefab, Vector3 pos, Quaternion rot, long me, string profileName)
        {
            GameObject go = (GameObject)UnityEngine.Object.Instantiate(prefab, pos, rot);
            ZNetView nv = go.GetComponent<ZNetView>();
            if (nv == null || !nv.IsValid()) { UnityEngine.Object.Destroy(go); return null; }
            Piece piece = go.GetComponent<Piece>();
            if (piece != null) piece.SetCreator(me, PlatformManager.DistributionPlatform.LocalUser.PlatformUserID);
            PrivateArea area = go.GetComponent<PrivateArea>();
            if (area != null) area.Setup(profileName);
            WearNTear wnt = go.GetComponent<WearNTear>();
            if (wnt != null) wnt.OnPlaced();
            if (piece != null && piece.m_placeEffect != null) piece.m_placeEffect.Create(pos, rot, go.transform, 1f, -1);
            return nv.GetZDO();
        }

        // takes count x the ward's recipe from the inventory, after checking all of it is there
        static string Pay(Player p, Piece piece, int count)
        {
            if (piece == null || piece.m_resources == null || piece.m_resources.Length == 0) return ", cost: none (the ward has no recipe)";
            Inventory inv = p.GetInventory();
            List<string> parts = new List<string>();
            foreach (Piece.Requirement r in piece.m_resources)
            {
                if (r.m_resItem == null || r.m_amount <= 0) continue;
                string item = r.m_resItem.m_itemData.m_shared.m_name;
                int need = r.m_amount * count, have = inv.CountItems(item, -1, true);
                if (have < need) throw new CommandException("cost=1: " + count + " ward(s) need " + need + " " + Prefabs.BaseName(r.m_resItem.gameObject) + ", you have " + have + "; nothing placed");
            }
            foreach (Piece.Requirement r in piece.m_resources)
            {
                if (r.m_resItem == null || r.m_amount <= 0) continue;
                inv.RemoveItem(r.m_resItem.m_itemData.m_shared.m_name, r.m_amount * count, -1, true);
                parts.Add(r.m_amount * count + " " + Prefabs.BaseName(r.m_resItem.gameObject));
            }
            return ", cost: " + string.Join(", ", parts.ToArray());
        }

        static string Lines(List<Spot> spots, bool preview)
        {
            StringBuilder sb = new StringBuilder();
            int n = 0;
            foreach (Spot sp in spots)
            {
                if (!sp.Ok) continue;
                if (++n > MaxListed) { sb.Append("\n  ... and ").Append(CountOk(spots) - MaxListed).Append(" more"); break; }
                float moved = DomeWards.DistanceXZ(sp.Pos, new Vector3(sp.Lattice.X, 0f, sp.Lattice.Z));
                sb.Append("\n  ").Append(preview ? "would place at " : "ward at ").Append(Fmt.Vec(sp.Pos));
                if (moved > 0.01f) sb.Append(" (moved ").Append(Fmt.F(moved, 1)).Append(" m from a blocked spot)");
            }
            return sb.ToString();
        }

        static int CountOk(List<Spot> spots)
        {
            int n = 0;
            foreach (Spot sp in spots) if (sp.Ok) n++;
            return n;
        }

        static string Refusals(List<Spot> spots)
        {
            List<string> parts = new List<string>();
            foreach (Spot sp in spots)
            {
                if (sp.Ok) continue;
                if (parts.Count == MaxListed) { parts.Add("..."); break; }
                parts.Add(Fmt.F(sp.Lattice.X, 1) + "," + Fmt.F(sp.Lattice.Z, 1) + " " + sp.Why);
            }
            return string.Join("; ", parts.ToArray());
        }

        // ---- list and remove ------------------------------------------------------------------------

        string List()
        {
            string world = Host.RequireServer();
            Rescan();
            if (groups.Count == 0) return "no domes in world " + world + ": op=dome&step=mark twice, then op=dome&step=build&from=coords";
            Player p = Player.m_localPlayer;
            StringBuilder sb = new StringBuilder();
            sb.Append(groups.Count).Append(" dome(s) in world ").Append(world);
            foreach (KeyValuePair<string, List<ZDO>> kv in groups)
            {
                List<ZDO> wards = kv.Value;
                ZDO z0 = wards[0];
                Vector3 c = DomeWards.Centre(z0);
                int on = 0;
                Dictionary<long, string> permitted = new Dictionary<long, string>();
                foreach (ZDO z in wards)
                {
                    WardState st = DomeWards.Read(z);
                    if (st.Enabled) on++;
                    foreach (KeyValuePair<long, string> pp in st.Permitted) permitted[pp.Key] = pp.Value;
                }
                sb.Append("\n  ").Append(GroupName(kv.Key)).Append(" (").Append(kv.Key).Append("): centre ").Append(Fmt.F(c.x, 1)).Append(",").Append(Fmt.F(c.z, 1))
                    .Append(", radius ").Append(Fmt.F(z0.GetFloat(DomeWards.KeyRadius, 0f), 1)).Append(" m, wards ").Append(wards.Count)
                    .Append("/").Append(z0.GetInt(DomeWards.KeyCount, wards.Count)).Append(", on ").Append(on).Append("/").Append(wards.Count)
                    .Append(", permitted ").Append(permitted.Count);
                if (p != null) sb.Append(", ").Append(Fmt.F(DomeWards.DistanceXZ(p.transform.position, c), 0)).Append(" m from you");
            }
            return sb.ToString();
        }

        string Remove(Args a)
        {
            Host.RequireServer();
            Player p = Players.Local;
            Rescan();
            string id = null;
            RaycastHit hit;
            if (Aim.SolidHit(60f, out hit))
            {
                ZNetView nv = hit.collider.GetComponentInParent<ZNetView>();
                if (nv != null && nv.IsValid()) id = nv.GetZDO().GetString(DomeWards.KeyDome, "");
                if (id != null && id.Length == 0) id = null;
            }
            bool aimed;
            Vector3 at = Aim.Point(p, out aimed);
            if (id == null)
            {
                float best = float.MaxValue;
                foreach (KeyValuePair<string, List<ZDO>> kv in groups)
                {
                    ZDO z0 = kv.Value[0];
                    float d = DomeWards.DistanceXZ(at, DomeWards.Centre(z0));
                    if (d <= z0.GetFloat(DomeWards.KeyRadius, 0f) && d < best) { best = d; id = kv.Key; }
                }
            }
            if (id == null || !groups.ContainsKey(id)) throw new CommandException("no dome at the crosshair (" + Fmt.Vec(at) + "): aim at a dome ward or into a zone; op=dome&step=list");
            List<ZDO> wards = groups[id];
            if (!a.Flag("confirm", false))
                return "would remove " + GroupName(id) + " (" + id + ") with " + wards.Count + " ward(s): add confirm=1";
            int n = 0;
            foreach (ZDO z in wards)
            {
                if (!DomeWards.Exists(z)) continue;
                DomeWards.Remove(z);
                n++;
            }
            string removed = GroupName(id);
            groups.Remove(id);
            groupNos.Remove(id);
            return "removed " + removed + " (" + id + "): " + n + " ward(s)";
        }

        // ---- group sync (host) ----------------------------------------------------------------------

        void Rescan()
        {
            List<ZDO> all = DomeWards.AllWards();
            groups = DomeWards.Groups(all);
            NumberGroups();
            DomePlayers.Record(all);
            nextRescan = Time.time + RescanSeconds;
        }

        // starts the host's sync loop once per world; Player.OnSpawned (DomePatches) and every dome command call it
        internal void EnsureSync()
        {
            if (syncRunning || !Host.IsServer || ZDOMan.instance == null) return;
            syncRunning = true;
            nextRescan = 0f;
            Later(SyncSeconds, Tick);
        }

        void Tick()
        {
            if (!Host.IsServer || ZDOMan.instance == null) { syncRunning = false; return; }
            try
            {
                if (Time.time >= nextRescan) Rescan();
                foreach (KeyValuePair<string, List<ZDO>> kv in groups) SyncGroup(kv.Key, kv.Value);
            }
            catch (Exception e) { HomeApi.LogError("dome: sync: " + e); }
            Later(SyncSeconds, Tick);
        }

        // a ward whose state differs from the state last copied to the group was changed by a player (on/off by its
        // creator, a player added or removed while it was off): its state goes to every ward of the group.
        // Two changed in the same tick: the one with the newer ZDO data wins.
        void SyncGroup(string id, List<ZDO> wards)
        {
            ZDO source = null;
            foreach (ZDO z in wards)
            {
                if (!DomeWards.Exists(z)) { nextRescan = 0f; continue; }
                if (DomeWards.Read(z).Signature() == z.GetString(DomeWards.KeySig, "")) continue;
                if (source == null || z.DataRevision > source.DataRevision) source = z;
            }
            if (source == null) return;
            WardState st = DomeWards.Read(source);
            string sig = st.Signature();
            int copied = 0;
            foreach (ZDO z in wards)
            {
                if (!DomeWards.Exists(z)) continue;
                if (DomeWards.Read(z).Signature() != sig) { DomeWards.Write(z, st); copied++; }
                if (z.GetString(DomeWards.KeySig, "") != sig) { DomeWards.Claim(z); z.Set(DomeWards.KeySig, sig); }
            }
            Vector3 at = source.GetPosition();
            HomeApi.Log("dome " + id + ": ward at " + Fmt.F(at.x, 1) + "," + Fmt.F(at.z, 1) + " changed to " + (st.Enabled ? "on" : "off")
                + ", " + st.Permitted.Count + " permitted; copied to " + copied + " other ward(s)");
        }

        // ---- lifecycle ------------------------------------------------------------------------------

        protected override void OnLoad()
        {
            Current = this;
            DomePatches.Apply(this);
            if (Player.m_localPlayer != null) EnsureSync();
        }

        protected override void OnUnload() { Current = null; DomeMenu.Close(); }

        protected override void OnWorldExit()
        {
            syncRunning = false;
            groups.Clear();
            groupNos.Clear();
            DomeMenu.Close();
            menu = null;
            nextMarkIsEnd = false;
        }

        internal PatchSet PatchesForDome { get { return Patches; } }
    }
}
