using System;
using MandalaLogics.Path;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;

namespace MandalaLogics.SurfaceTerminal.Layout.Components;

public class FileDialogLine : SurfaceLine
{
    public PathBase Path { get; set; }

    private readonly string? _displayText;
    
    public FileDialogLine(PathBase path)
    {
        Path = path;
    }
    
    public FileDialogLine(PathBase path, string displayText)
    {
        Path = path;
        _displayText = displayText;
    }
    
    public override void Render(ISurface<ConsoleChar> surface, ulong frameNumber)
    {
        var csb = new ConsoleStringBuilder();

        var decoration = new ConsoleDecoration(null, Selected ? ConsoleColor.DarkGray : null);

        if (_displayText is null)
        {
            if (Path.IsDir) csb.Append("→ ", decoration);
            
            csb.Append(Path.EndPointName, decoration);
        }
        else
        {
            csb.Append(_displayText, decoration);
        }
        
        csb.GetConsoleString().WriteToSurface(surface, SurfaceWriteOptions.None, 0, 0);
    }

    protected override bool StateChangeRequested(SurfaceLineState state) => true;

    protected override void OnKeyPressed(ConsoleKeyInfo keyInfo) { }
}