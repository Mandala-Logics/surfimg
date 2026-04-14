using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;

namespace MandalaLogics.SurfaceTerminal.Layout.Components
{
    public class ListPanel : SurfacePanel, IReadOnlyDictionary<string, SurfaceLine>
    {
        public event EventHandler? SelectedKeyChanged;
        
        public int Count => Lines.Count;
        public string SelectedKey { get; private set; } = string.Empty;
        public SurfaceLine this[string key] => Lines[key];
        public IEnumerable<string> Keys => ((IReadOnlyDictionary<string, SurfaceLine>)Lines).Keys;
        public IEnumerable<SurfaceLine> Values => ((IReadOnlyDictionary<string, SurfaceLine>)Lines).Values;
        
        protected readonly Dictionary<string, SurfaceLine> Lines = new();
        protected SurfaceLine? SelectedLine = null;
        
        public override void Render(ISurface<ConsoleChar> surface, ulong frameNumber)
        {
            lock (Lines)
            {
                using var e = (IEnumerator<KeyValuePair<string, SurfaceLine>>)Lines.GetEnumerator();

                var h = surface.Height - 1;

                if (h <= 0) return;

                var ls = new Queue<SurfaceLine>();
                var removed = false;
                var reachedEnd = false;

                while (e.MoveNext())
                {
                    ls.Enqueue(e.Current.Value);

                    if (e.Current.Value.Equals(SelectedLine))
                    {
                        while (ls.Count != h)
                        {
                            if (ls.Count > h)
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
                
                if (!e.MoveNext())
                {
                    reachedEnd = true;
                }
                
                if (Lines.Count > h)
                {
                    var builder = new ConsoleStringBuilder(3);

                    builder.Append('↑', 
                        new ConsoleDecoration(removed ? null : ConsoleColor.DarkGray, null));
                
                    builder.Append(ConsoleChar.WhiteSpace);

                    builder.Append('↓', 
                        new ConsoleDecoration(reachedEnd ? ConsoleColor.DarkGray : null, null));
                
                    builder.GetConsoleString()
                        .WriteToSurface(surface, SurfaceWriteOptions.None, 0, surface.Height - 1);
                }

                var y = -1;

                while (ls.TryDequeue(out var line))
                {
                    line.Render(surface.SliceLine(++y), frameNumber);
                }
            }
        }

        protected override void OnDeselected() { }

        protected override void OnSelected() { }

        protected override void OnKeyPressed(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                    SelectPrev();
                    break;
                case ConsoleKey.DownArrow:
                    SelectNext();
                    break;
                default:
                    SelectedLine?.KeyPressed(keyInfo);
                    break;
            }
        }

        protected override bool OnLineStateTryChange(SurfaceLine line, SurfaceLineState newState)
        {
            lock (Lines)
            {
                if (newState == SurfaceLineState.Selected)
                {
                    if (line.Equals(SelectedLine)) return true;
                    else return false;
                }
                else if (newState == SurfaceLineState.Deselected)
                {
                    if (line.Equals(SelectedLine))
                    {
                        SelectNext();

                        return !SelectedLine.Equals(line);
                    }
                
                    return true;
                }
                else
                {
                    return true;
                }
            }
        }

        public void SelectNext()
        {
            if (SelectedLine is null) return;
            
            lock (Lines)
            {
                using var e = (IEnumerator<KeyValuePair<string, SurfaceLine>>)Lines.GetEnumerator();

                while (e.MoveNext())
                {
                    if (SelectedLine.Equals(e.Current.Value))
                    {
                        while (e.MoveNext())
                        {
                            if (e.Current.Value.TrySelect())
                            {
                                SelectedKey = e.Current.Key;
                                SelectedLine.TryDeselect();
                                SelectedLine = e.Current.Value;
                                
                                SelectedKeyChanged?.Invoke(this, EventArgs.Empty);
                                
                                return;

                            }
                        }
                        
                        e.Reset();

                        while (e.MoveNext())
                        {
                            if (SelectedLine.Equals(e.Current.Value)) return;
                            
                            if (e.Current.Value.TrySelect())
                            {
                                SelectedKey = e.Current.Key;
                                SelectedLine.TryDeselect();
                                SelectedLine = e.Current.Value;
                                
                                SelectedKeyChanged?.Invoke(this, EventArgs.Empty);
                                
                                return;

                            }
                        }

                        return;
                    }
                }
            }
        }

        public void SelectPrev()
        {
            if (SelectedLine is null) return;
            
            lock (Lines)
            {
                var ls = new List<KeyValuePair<string, SurfaceLine>>(Lines.Count);
                int s = -1;
                
                foreach (var kvp in Lines)
                {
                    ls.Add(kvp);
                    
                    if (kvp.Value.Selected) s = ls.Count - 1;
                }

                for (var x = s - 1; x >= 0; x--)
                {
                    if (ls[x].Value.TrySelect())
                    {
                        ls[s].Value.TryDeselect();
                        SelectedLine = ls[x].Value;
                        SelectedKey = ls[x].Key;
                        
                        SelectedKeyChanged?.Invoke(this, EventArgs.Empty);
                        
                        return;
                    }
                }

                for (var x = ls.Count - 1; x > s; x++)
                {
                    if (ls[x].Value.TrySelect())
                    {
                        ls[s].Value.TryDeselect();
                        SelectedLine = ls[x].Value;
                        SelectedKey = ls[x].Key;
                        
                        SelectedKeyChanged?.Invoke(this, EventArgs.Empty);
                        
                        return;
                    }
                }
            }
        }

        public void Add(string key, SurfaceLine line)
        {
            lock (Lines)
            {
                if (!Lines.TryAdd(key, line))
                    throw new LayoutException(LayoutExceptionReason.KeyAlreadyExists);

                line.Owner = this;

                if (SelectedLine is null && line.TrySelect())
                {
                    SelectedLine = line;
                    SelectedKey = key;
                    
                    SelectedKeyChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void Clear()
        {
            lock (Lines)
            {
                Lines.Clear();
                SelectedLine = null;
            }
        }

        public IEnumerator<KeyValuePair<string, SurfaceLine>> GetEnumerator()
        {
            return Lines.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Lines).GetEnumerator();
        }

        public bool ContainsKey(string key)
        {
            return Lines.ContainsKey(key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out SurfaceLine value)
        {
            return Lines.TryGetValue(key, out value);
        }

        
    }
}