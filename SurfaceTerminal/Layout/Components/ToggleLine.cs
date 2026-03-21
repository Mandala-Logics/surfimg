using System;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;

namespace MandalaLogics.SurfaceTerminal.Layout.Components;

public class ToggleLine : SurfaceLine
{
    public event EventHandler? ToggleChanged;
    
    public bool ToggleState
    {
        get => _toggle;
        set
        {
            if (value == _toggle) return;

            _toggle = value;
            
            ToggleChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    
    public string ToggleName { get; }

    private bool _toggle;

    public ToggleLine(string toggleName, bool initialState)
    {
        ToggleName = toggleName;
        _toggle = initialState;
    }
    
    public override void Render(ISurface<ConsoleChar> surface, ulong frameNumber)
    {
        var csb = new ConsoleStringBuilder();
        
        csb.Append(ToggleName,
            new ConsoleDecoration(null, Selected ? ConsoleColor.DarkGray : null));

        csb.Append(": ");
        
        csb.Append(_toggle ? "True" : "False");
        
        csb.GetConsoleString().WriteToSurface(surface, SurfaceWriteOptions.Centered, 0, 0);
    }

    protected override bool StateChangeRequested(SurfaceLineState state) => true;

    protected override void OnKeyPressed(ConsoleKeyInfo keyInfo)
    {
        if (!Enabled) return;

        if (keyInfo.Key is ConsoleKey.Enter or ConsoleKey.RightArrow or ConsoleKey.LeftArrow)
        {
            _toggle = !_toggle;
            
            ToggleChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}