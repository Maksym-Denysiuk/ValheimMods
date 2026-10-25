using System.Collections.Generic;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // harvest (W2-world-field, 2026-10-25): picks every ripe Pickable on the cultivated patch you stand on and the cultivated soil
    // joined to it (FieldPatch.Sample, 8 neighbours). A vanilla pick: Pickable.Interact(you, false, false), the same call as pressing E,
    // so the owner drops the items at each crop and auto-pickup takes the ones within reach; the rest lie on the ground.
    // Pickables that are not ripe, already picked or off the patch are left alone; wild pickables standing on the patch are picked too.
    // collect=1 (Maksym 2026-10-25, the spell uses it): no drops on the field: the same amount the game would drop (Pickable.RPC_Pick:
    // Game.ScaleDrops of m_itemPrefab x m_amount, plus m_extraDrops) goes into your inventory, the crop is marked picked through the
    // game's own RPC_SetPicked; what does not fit is dropped at your feet as one stack per item.
    public sealed class HarvestCommand : CommandBase
    {
        const int MaxPicked = 10000;   // per cast (was 2500: a 4000 m2 field stopped at half, test 2026-10-25)
        const int BatchSize = 500;     // picks per step, BatchGap s apart: no frame freeze, no burst of drops in one frame
        const float BatchGap = 0.1f;

        public override string Name { get { return "harvest"; } }
        protected override string Usage
        {
            get { return "harvest [collect=0 (1 = the harvest goes into your inventory, the rest at your feet; no drops on the field)] - picks every ripe crop on the cultivated patch you stand on (and the soil joined to it), as by hand: drops at each crop; cap " + MaxPicked + " picks, " + FieldPatch.MaxSamples + " soil samples"; }
        }

        protected override string Execute(Args a)
        {
            Host.RequireWorld();
            Player p = Players.Local;
            FieldPatch patch = FieldPatch.Sample(p.transform.position);
            if (!patch.StartSoil) return "nothing harvested: stand on cultivated soil";
            bool collect = a.Flag("collect", false);
            int picked = 0, notRipe = 0, capped = 0;
            List<Pickable> todo = new List<Pickable>();
            Dictionary<string, int> byName = new Dictionary<string, int>();
            foreach (Pickable pk in UnityEngine.Object.FindObjectsOfType<Pickable>())
            {
                if (pk == null || !patch.Contains(pk.transform.position)) continue;
                if (!pk.CanBePicked()) { notRipe++; continue; }
                if (picked >= MaxPicked) { capped++; continue; }
                todo.Add(pk);
                picked++;
                string name = Prefabs.BaseName(pk.gameObject);
                int n; byName.TryGetValue(name, out n); byName[name] = n + 1;
            }
            int batches = (todo.Count + BatchSize - 1) / BatchSize;
            for (int b = 0; b < batches; b++)
            {
                int from = b * BatchSize, to = Mathf.Min(todo.Count, from + BatchSize);
                System.Action step = delegate
                {
                    Player me = Player.m_localPlayer;
                    if (me == null) return;
                    if (!collect) { for (int i = from; i < to; i++) if (todo[i] != null && todo[i].CanBePicked()) todo[i].Interact(me, false, false); return; }
                    Dictionary<GameObject, int> got = new Dictionary<GameObject, int>();
                    for (int i = from; i < to; i++) if (todo[i] != null && todo[i].CanBePicked()) Collect(todo[i], got);
                    Store(me, got);
                };
                if (b == 0) step(); else Later(b * BatchGap, step);
            }
            StringBuilder per = new StringBuilder();
            foreach (KeyValuePair<string, int> kv in byName) per.Append(per.Length > 0 ? " " : "").Append(kv.Key).Append("=").Append(kv.Value);
            return "harvested " + picked + (batches > 1 ? " in " + batches + " batches over " + Fmt.F((batches - 1) * BatchGap, 1) + " s" : "") + (picked > 0 ? " (" + per + ")" : "") + " on the patch under you (about " + Fmt.F(patch.Area, 0) + " m2); "
                + notRipe + " not ripe or already picked" + (capped > 0 ? ", " + capped + " left over the cap of " + MaxPicked + " per cast: cast again for the rest" : "")
                + (patch.OverCap ? "; sampling stopped at the cap of " + FieldPatch.MaxSamples + " cells, crops beyond it were left" : "")
                + (picked > 0 ? (collect ? "; into your inventory, what does not fit lies at your feet" : "; drops lie at each crop, auto-pickup takes the ones within reach") : "");
        }
            // what the game's own pick would drop, counted instead of dropped; the crop is marked picked as by the game
        static void Collect(Pickable pk, Dictionary<GameObject, int> got)
        {
            ZNetView nv = pk.GetComponent<ZNetView>();
            if (nv == null || !nv.IsValid()) return;
            if (pk.m_itemPrefab != null)
            {
                int n = pk.m_dontScale ? pk.m_amount : Mathf.Max(pk.m_minAmountScaled, Game.instance.ScaleDrops(pk.m_itemPrefab, pk.m_amount));
                Add(got, pk.m_itemPrefab, n);
            }
            if (pk.m_extraDrops != null && pk.m_extraDrops.m_drops.Count > 0)
                foreach (ItemDrop.ItemData d in pk.m_extraDrops.GetDropListItems())
                    if (d != null && d.m_dropPrefab != null) Add(got, d.m_dropPrefab, d.m_stack);
            nv.InvokeRPC(ZNetView.Everybody, "RPC_SetPicked", true);
        }

        static void Add(Dictionary<GameObject, int> got, GameObject prefab, int n)
        {
            if (n <= 0) return;
            int had; got.TryGetValue(prefab, out had); got[prefab] = had + n;
        }

        // into the inventory; the rest dropped at the player's feet, one stack per item (split by the item's max stack)
        static void Store(Player me, Dictionary<GameObject, int> got)
        {
            Inventory inv = me.GetInventory();
            foreach (KeyValuePair<GameObject, int> kv in got)
            {
                int left = kv.Value;
                ItemDrop id = kv.Key.GetComponent<ItemDrop>();
                int max = id != null && id.m_itemData != null && id.m_itemData.m_shared != null ? Mathf.Max(1, id.m_itemData.m_shared.m_maxStackSize) : 50;
                while (left > 0)
                {
                    int n = Mathf.Min(left, max);
                    if (inv != null && inv.AddItem(kv.Key, n)) { left -= n; continue; }
                    GameObject dropped = (GameObject)UnityEngine.Object.Instantiate(kv.Key, me.transform.position + Vector3.up, Quaternion.identity);
                    ItemDrop dd = dropped.GetComponent<ItemDrop>();
                    if (dd != null) dd.SetStack(n);
                    left -= n;
                }
            }
        }
    }
}
