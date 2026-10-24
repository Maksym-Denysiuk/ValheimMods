using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HomeCheat.Kit;
using UnityEngine;

namespace HomeCheat.Features.TransformCommands
{
    // Lives on ANOTHER player's object in the host game. Spawns one networked Crow (AI removed) that follows that player's real position
    // and shows the sitting model on the ground / the flapping one in the air, hides the player's human renderers on this screen,
    // and removes everything on revert, on duration end, or when the player leaves.
    [DefaultExecutionOrder(1000)]
    public sealed class TransformRemote : MonoBehaviour
    {
        static readonly int landedHash = (int)typeof(Player).Assembly.GetType("ZDOVars").GetField("s_landed", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
        Player player;
        string prefabName;
        float until, nextClaim, nextHide;
        GameObject netCrow;
        ZNetView netView;
        ZSyncAnimation netAnim;
        readonly List<Renderer> netRenderers = new List<Renderer>();
        readonly List<Renderer> hidden = new List<Renderer>();
        readonly List<Renderer> scratch = new List<Renderer>();

        public void Init(Player p, string prefab, float duration)
        {
            player = p; prefabName = prefab; until = duration > 0f ? Time.time + duration : 0f;
        }

        public static int RemoveAll(Player p)
        {
            int n = 0;
            foreach (MonoBehaviour mb in p.GetComponents<MonoBehaviour>())
                if (mb != null && mb.GetType().Name == "TransformRemote") { UnityEngine.Object.DestroyImmediate(mb); n++; }
            return n;
        }

        void LateUpdate()
        {
            if (player == null) { Destroy(this); return; }
            if (until > 0f && Time.time >= until) { Destroy(this); return; }
            // hide their body on this screen
            GameObject vis = player.GetVisual();
            if (vis != null)
            {
                scratch.Clear();
                vis.GetComponentsInChildren<Renderer>(true, scratch);
                foreach (Renderer r in scratch) if (r != null && r.enabled) { r.enabled = false; hidden.Add(r); }
            }
            if (netCrow == null || netView == null || !netView.IsValid())
            {
                if (netCrow != null && ZNetScene.instance != null) ZNetScene.instance.Destroy(netCrow);
                netCrow = Prefabs.Spawn(prefabName, player.transform.position, player.transform.rotation);
                if (netCrow == null) { Destroy(this); return; }
                netView = netCrow.GetComponent<ZNetView>();
                netAnim = netCrow.GetComponent<ZSyncAnimation>();
                RandomFlyingBird b = netCrow.GetComponent<RandomFlyingBird>();
                if (b != null) UnityEngine.Object.DestroyImmediate(b);
                foreach (Collider c in netCrow.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                netRenderers.Clear();
                netRenderers.AddRange(netCrow.GetComponentsInChildren<Renderer>(true));
                HomeApi.Log("transform: networked " + prefabName + " follows " + player.GetPlayerName());
            }
            netCrow.transform.position = player.transform.position;
            netCrow.transform.rotation = player.transform.rotation;
            // unlike our own form, this crow stays visible on the host screen: it is the only thing standing in for the hidden body
            if (netView != null && netView.IsValid())
            {
                if (Time.time >= nextClaim) { nextClaim = Time.time + 2f; if (!netView.IsOwner()) netView.ClaimOwnership(); }
                if (netView.IsOwner())
                {
                    bool air = !player.IsOnGround();
                    netView.GetZDO().Set(landedHash, !air);
                    if (netAnim != null) netAnim.SetBool("flapping", air);
                }
            }
        }

        void OnDestroy()
        {
            foreach (Renderer r in hidden) if (r != null) r.enabled = true;
            hidden.Clear();
            if (netCrow != null && ZNetScene.instance != null) ZNetScene.instance.Destroy(netCrow);
        }
    }
}
