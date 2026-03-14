using System;

namespace MandalaLogics.SurfaceTerminal.Layout
{
    public delegate void SurfaceLayoutKeyPressedEventHandler
        (SurfaceLayout sender, SurfaceLayoutKeyPressedEventArgs args);
    
    public class SurfaceLayoutKeyPressedEventArgs : EventArgs
    {
        public bool Cancel { get; set; } = false;
        public ConsoleKeyInfo KeyInfo { get; }

        internal SurfaceLayoutKeyPressedEventArgs(ConsoleKeyInfo cki)
        {
            KeyInfo = cki;
        }
    }
}