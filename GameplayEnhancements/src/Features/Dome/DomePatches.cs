// Starts the host's group sync when the local player spawns in a world (a world loaded far from every dome loads no
// ward, so PrivateArea.Awake is no signal). The sync itself lives in DomeCommand; this postfix only calls EnsureSync.
// TextInput.IsVisible: true while the ward menu (DomeMenu) is open, so the game frees the cursor and the player takes no input.
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
            if (target == null) HomeApi.LogError("dome: Player.OnSpawned not found: the group sync starts with the first dome command");
            else cmd.PatchesForDome.Patch(target, null, typeof(DomePatches).GetMethod("OnSpawnedPostfix"));

            MethodInfo input = typeof(TextInput).GetMethod("IsVisible", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (input == null) HomeApi.LogError("dome: TextInput.IsVisible not found: the ward menu cannot free the cursor");
            else cmd.PatchesForDome.Patch(input, null, typeof(DomePatches).GetMethod("TextInputPostfix"));
        }

        public static void OnSpawnedPostfix(Player __instance)
        {
            DomeCommand cmd = DomeCommand.Current;
            if (cmd == null || __instance != Player.m_localPlayer) return;
            cmd.EnsureSync();
        }

        public static void TextInputPostfix(ref bool __result)
        {
            if (DomeMenu.Current != null) __result = true;
        }
    }
}
