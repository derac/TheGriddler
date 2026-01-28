using System;
using System.Drawing;
using System.Windows;

namespace TheGriddler
{
    public class MainController : IDisposable
    {
        private GlobalHook _hook;
        private Settings _settings;
        private GridOverlay? _overlay;
        private IntPtr _targetHWnd;
        private bool _isDragging;
        private bool _isLButtonDown;
        private bool _suppressRightUp;

        public MainController()
        {
            _settings = Settings.Instance;
            _hook = new GlobalHook();
            
            _hook.LeftButtonDown += OnLeftButtonDown;
            _hook.RightButtonDown += OnRightButtonDown;
            _hook.KeyDown += OnKeyDown;
            _hook.KeyUp += OnKeyUp;
            _hook.MouseMoved += OnMouseMoved;
        }

        private void OnMouseMoved(System.Drawing.Point pos)
        {
            if (_isDragging && _overlay != null)
            {
                // Pass raw screen coordinates (physical pixels)
                // GridOverlay handles DPI and offset conversion internally
                _overlay.UpdateMouse(new System.Windows.Point(pos.X, pos.Y));
            }
        }

        private void OnLeftButtonDown(bool down)
        {
            _isLButtonDown = down;
            if (!down)
            {
                if (_isDragging)
                {
                    SnapAndClose();
                }
            }
        }

        private bool HandleRightClick()
        {
            if (_isLButtonDown && !_isDragging)
            {
                ActivateGrid();
                return true;
            }
            else if (_isDragging && _overlay != null)
            {
                if (!_overlay.IsSelecting)
                {
                    // Start selection at current mouse position
                    var cursorPosition = System.Windows.Forms.Cursor.Position;
                    _overlay.StartSelection(new System.Windows.Point(cursorPosition.X, cursorPosition.Y));
                }
                else
                {
                    // Finish selection
                    SnapAndClose();
                }
                return true;
            }
            return false;
        }

        private bool OnRightButtonDown(bool down)
        {
            if (down)
            {
                if (HandleRightClick())
                {
                    _suppressRightUp = true;
                    return true;
                }
            }
            else
            {
                if (_suppressRightUp)
                {
                    _suppressRightUp = false;
                    return true;
                }
            }
            return false;
        }

        private void OnKeyDown(int vkCode)
        {
            // Map Space/LControl to Right-Click logic
            if (vkCode == 32 || vkCode == 162)
            {
                HandleRightClick();
            }
        }

        private void OnKeyUp(int vkCode)
        {
        }


        private async void ActivateGrid()
        {
            if (_isDragging) return;

            // Offload the heavy work to ensure the Hook callback returns immediately
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var cursorPosition = System.Windows.Forms.Cursor.Position;
                _targetHWnd = WindowManager.GetTargetWindow(new System.Drawing.Point(cursorPosition.X, cursorPosition.Y));

                if (_targetHWnd != IntPtr.Zero)
                {
                    // Break the OS drag loop so we can take over
                    WindowManager.BreakDragLoop(_targetHWnd);

                    // Ensure restored BEFORE waiting, to give it time to animate/update
                    WindowManager.EnsureRestored(_targetHWnd);
                    
                    _isDragging = true;
                    _overlay = new GridOverlay(_settings, _targetHWnd);
                    _overlay.Show();
                    
                    // Immediately start selection from the current cursor position
                    // so the first Right Click activates AND starts the span.
                    _overlay.StartSelection(new System.Windows.Point(cursorPosition.X, cursorPosition.Y));
                }
            });
        }

        private void SnapAndClose()
        {
            if (_overlay != null)
            {
                if (_overlay.IsSelecting)
                {
                    _overlay.Snap(final: true);
                }
                _overlay.Close();
                _overlay = null;
            }
            _isDragging = false;
        }

        private void CloseOverlay()
        {
            if (_overlay != null)
            {
                _overlay.Close();
                _overlay = null;
            }
        }

        public void Dispose()
        {
            _hook.Dispose();
            CloseOverlay();
        }
    }
}
