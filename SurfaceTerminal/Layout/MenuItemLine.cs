using System;
using System.Runtime.InteropServices;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;

namespace MandalaLogics.SurfaceTerminal.Layout
{
    public class MenuItemLine : SurfaceLine
    {
        public event SurfaceLineEventHandler? OnClicked;
        
        public string Text { get; }

        public MenuItemLine(string buttonText)
        {
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
            
            
            cs.WriteToSurface(surface, SurfaceWriteOptions.None, 0, 0);
        }

        protected override bool StateChangeRequested(SurfaceLineState state)
        {
            switch (state)
            {
                case SurfaceLineState.Selected:
                case SurfaceLineState.Deselected:
                    return true;
                case SurfaceLineState.Enabled:
                    return true;
                case SurfaceLineState.Disabled:
                    return true;
                case SurfaceLineState.Activated:
                    OnClicked?.Invoke(this);
                    return false;
                default:
                    return false;
            }
        }

        protected override void OnKeyPressed(ConsoleKeyInfo keyInfo)
        {
            throw new NotImplementedException();
        }
    }
}