using System;
using System.Collections.Generic;
using System.Linq;

namespace MandalaLogics.SurfaceTerminal.Text
{
    public sealed class ConsoleStringBuilder
    {
        public int Length => _parts.Sum(arr => arr.Length);
        
        private readonly List<ConsoleChar[]> _parts;

        public ConsoleStringBuilder()
        {
            _parts = new List<ConsoleChar[]>();
        }

        public ConsoleStringBuilder(int capacity)
        {
            _parts = new List<ConsoleChar[]>(capacity);
        }

        public void RemoveLast()
        {
            _parts.RemoveAt(_parts.Count - 1);
        }

        public void Clear()
        {
            _parts.Clear();
        }
        
        public void Append(ConsoleChar[] chars)
        {
            _parts.Add(chars);
        }

        public void Append(IEnumerable<ConsoleChar> chars)
        {
            _parts.Add(chars.ToArray());
        }

        public void Append(ConsoleChar c)
        {
            _parts.Add(new ConsoleChar[] { c });
        }
        
        public void Append(char c)
        {
            _parts.Add(new ConsoleChar[] { new ConsoleTextChar(c, new ConsoleDecoration()) });
        }
        
        public void Append(char c, ConsoleDecoration decoration)
        {
            _parts.Add(new ConsoleChar[] { new ConsoleTextChar(c, decoration) });
        }

        public void Append(string text)
        {
            var arr = new ConsoleChar[text.Length];

            for (var x = 0; x < arr.Length; x++)
            {
                arr[x] = new ConsoleTextChar(text[x], new ConsoleDecoration());
            }

            _parts.Add(arr);
        }
        
        public void Append(string text, ConsoleDecoration decoration)
        {
            var arr = new ConsoleChar[text.Length];

            for (var x = 0; x < arr.Length; x++)
            {
                arr[x] = new ConsoleTextChar(text[x], decoration);
            }

            _parts.Add(arr);
        }

        public ConsoleString GetConsoleString()
        {
            int count = _parts.Sum(part => part.Length);

            var output = new ConsoleChar[count];
            var pos = -1;

            foreach (var part in _parts)
            {
                foreach (var c in part)
                {
                    pos++;

                    output[pos] = c;
                }
            }

            return new ConsoleString(output);
        }
    }
}