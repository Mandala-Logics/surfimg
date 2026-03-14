using System;

namespace MandalaLogics.SurfaceTerminal.Surfaces
{
    public readonly struct SurfaceRect
    {
        public int Top { get; }
        public int Left { get; }
        public int Width { get; }
        public int Height { get; }

        public int Right => Left + Width;
        public int Bottom => Top + Height;

        public int Area => Width * Height;
        public bool IsEmpty => Width <= 0 || Height <= 0;

        public SurfaceRect(int top, int left, int width, int height)
        {
            Top = top;
            Left = left;
            Width = width;
            Height = height;
        }

        public SurfaceRect GetIntersection(SurfaceRect other)
        {
            int left = Math.Max(Left, other.Left);
            int top = Math.Max(Top, other.Top);
            int right = Math.Min(Right, other.Right);
            int bottom = Math.Min(Bottom, other.Bottom);

            int w = right - left;
            int h = bottom - top;

            if (w <= 0 || h <= 0) return default;

            return new SurfaceRect(top, left, w, h);
        }

        public bool Intersects(SurfaceRect other) => !GetIntersection(other).IsEmpty;

        public bool ContainsPoint(int x, int y)
        {
            return x >= Left && x <= Right && y >= Top && y <= Bottom;
        }

        public bool Contains(SurfaceRect other)
        {
            if (other.IsEmpty) return true; 
            if (IsEmpty) return false;

            return other.Left >= Left
            && other.Top >= Top
            && other.Right <= Right
            && other.Bottom <= Bottom;
        }

        public SurfaceRect TakeTop(int amount)
        {
            if (amount > Height) throw new ArgumentOutOfRangeException(nameof(amount));
            
            return new SurfaceRect(Top, Left, Width, amount);
        }

        public SurfaceRect TakeBottom(int amount)
        {
            if (amount > Height) throw new ArgumentOutOfRangeException(nameof(amount));

            return new SurfaceRect(Top + Height - amount - 1, Left, Width, amount + 1);
        }

        public SurfaceRect TakeLeft(int amount)
        {
            if (amount > Width) throw new ArgumentOutOfRangeException(nameof(amount));
            
            return new SurfaceRect(Top, Left, amount, Height);
        }

        public SurfaceRect TakeRight(int amount)
        {
            if (amount > Width) throw new ArgumentOutOfRangeException(nameof(amount));

            return new SurfaceRect(Top, Left + Width - amount - 1, amount + 1, Height);
        }

        public SurfaceRect TakeCenter(int width, int height)
        {
            if (width > Width || width < 0) throw new ArgumentOutOfRangeException(nameof(width));

            if (height > Height || height < 0) throw new ArgumentOutOfRangeException(nameof(height));

            var xPadding = Math.DivRem(Width - width, 2, out var xRem) + xRem;
            var yPadding = Math.DivRem(Height - height, 2, out var yRem) + yRem;

            return new SurfaceRect(Top + yPadding, Left + xPadding, width, height);
        }
    }
}
