using System;
using System.Collections.Generic;

namespace MandalaLogics.SurfaceTerminal.Surfaces
{
    public static class SurfaceExtensions
    {
        public static SurfaceRect GetBounds<T>(this ISurface<T> surface)
        {
            if (surface is CompositeSurface<T> composite)
            {
                return composite.Bounds;
            }
            else
            {
                return new SurfaceRect(0, 0, surface.Width, surface.Height);
            }
        }
        
        public static int Area<T>(this ISurface<T> surface)
        {
            return surface.Width * surface.Height;
        }

        public static void Write<T>(this ISurface<T> surface, IEnumerable<T> arr)
        {
            using var e = arr.GetEnumerator();

            for (var y = 0; y < surface.Height; y++)
            {
                for (var x = 0; x < surface.Width; x++)
                {
                    if (!e.MoveNext()) return;

                    surface[x, y] = e.Current;
                }
            }
        }

        /// <summary>
        /// Proceeds around the surface from (0, 0) in a clockwise direction invoking the specified action.
        /// </summary>
        /// <param name="action">An action with the x and y values of the cell.</param>
        public static void ForEachEdge<T>(this ISurface<T> surface, Action<int, int> action)
        {
            int x;
            int y = 0;
            
            for (x = 0; x < surface.Width; x++) action.Invoke(x, y);

            x = surface.Width - 1;
            
            for (y = 1; y < surface.Height; y++) action.Invoke(x, y);

            y = surface.Height - 1;
            
            for (x = surface.Width - 2; x >= 0; x--) action.Invoke(x, y);

            x = 0;
            
            for (y = surface.Height - 2; y >= 1; y--) action.Invoke(x, y);
        }

        /// <summary>
        /// Trims the specified number of cells from the edge of the surface and provides a new view.
        /// </summary>
        public static ISurface<T> Trim<T>(this ISurface<T> surface, int amount)
        {
            var slice = new SurfaceRect(amount, amount, surface.Width - amount * 2, surface.Height - amount * 2);

            if (slice.Area <= 0) throw new InvalidOperationException($"Cannot trim {amount} from this surface; the resulting slice is zero.");

            return surface.Slice(slice);
        }

        /// <summary>
        /// Runs an action on each cell of the surface, width-first.
        /// </summary>
        public static void ForEach<T>(this ISurface<T> surface, Action<int, int> action)
        {
            for (var x = 0; x < surface.Width; x++)
                for(var y = 0; y < surface.Height; y++)
                    action.Invoke(x, y);
        }
    }
}