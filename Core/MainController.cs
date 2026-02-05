using System;
using System.Drawing;
using System.Windows;

using TheGriddler.Models;
using TheGriddler.Views;

namespace TheGriddler.Core;

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
        
        // Initialize state to avoid missing the first drag if app started while button was down
        _isLButtonDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LBUTTON) & 0x8000) != 0;

        _hook.LeftButtonDown += OnLeftButtonDown;
        _hook.RightButtonDown += OnRightButtonDown;
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
        if (_isLButtonDown != down)
        {
            _isLButtonDown = down;
        }
        
        if (!down && _isDragging)
        {
            SnapAndClose();
        }
    }

    private bool HandleRightClick()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Logger.Log($"HandleRightClick: START");
        
        // Re-check left button state using GetAsyncKeyState to be absolutely sure we aren't out of sync
        bool physicalLButtonDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LBUTTON) & 0x8000) != 0;
        if (_isLButtonDown != physicalLButtonDown)
        {
            Logger.Log($"HandleRightClick: LButton state corrected from {_isLButtonDown} to {physicalLButtonDown}");
            _isLButtonDown = physicalLButtonDown;
        }

        if (_isLButtonDown && !_isDragging)
        {
            var cursorPosition = System.Windows.Forms.Cursor.Position;
            IntPtr target = WindowManager.GetTargetWindow(new System.Drawing.Point(cursorPosition.X, cursorPosition.Y));
            Logger.Log($"HandleRightClick: GetTargetWindow returned {target:X} at {sw.ElapsedMilliseconds}ms");
            
            if (target != IntPtr.Zero)
            {
                // Verify if the window is truly in a move/size loop
                uint processId;
                uint threadId = NativeMethods.GetWindowThreadProcessId(target, out processId);
                
                NativeMethods.GUITHREADINFO guiInfo = new NativeMethods.GUITHREADINFO();
                guiInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(guiInfo);
                
                if (NativeMethods.GetGUIThreadInfo(threadId, ref guiInfo))
                {
                    // Check if flags indicate move/size OR if hwndMoveSize is set (either/or check is robust)
                    bool isMoving = (guiInfo.flags & NativeMethods.GUI_INMOVESIZE) != 0 || guiInfo.hwndMoveSize != IntPtr.Zero;
                    Logger.Log($"HandleRightClick: GUI_INMOVESIZE check - flags=0x{guiInfo.flags:X}, hwndMoveSize={guiInfo.hwndMoveSize:X}, hwndCapture={guiInfo.hwndCapture:X}, isMoving={isMoving} at {sw.ElapsedMilliseconds}ms");
                    
                    if (!isMoving)
                    {
                        // Not dragging/sizing -> fail gracefully (pass through right click)
                        Logger.Log($"HandleRightClick: Window not in move/size mode, passing through");
                        return false;
                    }
                }
                else
                {
                    Logger.Log($"HandleRightClick: GetGUIThreadInfo FAILED for threadId={threadId}");
                }

                // Break drag loop IMMEDIATELY on the hook thread
                Logger.Log($"HandleRightClick: Calling BreakDragLoop at {sw.ElapsedMilliseconds}ms");
                WindowManager.BreakDragLoop(target);
                Logger.Log($"HandleRightClick: BreakDragLoop completed at {sw.ElapsedMilliseconds}ms");
                
                // Verify the drag was actually broken
                NativeMethods.GUITHREADINFO postBreakInfo = new NativeMethods.GUITHREADINFO();
                postBreakInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(postBreakInfo);
                if (NativeMethods.GetGUIThreadInfo(threadId, ref postBreakInfo))
                {
                    bool stillMoving = (postBreakInfo.flags & NativeMethods.GUI_INMOVESIZE) != 0 || postBreakInfo.hwndMoveSize != IntPtr.Zero;
                    Logger.Log($"HandleRightClick: POST-BREAK check - flags=0x{postBreakInfo.flags:X}, hwndMoveSize={postBreakInfo.hwndMoveSize:X}, stillMoving={stillMoving}");
                    if (stillMoving)
                    {
                        Logger.Log($"HandleRightClick: WARNING - BreakDragLoop did NOT cancel the native drag!");
                    }
                }
                
                Logger.Log($"HandleRightClick: Calling ActivateGrid at {sw.ElapsedMilliseconds}ms");
                ActivateGrid(target, cursorPosition);
                Logger.Log($"HandleRightClick: ActivateGrid call returned at {sw.ElapsedMilliseconds}ms (async work continues)");
                // We are now technically "dragging" (activating). 
                // Any following right-clicks should be blocked too.
                return true;
            }
        }
        else if (_isDragging)
        {
            // If we are already dragging/activating, we ALWAYS handle (block) the right-click
            if (_overlay != null)
            {
                if (!_overlay.IsSelecting)
                {
                    var cursorPosition = System.Windows.Forms.Cursor.Position;
                    _overlay.StartSelection(new System.Windows.Point(cursorPosition.X, cursorPosition.Y));
                }
                else
                {
                    SnapAndClose();
                }
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
            // Always check _suppressRightUp first
            if (_suppressRightUp)
            {
                _suppressRightUp = false;
                return true;
            }
            
            // If we are dragging, we should also block the RightUp even if suppress was missed somehow
            if (_isDragging) return true;
        }
        return false;
    }

    private async void ActivateGrid(IntPtr target, System.Drawing.Point startPoint)
    {
        try
        {
            _targetHWnd = target;
            _isDragging = true;

            // Offload the heavy work (WPF window creation) to ensure we don't block the UI thread too much
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Ensure restored BEFORE showing overlay
                WindowManager.EnsureRestored(_targetHWnd);
                
                _overlay = new GridOverlay(_settings, _targetHWnd, startPoint);
                _overlay.Show();
                
                // Immediately start selection from the captured cursor position
                _overlay.StartSelection(new System.Windows.Point(startPoint.X, startPoint.Y));
            });
        }
        catch (Exception ex)
        {
            Logger.Log($"Error activating grid: {ex.Message}");
            _isDragging = false;
        }
    }

    private async void SnapAndClose()
    {
        if (_overlay != null)
        {
            try
            {
                if (_overlay.IsSelecting)
                {
                    await _overlay.SnapAsync(final: true);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error during snap: {ex.Message}");
            }
            finally
            {
                _overlay.Close();
                _overlay = null;
            }
        }
        _isDragging = false;
    }

    private void CloseOverlay()
    {
        if (_overlay != null)
        {
            try { _overlay.Close(); } catch { }
            _overlay = null;
        }
    }

    public void Dispose()
    {
        _hook.Dispose();
        CloseOverlay();
    }
}
