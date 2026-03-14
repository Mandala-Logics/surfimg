using System;
using System.Collections;
using System.Collections.Generic;

namespace MandalaLogics.SurfaceTerminal.Surfaces
{
    public class Composite<T> : ISurface<T>
    {
        /// <summary>
        /// The bounding rectangle in composite/global coordinates.
        /// Right/Bottom are exclusive.
        /// </summary>
        public SurfaceRect Bounds { get; private set; }

        public int Width => Bounds.Width;
        public int Height => Bounds.Height;
        public int Count => Bounds.Area;

        public T this[int x, int y]
        {
            get
            {
                if (!Bounds.ContainsPoint(x, y))
                    throw new SurfaceException(SurfaceExceptionReason.OutOfBounds);

                // Topmost wins: last added is considered on top.
                for (int i = _surfaces.Count - 1; i >= 0; i--)
                {
                    if (_surfaces[i].TryGet(x, y, out var val))
                        return val;
                }

                throw new SurfaceException(SurfaceExceptionReason.CompositeEmpty);
            }
            set
            {
                if (!Bounds.ContainsPoint(x, y))
                    throw new SurfaceException(SurfaceExceptionReason.OutOfBounds);

                // Write-through to all surfaces that intersect that point.
                foreach (var surface in _surfaces)
                    surface.TrySet(x, y, value);
            }
        }

        // Ordered list so layering is deterministic.
        private readonly List<CompositeSurface<T>> _surfaces = new List<CompositeSurface<T>>();

        public void Add(CompositeSurface<T> surface)
        {
            if (surface is null) throw new ArgumentNullException(nameof(surface));

            _surfaces.Add(surface);
            CalculateBounds();
        }

        public void Apply(ISurface<T> surface, int x, int y)
        {
            if (surface is null) throw new ArgumentNullException(nameof(surface));

            var cs = new CompositeSurface<T>(surface, y, x);
            _surfaces.Add(cs);

            CalculateBounds();
        }

        private void CalculateBounds()
        {
            if (_surfaces.Count == 0)
            {
                Bounds = default;
                return;
            }

            var top = _surfaces[0].Bounds.Top;
            var left = _surfaces[0].Bounds.Left;
            var right = _surfaces[0].Bounds.Right;
            var bottom = _surfaces[0].Bounds.Bottom;

            for (int i = 1; i < _surfaces.Count; i++)
            {
                var b = _surfaces[i].Bounds;
                if (b.Top < top) top = b.Top;
                if (b.Left < left) left = b.Left;
                if (b.Right > right) right = b.Right;
                if (b.Bottom > bottom) bottom = b.Bottom;
            }

            Bounds = new SurfaceRect(top, left, right - left, bottom - top);
        }

        public ISurface<T> Slice(SurfaceRect slice)
        {
            var intersection = Bounds.GetIntersection(slice);

            if (intersection.IsEmpty)
                throw new SurfaceException(SurfaceExceptionReason.SliceNotInBounds);

            var result = new Composite<T>();

            foreach (var surface in _surfaces)
            {
                if (!surface.Bounds.Intersects(intersection))
                    continue;

                if (surface.Slice(intersection) is CompositeSurface<T> cs)
                    result.Add(cs);
            }

            if (result._surfaces.Count == 0)
                throw new SurfaceException(SurfaceExceptionReason.CompositeEmpty);

            return result;
        }

        public void Paste(ISurface<T> source, int x, int y)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (_surfaces.Count == 0) return;

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

        public void Fill(T value)
        {
            foreach (var surface in _surfaces)
                surface.Fill(value);
        }

        public ISurface<T> SliceLine(int line)
        {
            return Slice(new SurfaceRect(line, Bounds.Left, Bounds.Width, 1));
        }

        public bool TryGet(int x, int y, out T value)
        {
            if (!Bounds.ContainsPoint(x, y))
            {
                value = default;
                return false;
            }

            for (int i = _surfaces.Count - 1; i >= 0; i--)
            {
                if (_surfaces[i].TryGet(x, y, out value))
                    return true;
            }

            value = default;
            return false;
        }

        public bool TrySet(int x, int y, T value)
        {
            if (!Bounds.ContainsPoint(x, y))
                return false;

            var any = false;
            foreach (var surface in _surfaces)
                any |= surface.TrySet(x, y, value);

            return any;
        }

        public IEnumerator<T> GetEnumerator() => new SurfaceEnumerator<T>(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}