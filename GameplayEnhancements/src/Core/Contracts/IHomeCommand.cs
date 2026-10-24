// The minimal contract of a command. 'op=<Name>&k=v...' reaches Run on the Unity main thread.
// Prefer CommandBase: it adds typed Args, sub-actions, error handling and the load/unload lifecycle.
using System.Collections.Generic;

namespace HomeCheat
{
    public interface IHomeCommand
    {
        string Name { get; }
        string Help { get; }
        string Run(Dictionary<string, string> a);
    }
}
