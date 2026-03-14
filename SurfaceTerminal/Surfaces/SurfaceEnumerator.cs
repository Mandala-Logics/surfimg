using System.Collections;
using System.Collections.Generic;

namespace MandalaLogics.SurfaceTerminal.Surfaces
{
    internal sealed class SurfaceEnumerator<T> : IEnumerator<T>
    {
        public T Current { get; private set; } = default!;
        object? IEnumerator.Current => Current;

        private readonly ISurface<T> _surface;
        private int _y;
        private int _x;

        public SurfaceEnumerator(ISurface<T> surface)
        {
            _surface = surface;
            Reset();
        }

        public bool MoveNext()
        {
            if (++_x >= _surface.Width)
            {
                _x = 0;
                _y++;
            }

            if (_y >= _surface.Height) return false;

            Current = _surface[_x, _y];
            return true;
        }

        public void Reset()
        {
            _y = 0;
            _x = -1;
        }

        public void Dispose() { }
    }
}
