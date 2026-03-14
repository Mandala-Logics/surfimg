using System;

namespace MandalaLogics.SurfaceTerminal.Layout
{
    public enum LayoutExceptionReason
    {
        NodeNotSplit,
        BadIndex,
        NodeAlreadySplit,
        KeyAlreadyExists,
        PanelNotSet,
        KeyNotFound,
        PanelCannotBeSelected,
        LayoutDescriptionNotValid,
        ProgrammerException
    }
    
    public class LayoutException : Exception
    {
        public LayoutExceptionReason Reason { get; }

        public LayoutException(LayoutExceptionReason reason) : base(reason.ToString())
        {
            Reason = reason;
        }

        public LayoutException(LayoutExceptionReason reason, string message) : base(message)
        {
            
        }
    }
}