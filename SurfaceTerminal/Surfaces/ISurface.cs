using System.Collections.Generic;

public enum SurfaceDirection { Horizonal, Vertical }

namespace MandalaLogics.SurfaceTerminal.Surfaces
{
    public interface ISurface<T> : IReadOnlyCollection<T>
    {
        public int Width { get; }
        public int Height { get; }
        public T this[int x, int y] { get; set; }

        public ISurface<T> Slice(SurfaceRect slice);

        public void Paste(ISurface<T> source, int x, int y);

        public void Fill(T value);

        public ISurface<T> SliceLine(int line);

        bool TryGet(int x, int y, out T value);

        bool TrySet(int x, int y, T value);
    }
}