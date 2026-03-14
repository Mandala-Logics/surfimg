using System;

namespace MandalaLogics.CommandParsing
{
    public sealed class CommandParsingException : Exception
    {
        public int Line { get; } = -1;

        public CommandParsingException(string message) : base(message)
        {
        }

        public CommandParsingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public sealed class WrongImplimentationException : Exception
    {
        public WrongImplimentationException(string message) : base(message) {}
    }
}