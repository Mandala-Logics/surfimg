namespace MandalaLogics.SurfaceTerminal.Text
{
    public abstract class ConsoleChar
    {
        public static readonly ConsoleChar WhiteSpace = new ConsoleTextChar(' ', new ConsoleDecoration());
        public static readonly ConsoleChar Null = new ConsoleTextChar('\0', new ConsoleDecoration());
        
        public abstract ConsoleDecoration Decoration { get; }

        public char Char => GetChar(0UL);

        public bool IsWhiteSpace
        {
            get
            {
                var c = GetChar(0UL);

                return char.IsWhiteSpace(c) || c == '\n';
            }
        }
        
        /// <summary>
        /// Gets the character represented by this class for the corresponding frameNumber.
        /// </summary>
        /// <param name="frameNumber">The number of the frame being rendered,
        ///  or 0 if the character is required without the frame being rendered
        ///  - for example in the case of rendering to a string</param>
        public abstract char GetChar(ulong frameNumber);

        internal BufferChar GetBufferChar(ulong frameNumber)
        {
            return new BufferChar(GetChar(frameNumber), Decoration);
        }
    }
}