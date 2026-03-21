using System.Reflection.Metadata;
using MandalaLogics.Command;
using MandalaLogics.SurfaceTerminal;
using MandalaLogics.SurfaceTerminal.Layout;
using MandalaLogics.SurfaceTerminal.Layout.Components;
using MandalaLogics.SurfaceTerminal.Parsing;
using MandalaLogics.SurfaceTerminal.Text;

namespace surfImg;

internal static partial class Program
{
    private static readonly ListDisplayPanel footerPanel = new();
    private static readonly ImageDisplayPanel displayPanel = new();
    private static readonly ListDisplayPanel aboutPanel = new();
    private static readonly TabPanel tabPanel = new();
    private static readonly ListPanel optionsPanel = new();
    private static readonly SubLayoutPanel subLayoutPanel = new();
    
    private static readonly SurfaceLayout displayLayout = new();
    
    private static readonly TextDisplayLine infoLine = new();
    private static readonly TextDisplayLine arrowLine = new();
    
    private static void SetUpLayout()
    {
        var layout = LayoutDeserializer.Read(CommandHelper.GetAssemblyStreamReader("layout.surf"));

        var headerPanel = new TextDisplayPanel()
        {
            Options = SurfaceWriteOptions.Centered | SurfaceWriteOptions.WrapText,
            Text = new ConsoleString("Mandala Logics Surface Image Viewer.", 
                new ConsoleDecoration(null, ConsoleColor.DarkGray)),
            Fill = true
        };
        
        subLayoutPanel.Layout.RootNode.DrawOutline = false;
        
        layout.SetPanel("header", headerPanel);
        layout.SetPanel("main", subLayoutPanel);
        layout.SetPanel("tabs", tabPanel);
        
        layout.BeforeKeyPressed += LayoutOnBeforeKeyPressed;
        
        SetUpTabPanel();
        SetUpAboutPanel();
        SetUpOptionsPanel();
        SetUpDisplayLayout();

        subLayoutPanel.Layout = displayLayout;
        
        layout.SelectPanel("main");
        
        SurfaceTerminal.Display(layout);
    }

    private static void SetUpDisplayLayout()
    {
        infoLine.Options = SurfaceWriteOptions.Centered;
        arrowLine.Options = SurfaceWriteOptions.Centered;
            
        footerPanel.Add(infoLine);
        footerPanel.Add(arrowLine);
        
        displayLayout.RootNode.SplitReverse(2, LayoutSplitDirection.Horizonal);
        
        displayLayout.RootNode[2].SetPanel("footer", footerPanel);
        displayLayout.RootNode[1].SetPanel("display", displayPanel);

        displayLayout.RootNode[1].DrawOutline = false;
        displayLayout.RootNode[2].DrawOutline = false;
    }

    private static void SetUpTabPanel()
    {
        tabPanel.Add("viewer", new MenuItemLine("Viewer", null));
        tabPanel.Add("options", new MenuItemLine("Options", null));
        tabPanel.Add("about", new MenuItemLine("About", null));
        
        tabPanel.SelectedKeyChanged += TabPanelOnSelectedKeyChanged;
    }

    private static void SetUpOptionsPanel()
    {
        var fillLine = new OptionsLine("Image Fill")
        {
            { "ratio", "Keep Ratio" },
            { "fill", "Fill" }
        };

        var colourLine = new ToggleLine("Use Colour", true);
        
        fillLine.SelectedKeyChanged += FillLineOnSelectedKeyChanged;
        colourLine.ToggleChanged += ColourLineOnToggleChanged;
        
        optionsPanel.Add("a", new TextDisplayLine());
        optionsPanel.Add("fill", fillLine);
        optionsPanel.Add("colour", colourLine);
    }

    private static void ColourLineOnToggleChanged(object? sender, EventArgs e)
    {
        displayPanel.UseColour = ((ToggleLine)sender).ToggleState;
        
        displayPanel.Redraw();
    }

    private static void FillLineOnSelectedKeyChanged(object? sender, EventArgs e)
    {
        var line = (OptionsLine)sender;

        if (line.SelectedKey == "fill")
        {
            displayPanel.KeepRatio = false;
        }
        else
        {
            displayPanel.KeepRatio = true;
        }
        
        displayPanel.Redraw();
    }

    private static void SetUpAboutPanel()
    {
        aboutPanel.Add(new TextDisplayLine());
        
        aboutPanel.Add(new TextDisplayLine()
        {
            Text = "https://github.com/Mandala-Logics/",
            Options = SurfaceWriteOptions.Centered, 
            Decoration = new ConsoleDecoration(ConsoleColor.DarkRed, ConsoleColor.Yellow)
        });
        
        aboutPanel.Add(new TextDisplayLine());
        
        aboutPanel.Add(new TextDisplayLine()
        {
            Text = "(c) Mandala Logics - MIT Licence",
            Options = SurfaceWriteOptions.Centered, 
            Decoration = new ConsoleDecoration(ConsoleColor.Gray, null)
        });
    }
}