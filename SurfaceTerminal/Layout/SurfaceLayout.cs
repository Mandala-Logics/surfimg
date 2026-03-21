using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using MandalaLogics.Collection;
using MandalaLogics.SurfaceTerminal.Layout.Components;
using MandalaLogics.Threading;

namespace MandalaLogics.SurfaceTerminal.Layout
{
    public sealed class SurfaceLayout : IReadOnlyDictionary<string, SurfacePanel>
    {
        public event SurfaceLayoutBeforeKeyPressedEventHandler? BeforeKeyPressed;
        internal event SurfaceLayoutBeforeKeyPressedEventHandler? BeforeBeforeKeyPressed;
        public event SurfaceLayoutAfterKeyPressedEventHandler? AfterKeyPressed;
        
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

        protected readonly ConcurrentDictionary<string, SurfacePanel> _panels;
        protected readonly SyncedList<SurfaceLayoutNode> _nodes;
            
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

        public SurfaceLayoutNode GetNode(string key)
        {
            lock (SyncRoot)
            {
                if (!_panels.TryGetValue(key, out var panel))
                    throw new LayoutException(LayoutExceptionReason.KeyNotFound);
                
                foreach (var node in _nodes)
                {
                    if (panel.Equals(node._panel))
                    {
                        return node;
                    }
                }
            }
            
            throw new LayoutException(LayoutExceptionReason.KeyNotFound);
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
                        
                        if (_selectedNode == -1)
                        {
                            _selectedNode = node.Index;
                            
                            newPanel.Selected();
                        }
                        else if (oldPanel.IsSelected)
                        {
                            oldPanel.Deselected();

                            newPanel.Selected();
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
                    if (node.Panel.IsSelected) node.Panel.Deselected();
                }
                
                if (_selectedNode == -1)
                {
                    _selectedNode = node.Index;
                    
                    panel.Selected();
                }
            }
        }

        public void SelectPanel(string key)
        {
            lock (SyncRoot)
            {
                if (!_panels.TryGetValue(key, out var panel))
                    throw new LayoutException(LayoutExceptionReason.KeyNotFound);

                foreach (var node in _nodes)
                {
                    if (!node.IsSplit && node.Panel.Equals(panel))
                    {
                        SelectedPanel?.Deselected();
                        
                        _selectedNode = node.Index;
                        
                        SelectedPanel?.Selected();

                        return;
                    }
                }
            }
        }

        internal void OnKeyPressed(ConsoleKeyInfo keyInfo, ThreadController tc)
        {
            lock (SyncRoot)
            {
                var args = new SurfaceLayoutKeyPressedEventArgs(keyInfo);
                
                BeforeBeforeKeyPressed?.Invoke(this, args);

                if (args.Cancel) return;
                
                args = new SurfaceLayoutKeyPressedEventArgs(keyInfo);
                    
                BeforeKeyPressed?.Invoke(this, args);

                if (args.Cancel) return;
                    
                if (_selectedNode != -1) _nodes[_selectedNode].Panel.KeyPressed(keyInfo);
                
                AfterKeyPressed?.Invoke(this, keyInfo);
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