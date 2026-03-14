using System;
using MandalaLogics.SurfaceTerminal.Surfaces;
using MandalaLogics.SurfaceTerminal.Text;

namespace MandalaLogics.SurfaceTerminal.Layout
{
    public enum SurfaceLineState
    {
        None = 0,
        Selected,
        Disabled,
        Activated,
        Enabled,
        Deactivated,
        Deselected
    }

    public delegate void SurfaceLineEventHandler(SurfaceLine sender);
    
    public abstract class SurfaceLine
    {
        public event SurfaceLineEventHandler? OnStateChanged;
        
        public SurfacePanel? Owner { get; internal set; }
        
        /// <summary>
        /// The last state successfully applied to this line.
        /// </summary>
        public SurfaceLineState State { get; private set; }

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (value) TryEnable();
                else TryDisable();
            }
        }
        
        public bool Selected
        {
            get => _selected;
            set
            {
                if (value) TrySelect();
                else TryDeselect();
            }
        }
        
        public bool Active
        {
            get => _activated;
            set
            {
                if (value) TryActivate();
                else TryDeactivate();
            }
        }
        
        private bool _enabled = true;
        private bool _selected = false;
        private bool _activated = true;
        
        public abstract void Render(ISurface<ConsoleChar> surface, ulong frameNumber);
        
        protected abstract bool StateChangeRequested(SurfaceLineState state);

        internal void KeyPressed(ConsoleKeyInfo keyInfo)
        {
            OnKeyPressed(keyInfo);
        }
        
        protected abstract void OnKeyPressed(ConsoleKeyInfo keyInfo);
        
        private bool TryChangeState(SurfaceLineState newState)
        {
            if (Owner is null) return false;

            if (newState == State) return false;

            if (Owner.LineStateTryChange(this, newState))
            {
                State = newState;
                
                OnStateChanged?.Invoke(this);

                switch (newState)
                {
                    case SurfaceLineState.Selected:
                        _selected = true;
                        break;
                    case SurfaceLineState.Disabled:
                        _enabled = false;
                        break;
                    case SurfaceLineState.Activated:
                        _activated = true;
                        break;
                    case SurfaceLineState.Enabled:
                        _enabled = true;
                        break;
                    case SurfaceLineState.Deactivated:
                        _activated = false;
                        break;
                    case SurfaceLineState.Deselected:
                        _selected = false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
                }
                
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TrySelect()
        {
            if (_selected) return true;

            if (StateChangeRequested(SurfaceLineState.Selected))
            {
                _selected = true;

                State = SurfaceLineState.Selected;
                
                OnStateChanged?.Invoke(this);

                return true;
            }

            return false;
        }

        public bool TryActivate()
        {
            if (_activated) return true;
            
            if (StateChangeRequested(SurfaceLineState.Activated))
            {
                _activated = true;
                
                State = SurfaceLineState.Activated;
                
                OnStateChanged?.Invoke(this);

                return true;
            }

            return false;
        }
        
        public bool TryEnable()
        {
            if (_enabled) return true;
            
            if (StateChangeRequested(SurfaceLineState.Enabled))
            {
                _enabled = true;
                
                State = SurfaceLineState.Enabled;
                
                OnStateChanged?.Invoke(this);

                return true;
            }

            return false;
        }

        public bool TryDisable()
        {
            if (!_enabled) return true;
            
            if (StateChangeRequested(SurfaceLineState.Disabled))
            {
                _enabled = false;
                
                State = SurfaceLineState.Disabled;
                
                OnStateChanged?.Invoke(this);
                
                return true;
            }

            return false;
        }

        public bool TryDeactivate()
        {
            if (!_activated) return true;
            
            if (StateChangeRequested(SurfaceLineState.Deactivated))
            {
                _activated = false;
                
                State = SurfaceLineState.Deactivated;
                
                OnStateChanged?.Invoke(this);

                return true;
            }

            return false;
        }

        public bool TryDeselect()
        {
            if (!_selected) return true;
            
            if (StateChangeRequested(SurfaceLineState.Deselected))
            {
                _selected = false;
                
                State = SurfaceLineState.Deselected;
                
                OnStateChanged?.Invoke(this);

                return true;
            }

            return false;
        }
    }
}