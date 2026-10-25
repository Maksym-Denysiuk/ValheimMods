using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // spawn: any prefab a few metres ahead of the player, with a little jitter.
    // player=<nick> spawns ahead of that player instead (another player: host only); yaw= / pitch= turn each object.
    // Item prefabs (ItemDrop) fall to the ground there, as they do with place; creatures get level= stars.
    // where= (Maksym 2026-10-25, the Spawn... form asks it): ahead (default, as before), crosshair (at the point you look at,
    // your own crosshair, player= ignored) or inventory (items only: into your inventory, what does not fit lies at your feet;
    // player=<another> drops them at his feet instead, host only, since his inventory is on his own client).
    // quality= (Maksym 2026-10-25, the spell passes max): item level 1..the item's max (max = its top level); every spawned item
    // has the full durability of that level, wherever it lands.
    public sealed class SpawnCommand : CommandBase
    {
        const int MaxCount = 100;

        public override string Name { get { return "spawn"; } }
        protected override string Usage
        {
            get
            {
                return "spawn prefab=<name> [count=1 (max " + MaxCount + ")] [dist=4] [level=0 (star level of creatures)]"
                    + " [quality=1 (items: level 1..the item's max, or max; durability always full)] [where=ahead|crosshair|inventory (inventory: items only; player=<another> drops them at his feet)]"
                    + " [player=<nick> (ahead of that player; another player: host only)] [yaw=0] [pitch=0 (degrees, world axes)] [tamed=0 (1 = creatures spawn tamed; a test aid for tamed-animal hits)]";
            }
        }

        protected override string Execute(Args a)
        {
            Host.RequireWorld();
            Player p = Players.Local;
            string prefab = a.Str("prefab", "");
            int count = a.Int("count", 1), level = a.Int("level", 0);
            bool tamed = a.Flag("tamed", false);
            float dist = a.Float("dist", 4f), yaw = a.Float("yaw", 0f), pitch = a.Float("pitch", 0f);
            string where = a.Str("where", "ahead").ToLowerInvariant();
            if (where != "ahead" && where != "crosshair" && where != "inventory") throw new CommandException("where must be ahead, crosshair or inventory");
            Caps.Check("count", count, MaxCount);
            GameObject go = Prefabs.Require(prefab, "(try op=list&filter=...)");
            ItemDrop proto = go.GetComponent<ItemDrop>();
            bool item = proto != null;
            int quality = item ? Quality(proto, a.Str("quality", "1")) : 1;
            string q = item ? " (quality " + quality + ", full durability)" : "";
            Player at = p;
            string nick = a.Str("player", "");
            if (nick.Length > 0 && where == "ahead")
            {
                at = Players.Find(nick);
                if (at == null) throw new CommandException("player '" + nick + "' is not online");
                if (at != p) Host.RequireServer();
            }
            if (where == "inventory")
            {
                if (!item) throw new CommandException(prefab + " is not an item: only items go into the inventory (use where=crosshair or ahead)");
                Player to = nick.Length > 0 ? Players.Find(nick) : p;
                if (to == null) throw new CommandException("player '" + nick + "' is not online");
                if (to == p)
                {
                    int max = Mathf.Max(1, proto.m_itemData.m_shared.m_maxStackSize), left = count, dropped = 0;
                    Inventory inv = p.GetInventory();
                    while (left > 0)
                    {
                        int n = Mathf.Min(left, max);
                        if (inv == null || !inv.AddItem(Made(proto, go, quality, n))) { Drop(go, quality, n, p.transform.position + Vector3.up); dropped += n; }
                        left -= n;
                    }
                    return "spawned " + count + " " + prefab + q + " into your inventory" + (dropped > 0 ? "; " + dropped + " did not fit and lie at your feet" : "");
                }
                // another player's inventory lives on his own client (no mod there): the items drop at his feet, auto-pickup takes them in
                Host.RequireServer();
                Vector3 feet = to.transform.position + Vector3.up * 0.3f;
                for (int i = 0; i < count; i++) Drop(go, quality, 1, feet);
                return "spawned " + count + " " + prefab + q + " at " + to.GetPlayerName() + "'s feet (his inventory is on his own game: auto-pickup takes them in when there is room)";
            }
            Vector3 pos;
            bool aimed = false;
            if (where == "crosshair") pos = Aim.Point(p, out aimed) + Vector3.up * 0.5f;
            else pos = at.transform.position + at.transform.forward * dist + Vector3.up;
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            int made = 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 jitter = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0f, UnityEngine.Random.Range(-0.5f, 0.5f));
                GameObject inst = (GameObject)UnityEngine.Object.Instantiate(go, pos + jitter, rot);
                if (item) SetItem(inst, quality, 1);
                Character c = inst.GetComponent<Character>();
                if (c != null && level > 0) c.SetLevel(level);
                if (c != null && tamed && !(c is Player)) c.SetTamed(true);
                made++;
            }
            string who = where == "crosshair" ? (aimed ? " at the crosshair" : " 8 m ahead (nothing under the crosshair within 60 m)")
                : at == p ? "" : " ahead of " + at.GetPlayerName();
            string turn = yaw != 0f || pitch != 0f ? " (yaw " + Fmt.F(yaw, 0) + ", pitch " + Fmt.F(pitch, 0) + ")" : "";
            return "spawned " + made + " " + prefab + (item ? " (items, dropped)" + q : "") + (tamed ? " (tamed)" : "") + who + turn;
        }

        // quality=1|<n>|max -> 1..the item's top level
        static int Quality(ItemDrop proto, string v)
        {
            int top = Mathf.Max(1, proto.m_itemData.m_shared.m_maxQuality), q;
            if (v.Equals("max", System.StringComparison.OrdinalIgnoreCase)) return top;
            if (!int.TryParse(v, out q) || q < 1) throw new CommandException("quality must be 1.." + top + " or max");
            return Mathf.Min(q, top);
        }

        // a stack of the item at that level with full durability (as give makes it)
        static ItemDrop.ItemData Made(ItemDrop proto, GameObject go, int quality, int stack)
        {
            ItemDrop.ItemData it = proto.m_itemData.Clone();
            it.m_dropPrefab = go;
            it.m_stack = stack;
            it.m_quality = quality;
            it.m_durability = it.GetMaxDurability();
            return it;
        }

        static void Drop(GameObject go, int quality, int stack, Vector3 at)
        {
            SetItem((GameObject)UnityEngine.Object.Instantiate(go, at, Quaternion.identity), quality, stack);
        }

        // level, stack and full durability on a dropped item, written to its ZDO so every player and a reload see them
        static void SetItem(GameObject inst, int quality, int stack)
        {
            ItemDrop d = inst.GetComponent<ItemDrop>();
            if (d == null) return;
            d.m_itemData.m_quality = quality;
            d.m_itemData.m_stack = stack;
            d.m_itemData.m_durability = d.m_itemData.GetMaxDurability();
            ZNetView nv = inst.GetComponent<ZNetView>();
            if (nv != null && nv.GetZDO() != null) ItemDrop.SaveToZDO(d.m_itemData, nv.GetZDO());
        }
    }
}
