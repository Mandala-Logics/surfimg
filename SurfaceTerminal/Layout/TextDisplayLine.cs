using System;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;

namespace MandalaLogics.SurfaceTerminal.Layout
{
    public class TextDisplayLine : SurfaceLine
    {
        public ConsoleString Text { get; set; } = ConsoleString.Empty;
        public ConsoleDecoration Decoration { get; set; } = default;
        public SurfaceWriteOptions Options { get; set; } = SurfaceWriteOptions.None;
        
        public override void Render(ISurface<ConsoleChar> surface, ulong frameNumber)
        {
            Text.WriteToSurface(surface, Options, 0, 0);
        }

        protected override bool StateChangeRequested(SurfaceLineState state) => false;
        protected override void OnKeyPressed(ConsoleKeyInfo keyInfo) { }
    }
}