// teleport to=map: while a click is armed, the large map's left click teleports instead of selecting a pin; closing the map disarms.
// Applied by TeleportCommand.EnsurePatched (Patches.Patch, once a world is loaded); does nothing while no click is armed.
using System;

namespace HomeCheat.Features.TeleportCommands
{
    public static class TeleportMapPatch
    {
        // prefix on Minimap.OnMapLeftClick: false = the game's pin handling is skipped for this click
        public static bool ClickPrefix(Minimap __instance)
        {
            TeleportCommand c = TeleportCommand.Current;
            if (c == null || !c.MapArmed) return true;
            try { c.MapClicked(__instance); }
            catch (CommandException e) { Kit.Notify.Error("ERROR: " + e.Message); }
            catch (Exception e) { HomeApi.LogError("teleport map click: " + e); Kit.Notify.Error("ERROR: teleport failed, see the log"); }
            return false;
        }

        // postfix on Minimap.SetMapMode
        public static void ModePostfix()
        {
            TeleportCommand c = TeleportCommand.Current;
            if (c != null) c.MapModeChanged();
        }
    }
}
