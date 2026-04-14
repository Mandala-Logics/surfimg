using System;
using System.Collections.Generic;
using MandalaLogics.Path;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;

namespace MandalaLogics.SurfaceTerminal.Layout.Components;

public enum FileDialogType { FilesOnly, FoldersOnly, Both }

public class FileDialogPanel : SurfacePanel
{
    public event EventHandler? PathClicked;
    
    public PathBase RootDirPath
    {
        get => _rootDir;
        
        set
        {
            lock (_sync)
            {
                if (!value.Exists)
                    throw new ArgumentException("Path does not exist.");

                if (!value.IsDir)
                    throw new ArgumentException("Path does not point to a dir.");

                if (!_currentDir.IsDescendentOf(value)) _currentDir = value;

                _rootDir = value;
                
                SetUpList();
            }
        }
    }

    public PathBase CurrentDir
    {
        get => _currentDir;
        
        set
        {
            lock (_sync)
            {
                if (!value.Exists)
                    throw new ArgumentException("Path does not exist.");

                if (!value.IsDir)
                    throw new ArgumentException("Path does not point to a dir.");

                if (!value.IsDescendentOf(_rootDir) && value.Equals(_rootDir))
                    throw new ArgumentException("This path is not a descendant of the root path.");

                _currentDir = value;
                
                SetUpList();
            }
        }
    }
    
    public PathBase? SelectedPath { get; private set; }

    public FileDialogType Type
    {
        get => _type;
        set
        {
            lock (_sync)
            {
                _type = value;
                SetUpList();
            }
        }
    }

    private FileDialogType _type;

    private PathBase _rootDir;
    private PathBase _currentDir;

    private readonly List<FileDialogLine> _lines = new();
    private FileDialogLine? _selectedLine;

    private readonly object _sync = new();

    public FileDialogPanel(PathBase rootDirPath, FileDialogType type)
    {
        _rootDir = rootDirPath;
        _currentDir = rootDirPath;

        if (!rootDirPath.Exists)
            throw new ArgumentException("Root path does not exist.");

        _type = type;
        
        SetUpList();
    }

    public override void Render(ISurface<ConsoleChar> surface, ulong frameNumber)
    {
        lock (_sync)
        {
            using var e = (IEnumerator<FileDialogLine>)_lines.GetEnumerator();

            var h = surface.Height - 2;

            if (h <= 0) return;

            var ls = new Queue<FileDialogLine>();
            var removed = false;
            var reachedEnd = false;

            while (e.MoveNext())
            {
                ls.Enqueue(e.Current);

                if (e.Current.Equals(_selectedLine))
                {
                    while (ls.Count != h)
                    {
                        if (ls.Count > h)
                        {
                            ls.Dequeue();
                            removed = true;
                        }
                        else
                        {
                            if (!e.MoveNext())
                            {
                                reachedEnd = true;
                                break;
                            }
                            
                            ls.Enqueue(e.Current);
                        }
                    }

                    break;
                }
            }
            
            if (!e.MoveNext())
            {
                reachedEnd = true;
            }
            
            if (_lines.Count > h)
            {
                var builder = new ConsoleStringBuilder(3);

                builder.Append('↑', 
                    new ConsoleDecoration(removed ? null : ConsoleColor.DarkGray, null));
                
                builder.Append(ConsoleChar.WhiteSpace);

                builder.Append('↓', 
                    new ConsoleDecoration(reachedEnd ? ConsoleColor.DarkGray : null, null));
                
                builder.GetConsoleString()
                    .WriteToSurface(surface, SurfaceWriteOptions.None, 0, surface.Height - 2);
            }

            var cs = new ConsoleString(_currentDir.Path,
                new ConsoleDecoration(ConsoleColor.Black, ConsoleColor.DarkYellow));
            
            cs.WriteToSurface(surface, SurfaceWriteOptions.None, 0, surface.Height - 1);

            var y = -1;

            while (ls.TryDequeue(out var line))
            {
                line.Render(surface.SliceLine(++y), frameNumber);
            }
        }
    }

    protected override void OnDeselected() { }

    protected override void OnSelected() { }

    protected override void OnKeyPressed(ConsoleKeyInfo keyInfo)
    {
        lock (_sync)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                    SelectPrev();
                    break;
                case ConsoleKey.DownArrow:
                    SelectNext();
                    break;
                case ConsoleKey.Enter:
                    PathClicked?.Invoke(this, EventArgs.Empty);
                    break;
                case ConsoleKey.Backspace:
                case ConsoleKey.LeftArrow:
                
                    if (_currentDir != _rootDir)
                    {
                        _currentDir = _currentDir.GetContainingDir();
                    
                        SetUpList();
                    }
                
                    break;
                case ConsoleKey.RightArrow:

                    if (SelectedPath?.IsDir ?? false)
                    {
                        _currentDir = SelectedPath;
                    
                        SetUpList();
                    }
                
                    break;
            }
        }
    }

    public void SelectPath(PathBase path)
    {
        lock (_sync)
        {
            if (!path.IsDescendentOf(_rootDir) && !path.Equals(_rootDir))
                throw new ArgumentException("This path is not a descendant of the root dir.");

            if (path.IsRootPath)
            {
                _currentDir = _rootDir;
            
                SetUpList();
            }
            else if (path.IsDir)
            {
                _currentDir = path;
            
                SetUpList();
            }
            else
            {
                _currentDir = path.GetContainingDir();
            
                SetUpList();

                foreach (var line in _lines)
                {
                    if (line.Path.Equals(path))
                    {
                        _selectedLine = line;
                        SelectedPath = path;
                    }
                }
            }
        }
    }
    
    public void SelectNext()
    {
        if (_selectedLine is null) return;
        
        lock (_sync)
        {
            using var e = (IEnumerator<FileDialogLine>)_lines.GetEnumerator();

            while (e.MoveNext())
            {
                if (_selectedLine.Equals(e.Current))
                {
                    while (e.MoveNext())
                    {
                        if (e.Current.TrySelect())
                        {
                            _selectedLine.TryDeselect();
                            _selectedLine = e.Current;
                            SelectedPath = e.Current.Path;
                            
                            return;
                        }
                    }
                    
                    e.Reset();

                    while (e.MoveNext())
                    {
                        if (_selectedLine.Equals(e.Current)) return;
                        
                        if (e.Current.TrySelect())
                        {
                            _selectedLine.TryDeselect();
                            _selectedLine = e.Current;
                            SelectedPath = e.Current.Path;
                            
                            return;
                        }
                    }

                    return;
                }
            }
        }
    }

    public void SelectPrev()
    {
        if (_selectedLine is null) return;
        
        lock (_sync)
        {
            var ls = new List<FileDialogLine>(_lines.Count);
            int s = -1;
            
            foreach (var line in _lines)
            {
                ls.Add(line);
                
                if (line.Equals(_selectedLine)) s = ls.Count - 1;
            }

            for (var x = s - 1; x >= 0; x--)
            {
                if (ls[x].TrySelect())
                {
                    ls[s].TryDeselect();
                    _selectedLine = ls[x];
                    SelectedPath = ls[x].Path;
                    
                    return;
                }
            }

            for (var x = ls.Count - 1; x > s; x++)
            {
                if (ls[x].TrySelect())
                {
                    ls[s].TryDeselect();
                    _selectedLine = ls[x];
                    SelectedPath = ls[x].Path;
                    
                    return;
                }
            }
        }
    }

    protected override bool OnLineStateTryChange(SurfaceLine line, SurfaceLineState newState) => false;

    private void SetUpList()
    {
        lock (_sync)
        {
            IEnumerable<PathBase> dir;
            
            var access = _currentDir.Access;

            if (access.HasFlag(AccessLevel.Read))
            {
                dir = _type switch
                {
                    FileDialogType.Both => _currentDir.Dir(),
                    FileDialogType.FilesOnly => _currentDir.EnumerateFiles(),
                    _ => _currentDir.EnumerateDirs()
                };
            }
            else
            {
                if (!_currentDir.IsRootPath)
                {
                    _currentDir = _currentDir.GetContainingDir();

                    return;
                }
                else
                {
                    dir = new List<PathBase>();
                }
            }
            
            _lines.Clear();

            _lines.Add(new FileDialogLine(_currentDir, "."));

            foreach (var path in dir)
            {
                _lines.Add(new FileDialogLine(path));
            }

            _selectedLine = _lines[0];
            SelectedPath = _lines[0].Path;

            _selectedLine.TrySelect();
        }
    }
}