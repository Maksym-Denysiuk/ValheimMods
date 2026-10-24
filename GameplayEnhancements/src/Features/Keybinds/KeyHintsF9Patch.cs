// vanilla F9 cycles the gamepad controller layout; with the key free it can be a console bind (F9 save)
using System.Collections.Generic;
using HarmonyLib;

namespace HomeCheat.Features.Keybinds
{
    [HarmonyPatch(typeof(KeyHints), "Update")]
    internal static class KeyHintsF9Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> ins)
        {
            return KeybindsCommand.Retarget(ins, KeybindsCommand.F9, 0, "KeyHints.Update");
        }
    }
}
