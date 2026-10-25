// The player left the world: logout, a lost connection, or quit. ZNetScene.OnDestroy runs on every one of these exits, once
// the net scene is torn down (Game.Logout is not enough: it can wait for a disk-space dialog and a client that loses its host
// never calls it). Checked with il-reader on Valheim 1.0.20: OnDestroy only logs "Net scene destroyed" and clears s_instance.
// The plugin sets OnExit: the registry cancels every timer and destroys the commands' world objects (CommandBase.WorldExit).
using System;
using HarmonyLib;

namespace HomeCheat.Builtins
{
    [HarmonyPatch(typeof(ZNetScene), "OnDestroy")]
    internal static class WorldExitPatch
    {
        internal static Action OnExit;

        static void Postfix()
        {
            try { if (OnExit != null) OnExit(); }
            catch { }
        }
    }
}
