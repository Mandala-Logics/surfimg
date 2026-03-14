using System;

namespace MandalaLogics.SurfaceTerminal.Text
{
    public readonly struct ConsoleDecoration : IEquatable<ConsoleDecoration>
    {
        public ConsoleColor ForeColour => _fore ?? SurfaceTerminal.ForeColour;
        public ConsoleColor BackColour => _back ?? SurfaceTerminal.BackColour;
        
        private readonly ConsoleColor? _fore;
        private readonly ConsoleColor? _back;

        public ConsoleDecoration(ConsoleColor? foreColour, ConsoleColor? backColour)
        {
            _fore = foreColour;
            _back = backColour;
        }

        internal void Apply()
        {
            System.Console.BackgroundColor = BackColour;
            System.Console.ForegroundColor = ForeColour;
        }

        public bool Equals(ConsoleDecoration other)
        {
            return _fore == other._fore && _back == other._back;
        }

        public override bool Equals(object? obj)
        {
            return obj is ConsoleDecoration other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_fore, _back);
        }
    }
}