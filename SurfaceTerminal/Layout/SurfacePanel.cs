using System;
using MandalaLogics.SurfaceTerminal.Layout.Components;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;

namespace MandalaLogics.SurfaceTerminal.Layout
{
    public abstract class SurfacePanel
    {
        public static readonly EmptyPanel Empty = new EmptyPanel();
        public bool IsSelected { get; private set; }
        
        protected SurfacePanel() {}

        public abstract void Render(ISurface<ConsoleChar> surface, ulong frameNumber);

        internal void Deselected()
        {
            IsSelected = false;
            OnDeselected();
        }

        internal void Selected()
        {
            IsSelected = true;
            OnSelected();
        }

        internal void KeyPressed(ConsoleKeyInfo keyInfo) => OnKeyPressed(keyInfo);

        protected abstract void OnDeselected();

        protected abstract void OnSelected();

        protected abstract void OnKeyPressed(ConsoleKeyInfo keyInfo);

        protected abstract bool OnLineStateTryChange(SurfaceLine line, SurfaceLineState newState);

        internal bool LineStateTryChange(SurfaceLine line, SurfaceLineState newState) =>
            OnLineStateTryChange(line, newState);
    }
}