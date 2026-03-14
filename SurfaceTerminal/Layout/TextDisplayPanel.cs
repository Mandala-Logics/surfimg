using System;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;

namespace MandalaLogics.SurfaceTerminal.Layout
{
    public class TextDisplayPanel : SurfacePanel
    {
        public override bool CanBeSelected => false;
        public ConsoleString Text { get; set; }
        public SurfaceWriteOptions Options { get; set; } = SurfaceWriteOptions.None;
        public bool Fill { get; set; } = false;
        

        public void SetText(string text, ConsoleDecoration decoration)
        {
            Text = new ConsoleString(text, decoration);
        }

        public void SetText(string text)
        {
            Text = new ConsoleString(text);
        }
        
        public override void Render(ISurface<ConsoleChar> surface, ulong frameNumber)
        {
            Text.WriteToSurface(surface, Options, 0, 0);
        }

        protected override void OnDeselected() { }

        protected override void OnSelected() { }

        protected override void OnKeyPressed(ConsoleKeyInfo keyInfo) { }

        protected override bool OnLineStateTryChange(SurfaceLine line, SurfaceLineState newState) => true;
    }
}