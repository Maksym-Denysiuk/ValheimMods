// Guards for "a world is loaded" and "this game hosts it". Use them first in Execute, before touching shared world data
// or another player: they throw a CommandException with a clear answer instead of a NullReferenceException.
namespace HomeCheat.Kit
{
    public static class Host
    {
        // true only while a world is loaded and this game is its server (a local host or a dedicated server)
        public static bool IsServer
        {
            get { return ZNet.instance != null && ZNet.instance.IsServer(); }
        }

        // the loaded world's name; throws when the game is in the main menu or still loading
        public static string RequireWorld()
        {
            if (ZNet.instance == null || ZNetScene.instance == null || ZoneSystem.instance == null)
                throw new CommandException("no world loaded: start or join a world first");
            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0)
                throw new CommandException("the world is still loading (no ObjectDB yet): try again in a moment");
            return ZNet.instance.GetWorldName();
        }

        // RequireWorld, then refuses when another PC hosts the world: shared world data and other players are host-only
        public static string RequireServer()
        {
            string world = RequireWorld();
            if (!ZNet.instance.IsServer())
                throw new CommandException("host only: this game does not host world " + world + " (join as host to use this)");
            return world;
        }
    }
}
