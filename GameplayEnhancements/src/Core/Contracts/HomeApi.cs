// The one static entry point to the core services, for command DLLs. The plugin sets it up in Awake; there is no DI container.
//   Exec("op=heal")   runs a command through the same dispatcher as the inbox (main thread only)
//   Log / LogError    BepInEx log of the plugin
//   Scheduler         owner-scoped timers (CommandBase.Later wraps it)
using System;
using HomeCheat.Services;

namespace HomeCheat
{
    public static class HomeApi
    {
        internal static CommandDispatcher Dispatcher;
        internal static CommandRegistry Registry;
        internal static Scheduler Timers;
        internal static Action<string> InfoSink, ErrorSink;

        public static Scheduler Scheduler { get { return Timers; } }

        public static string Exec(string form)
        {
            if (Dispatcher == null) return "ERROR: HomeCheat core not started";
            return Dispatcher.Exec(form);
        }

        public static void Log(string msg)
        {
            if (InfoSink != null) InfoSink(msg);
        }

        public static void LogError(string msg)
        {
            if (ErrorSink != null) ErrorSink(msg);
        }
    }
}
