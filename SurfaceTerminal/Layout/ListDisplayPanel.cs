using System;
using System.Collections;
using System.Collections.Generic;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;

namespace MandalaLogics.SurfaceTerminal.Layout
{
    public class ListDisplayPanel : SurfacePanel, IList<SurfaceLine>
    {
        public override bool CanBeSelected => false;

        private readonly List<SurfaceLine> _lines = new List<SurfaceLine>();
        
        public override void Render(ISurface<ConsoleChar> surface, ulong frameNumber)
        {
            var y = -1;
            
            foreach (var line in _lines)
            {
                if (++y >= surface.Height) return;

                var slice = surface.SliceLine(y);
                
                line.Render(slice, frameNumber);
            }
        }

        protected override void OnDeselected() { }

        protected override void OnSelected() { }

        protected override void OnKeyPressed(ConsoleKeyInfo keyInfo) { }

        protected override bool OnLineStateTryChange(SurfaceLine line, SurfaceLineState newState) => true;
        
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

        public bool Remove(SurfaceLine item)
        {
            return _lines.Remove(item);
        }

        public int Count => _lines.Count;

        public bool IsReadOnly => ((ICollection<SurfaceLine>)_lines).IsReadOnly;

        public int IndexOf(SurfaceLine item)
        {
            return _lines.IndexOf(item);
        }

        public void Insert(int index, SurfaceLine item)
        {
            _lines.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _lines.RemoveAt(index);
        }

        public SurfaceLine this[int index]
        {
            get => _lines[index];
            set => _lines[index] = value;
        }
    }
}