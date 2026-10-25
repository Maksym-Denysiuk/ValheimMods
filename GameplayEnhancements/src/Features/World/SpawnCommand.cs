using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // spawn: any prefab a few metres ahead of the player, with a little jitter.
    // player=<nick> spawns ahead of that player instead (another player: host only); yaw= / pitch= turn each object.
    // Item prefabs (ItemDrop) fall to the ground there, as they do with place; creatures get level= stars.
    public sealed class SpawnCommand : CommandBase
    {
        const int MaxCount = 100;

        public override string Name { get { return "spawn"; } }
        protected override string Usage
        {
            get
            {
                return "spawn prefab=<name> [count=1 (max " + MaxCount + ")] [dist=4] [level=0 (star level of creatures)]"
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
            Caps.Check("count", count, MaxCount);
            GameObject go = Prefabs.Require(prefab, "(try op=list&filter=...)");
            Player at = p;
            string nick = a.Str("player", "");
            if (nick.Length > 0)
            {
                at = Players.Find(nick);
                if (at == null) throw new CommandException("player '" + nick + "' is not online");
                if (at != p) Host.RequireServer();
            }
            Vector3 pos = at.transform.position + at.transform.forward * dist + Vector3.up;
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            bool item = go.GetComponent<ItemDrop>() != null;
            int made = 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 jitter = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0f, UnityEngine.Random.Range(-0.5f, 0.5f));
                GameObject inst = (GameObject)UnityEngine.Object.Instantiate(go, pos + jitter, rot);
                Character c = inst.GetComponent<Character>();
                if (c != null && level > 0) c.SetLevel(level);
                if (c != null && tamed && !(c is Player)) c.SetTamed(true);
                made++;
            }
            string who = at == p ? "" : " ahead of " + at.GetPlayerName();
            string turn = yaw != 0f || pitch != 0f ? " (yaw " + Fmt.F(yaw, 0) + ", pitch " + Fmt.F(pitch, 0) + ")" : "";
            return "spawned " + made + " " + prefab + (item ? " (items, dropped)" : "") + (tamed ? " (tamed)" : "") + who + turn;
        }
    }
}
