// form text -> parameters -> the command named by op -> answer text. Main thread only: the game is not thread safe.
using System;
using System.Collections.Generic;
using System.Threading;

namespace HomeCheat.Services
{
    internal sealed class CommandDispatcher
    {
        readonly CommandRegistry registry;
        readonly int mainThread;

        public CommandDispatcher(CommandRegistry r)
        {
            registry = r;
            mainThread = Thread.CurrentThread.ManagedThreadId;
        }

        public string Exec(string form)
        {
            return Dispatch(FormParser.Parse(form));
        }

        public string Dispatch(Dictionary<string, string> a)
        {
            if (Thread.CurrentThread.ManagedThreadId != mainThread) return "ERROR: commands run on the Unity main thread only";
            string op;
            if (!a.TryGetValue("op", out op) || op.Length == 0) return "ERROR: op required. ops: " + string.Join(" ", registry.Names().ToArray());
            IHomeCommand c;
            if (!registry.TryGet(op.ToLowerInvariant(), out c))
                return "ERROR: unknown op '" + op + "'. ops: " + string.Join(" ", registry.Names().ToArray());
            try { return c.Run(a); }
            catch (Exception e)
            {
                // CommandBase answers its own errors; this catches plain IHomeCommand implementations
                HomeApi.LogError(op + ": " + e);
                return "ERROR: " + e.Message;
            }
        }
    }
}
