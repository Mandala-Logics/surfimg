using System;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;

namespace MandalaLogics.SurfaceTerminal.Layout.Components
{
    public class TextInputPanel : SurfacePanel
    {
        public ConsoleDecoration TextDecoration { get; set; } = default;
        public string Text { get; set; } = string.Empty;
        public SurfaceWriteOptions TextDisplay { get; set; } = SurfaceWriteOptions.None;
        
        public override void Render(ISurface<ConsoleChar> surface, ulong frameNumber)
        {
            string text = Text;
            
            if (IsSelected && frameNumber % 32 > 16)
            {
                text += '█';
            }
            else
            {
                text += ' ';
            }
            
            var s = new ConsoleString(text, TextDecoration);
            
            s.WriteToSurface(surface, TextDisplay, 0, 0);
        }

        protected override void OnDeselected() { }

        protected override void OnSelected() { }

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
                    
                    case ConsoleKey.Enter:

                        Text += '\n';

                        break;
                }
            }
            else
            {
                Text += keyInfo.KeyChar;
            }
        }

        protected override bool OnLineStateTryChange(SurfaceLine line, SurfaceLineState newState) => true;
    }
}