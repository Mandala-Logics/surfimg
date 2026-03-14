using System;
using MandalaLogics.SurfaceTerminal.Surfaces;

namespace MandalaLogics.SurfaceTerminal.Text
{
    [Flags]
    public enum ConnectDirection
    { 
        None = 0b0,
        Up = 0b1, 
        Down = 0b10, 
        Left = 0b100, 
        Right = 0b1000, 
        UpDown = Up | Down, 
        LeftRight = Left | Right,
        UpLeft = Up | Left,
        UpRight = Up | Right,
        DownRight = Down | Right,
        DownLeft = Down | Left,
        LeftUpRight = Left | Up | Right,
        UpRightDown = Up | Right | Down,
        RightDownLeft = Right | Down | Left,
        DownLeftUp = Down | Left | Up,
        All = Up | Down | Left | Right
    }
    
    public class LayoutChar : ConsoleChar
    {
        public ConnectDirection Directions { get; private set; } = ConnectDirection.None;
        
        public bool Bold { get; set; }
        
        public override ConsoleDecoration Decoration { get; } = default;

        public void Relate(ISurface<ConsoleChar> surface, int x, int y)
        {
            if (surface.TryGet(x - 1, y, out var l) && l is LayoutChar)
            {
                Directions |= ConnectDirection.Left;
            }
            
            if (surface.TryGet(x, y - 1, out var t) && t is LayoutChar)
            {
                Directions |= ConnectDirection.Up;
            }
            
            if (surface.TryGet(x + 1, y, out var r) && r is LayoutChar)
            {
                Directions |= ConnectDirection.Right;
            }
            
            if (surface.TryGet(x, y + 1, out var d) && d is LayoutChar)
            {
                Directions |= ConnectDirection.Down;
            }
        }
        
        public override char GetChar(ulong frameNumber)
        {
            char c;
                
            switch (Directions)
            {
                case ConnectDirection.All:
                    c = Bold ? '╋' : '┼';
                    break;
                case ConnectDirection.UpDown:
                    c =  Bold ? '┃' : '│';
                    break;
                case ConnectDirection.LeftRight:
                    c = Bold ? '━' : '─';
                    break;
                case ConnectDirection.UpLeft:
                    c = Bold ? '┛' : '┘';
                    break;
                case ConnectDirection.UpRight:
                    c = Bold ? '┗' : '└';
                    break;
                case ConnectDirection.DownRight:
                    c = Bold ? '┏' : '┌';
                    break;
                case ConnectDirection.DownLeft:
                    c = Bold ? '┓' : '┐';
                    break;
                case ConnectDirection.LeftUpRight:
                    c = Bold ? '┻' : '┴';
                    break;
                case ConnectDirection.UpRightDown:
                    c = Bold ? '┣' : '├';
                    break;
                case ConnectDirection.RightDownLeft:
                    c = Bold ? '┳' : '┬';
                    break;
                case ConnectDirection.DownLeftUp:
                    c = Bold ? '┫' : '┤';
                    break;
                default:
                    c = '·';
                    break;
            }

            return c;
        }
    }
}