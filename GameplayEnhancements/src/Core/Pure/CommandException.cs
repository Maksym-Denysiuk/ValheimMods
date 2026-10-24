// A user-facing command error: CommandBase.Run turns it into "ERROR: <message>" without a stack trace in the log.
using System;

namespace HomeCheat
{
    public sealed class CommandException : Exception
    {
        public CommandException(string message) : base(message) { }
    }
}
