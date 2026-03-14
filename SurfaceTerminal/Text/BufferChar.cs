using System;

namespace MandalaLogics.SurfaceTerminal.Text
{
    public readonly struct BufferChar : IEquatable<BufferChar>
    {
        public static readonly BufferChar WhiteSpace = new BufferChar(' ', default);
        
        public ConsoleDecoration Decoration { get; }
        public char Char { get; }

        public BufferChar(char c, ConsoleDecoration decoration)
        {
            Char = c;
            Decoration = decoration;
        }

        public bool Equals(BufferChar other)
        {
            return Decoration.Equals(other.Decoration) && Char == other.Char;
        }

        public override bool Equals(object? obj)
        {
            return obj is BufferChar other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Decoration, Char);
        }
    }
}