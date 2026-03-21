using System;

namespace MandalaLogics.SurfaceTerminal.Layout
{
    public delegate void SurfaceLayoutBeforeKeyPressedEventHandler
        (object sender, SurfaceLayoutKeyPressedEventArgs args);

    public delegate void SurfaceLayoutAfterKeyPressedEventHandler
        (object sender, ConsoleKeyInfo keyInfo);
    
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