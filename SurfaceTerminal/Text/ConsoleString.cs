using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MandalaLogics.SurfaceTerminal.Surfaces;

namespace MandalaLogics.SurfaceTerminal.Text
{
    [Flags]
    public enum SurfaceWriteOptions
    {
        None = 0,
        Centered = 0b1,
        WrapText = 0b10,
        RightJustify = 0b100
    }
    
    public readonly struct ConsoleString : IReadOnlyList<ConsoleChar>
    {
        public static readonly ConsoleString Empty = new ConsoleString();

        public bool IsNull => _chars is null;
        public bool IsEmpty => IsNull || _chars.Length == 0;
        public int Count => _chars.Length;
        public ConsoleChar this[int index] => _chars[index];
        
        private readonly ConsoleChar[] _chars;
        
        public ConsoleString(IEnumerable<ConsoleChar> chars)
        {
            _chars = chars.ToArray();
        }

        public ConsoleString(string text, ConsoleDecoration decoration)
        {
            _chars = new ConsoleChar[text.Length];

            for (var x = 0; x < text.Length; x++)
            {
                _chars[x] = new ConsoleTextChar(text[x], decoration);
            }
        }
        
        public ConsoleString(string text)
        {
            _chars = new ConsoleChar[text.Length];

            for (var x = 0; x < text.Length; x++)
            {
                _chars[x] = new ConsoleTextChar(text[x], default);
            }
        }

        internal ConsoleString(ConsoleChar[] chars)
        {
            _chars = chars;
        }

        public ConsoleString(ConsoleChar c)
        {
            _chars = new ConsoleChar[] { c };
        }

        public ConsoleString Slice(int offset, int count)
        {
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));

            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

            if (offset + count > _chars.Length)
                throw new InvalidOperationException(
                    "The sum of offset and count is greater than the length of the string.");

            if (count == 0) return Empty;

            return new ConsoleString(_chars[offset .. (offset + count)]);
        }

        public List<ConsoleString> BreakByWhitespace(out List<ConsoleChar> whitespace)
        {
            var ls = new List<ConsoleChar>();
            var ret = new List<ConsoleString>();
            
            var spaces = new List<ConsoleChar>();
            
            for (var x = 0; x < Count; x++)
            {
                if (this[x].IsWhiteSpace)
                {
                    if (ls.Count == 0) continue;

                    spaces.Add(this[x]);
                    
                    ret.Add(new ConsoleString(ls));
                    ls.Clear();
                }
                else
                {
                    ls.Add(this[x]);
                }
            }
            
            if (ls.Count > 0) ret.Add(new ConsoleString(ls));

            whitespace = spaces;
            return ret;
        }

        public List<ConsoleString> BreakIntoSegments(int segmentLength)
        {
            var ls = new List<ConsoleChar>(Count / segmentLength + 1);
            var ret = new List<ConsoleString>();
            
            foreach (var c in this)
            {
                ls.Add(c);

                if (ls.Count >= segmentLength)
                {
                    ret.Add(new ConsoleString(ls));
                    ls.Clear();
                }
            }

            if (ls.Count > 0)
            {
                ret.Add(new ConsoleString(ls));
            }

            return ret;
        }

        public void WriteToSurface(ISurface<ConsoleChar> surface, SurfaceWriteOptions options,
            int startX, int startY)
        {
            if (IsNull) return;
            
            if (startX < 0 || startX >= surface.Width)
                throw new ArgumentOutOfRangeException(nameof(startX));

            if (startY < 0 || startY >= surface.Height)
                throw new ArgumentOutOfRangeException(nameof(startY));

            if (options.HasFlag(SurfaceWriteOptions.WrapText))
            {
                var words = BreakByWhitespace(out var ws);
                using var e = ws.GetEnumerator();
                
                if (words.Count == 0) return;

                var lines = new List<ConsoleStringBuilder> { new ConsoleStringBuilder() };
                var l = 0;

                int w = surface.Width - startX; // first line starts at startX

                foreach (var word in words)
                {
                    e.MoveNext();
                    
                    if (word.Count + 1 <= w - lines[l].Length)
                    {
                        // word fits on current line
                        lines[l].Append(word);

                        if (e.Current?.Char != '\n')
                        {
                            lines[l].Append(e.Current ?? ConsoleChar.WhiteSpace);
                        }
                    }
                    else
                    {
                        // move to next line if current one already has content
                        if (lines[l].Length > 0)
                        {
                            lines.Add(new ConsoleStringBuilder());
                            l++;
                            w = surface.Width; // subsequent lines start at x = 0

                            if (word.Count + 1 <= w)
                            {
                                lines[l].Append(word);
                                lines[l].Append(e.Current ?? ConsoleChar.WhiteSpace);
                                continue;
                            }
                        }

                        // word is too long for one line: split it
                        var parts = word.BreakIntoSegments(w);

                        bool firstPart = true;

                        foreach (var part in parts)
                        {
                            if (!firstPart || lines[l].Length > 0)
                            {
                                lines.Add(new ConsoleStringBuilder());
                                l++;
                            }

                            lines[l].Append(part);
                            firstPart = false;
                        }

                        if (lines[l].Length < w)
                            lines[l].Append(e.Current ?? ConsoleChar.WhiteSpace);
                    }

                    if ((e.Current?.GetChar(0UL) ?? ' ') == '\n')
                    {
                        lines.Add(new ConsoleStringBuilder());
                        l++;
                    }
                }

                if (lines[l].Length > 0)
                    lines[l].RemoveLast(); // remove trailing space from final line

                for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                {
                    int y = startY + lineIndex;
                    if (y >= surface.Height) return;

                    var s = lines[lineIndex].GetConsoleString();

                    int lineStartX = lineIndex == 0 ? startX : 0;
                    int availableWidth = surface.Width - lineStartX;

                    int leadingSpaces;
                    if (options.HasFlag(SurfaceWriteOptions.Centered))
                    {
                        leadingSpaces = Math.DivRem(availableWidth - s.Count, 2, out var rem) + rem;
                        if (leadingSpaces < 0) leadingSpaces = 0;
                    }
                    else if (options.HasFlag(SurfaceWriteOptions.RightJustify))
                    {
                        leadingSpaces = availableWidth - s.Count;
                        if (leadingSpaces < 0) leadingSpaces = 0;
                    }
                    else
                    {
                        leadingSpaces = 0;
                    }

                    int x = lineStartX;

                    for (int i = 0; i < leadingSpaces && x < surface.Width; i++, x++)
                        surface[x, y] = ConsoleChar.WhiteSpace;

                    for (int i = 0; i < s.Count && x < surface.Width; i++, x++)
                        surface[x, y] = s[i];
                }
            }
            else
            {
                // single-line write from the requested start position
                surface = surface.Slice(new SurfaceRect(startY, startX, surface.Width - startX, 1));
                WriteToSurface(surface, options | SurfaceWriteOptions.WrapText, 0, 0);
            }
        }

        public IEnumerator<ConsoleChar> GetEnumerator() => ((IEnumerable<ConsoleChar>)_chars).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _chars.GetEnumerator();

        public override string ToString()
        {
            var chars = new char[Count];

            for (var x = 0; x < Count; x++)
            {
                chars[x] = _chars[x].GetChar(0);
            }

            return new string(chars);
        }

        public static ConsoleString operator +(ConsoleString a, ConsoleString b)
        {
            var chars = new ConsoleChar[a.Count + b.Count];

            int pos = -1;

            foreach (var c in a)
            {
                pos++;

                chars[pos] = c;
            }

            foreach (var c in b)
            {
                pos++;

                chars[pos] = c;
            }

            return new ConsoleString(chars);
        }

        public static implicit operator ConsoleString(string text)
        {
            return new ConsoleString(text);
        }

        public static ConsoleString BlankString(int count)
        {
            return new string(' ', count);
        }
    }
}