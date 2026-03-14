using System;
using System.Collections;
using System.Collections.Generic;

namespace MandalaLogics.SurfaceTerminal.Surfaces
{
    public class CompositeSurface<T> : ISurface<T>
    {
        private readonly ISurface<T> _surface;
        public SurfaceRect Bounds { get; }

        public int Count => _surface.Count;
        public int Width => _surface.Width;
        public int Height => _surface.Height;

        public T this[int x, int y]
        {
            get
            {
                if (x < Bounds.Left || x >= Bounds.Right || y < Bounds.Top || y >= Bounds.Bottom)
                    throw new SurfaceException(SurfaceExceptionReason.OutOfBounds);
                
                return _surface[x - Bounds.Left, y - Bounds.Top];
            }
            set
            {
                if (x < Bounds.Left || x >= Bounds.Right || y < Bounds.Top || y >= Bounds.Bottom)
                    throw new SurfaceException(SurfaceExceptionReason.OutOfBounds);

                _surface[x - Bounds.Left, y - Bounds.Top] = value;
            }
        }

        public CompositeSurface(SurfaceRect bounds)
        {
            Bounds = bounds;
            _surface = new Surface<T>(bounds.Width, bounds.Height);
        }

        public CompositeSurface(ISurface<T> surface, int top, int left)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
            Bounds = new SurfaceRect(top, left, surface.Width, surface.Height);
        }

        public ISurface<T> Slice(SurfaceRect slice)
        {
            var intersection = Bounds.GetIntersection(slice);

            if (intersection.IsEmpty)
                throw new SurfaceException(SurfaceExceptionReason.SliceNotInBounds);

            var localRect = new SurfaceRect(
                top: intersection.Top - Bounds.Top,
                left: intersection.Left - Bounds.Left,
                width: intersection.Width,
                height: intersection.Height);

            return new CompositeSurface<T>(new SurfaceView<T>(_surface, localRect), intersection.Top, intersection.Left);
        }

        public void Paste(ISurface<T> source, int x, int y)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            var placed = new SurfaceRect(y, x, source.Width, source.Height);
            var intersection = Bounds.GetIntersection(placed);

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

        public void Fill(T value) => _surface.Fill(value);

        public ISurface<T> SliceLine(int line)
        {
            return Slice(new SurfaceRect(line, Bounds.Left, Bounds.Width, 1));
        }

        public bool TryGet(int x, int y, out T value)
        {
            if (x < Bounds.Left || x >= Bounds.Right || y < Bounds.Top || y >= Bounds.Bottom)
            {
                value = default;
                return false;
            }

            value = _surface[x - Bounds.Left, y - Bounds.Top];
            return true;
        }

        public bool TrySet(int x, int y, T value)
        {
            if (x < Bounds.Left || x >= Bounds.Right || y < Bounds.Top || y >= Bounds.Bottom)
                return false;

            _surface[x - Bounds.Left, y - Bounds.Top] = value;
            return true;
        }

        public IEnumerator<T> GetEnumerator() => new SurfaceEnumerator<T>(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
