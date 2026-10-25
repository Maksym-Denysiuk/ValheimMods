// vanilla F11 saves a game screenshot (GameCamera.ScreenShot) in game and in the main menu; F11 is the stats window key now.
// Steam's own F12 screenshot is not touched; the free-fly camera's mouse-click screenshot stays.
using System.Collections.Generic;
using HarmonyLib;

namespace HomeCheat.Features.Keybinds
{
    [HarmonyPatch(typeof(GameCamera), "LateUpdate")]
    internal static class GameCameraF11Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> ins)
        {
            return KeybindsCommand.Retarget(ins, KeybindsCommand.F11, 0, "GameCamera.LateUpdate");
        }
    }

    [HarmonyPatch(typeof(FejdStartup), "LateUpdate")]
    internal static class FejdStartupF11Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> ins)
        {
            return KeybindsCommand.Retarget(ins, KeybindsCommand.F11, 0, "FejdStartup.LateUpdate");
        }
    }
}
