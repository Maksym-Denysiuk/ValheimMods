using System;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // give: items into the local player's inventory, split into full stacks
    public sealed class GiveCommand : CommandBase
    {
        public override string Name { get { return "give"; } }
        protected override string Usage { get { return "give prefab=<item> [amount=1] [quality=1] - items into your inventory"; } }

        protected override string Execute(Args a)
        {
            Player p = Players.Local;
            string prefab = a.Required("prefab", "e.g. prefab=Wood");
            int amount = a.Int("amount", 1), quality = a.Int("quality", 1);
            GameObject go = ObjectDB.instance.GetItemPrefab(prefab);
            if (go == null) throw new CommandException("no item prefab '" + prefab + "'");
            ItemDrop drop = go.GetComponent<ItemDrop>();
            int max = Math.Max(1, drop.m_itemData.m_shared.m_maxStackSize);
            Inventory inv = p.GetInventory();
            int left = amount, added = 0;
            while (left > 0)
            {
                int put = Math.Min(left, max);
                ItemDrop.ItemData it = drop.m_itemData.Clone();
                it.m_dropPrefab = go;
                it.m_stack = put;
                it.m_quality = quality;
                it.m_durability = it.GetMaxDurability();
                if (!inv.AddItem(it)) break;
                added += put; left -= put;
            }
            return "gave " + added + "/" + amount + " " + prefab + (left > 0 ? " (inventory full)" : "");
        }
    }
}
