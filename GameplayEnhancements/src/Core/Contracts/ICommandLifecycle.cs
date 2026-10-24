// Called by the registry: Load after the command is registered, Unload when a newer build of its DLL replaces it.
// Constructors only set fields; everything with a side effect (patches, GameObjects, files) belongs in Load.
namespace HomeCheat
{
    public interface ICommandLifecycle
    {
        void Load();
        void Unload();
    }
}
