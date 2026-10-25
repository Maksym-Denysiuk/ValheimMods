// Starts the host's group sync when the local player spawns in a world (a world loaded far from every dome loads no
// ward, so PrivateArea.Awake is no signal). The sync itself lives in DomeCommand; this postfix only calls EnsureSync.
using System.Reflection;
using HomeCheat.Kit;

namespace HomeCheat.Features.Dome
{
    public static class DomePatches
    {
        internal static void Apply(DomeCommand cmd)
        {
            MethodInfo target = null;
            foreach (MethodInfo m in typeof(Player).GetMethods(Refl.InstanceAll))
                if (m.Name == "OnSpawned") { target = m; break; }
            if (target == null) { HomeApi.LogError("dome: Player.OnSpawned not found: the group sync starts with the first dome command"); return; }
            cmd.PatchesForDome.Patch(target, null, typeof(DomePatches).GetMethod("OnSpawnedPostfix"));
        }

        public static void OnSpawnedPostfix(Player __instance)
        {
            DomeCommand cmd = DomeCommand.Current;
            if (cmd == null || __instance != Player.m_localPlayer) return;
            cmd.EnsureSync();
        }
    }
}
