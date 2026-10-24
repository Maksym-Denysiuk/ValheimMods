// Visual or sound prefabs at a point, removed again after a lifetime (most fx_ prefabs clean themselves up; looping ones do not).
// The removal timer belongs to the command that spawned the effect, so unloading the command cancels it.
using UnityEngine;

namespace HomeCheat.Kit
{
    public static class Fx
    {
        // spawn 'prefab' at 'pos'; after 'life' seconds (0 = never) it is destroyed. null when the prefab is unknown.
        public static GameObject Spawn(CommandBase owner, string prefab, Vector3 pos, float life)
        {
            GameObject go = Prefabs.Spawn(prefab, pos, Quaternion.identity);
            if (go != null && life > 0f) owner.Later(life, delegate { Destroy(go); });
            return go;
        }

        // networked objects through ZNetScene (synced to everyone), local ones through Unity
        public static void Destroy(GameObject go)
        {
            if (go == null) return;
            ZNetView nv = go.GetComponent<ZNetView>();
            if (nv != null && nv.IsValid()) ZNetScene.instance.Destroy(go);
            else UnityEngine.Object.Destroy(go);
        }
    }
}
