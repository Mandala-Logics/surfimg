using System;

namespace MandalaLogics.SurfaceTerminal
{
    public class ConsoleScreenException : Exception
    {
        public ConsoleScreenException(string message) : base(message) {}
        public ConsoleScreenException(string message, Exception inner) : base(message, inner) {}
    }
}