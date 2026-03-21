using System;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;
using MandalaLogics.Threading;

namespace MandalaLogics.SurfaceTerminal.Layout.Components;

public class SubLayoutPanel : SurfacePanel
{
    public SurfaceLayout Layout { get; set; } = new SurfaceLayout();
    
    public override void Render(ISurface<ConsoleChar> surface, ulong frameNumber)
    {
        Layout.RootNode.Render(surface, frameNumber);
    }

    protected override void OnDeselected() { }

    protected override void OnSelected() { }

    protected override void OnKeyPressed(ConsoleKeyInfo keyInfo)
    {
        Layout.OnKeyPressed(keyInfo, ThreadController.Null);
    }

    protected override bool OnLineStateTryChange(SurfaceLine line, SurfaceLineState newState) => false;
}