using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using MandalaLogics.Collection;
using MandalaLogics.Threading;

namespace MandalaLogics.SurfaceTerminal.Layout
{
    public class SurfaceLayout : IReadOnlyDictionary<string, SurfacePanel>
    {
        public event SurfaceLayoutKeyPressedEventHandler? KeyPressed;
        
        public SurfaceLayoutNode RootNode { get; }

        public SurfacePanel this[string key]
        {
            get
            {
                lock (SyncRoot)
                {
                    if (!_panels.TryGetValue(key, out var panel))
                        throw new LayoutException(LayoutExceptionReason.KeyNotFound);

                    return panel;
                }
            }
        }
        
        public IEnumerable<string> Keys => ((IReadOnlyDictionary<string, SurfacePanel>)_panels).Keys;
        
        public IEnumerable<SurfacePanel> Values => ((IReadOnlyDictionary<string, SurfacePanel>)_panels).Values;
        
        public int Count => _panels.Count;

        public int? MaxWidth { get; }
        public int? MaxHeight { get; }

        public SurfaceLayoutNode? SelectedNode
        {
            get
            {
                lock (SyncRoot)
                {
                    if (_selectedNode == -1) return null;

                    return _nodes[_selectedNode];
                }
            }
        }

        public SurfacePanel? SelectedPanel
        {
            get
            {
                lock (SyncRoot)
                {
                    var node = SelectedNode;

                    if (node is null) return null;

                    return node.Panel;
                }
            }
        }

        public readonly object SyncRoot = new object();

        private readonly ConcurrentDictionary<string, SurfacePanel> _panels;
        private readonly SyncedList<SurfaceLayoutNode> _nodes;
            
        private int _lastIndex = 0;
        private int _selectedNode = -1;

        public SurfaceLayout()
        {
            RootNode = new SurfaceLayoutNode(this)
            {
                Index = 0
            };

            _panels = new ConcurrentDictionary<string, SurfacePanel>();

            _nodes = new SyncedList<SurfaceLayoutNode> { RootNode };
        }
        
        public SurfaceLayout(int? maxWidth, int? maxHeight) : this()
        {
            if (maxWidth is { } && maxWidth < 3)
                throw new ArgumentOutOfRangeException(nameof(maxWidth));
            
            if (maxHeight is { } && maxHeight < 3)
                throw new ArgumentOutOfRangeException(nameof(maxHeight));

            MaxWidth = maxWidth;
            MaxHeight = maxHeight;
        }

        public void SetPanel(string key, SurfacePanel newPanel)
        {
            lock (SyncRoot)
            {
                if (!_panels.ContainsKey(key))
                    throw new LayoutException(LayoutExceptionReason.KeyNotFound);

                var oldPanel = _panels[key];

                foreach (var node in _nodes)
                {
                    if (node.PanelIsSet && node.Panel.Equals(oldPanel))
                    {
                        node._panel = newPanel;
                        
                        if (_selectedNode == -1 && newPanel.CanBeSelected)
                        {
                            _selectedNode = node.Index;
                        }
                        else if (oldPanel.IsSelected)
                        {
                            oldPanel.Deselected();

                            if (!newPanel.CanBeSelected && !SelectNextPanel())
                            {
                                _selectedNode = -1;
                            }
                        }
                    }
                }

                _panels[key] = newPanel;
            }
        }

        internal void GetNewNodeIndex(SurfaceLayoutNode node)
        {
            lock (SyncRoot)
            {
                node.Index = ++_lastIndex;
                _nodes.Add(node);
            }
        }

        internal void TrySetPanel(SurfaceLayoutNode node, string key, SurfacePanel panel)
        {
            lock (SyncRoot)
            {
                if (!_panels.TryAdd(key, panel))
                    throw new LayoutException(LayoutExceptionReason.KeyAlreadyExists);

                if (node.PanelIsSet)
                {
                    node.Panel.Returning -= PanelOnReturning;
                    
                    if (node.Panel.IsSelected) node.Panel.Deselected();
                }
                
                if (_selectedNode == -1 && panel.CanBeSelected)
                {
                    _selectedNode = node.Index;
                    
                    panel.Returning += PanelOnReturning;
                    
                    panel.Selected();
                }
            }
        }

        private void PanelOnReturning(object sender, EventArgs e)
        {
            SelectNextPanel();
        }

        public void SelectPanel(string key)
        {
            lock (SyncRoot)
            {
                if (!_panels.TryGetValue(key, out var panel))
                    throw new LayoutException(LayoutExceptionReason.KeyNotFound);

                if (!panel.CanBeSelected)
                    throw new LayoutException(LayoutExceptionReason.PanelCannotBeSelected);

                foreach (var node in _nodes)
                {
                    if (node.Panel.Equals(panel))
                    {
                        SelectedPanel?.Deselected();
                        
                        _selectedNode = node.Index;
                        
                        SelectedPanel?.Selected();

                        return;
                    }
                }
            }
        }

        public bool SelectNextPanel()
        {
            var a = _selectedNode;

            for (int b = _selectedNode + 1; b < _nodes.Count; b++)
            {
                if (_nodes[b].PanelIsSet && _nodes[b].Panel.CanBeSelected)
                {
                    _selectedNode = b;
                }
            }

            if (a != _selectedNode) //selected node has changed
            {
                if (a != -1) _nodes[a].Panel.Deselected();
                _nodes[_selectedNode].Panel.Selected();
                return true;
            }

            for (int b = 0; b < _selectedNode; b++)
            {
                if (_nodes[b].PanelIsSet && _nodes[b].Panel.CanBeSelected)
                {
                    _selectedNode = b;
                }
            }
                    
            if (a != _selectedNode) //selected node has changed
            {
                if (a != -1) _nodes[a].Panel.Deselected();
                _nodes[_selectedNode].Panel.Selected();
                return true;
            }

            return false;
        }

        internal void OnKeyPressed(ConsoleKeyInfo keyInfo, ThreadController tc)
        {
            lock (SyncRoot)
            {
                if (keyInfo.Key == ConsoleKey.Tab) //change panel
                {
                    SelectNextPanel();
                }
                else
                {
                    var args = new SurfaceLayoutKeyPressedEventArgs(keyInfo);
                    
                    KeyPressed?.Invoke(this, args);

                    if (args.Cancel) return;
                    
                    if (_selectedNode != -1) _nodes[_selectedNode].Panel.KeyPressed(keyInfo);
                }
            }
        }

        public IEnumerator<KeyValuePair<string, SurfacePanel>> GetEnumerator()
        {
            return _panels.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_panels).GetEnumerator();
        }

        public bool ContainsKey(string key)
        {
            return _panels.ContainsKey(key);
        }

        public bool TryGetValue(string key, out SurfacePanel value)
        {
            return _panels.TryGetValue(key, out value);
        }

        
    }
}