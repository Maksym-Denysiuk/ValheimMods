// Starts the host's trap watcher when the local player spawns in a world (leaving a world cancels every timer, and traps
// placed in an earlier session must still go off or expire). The watcher itself lives in TrapCommand.
using System.Reflection;
using HomeCheat.Kit;

namespace HomeCheat.Features.Traps
{
    public static class TrapPatches
    {
        internal static void Apply(TrapCommand cmd)
        {
            MethodInfo target = null;
            foreach (MethodInfo m in typeof(Player).GetMethods(Refl.InstanceAll))
                if (m.Name == "OnSpawned") { target = m; break; }
            if (target == null) { HomeApi.LogError("trap: Player.OnSpawned not found: the watcher starts with the first trap command"); return; }
            cmd.PatchesForTraps.Patch(target, null, typeof(TrapPatches).GetMethod("OnSpawnedPostfix"));
        }

        public static void OnSpawnedPostfix(Player __instance)
        {
            TrapCommand cmd = TrapCommand.Current;
            if (cmd == null || __instance != Player.m_localPlayer) return;
            cmd.EnsureWatch();
        }
    }
}
