using MandalaLogics.SurfaceTerminal.Layout;
using MandalaLogics.SurfaceTerminal.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace surfImg;

internal static partial class Program
{
    private static void LayoutOnKeyPressed(SurfaceLayout sender, SurfaceLayoutKeyPressedEventArgs args)
    {
        args.Cancel = true;

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
        
        SetUpArrows();

        _infoLine.Text = new ConsoleString(_dir[_curr].Path);
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

        _arrowLine.Text = builder.GetConsoleString();
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

                _displayPanel.Load(img);

                return;
            }
            catch (Exception e) when (e is UnknownImageFormatException || e is FileNotFoundException)
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

                _displayPanel.Load(img);

                return;
            }
            catch (Exception e) when (e is UnknownImageFormatException || e is FileNotFoundException)
            {
            }
            
        } while (true);
    }
}