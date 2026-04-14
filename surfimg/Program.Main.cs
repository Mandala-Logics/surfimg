using MandalaLogics;
using MandalaLogics.Command;
using MandalaLogics.CommandParsing;
using MandalaLogics.Path;
using MandalaLogics.SurfaceTerminal;
using MandalaLogics.SurfaceTerminal.Layout.Components;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace surfImg;

internal static partial class Program
{
    private static ParsedCommand _parsedCommand;
    private static PathBase _folderPath = null!;
    private static PathBase? _currentFile;
    private static List<PathBase> _dir = null!;
    
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
                displayPanel.Load(_currentFile?.Path!);
            }
            catch (Exception e) when (e is UnknownImageFormatException or FileNotFoundException)
            {
                Console.WriteLine("Could not find any image files.");
                Environment.Exit(0);
                return;
            }

            ScrapeFolder();
        }
        else
        {
            _currentFile = ScrapeFolder();

            if (_currentFile is null)
            {
                Console.WriteLine("Could not find any image files.");
                Environment.Exit(0);
                return;
            }
        }

        infoLine.Text = _currentFile!.Path;
        
        SetUpArrows();
        
        SurfaceTerminal.Start();
    }

    private static PathBase? ScrapeFolder()
    {
        _dir = _folderPath.Dir().Where(pb => pb.IsFile).ToList();

        foreach (var pb in _dir)
        {
            try
            {
                var img = Image.Load<Rgba32>(pb.Path);
                    
                displayPanel.Load(img);

                _curr = _dir.IndexOf(pb);

                return pb;
            }
            catch (UnknownImageFormatException) { }
            catch (InvalidImageContentException) { }
            catch (FileNotFoundException) { }
        }
        
        SurfaceTerminal.Layout.SetPanel("main", new TextDisplayPanel() { Text = "No image to display."});
        return null;
    }
}