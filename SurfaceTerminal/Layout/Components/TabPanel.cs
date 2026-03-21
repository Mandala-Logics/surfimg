using System;
using System.Collections.Generic;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;

namespace MandalaLogics.SurfaceTerminal.Layout.Components;

public sealed class TabPanel : ListPanel
{
    public override void Render(ISurface<ConsoleChar> surface, ulong frameNumber)
    {
        lock (Lines)
        {
            if (surface.Width <= 4) return;
        
            if (surface.Height > 1) surface = surface.SliceLine(0);

            var bounds = surface.GetBounds();

            var arrowSpace = surface.Slice(bounds.TakeRight(3));

            surface = surface.Slice(bounds.TakeLeft(surface.Width - 3));

            var capacity = Math.DivRem(surface.Width, 26, out _);

            if (capacity == 0) return;
            
            using var e = (IEnumerator<KeyValuePair<string, SurfaceLine>>)Lines.GetEnumerator();

            var ls = new Queue<SurfaceLine>();
            var removed = false;
            var reachedEnd = false;

            while (e.MoveNext())
            {
                ls.Enqueue(e.Current.Value);

                if (e.Current.Value.Equals(SelectedLine))
                {
                    while (ls.Count != capacity)
                    {
                        if (ls.Count > capacity)
                        {
                            ls.Dequeue();
                            removed = true;
                        }
                        else
                        {
                            if (!e.MoveNext())
                            {
                                reachedEnd = true;
                                break;
                            }
                                
                            ls.Enqueue(e.Current.Value);
                        }
                    }

                    break;
                }
            }
            
            if (Lines.Count > capacity)
            {
                ConsoleStringBuilder builder = new ConsoleStringBuilder(3);
                
                builder.Append('<', 
                    new ConsoleDecoration(removed ? null : ConsoleColor.DarkGray, null));
                    
                builder.Append(ConsoleChar.WhiteSpace);

                builder.Append('>', 
                    new ConsoleDecoration(reachedEnd ? ConsoleColor.DarkGray : null, null));
                
                builder.GetConsoleString().WriteToSurface(arrowSpace, SurfaceWriteOptions.None, 0, 0);
            }

            var x = 0;

            while (ls.TryDequeue(out var line))
            {
                var slice = surface.Slice(new SurfaceRect(0, x, 24, 1));
                
                line.Render(slice, frameNumber);

                x += 25;
            }
        }
    }

    protected override void OnKeyPressed(ConsoleKeyInfo keyInfo)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.LeftArrow:
            case ConsoleKey.UpArrow:
                SelectPrev();
                break;
            case ConsoleKey.Tab:
            case ConsoleKey.RightArrow:
            case ConsoleKey.DownArrow:
                SelectNext();
                break;
            default:
                SelectedLine?.KeyPressed(keyInfo);
                break;
        }
    }
}