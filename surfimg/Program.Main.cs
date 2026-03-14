using MandalaLogics.Command;
using MandalaLogics.CommandParsing;
using MandalaLogics.Encoding;
using MandalaLogics.Path;
using MandalaLogics.SurfaceTerminal;
using MandalaLogics.SurfaceTerminal.Layout;
using MandalaLogics.SurfaceTerminal.Parsing;
using MandalaLogics.SurfaceTerminal.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace surfImg;

internal static partial class Program
{
    private static ParsedCommand _parsedCommand;
    private static PathBase _folderPath = null!;
    private static ListDisplayPanel _footerPanel = null!;
    private static ImageDisplayPanel _displayPanel = null!;
    private static PathBase? _currentFile;
    private static List<PathBase> _dir;

    private static TextDisplayLine _infoLine = null!;
    private static TextDisplayLine _arrowLine = null!;

    private static int _curr = -1;
    
    private static void Main(string[] args)
    {
        var cl = new CommandLine(args);

        var tree = new CommandTree(CommandHelper.GetAssemblyStreamReader("main.command"));

        try
        {
            _parsedCommand = tree.ParseCommandLine(cl);
        }
        catch (CommandParsingException e)
        {
            Console.WriteLine(e.Message);
            Environment.Exit(0);
        }

        if (_parsedCommand.HasSwitch("help"))
        {
            CommandHelper.DisplayAssemblyFile("help.txt");
            Environment.Exit(0);
        }

        try
        {
            _folderPath = PathBase.FindCommandPath(LinuxPath.GetWorkingDir(),
                _parsedCommand.GetArgumentValue("path").FirstOrDefault() ?? ".");
        }
        catch (PathParsingException e)
        {
            Console.WriteLine($"Cannot find the specified path: {e.Message}");
        }
        
        SetUpLayout();
        
        if (_folderPath.IsFile)
        {
            _currentFile = _folderPath;
            _folderPath = _currentFile.GetContainingDir();
            
            try
            {
                _displayPanel.Load(_currentFile?.Path ?? throw new PlaceholderException());
            }
            catch (Exception e) when (e is UnknownImageFormatException || e is FileNotFoundException)
            {
                Console.WriteLine("Unable to open this file.");
                Environment.Exit(0);
            }
        }
        else
        {
            _currentFile = ScrapeFolder();
        }

        if (_currentFile is null)
        {
            Console.WriteLine("Unable to open this file.");
            Environment.Exit(0);
        }

        _infoLine.Text = _currentFile.Path;
        
        SetUpArrows();
        
        SurfaceTerminal.Start();
    }

    private static void SetUpLayout()
    {
        var layout = LayoutDeserializer.Read(CommandHelper.GetAssemblyStreamReader("layout.surf"));

        var headerPanel = new TextDisplayPanel()
        {
            Options = SurfaceWriteOptions.Centered,
            Text = new ConsoleString("Mandala Logics Surface Image Viewer", 
                new ConsoleDecoration(null, ConsoleColor.DarkGray)),
            Fill = true
        };

        _footerPanel = new ListDisplayPanel();
        _displayPanel = new ImageDisplayPanel();

        _infoLine = new TextDisplayLine();
        _arrowLine = new TextDisplayLine();

        _infoLine.Options = SurfaceWriteOptions.Centered;
        _arrowLine.Options = SurfaceWriteOptions.Centered;
            
        _footerPanel.Add(_infoLine);
        _footerPanel.Add(_arrowLine);
        
        layout.SetPanel("header", headerPanel);
        layout.SetPanel("display", _displayPanel);
        layout.SetPanel("footer", _footerPanel);
        
        layout.KeyPressed += LayoutOnKeyPressed;
        
        SurfaceTerminal.Display(layout);
    }

    private static PathBase? ScrapeFolder()
    {
        _dir = _folderPath.Dir().Where(pb => pb.IsFile).ToList();

        foreach (var pb in _dir)
        {
            try
            {
                var img = Image.Load<Rgba32>(pb.Path);
                    
                _displayPanel.Load(img);

                _curr = _dir.IndexOf(pb);

                return pb;
            }
            catch (UnknownImageFormatException) { }
            catch (FileNotFoundException) { }
        }
        
        SurfaceTerminal.Layout.SetPanel("display", new TextDisplayPanel() { Text = "No image to display."});
        return null;
    }
}