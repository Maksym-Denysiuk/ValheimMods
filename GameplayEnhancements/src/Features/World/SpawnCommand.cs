using HomeCheat.Kit;
using Splatform;
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
    // Constructions (Maksym 2026-10-25: workbench, portal, furnace, ships): a prefab with a Piece is built as the hammer builds it
    // (creator = you, so the hammer can take it down; WearNTear.OnPlaced; its place effect), standing on the ground, facing you
    // (yaw= turns it), several side by side in a row; a ship floats on the water when the ground there is under water;
    // tag= names a portal (TeleportWorld) for pairing.
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
                    + " [player=<nick> (ahead of that player; another player: host only)] [yaw=0] [pitch=0 (degrees, world axes; constructions: yaw turns it from facing you, pitch ignored)] [tamed=0 (1 = creatures spawn tamed; a test aid for tamed-animal hits)]"
                    + " [tag=<portal name> (portals: the tag that pairs two portals)]; constructions (a Piece: workbench, portal, furnace, ships) are built as by the hammer: on the ground, you as creator, in a row";
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
                // constructions are built in the world only (Maksym 2026-10-25: "should not work with inventory option")
                if (go.GetComponent<Piece>() != null) throw new CommandException(prefab + " is a construction: it is built in the world only (Where = Crosshair), never into an inventory");
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
            if (go.GetComponent<Piece>() != null) return Build(go, p, at, pos, where == "crosshair", aimed, count, yaw, a.Str("tag", ""));
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

        // a construction as Player.PlacePiece makes it (as dome does): on the ground (a ship on the water when the ground is under
        // it), facing the player it is built for, yaw turning it; count > 1 side by side to the right, one width + 0.5 m apart
        string Build(GameObject go, Player p, Player at, Vector3 pos, bool crosshair, bool aimed, int count, float yaw, string tag)
        {
            Host.RequireServer();
            bool ship = go.GetComponent<Ship>() != null;
            Vector3 fwd = at.transform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            Quaternion rot = Quaternion.LookRotation(-fwd) * Quaternion.Euler(0f, yaw, 0f);
            float water = ZoneSystem.instance.m_waterLevel;
            long me = p.GetPlayerID();
            PlatformUserID uid = PlatformManager.DistributionPlatform.LocalUser.PlatformUserID;
            float step = Width(go) + 0.5f;
            int made = 0, onWater = 0, tagged = 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 spot = pos + right * ((i - (count - 1) / 2f) * step);
                float ground = Ground.Height(spot);
                spot.y = ship && ground < water ? water : ground;
                if (ship && ground < water) onWater++;
                GameObject inst = (GameObject)UnityEngine.Object.Instantiate(go, spot, rot);
                ZNetView nv = inst.GetComponent<ZNetView>();
                if (nv == null || !nv.IsValid()) { UnityEngine.Object.Destroy(inst); throw new CommandException(go.name + " is not a networked piece: it cannot be built"); }
                Piece piece = inst.GetComponent<Piece>();
                piece.SetCreator(me, uid);
                WearNTear wnt = inst.GetComponent<WearNTear>();
                if (wnt != null) wnt.OnPlaced();
                if (piece.m_placeEffect != null) piece.m_placeEffect.Create(spot, rot, inst.transform, 1f, -1);
                if (tag.Length > 0 && inst.GetComponent<TeleportWorld>() != null) { nv.GetZDO().Set("tag", tag); tagged++; }
                made++;
            }
            string where = crosshair ? (aimed ? " at the crosshair" : " 8 m ahead (nothing under the crosshair within 60 m)") : at == p ? " ahead of you" : " ahead of " + at.GetPlayerName();
            return "built " + made + " " + go.name + where + ", facing " + (at == p ? "you" : at.GetPlayerName()) + (yaw != 0f ? " turned " + Fmt.F(yaw, 0) + " deg" : "")
                + (count > 1 ? ", in a row " + Fmt.F(step, 1) + " m apart" : "") + (onWater > 0 ? ", " + onWater + " on the water" : "")
                + (tagged > 0 ? ", portal tag '" + tag + "'" : tag.Length > 0 ? " (tag= ignored: not a portal)" : "")
                + "; you are the creator (the hammer can take it down); it stays in the world";
        }

        // the widest box collider of the prefab across x and z (scaled), 2 m without one: the row step of constructions
        static float Width(GameObject go)
        {
            float w = 0f;
            foreach (BoxCollider b in go.GetComponentsInChildren<BoxCollider>(true))
            {
                if (b.isTrigger) continue;
                Vector3 sz = Vector3.Scale(b.size, b.transform.lossyScale);
                w = Mathf.Max(w, Mathf.Max(sz.x, sz.z));
            }
            return w > 0.1f ? w : 2f;
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
