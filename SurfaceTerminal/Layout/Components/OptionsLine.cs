using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;

namespace MandalaLogics.SurfaceTerminal.Layout.Components;

public class OptionsLine : SurfaceLine, IReadOnlyDictionary<string, string>
{
    public event EventHandler? SelectedKeyChanged;
    
    public int Count => _options.Count;
    public string this[string key] => _options[key];
    public IEnumerable<string> Keys => ((IReadOnlyDictionary<string, string>)_options).Keys;
    public IEnumerable<string> Values => ((IReadOnlyDictionary<string, string>)_options).Values;
    public string? SelectedKey { get; private set; }
    public string OptionName { get; }

    private readonly Dictionary<string, string> _options = new();

    public OptionsLine(string optionName)
    {
        OptionName = optionName;
    }
    
    public override void Render(ISurface<ConsoleChar> surface, ulong frameNumber)
    {
        if (surface.Width < 5) return;
        
        var x = -1;
            
        foreach (var key in _options.Keys)
        {
            x++;

            if (key.Equals(SelectedKey)) break;
        }

        var csb = new ConsoleStringBuilder();
        
        csb.Append(OptionName,
            new ConsoleDecoration(null, Selected ? ConsoleColor.DarkGray : null));
        
        csb.Append(": ");
        
        csb.Append('<', new ConsoleDecoration(x > 0 ? null : ConsoleColor.DarkGray, null));
        
        csb.Append(ConsoleChar.WhiteSpace);

        if (SelectedKey is not null)
        {
            csb.Append(new ConsoleString(_options[SelectedKey]));
        }
        else
        {
            csb.Append(ConsoleChar.WhiteSpace);
        }
        
        csb.Append(ConsoleChar.WhiteSpace);
        
        csb.Append('>', new ConsoleDecoration(x < _options.Count - 1 ? null : ConsoleColor.DarkGray, null));
        
        csb.GetConsoleString().WriteToSurface(surface, SurfaceWriteOptions.Centered, 0, 0);
    }

    protected override bool StateChangeRequested(SurfaceLineState state) => true;

    protected override void OnKeyPressed(ConsoleKeyInfo keyInfo)
    {
        if (!Enabled) return;

        switch (keyInfo.Key)
        {
            case ConsoleKey.RightArrow:
                TryAdvance();
                break;
            case ConsoleKey.LeftArrow:
                TryReverse();
                break;
        }
    }

    protected void TryAdvance()
    {
        using var e = GetEnumerator();

        while (e.MoveNext())
        {
            if (e.Current.Key.Equals(SelectedKey))
            {
                if (e.MoveNext())
                {
                    SelectedKey = e.Current.Key;
                    
                    SelectedKeyChanged?.Invoke(this, EventArgs.Empty);
                }

                return;
            }
        }
    }
    
    protected void TryReverse()
    {
        using var e = GetEnumerator();
        int x = -1;

        while (e.MoveNext())
        {
            x++;
            
            if (e.Current.Key.Equals(SelectedKey))
            {
                if (x > 0)
                {
                    e.Reset();

                    for (var a = 0; a < x; a++)
                    {
                        e.MoveNext();
                    }

                    SelectedKey = e.Current.Key;
                    
                    SelectedKeyChanged?.Invoke(this, EventArgs.Empty);
                }

                return;
            }
        }
    }

    public void Add(string key, string option)
    {
        _options.Add(key, option);

        if (SelectedKey is null)
        {
            SelectedKey = key;
            
            SelectedKeyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    
    public bool ContainsKey(string key)
    {
        return _options.ContainsKey(key);
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
    {
        return _options.TryGetValue(key, out value);
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        return _options.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_options).GetEnumerator();
    }
}