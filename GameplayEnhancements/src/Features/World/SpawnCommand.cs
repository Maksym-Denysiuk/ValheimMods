using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.WorldCommands
{
    // spawn: any prefab a few metres ahead of the player, with a little jitter
    public sealed class SpawnCommand : CommandBase
    {
        public override string Name { get { return "spawn"; } }
        protected override string Usage { get { return "spawn prefab=<name> [count=1] [dist=4] [level=0 (star level of creatures)]"; } }

        protected override string Execute(Args a)
        {
            Player p = Players.Local;
            string prefab = a.Str("prefab", "");
            int count = a.Int("count", 1), level = a.Int("level", 0);
            float dist = a.Float("dist", 4f);
            GameObject go = Prefabs.Require(prefab, "(try op=list&filter=...)");
            Vector3 pos = p.transform.position + p.transform.forward * dist + Vector3.up;
            int made = 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 jitter = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0f, UnityEngine.Random.Range(-0.5f, 0.5f));
                GameObject inst = (GameObject)UnityEngine.Object.Instantiate(go, pos + jitter, Quaternion.identity);
                Character c = inst.GetComponent<Character>();
                if (c != null && level > 0) c.SetLevel(level);
                made++;
            }
            return "spawned " + made + " " + prefab;
        }
    }
}
