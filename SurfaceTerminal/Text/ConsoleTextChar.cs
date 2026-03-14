namespace MandalaLogics.SurfaceTerminal.Text
{
    public class ConsoleTextChar : ConsoleChar
    {
        public override ConsoleDecoration Decoration { get; }

        private readonly char _char;
        
        public ConsoleTextChar(char c, ConsoleDecoration decoration)
        {
            _char = c;
            Decoration = decoration;
        }

        public override char GetChar(ulong frameNumber) => _char;
    }
}