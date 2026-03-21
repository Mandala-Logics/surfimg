using System;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;

namespace MandalaLogics.SurfaceTerminal.Layout.Components
{
    public class EmptyPanel : SurfacePanel
    {
        public override void Render(ISurface<ConsoleChar> surface, ulong frameNumber)
        {
            surface.Fill(ConsoleChar.WhiteSpace);
        }

        protected override void OnDeselected() { }

        protected override void OnSelected() { }

        protected override void OnKeyPressed(ConsoleKeyInfo keyInfo) { }

        protected override bool OnLineStateTryChange(SurfaceLine line, SurfaceLineState newState) => true;
    }
}