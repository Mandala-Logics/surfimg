using System;
using System.Collections;
using System.Collections.Generic;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;

namespace MandalaLogics.SurfaceTerminal.Layout
{
    public class ListPanel : SurfacePanel, IReadOnlyList<SurfaceLine>
    {
        public override bool CanBeSelected => true;
        public int Count => _lines.Count;
        
        public SurfaceLine this[int index]
        {
            get => _lines[index];
            set => _lines[index] = value;
        }
        
        private readonly List<SurfaceLine> _lines = new List<SurfaceLine>();
        private int _selectedLine = -1;
        
        public override void Render(ISurface<ConsoleChar> surface, ulong frameNumber)
        {
            if (surface.Height < 3) return;
            
            var listSpace = surface.Slice(surface.GetBounds().TakeTop(surface.Height - 1));
            
            var footerSpace = surface.Slice(surface.GetBounds().TakeBottom(1));

            var pages = Math.DivRem(_lines.Count, listSpace.Height, out var rem) + rem > 0 ? 1 : 0;

            var currentPage = Math.DivRem(_selectedLine, listSpace.Height, out _);
            
            if (pages > 1) //we need to render arrows
            {
                ConsoleString cs;
                
                if (currentPage > 0 && currentPage == pages - 1)
                {
                    cs = new ConsoleString("<  ");
                }
                else if (currentPage == 0)
                {
                    cs = new ConsoleString("  >");
                }
                else
                {
                    cs = new ConsoleString("< >");
                }
                
                cs.WriteToSurface(footerSpace, SurfaceWriteOptions.Centered, 0, 0);
            }

            var firstVisible = Math.DivRem(_lines.Count, listSpace.Height, out var lastVisible);

            firstVisible *= listSpace.Height;

            lastVisible += firstVisible;

            var y = -1;

            for (var x = firstVisible; x < lastVisible; x++)
            {
                var slice = 
                    listSpace.Slice(new SurfaceRect(++y, 2, surface.Width - 1, 1));
                
                _lines[x].Render(slice, frameNumber);

                if (_lines[x].Selected)
                {
                    listSpace[0, y] = new ConsoleTextChar('>', default);
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
                    SelectPrevLine();
                    break;
                case ConsoleKey.DownArrow:
                    SelectNextLine();
                    break;
                case ConsoleKey.LeftArrow:
                    SelectPrevLine();
                    break;
                case ConsoleKey.RightArrow:
                    SelectNextLine();
                    break;
                default:
                    if (_selectedLine != -1) _lines[_selectedLine].KeyPressed(keyInfo);
                    break;
            }
        }

        protected override bool OnLineStateTryChange(SurfaceLine line, SurfaceLineState newState)
        {
            if (newState == SurfaceLineState.Selected)
            {
                if (_selectedLine == -1 || _lines[_selectedLine].TryDeselect())
                {
                    _selectedLine = _lines.IndexOf(line);
                }
            }
            else if (newState == SurfaceLineState.Deselected)
            {
                SelectNextLine();
            }

            return true;
        }

        private void SelectNextLine()
        {
            if (_lines.Count == 0) return;

            if (_selectedLine == -1) return;

            for (var b = _selectedLine + 1; b < _lines.Count; b++)
            {
                if (_lines[b].TrySelect() 
                    && _lines[_selectedLine].TryDeselect())
                {
                    _selectedLine = b;
                    return;
                }
            }

            for (var b = 0; b < _selectedLine; b++)
            {
                if (_lines[b].TrySelect() 
                    && _lines[_selectedLine].TryDeselect())
                {
                    _selectedLine = b;
                    return;
                }
            }
        }

        private void SelectPrevLine()
        {
            if (_lines.Count == 0) return;

            if (_selectedLine == -1) return;
            
            for (var b = _selectedLine - 1; b >= 0; b--)
            {
                if (_lines[b].TrySelect() 
                    && _lines[_selectedLine].TryDeselect())
                {
                    _selectedLine = b;
                    return;
                }
            }

            for (var b = _lines.Count - 1; b > _selectedLine; b--)
            {
                if (_lines[b].TrySelect() 
                    && _lines[_selectedLine].TryDeselect())
                {
                    _selectedLine = b;
                    return;
                }
            }
        }

        public IEnumerator<SurfaceLine> GetEnumerator()
        {
            return _lines.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_lines).GetEnumerator();
        }

        public void Add(SurfaceLine item)
        {
            _lines.Add(item);

            if (_selectedLine == -1 && item.TrySelect())
            {
                _selectedLine = _lines.Count - 1;
            }
        }

        public void Clear()
        {
            _lines.Clear();
        }

        public bool Contains(SurfaceLine item)
        {
            return _lines.Contains(item);
        }

        public void CopyTo(SurfaceLine[] array, int arrayIndex)
        {
            _lines.CopyTo(array, arrayIndex);
        }

        public bool IsReadOnly => ((ICollection<SurfaceLine>)_lines).IsReadOnly;

        public int IndexOf(SurfaceLine item)
        {
            return _lines.IndexOf(item);
        }

        public void Insert(int index, SurfaceLine item)
        {
            _lines.Insert(index, item);
            
            if (_selectedLine == -1 && item.TrySelect())
            {
                _selectedLine = index;
            }
        }

        public void RemoveAt(int index)
        {
            if (_lines[index].State == SurfaceLineState.Selected)
            {
                _lines[index].TryDeselect();
            }
            
            _lines.RemoveAt(index);

            if (_selectedLine >= index) _selectedLine--;
        }
    }
}