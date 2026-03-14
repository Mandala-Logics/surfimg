using System;
using System.Collections;
using System.Collections.Generic;

namespace MandalaLogics.SurfaceTerminal.Surfaces
{
    public sealed class Surface<T> : ISurface<T>
    {
        public int Count => _buffer.Length;
        public int Width { get; }
        public int Height { get; }

        public T this[int x, int y]
        {
            get => _buffer[x, y];
            set => _buffer[x, y] = value;
        }

        private readonly T[,] _buffer;

        public Surface(int width, int height)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));

            Width = width;
            Height = height;
            _buffer = new T[width, height];
        }

        public ISurface<T> Slice(SurfaceRect slice)
        {
            var bounds = new SurfaceRect(0, 0, Width, Height);
            var intersection = bounds.GetIntersection(slice);

            if (intersection.IsEmpty)
                throw new SurfaceException(SurfaceExceptionReason.SliceNotInBounds);

            return new SurfaceView<T>(this, intersection);
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
                    _buffer[dx, dy] = source[sx, sy];
                }
            }
        }

        public void Fill(T value)
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    _buffer[x, y] = value;
        }

        public ISurface<T> SliceLine(int line)
        {
            var slice = new SurfaceRect(line, 0, Width, 1);
            return Slice(slice);
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

        public IEnumerator<T> GetEnumerator() => new SurfaceEnumerator<T>(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
