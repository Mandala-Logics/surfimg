using System;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Threading;
using MandalaLogics.SurfaceTerminal.Layout;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;
using MandalaLogics.Threading;

namespace MandalaLogics.SurfaceTerminal
{
    public static class SurfaceTerminal
    {
        public static event EventHandler? Terminated;
        
        private static readonly TimeSpan frameTime = TimeSpan.FromMilliseconds(50);
        private static readonly TextDisplayPanel errorPanel;
        
        public static ConsoleColor ForeColour { get; set; } = ConsoleColor.White;
        public static ConsoleColor BackColour { get; set; } = ConsoleColor.Black;
        public static bool Running => _displayThread.State.Running;
        public static bool DisplayingError { get; private set; }
        public static SurfaceLayout Layout => _layout;

        private static ThreadBase _displayThread = NullThread.CompletedThread;
        private static readonly MessageLoopThread<ConsoleKeyInfo> messageThread;
        private static ThreadBase _inputThread = NullThread.CompletedThread;
        private static SurfaceLayout? _layout;

        static SurfaceTerminal()
        {
            errorPanel = new TextDisplayPanel
            {
                Options = SurfaceWriteOptions.Centered | SurfaceWriteOptions.WrapText
            };
            
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;
            
            messageThread = new MessageLoopThread<ConsoleKeyInfo>((tc, cki) => _layout?.OnKeyPressed(cki, tc));
            messageThread.Start();
            
            messageThread.ThreadComplete += OnThreadComplete;
            
            errorPanel.Text = new ConsoleString($"No layout is set.", 
                new ConsoleDecoration(null, ConsoleColor.DarkRed));
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (Running)
            {
                _displayThread.AwaitAbort();
                messageThread.AwaitAbort();
                
                _inputThread.AwaitAbort();
                
                Terminated?.Invoke(null, EventArgs.Empty);
            }
        }

        public static void Display(SurfaceLayout layout)
        {
            _layout = layout;
        }

        public static void Start()
        {
            if (Running) throw new InvalidOperationException("SurfaceTerminal is already running.");
            
            Console.CursorVisible = false;

            _displayThread = new TaskThread(DisplayLoop);
            _displayThread.Start();

            _inputThread = new TaskThread(InputLoop);
            _inputThread.Start();
            
            _displayThread.ThreadComplete += OnThreadComplete;
            _inputThread.ThreadComplete += OnThreadComplete;
        }

        public static void Stop(Exception? e)
        {
            if (e is { })
            {
                var builder = new ConsoleStringBuilder();

                errorPanel.Options = SurfaceWriteOptions.WrapText;
            
                builder.Append(new ConsoleString("Exception encountered:\n",
                    new ConsoleDecoration(ConsoleColor.Black, ConsoleColor.DarkRed)));
            
                builder.Append(new ConsoleString(e.Message + "\n",
                    new ConsoleDecoration(ConsoleColor.DarkRed, null)));
                
                builder.Append(new ConsoleString(e.StackTrace ?? string.Empty));

                errorPanel.Text = builder.GetConsoleString();

                DisplayingError = true;
            }
            
            //_displayThread.AwaitAbort();
            messageThread.AwaitAbort();
            _inputThread.AwaitAbort();
        }

        private static void OnThreadComplete(ThreadBase sender, ThreadResult result)
        {
            if (result.Failed)
            {
                Console.WriteLine(result.Exception.Message);
                Console.WriteLine(result.Exception.StackTrace);

                _displayThread.AwaitAbort();
                messageThread.AwaitAbort();
                _inputThread.AwaitAbort();
            }
        }

        private static void InputLoop(ThreadController tc)
        {
            while (!tc.IsAbortRequested)
            {
                if (Console.KeyAvailable)
                {
                    var keyInfo = Console.ReadKey(true);
                    
                    messageThread.Add(keyInfo);
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        private static void DisplayLoop(ThreadController tc)
        {
            ISurface<ConsoleChar> buffer = new Surface<ConsoleChar>(Console.BufferWidth, Console.BufferHeight);
            ISurface<BufferChar> current = new Surface<BufferChar>(Console.BufferWidth, Console.BufferHeight);
            
            var nextWrite = DateTime.Now + frameTime;
            var frameNumber = 0UL;
            
            while (!tc.IsAbortRequested)
            {
                var w = Console.BufferWidth;
                var h = Console.BufferHeight;
                
                frameNumber++;
                
                if (w != buffer.Width || h != buffer.Height)
                {
                    buffer = new Surface<ConsoleChar>(w, h);
                    current = new Surface<BufferChar>(w, h);
                }
                
                buffer.Fill(ConsoleChar.WhiteSpace);

                if (_layout is null || DisplayingError)
                {
                    errorPanel.Render(buffer, frameNumber);
                }
                else
                {
                    if (_layout.MaxHeight is { } || _layout.MaxWidth is { })
                    {
                        var rect = buffer.GetBounds();

                        rect = rect.TakeCenter(
                            Math.Min(w, _layout.MaxWidth ?? int.MaxValue),
                            Math.Min(h, _layout.MaxHeight ?? int.MaxValue));
                        
                        var slice = buffer.Slice(rect);
                        
                        _layout.RootNode.Render(slice, frameNumber);
                    }
                    else
                    {
                        _layout.RootNode.Render(buffer, frameNumber);
                    }
                }
                
                buffer.ForEach((x, y) =>
                {
                    if (buffer[x, y] is LayoutChar lc)
                    {
                        lc.Relate(buffer, x, y);
                    }
                });

                var now = DateTime.Now;
                
                if (nextWrite > now) Thread.Sleep(nextWrite - now);

                for (var x = 0; x < w; x++)
                {
                    for (var y = 0; y < h; y++)
                    {
                        var buffChar = buffer[x, y].GetBufferChar(frameNumber);
                        
                        if (current[x, y].Equals(buffChar)) continue;
                        
                        current[x, y] = buffChar;
                        
                        Console.SetCursorPosition(x, y);
                    
                        Console.BackgroundColor = buffChar.Decoration.BackColour;
                        Console.ForegroundColor = buffChar.Decoration.ForeColour;
                        
                        Console.Write(buffChar.Char);
                    }
                }
                
                nextWrite = DateTime.Now + frameTime;
            }
        }
    }
}