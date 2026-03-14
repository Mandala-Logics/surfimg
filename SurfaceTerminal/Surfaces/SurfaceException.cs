using System;

namespace MandalaLogics.SurfaceTerminal.Surfaces
{
    public enum SurfaceExceptionReason
    {
        Null = 0,
        OutOfBounds,
        SliceNotInBounds,
        CompositeEmpty
    }
    
    public class SurfaceException : Exception
    {
        public SurfaceExceptionReason Reason { get; }

        public SurfaceException(SurfaceExceptionReason reason) : base(reason.ToString())
        {
            Reason = reason;
        }
    }
}