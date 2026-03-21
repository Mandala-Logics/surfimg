using System;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;

namespace MandalaLogics.SurfaceTerminal.Layout
{
    public enum LayoutSplitDirection { None, Vertical, Horizonal }
    
    public sealed class SurfaceLayoutNode
    {
        public SurfaceLayoutNode this[int index]
        {
            get
            {
                if (!IsSplit) throw new LayoutException(LayoutExceptionReason.NodeNotSplit);

                if (index == 1) return _child1;
                else if (index == 2) return _child2;
                else throw new LayoutException(LayoutExceptionReason.BadIndex);
            }
        }
        
        public bool IsSplit => SplitDirection != LayoutSplitDirection.None;
        public LayoutSplitDirection SplitDirection { get; private set; } = LayoutSplitDirection.None;

        public SurfacePanel Panel
        {
            get
            {
                if (_panel is { } p) return p;
                else if (IsSplit) throw new LayoutException(LayoutExceptionReason.NodeAlreadySplit);
                else throw new LayoutException(LayoutExceptionReason.PanelNotSet);
            }
        }
        
        public bool Visible { get; set; } = true;
        public bool PanelIsSet => _panel is { };
        public SurfaceLayoutNode? Parent { get; }
        public bool DrawOutline { get; set; } = true;
        public SurfaceLayoutNode Root => Parent is null ? this : Parent.Root;
        public SurfaceLayout Owner { get; }
        public int Index { get; internal set; }
        public double SplitRatio { get; private set; } = 0d;
        public int SplitLines { get; private set; } = 0;
        public bool ReverseSplit { get; private set; } = false;

        private SurfaceLayoutNode _child1 = null!;
        private SurfaceLayoutNode _child2 = null!;
        internal SurfacePanel? _panel;

        internal SurfaceLayoutNode(SurfaceLayout owner, SurfaceLayoutNode parent)
        {
            Parent = parent;
            Owner = owner;

            owner.GetNewNodeIndex(this);
        }

        internal SurfaceLayoutNode(SurfaceLayout owner)
        {
            Owner = owner;
        }
        
        public void Split(double ratio, LayoutSplitDirection direction)
        {
            if (IsSplit) throw new LayoutException(LayoutExceptionReason.NodeAlreadySplit);
            
            if (direction != LayoutSplitDirection.Horizonal && direction != LayoutSplitDirection.Vertical)
                throw new ArgumentException("Invalid direction.");
            
            if (ratio <= 0d || ratio >= 1d)
                throw new ArgumentException("Split ratio must be a positive number between 0 and 1.");

            SplitRatio = ratio;
            SplitDirection = direction;

            _child1 = new SurfaceLayoutNode(Owner, this);
            _child2 = new SurfaceLayoutNode(Owner, this);

            _panel = null;
        }

        public void Split(int lines, LayoutSplitDirection direction)
        {
            if (IsSplit) throw new LayoutException(LayoutExceptionReason.NodeAlreadySplit);
            
            if (direction != LayoutSplitDirection.Horizonal && direction != LayoutSplitDirection.Vertical)
                throw new ArgumentException("Invalid direction.");

            SplitLines = lines;
            SplitDirection = direction;
            
            _child1 = new SurfaceLayoutNode(Owner, this);
            _child2 = new SurfaceLayoutNode(Owner, this);
            
            _panel = null;
        }

        public void SplitReverse(int lines, LayoutSplitDirection direction)
        {
            Split(lines, direction);
            
            ReverseSplit = true;
        }
        
        public void SplitReverse(double ratio, LayoutSplitDirection direction)
        {
            Split(ratio, direction);
            
            ReverseSplit = true;
        }

        public void SetPanel(string key, SurfacePanel panel)
        {
            if (IsSplit) throw new LayoutException(LayoutExceptionReason.NodeAlreadySplit);

            Owner.TrySetPanel(this, key, panel);

            _panel = panel;
        }

        internal void Render(ISurface<ConsoleChar> surface, ulong frameNumber)
        {
            surface.ForEachEdge((x, y) =>
            {
                if (surface[x, y] is LayoutChar && DrawOutline) return;
                
                surface[x, y] = DrawOutline ? new LayoutChar() : ConsoleChar.WhiteSpace;
            });

            if (!Visible && !IsSplit) return;
            
            if (IsSplit)
            {
                int lines1;
                int lines2;
                int r;
                
                if (SplitRatio > 0d)
                {
                    if (SplitDirection == LayoutSplitDirection.Horizonal)
                    {
                        lines1 = (int)Math.Floor(surface.Height * SplitRatio);
                        lines2 = (int)Math.Floor(surface.Height * (1 - SplitRatio));
                        r = surface.Height - lines1 - lines2;
                    }
                    else
                    {
                        lines1 = (int)Math.Floor(surface.Width * SplitRatio);
                        lines2 = (int)Math.Floor(surface.Width * (1 - SplitRatio));
                        r = surface.Width - lines1 - lines2;
                    }
                    
                    lines1 += r;
                }
                else //split by lines
                {
                    lines1 = SplitLines;
                    
                    if (SplitDirection == LayoutSplitDirection.Horizonal)
                    {
                        lines2 = surface.Height - lines1;
                        r = surface.Height - lines1 - lines2;
                    }
                    else
                    {
                        lines2 = surface.Width - lines1;
                        r = surface.Width - lines1 - lines2;
                    }

                    lines2 += r;
                }

                if (lines1 < 3 && lines2 > 3)
                {
                    lines2 -= 3 - lines1;
                    lines1 = 3;
                }
                else if (lines1 < 3 && lines2 < 3) return;

                if (ReverseSplit)
                {
                    (lines1, lines2) = (lines2, lines1);
                }

                SurfaceRect rect1, rect2;
                var rect = new SurfaceRect(0, 0, surface.Width, surface.Height);

                if (SplitDirection == LayoutSplitDirection.Horizonal)
                {
                    rect1 = rect.TakeTop(lines1);
                    rect2 = rect.TakeBottom(lines2);
                }
                else
                {
                    rect1 = rect.TakeLeft(lines1);
                    rect2 = rect.TakeRight(lines2);
                }

                var surface1 = surface.Slice(rect1);
                var surface2 = surface.Slice(rect2);
                
                _child1.Render(surface1, frameNumber);
                _child2.Render(surface2, frameNumber);
            }
            else if (surface.Width >= 3 && surface.Height >= 3)
            {
                if (_panel is { })
                {
                    _panel.Render(surface.Trim(1), frameNumber);
                }
                else
                {
                    SurfacePanel.Empty.Render(surface.Trim(1), frameNumber);
                }
            }
        }
    }
}