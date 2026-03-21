using System;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;

namespace MandalaLogics.SurfaceTerminal.Layout.Components
{
    public class MenuItemLine : SurfaceLine
    {
        public string Text { get; }

        private readonly Action? _clickedAction;
        
        public MenuItemLine(string buttonText, Action? clickedAction)
        {
            _clickedAction = clickedAction;
            
            Text = buttonText;
        }
        
        public override void Render(ISurface<ConsoleChar> surface, ulong frameNumber)
        {
            ConsoleString cs;
            
            if (Enabled)
            {
                cs = new ConsoleString($"[{Text}]",
                    Selected ? new ConsoleDecoration(null, ConsoleColor.DarkGray)
                        : default);
            }
            else
            {
                cs = new ConsoleString($"[{Text}]", 
                    new ConsoleDecoration(ConsoleColor.DarkGray, null));
            }
            
            
            cs.WriteToSurface(surface, SurfaceWriteOptions.Centered, 0, 0);
        }

        protected override bool StateChangeRequested(SurfaceLineState state)
        {
            switch (state)
            {
                case SurfaceLineState.Selected:
                case SurfaceLineState.Deselected:
                    return Enabled;
                case SurfaceLineState.Enabled:
                    return true;
                case SurfaceLineState.Disabled:
                    return true;
                case SurfaceLineState.Activated:
                    _clickedAction?.Invoke();
                    return false;
                default:
                    return false;
            }
        }

        protected override void OnKeyPressed(ConsoleKeyInfo keyInfo)
        {
            if (keyInfo.Key == ConsoleKey.Enter)
            {
                _clickedAction?.Invoke();
            }
        }
    }
}