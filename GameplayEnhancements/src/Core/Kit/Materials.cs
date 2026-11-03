// Materials for HomeCheat's build and plant commands (card materials-nearby, Maksym 2026-11-03): per requirement, your
// inventory first, then safe chests within the radius nearest first, the rest is built free. Per piece, never all or nothing.
// Only our commands use it; the vanilla hammer and cultivator stay vanilla.
// Safe chest (this game version, read with il-reader 2026-11-03, after the 2026-10-28 update):
//   built by a player (a Piece with a creator; dungeon loot and tombstones are never touched);
//   Container.CheckAccess(your player id), private, called by reflection: Private = only its creator, so another
//   player's private chest is skipped;
//   PrivateArea.CheckAccess(pos, 0, flash false, wardCheck false), as Container.Interact asks: a ward you lack access to;
//   not in use: Container.IsInUse is only the owner's local flag, so the synced ZDO "InUse" int is read too, and a cart
//   (m_wagon) in use is skipped, as Container.RPC_RequestOpen refuses it.
// Before removing, the chest's ZDO is claimed (RPC_RequestOpen gives it to the opener the same way) and its inventory
// reloaded from the ZDO; the removal then saves through Container.OnContainerChanged.
// Allocation maths: Pure/MaterialSplit (selftest). Check-only runs count with a MaterialLedger and change nothing.
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace HomeCheat.Kit
{
    public sealed class MaterialsReport
    {
        public int FromYou, FromChests, Free;
        public int Skipped;   // chests in range that were not safe
        public readonly HashSet<int> ChestsUsed = new HashSet<int>();
        public readonly Dictionary<string, int> SkipReasons = new Dictionary<string, int>();
        // per item (display name): from you, from chests, free
        public readonly Dictionary<string, int[]> Items = new Dictionary<string, int[]>();
        internal readonly List<string> order = new List<string>();

        internal void Add(string item, int you, int chests, int free)
        {
            int[] t;
            if (!Items.TryGetValue(item, out t)) { t = new int[3]; Items[item] = t; order.Add(item); }
            t[0] += you; t[1] += chests; t[2] += free;
            FromYou += you; FromChests += chests; Free += free;
        }

        // "10 from you, 50 from 1 chest(s), 40 free (Wood 10/50/40); 1 chest(s) skipped (in a ward you lack access to)"
        public string Summary()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(FromYou).Append(" from you, ").Append(FromChests).Append(" from ").Append(ChestsUsed.Count).Append(" chest(s), ")
              .Append(Free).Append(" free");
            if (order.Count > 0)
            {
                sb.Append(" (");
                for (int i = 0; i < order.Count; i++)
                {
                    int[] t = Items[order[i]];
                    if (i > 0) sb.Append(", ");
                    sb.Append(order[i]).Append(' ').Append(t[0]).Append('/').Append(t[1]).Append('/').Append(t[2]);
                }
                sb.Append(")");
            }
            if (Skipped > 0)
            {
                sb.Append("; ").Append(Skipped).Append(" chest(s) skipped (");
                bool first = true;
                foreach (KeyValuePair<string, int> kv in SkipReasons)
                {
                    if (!first) sb.Append(", ");
                    sb.Append(kv.Key).Append(' ').Append(kv.Value);
                    first = false;
                }
                sb.Append(")");
            }
            return sb.ToString();
        }
    }

    // one build or plant run: the chests are found once, then Take per piece; Report adds up every piece
    public sealed class MaterialsRun
    {
        readonly Player me;
        readonly bool check;
        readonly List<Container> chests = new List<Container>();
        readonly MaterialLedger ledger = new MaterialLedger();
        public readonly MaterialsReport Report = new MaterialsReport();
        public bool CheckOnly { get { return check; } }

        internal MaterialsRun(Player p, float radius, bool checkOnly)
        {
            me = p;
            check = checkOnly;
            Vector3 at = p.transform.position;
            List<KeyValuePair<float, Container>> found = new List<KeyValuePair<float, Container>>();
            foreach (Container c in Object.FindObjectsOfType<Container>())
            {
                if (c == null) continue;
                float d = Vector3.Distance(at, c.transform.position);
                if (d > radius) continue;
                string why = Materials.Unsafe(c, p);
                if (why != null) { Skip(why); continue; }
                found.Add(new KeyValuePair<float, Container>(d, c));
            }
            found.Sort(delegate (KeyValuePair<float, Container> x, KeyValuePair<float, Container> y) { return x.Key.CompareTo(y.Key); });
            foreach (KeyValuePair<float, Container> kv in found) chests.Add(kv.Value);
        }

        void Skip(string why)
        {
            // dungeon loot and tombstones are not chests here: not counted as skipped
            if (why == Materials.NotBuilt) return;
            Report.Skipped++;
            int n;
            Report.SkipReasons.TryGetValue(why, out n);
            Report.SkipReasons[why] = n + 1;
        }

        // takes one piece's recipe (level 1 amounts); returns how many items of it were free
        public int Take(Piece.Requirement[] reqs)
        {
            List<KeyValuePair<string, int>> needs = new List<KeyValuePair<string, int>>();
            Dictionary<string, string> shown = new Dictionary<string, string>();
            if (reqs != null)
                foreach (Piece.Requirement r in reqs)
                {
                    if (r == null || r.m_resItem == null || r.m_amount <= 0) continue;
                    string item = r.m_resItem.m_itemData.m_shared.m_name;
                    needs.Add(new KeyValuePair<string, int>(item, r.m_amount));
                    if (!shown.ContainsKey(item)) shown[item] = Prefabs.BaseName(r.m_resItem.gameObject);
                }
            int freeAll = 0;
            foreach (KeyValuePair<string, int> need in MaterialSplit.Merge(needs))
            {
                string item = need.Key;
                int[] avail = new int[chests.Count + 1];
                avail[0] = Count(me.GetInventory(), item, 0);
                for (int i = 0; i < chests.Count; i++) avail[i + 1] = ChestCount(i, item);
                int free;
                int[] taken = MaterialSplit.Split(need.Value, avail, out free);
                int fromYou = 0, fromChests = 0;
                for (int i = 0; i < taken.Length; i++)
                {
                    if (taken[i] <= 0) continue;
                    int got = check ? taken[i] : Remove(i, item, taken[i]);
                    if (check) ledger.Add(i, item, got);
                    if (got < taken[i]) free += taken[i] - got;   // a chest changed between count and take: that part is free
                    if (i == 0) { fromYou = got; continue; }
                    fromChests += got;
                    if (got > 0) Report.ChestsUsed.Add(chests[i - 1].GetInstanceID());
                }
                Report.Add(shown[item], fromYou, fromChests, free);
                freeAll += free;
            }
            return freeAll;
        }

        int Count(Inventory inv, string item, int source)
        {
            int live = inv == null ? 0 : inv.CountItems(item, -1, true);
            return check ? ledger.Left(source, item, live) : live;
        }

        int ChestCount(int i, string item)
        {
            Container c = chests[i];
            if (c == null) return 0;
            if (!check && Materials.Unsafe(c, me) != null) return 0;   // opened or warded since the scan
            return Count(c.GetInventory(), item, i + 1);
        }

        // source 0 = you, i = chest i - 1; returns what was really removed
        int Remove(int source, string item, int amount)
        {
            Inventory inv;
            if (source == 0) inv = me.GetInventory();
            else
            {
                Container c = chests[source - 1];
                if (c == null || !Materials.Claim(c)) return 0;
                inv = c.GetInventory();
            }
            if (inv == null) return 0;
            int have = inv.CountItems(item, -1, true);
            int n = Mathf.Min(have, amount);
            if (n > 0) inv.RemoveItem(item, n, -1, true);
            return n;
        }
    }

    public static class Materials
    {
        public const float DefaultRadius = 50f;
        public const float MaxRadius = 100f;
        public const string Usage = "[cost=0 (nearby = materials from you, then safe chests within radius nearest first, the rest free; check = say what would be taken, change nothing)] [radius=50 (cap 100)]";

        internal const string NotBuilt = "not a built chest";

        // a run over many pieces (radius capped at MaxRadius)
        public static MaterialsRun Begin(Player p, float radius, bool checkOnly)
        {
            if (p == null) throw new CommandException("no local player: load a world first");
            return new MaterialsRun(p, Mathf.Clamp(radius, 0f, MaxRadius), checkOnly);
        }

        // one piece: your inventory, then safe chests within radius, the rest free; returns the free count
        public static int Take(Player p, Piece.Requirement[] reqs, float radius, out MaterialsReport report)
        {
            return Take(p, reqs, radius, false, out report);
        }

        public static int Take(Player p, Piece.Requirement[] reqs, float radius, bool checkOnly, out MaterialsReport report)
        {
            MaterialsRun run = Begin(p, radius, checkOnly);
            int free = run.Take(reqs);
            report = run.Report;
            return free;
        }

        // null = safe to take from; else the reason it is skipped
        internal static string Unsafe(Container c, Player me)
        {
            if (c == null || !c.gameObject.activeInHierarchy) return NotBuilt;
            if (c.GetComponentInParent<TombStone>() != null) return NotBuilt;
            Piece piece = c.GetComponentInParent<Piece>();
            if (piece == null || piece.GetCreator() == 0L) return NotBuilt;
            ZNetView nv = View(c);
            if (nv == null || !nv.IsValid()) return NotBuilt;
            if (checkAccess == null) checkAccess = typeof(Container).GetMethod("CheckAccess", Refl.InstanceAll, null, new System.Type[] { typeof(long) }, null);
            if (checkAccess == null || !(bool)checkAccess.Invoke(c, new object[] { me.GetPlayerID() })) return "private";
            if (!PrivateArea.CheckAccess(c.transform.position, 0f, false, false)) return "warded";
            if (c.IsInUse() || nv.GetZDO().GetInt(ZDOVars.s_inUse, 0) == 1) return "in use";
            Vagon cart = Refl.Get(c, "m_wagon") as Vagon;
            if (cart != null)
            {
                MethodInfo inUse = typeof(Vagon).GetMethod("InUse", Refl.InstanceAll);
                if (inUse != null && (bool)inUse.Invoke(cart, null)) return "in use";
            }
            return null;
        }

        static ZNetView View(Container c)
        {
            ZNetView nv = Refl.Get(c, "m_nview") as ZNetView;
            return nv != null ? nv : c.GetComponentInParent<ZNetView>();
        }

        static MethodInfo load, checkAccess;

        // the chest's ZDO becomes ours and its inventory is read from the ZDO, as opening it would do
        internal static bool Claim(Container c)
        {
            ZNetView nv = View(c);
            if (nv == null || !nv.IsValid()) return false;
            if (!nv.IsOwner()) nv.ClaimOwnership();
            if (!nv.IsOwner()) return false;
            if (load == null) load = typeof(Container).GetMethod("Load", Refl.InstanceAll);
            if (load != null) load.Invoke(c, null);
            return true;
        }
    }
}
