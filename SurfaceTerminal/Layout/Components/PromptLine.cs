using System;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;

namespace MandalaLogics.SurfaceTerminal.Layout.Components
{
    public class PromptLine : SurfaceLine
    {
        public string Prompt { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public override void Render(ISurface<ConsoleChar> surface, ulong frameNumber)
        {
            if (surface.Width < 2) return;
            
            var builder = new ConsoleStringBuilder();
            
            builder.Append(Prompt + " : ", new ConsoleDecoration(ConsoleColor.Gray, null));

            var displayString = Text;
            
            if (Selected && frameNumber % 32 > 16)
            {
                displayString += '█';
            }
            else
            {
                displayString += ' ';
            }

            builder.Append(displayString, default);

            var cs = builder.GetConsoleString();
            
            cs.WriteToSurface(surface, SurfaceWriteOptions.None, 0, 0);

            if (cs.Count > surface.Width)
            {
                surface[surface.Width - 1, 0] = new ConsoleTextChar('…', default);
            }
        }

        protected override bool StateChangeRequested(SurfaceLineState state)
        {
            switch (state)
            {
                case SurfaceLineState.Selected:
                case SurfaceLineState.Deselected:
                    return true;
                default:
                    return false;
            }
        }

        protected override void OnKeyPressed(ConsoleKeyInfo keyInfo)
        {
            if (char.IsControl(keyInfo.KeyChar))
            {
                switch (keyInfo.Key)
                {
                    case ConsoleKey.Backspace:
                        
                        if (Text.Length == 0) return;

                        Text = Text[..^1];
                            
                        break;
                }
            }
            else if (keyInfo.Modifiers == 0)
            {
                Text += keyInfo.KeyChar;
            }
        }
    }
}