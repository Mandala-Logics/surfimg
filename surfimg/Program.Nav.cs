using MandalaLogics.SurfaceTerminal;
using MandalaLogics.SurfaceTerminal.Layout;
using MandalaLogics.SurfaceTerminal.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace surfImg;

internal static partial class Program
{
    private static void LayoutOnBeforeKeyPressed(object sender, SurfaceLayoutKeyPressedEventArgs args)
    {
        if (args.KeyInfo.Key == ConsoleKey.Tab)
        {
            args.Cancel = true;
            
            tabPanel.SelectNext();
        }
        else if (tabPanel.SelectedKey == "viewer")
        {
            switch (args.KeyInfo.Key)
            {
                case ConsoleKey.LeftArrow:
                    TryReverse();
                    break;
                case ConsoleKey.RightArrow:
                    TryAdvance();
                    break;
                default:
                    return;
            }
            
            args.Cancel = true;
        
            SetUpArrows();

            infoLine.Text = new ConsoleString(_dir[_curr].Path);
        }
    }
    
    private static void TabPanelOnSelectedKeyChanged(object? sender, EventArgs e)
    {
        switch (tabPanel.SelectedKey)
        {
            case "about":
                SurfaceTerminal.Layout.SetPanel("main", aboutPanel);
                break;
            case "viewer":
                SurfaceTerminal.Layout.SetPanel("main", subLayoutPanel);
                break;
            case "options":
                SurfaceTerminal.Layout.SetPanel("main", optionsPanel);
                break;
        }
    }

    private static void SetUpArrows()
    {
        if (_dir.Count == 0) return;

        var builder = new ConsoleStringBuilder();

        if (_curr == 0)
        {
            builder.Append('<', new ConsoleDecoration(ConsoleColor.DarkGray, null));
            builder.Append(ConsoleChar.WhiteSpace);
            builder.Append('>', new ConsoleDecoration(ConsoleColor.White, null));
        }
        else if (_curr == _dir.Count - 1)
        {
            builder.Append('<', new ConsoleDecoration(ConsoleColor.White, null));
            builder.Append(ConsoleChar.WhiteSpace);
            builder.Append('>', new ConsoleDecoration(ConsoleColor.DarkGray, null));
        }
        else
        {
            builder.Append('<', new ConsoleDecoration(ConsoleColor.White, null));
            builder.Append(ConsoleChar.WhiteSpace);
            builder.Append('>', new ConsoleDecoration(ConsoleColor.White, null));
        }

        arrowLine.Text = builder.GetConsoleString();
    }

    private static void TryAdvance()
    {
        int start = _curr;
        
        do
        {
            if (++_curr >= _dir.Count) _curr = 0;

            if (start == _curr) return;

            try
            {
                var img = Image.Load<Rgba32>(_dir[_curr].Path);

                displayPanel.Load(img);

                return;
            }
            catch (Exception e) when (e is UnknownImageFormatException or FileNotFoundException)
            {
            }
            
        } while (true);
    }
    
    private static void TryReverse()
    {
        int start = _curr;
        
        do
        {
            if (--_curr < 0) _curr = _dir.Count - 1;

            if (start == _curr) return;

            try
            {
                var img = Image.Load<Rgba32>(_dir[_curr].Path);

                displayPanel.Load(img);

                return;
            }
            catch (Exception e) when (e is UnknownImageFormatException or FileNotFoundException)
            {
            }
            
        } while (true);
    }
}