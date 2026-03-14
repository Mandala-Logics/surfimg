using System;
using System.Collections;
using System.Collections.Generic;

namespace MandalaLogics.SurfaceTerminal.Surfaces
{
    internal sealed class SurfaceView<T> : ISurface<T>
    {
        public int Count => _view.Area;
        public int Width => _view.Width;
        public int Height => _view.Height;

        public T this[int x, int y]
        {
            get => _surface[x + _view.Left, y + _view.Top];
            set => _surface[x + _view.Left, y + _view.Top] = value;
        }

        private readonly ISurface<T> _surface;
        private readonly SurfaceRect _view;

        public SurfaceView(ISurface<T> surface, SurfaceRect view)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
            _view = view;
        }

        public ISurface<T> Slice(SurfaceRect slice)
        {
            var localBounds = new SurfaceRect(0, 0, Width, Height);
            var intersection = localBounds.GetIntersection(slice);

            if (intersection.IsEmpty)
                throw new SurfaceException(SurfaceExceptionReason.SliceNotInBounds);
            
            var rect = new SurfaceRect(
                top: _view.Top + intersection.Top,
                left: _view.Left + intersection.Left,
                width: intersection.Width,
                height: intersection.Height);

            return new SurfaceView<T>(_surface, rect);
        }

        public void Paste(ISurface<T> source, int x, int y)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            
            var placed = new SurfaceRect(y, x, source.Width, source.Height);
            var bounds = new SurfaceRect(0, 0, Width, Height);
            var intersection = bounds.GetIntersection(placed);

            if (intersection.IsEmpty) return;

            for (int dy = intersection.Top; dy < intersection.Bottom; dy++)
            {
                for (int dx = intersection.Left; dx < intersection.Right; dx++)
                {
                    int sx = dx - x;
                    int sy = dy - y;
                    this[dx, dy] = source[sx, sy];
                }
            }
        }

        public void Fill(T value)
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    this[x, y] = value;
        }
        
        public bool TryGet(int x, int y, out T value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                value = default;
                return false;
            }
            else
            {
                value = this[x, y];
                return true;
            }
        }

        public bool TrySet(int x, int y, T value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return false;
            }
            else
            {
                this[x, y] = value;
                return true;
            }
        }

        public ISurface<T> SliceLine(int line)
        {
            return Slice(new SurfaceRect(line, 0, Width, 1));
        }

        public IEnumerator<T> GetEnumerator() => new SurfaceEnumerator<T>(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
