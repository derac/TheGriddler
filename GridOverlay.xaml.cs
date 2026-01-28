using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Collections.Generic;

namespace TheGriddler;

public partial class GridOverlay : Window
{
    private Settings _settings;
    private IntPtr _targetHWnd;
    private System.Windows.Point? _startPos;
    private System.Windows.Point? _endPos;
    private System.Drawing.Rectangle _physicalBounds;
    private WindowManager.WindowBorders _cachedBorders;
    private int _rows;
    private int _columns;

    public bool IsSelecting => _startPos.HasValue;

    public GridOverlay(Settings settings, IntPtr targetHWnd)
    {
        InitializeComponent();
        _settings = settings;
        _targetHWnd = targetHWnd;

        // Cache the borders once at the start (window should be restored by now)
        _cachedBorders = WindowManager.GetWindowBorders(_targetHWnd);

        // Position and size the overlay
        var cursorPosition = System.Windows.Forms.Cursor.Position;
        var screen = System.Windows.Forms.Screen.FromPoint(cursorPosition);
        _physicalBounds = screen.WorkingArea;

        // Resolve per-monitor dimensions
        string friendlyName = WindowManager.GetFriendlyMonitorName(screen.DeviceName);
        var monitorConfig = _settings.GetOrCreateMonitorConfig(screen.DeviceName, friendlyName);
        _rows = monitorConfig.Rows;
        _columns = monitorConfig.Columns;

        // 1. Set basic properties so the window exists and is associated with the right monitor.
        // WPF's Window.Left/Top are in SYSTEM DIUs (logical units relative to primary monitor).
        var primaryScreen = System.Windows.Forms.Screen.PrimaryScreen;
        double systemScaleX = (primaryScreen?.Bounds.Width ?? 1.0) / System.Windows.SystemParameters.PrimaryScreenWidth;
        double systemScaleY = (primaryScreen?.Bounds.Height ?? 1.0) / System.Windows.SystemParameters.PrimaryScreenHeight;

        this.Left = _physicalBounds.Left / systemScaleX;
        this.Top = _physicalBounds.Top / systemScaleY;
        
        // We'll set physical size via Win32 after Show() to be absolutely sure.
        this.Width = 100; // Placeholder
        this.Height = 100; // Placeholder

        Logger.Log($"GridOverlay Logical Setup: Left={this.Left}, Top={this.Top} | Area={_physicalBounds} | SystemScale={systemScaleX:F2}");
        
        this.Loaded += (s, e) => {
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            // Position physically to the exact working area
            WindowManager.SetWindowPos(helper.Handle, IntPtr.Zero, _physicalBounds.Left, _physicalBounds.Top, _physicalBounds.Width, _physicalBounds.Height, 0x0040 /* SWP_SHOWWINDOW */);
            Logger.Log($"GridOverlay Physical Set: {_physicalBounds.Left},{_physicalBounds.Top} {_physicalBounds.Width}x{_physicalBounds.Height}");
        };
    }

    public void StartSelection(System.Windows.Point screenPos)
    {
        Logger.Log($"StartSelection physical screenPos: {screenPos}");
        
        // Use physical coordinates relative to the monitor bounds
        CalculateGridPosition(screenPos, out int col, out int row);
        
        _startPos = new System.Windows.Point(col, row); // Store Grid Coordinates (Col, Row) purely
        _endPos = _startPos;
        
        SelectionRect.Visibility = Visibility.Visible;
        UpdateSelection();
    }

    public void UpdateMouse(System.Windows.Point screenPos)
    {
        CalculateGridPosition(screenPos, out int col, out int row);
        _endPos = new System.Windows.Point(col, row);
        UpdateSelection();
    }

    private void CalculateGridPosition(System.Windows.Point screenPos, out int col, out int row)
    {
        // Relative physical offset
        double pX = screenPos.X - _physicalBounds.Left;
        double pY = screenPos.Y - _physicalBounds.Top;

        // Physical cell size
        double pCellW = (double)_physicalBounds.Width / _columns;
        double pCellH = (double)_physicalBounds.Height / _rows;

        col = (int)(pX / pCellW);
        row = (int)(pY / pCellH);

        // Clamp
        col = Math.Max(0, Math.Min(_columns - 1, col));
        row = Math.Max(0, Math.Min(_rows - 1, row));
    }

    private void UpdateSelection()
    {
        if (_startPos.HasValue && _endPos.HasValue)
        {
            // _startPos and _endPos now hold Grid Indices (Col, Row) directly!
            int startCol = (int)_startPos.Value.X;
            int startRow = (int)_startPos.Value.Y;
            int endCol = (int)_endPos.Value.X;
            int endRow = (int)_endPos.Value.Y;

            int minCol = Math.Min(startCol, endCol);
            int maxCol = Math.Max(startCol, endCol);
            int minRow = Math.Min(startRow, endRow);
            int maxRow = Math.Max(startRow, endRow);

            int colSpan = maxCol - minCol + 1;
            int rowSpan = maxRow - minRow + 1;

            // Visual Representation uses Logical Units (WPF)
            // Calculate boundaries instead of width/height to avoid rounding gaps
            double x_start = (double)minCol * ActualWidth / _columns;
            double x_end = (double)(minCol + colSpan) * ActualWidth / _columns;
            double y_start = (double)minRow * ActualHeight / _rows;
            double y_end = (double)(minRow + rowSpan) * ActualHeight / _rows;

            SelectionRect.Margin = new Thickness(x_start, y_start, 0, 0);
            SelectionRect.Width = x_end - x_start;
            SelectionRect.Height = y_end - y_start;

            // Snap (preview)
            _ = SnapAsync(final: false);
        }
    }

    private int _lastStartCol = -1, _lastStartRow = -1, _lastEndCol = -1, _lastEndRow = -1;

    public async System.Threading.Tasks.Task SnapAsync(bool final = true)
    {
        if (!_startPos.HasValue || !_endPos.HasValue) return;

        int startCol = (int)_startPos.Value.X;
        int startRow = (int)_startPos.Value.Y;
        int endCol = (int)_endPos.Value.X;
        int endRow = (int)_endPos.Value.Y;

        if (!final && startCol == _lastStartCol && startRow == _lastStartRow && endCol == _lastEndCol && endRow == _lastEndRow)
            return;

        _lastStartCol = startCol; _lastStartRow = startRow; _lastEndCol = endCol; _lastEndRow = endRow;

        // Short delay for final snap to let OS window transitions/animations settle
        // This is critical for DWM to report the correct 'extended frame bounds'.
        if (final) await System.Threading.Tasks.Task.Delay(50);

        int targetColStart = Math.Min(startCol, endCol);
        int targetRowStart = Math.Min(startRow, endRow);
        int targetColEnd = targetColStart + (Math.Abs(startCol - endCol) + 1);
        int targetRowEnd = targetRowStart + (Math.Abs(startRow - endRow) + 1);

        // Calculate boundaries in PHYSICAL coordinates
        double pX_start = _physicalBounds.Left + (targetColStart * (double)_physicalBounds.Width / _columns);
        double pX_end = _physicalBounds.Left + (targetColEnd * (double)_physicalBounds.Width / _columns);
        double pY_start = _physicalBounds.Top + (targetRowStart * (double)_physicalBounds.Height / _rows);
        double pY_end = _physicalBounds.Top + (targetRowEnd * (double)_physicalBounds.Height / _rows);

        int pX = (int)Math.Round(pX_start);
        int pY = (int)Math.Round(pY_start);
        int pWidth = (int)Math.Round(pX_end) - pX;
        int pHeight = (int)Math.Round(pY_end) - pY;

        Logger.Log($"Snap Physical Calc: Base={_physicalBounds.Left},{_physicalBounds.Top} | Cells={targetColStart},{targetRowStart} to {targetColEnd},{targetRowEnd} | Target={pX},{pY} {pWidth}x{pHeight}");

        // Refresh borders every time we snap, as the window might have moved between monitors
        // with different scaling factors during the drag.
        var currentBorders = WindowManager.GetWindowBorders(_targetHWnd);

        // Use the new Physical API which handles the specific DPI scaling logic internally
        WindowManager.SetWindowBoundsPhysical(_targetHWnd, new System.Drawing.Rectangle(pX, pY, pWidth, pHeight), currentBorders);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        SelectionRect.Visibility = Visibility.Collapsed;

        // Find the UniformGrid in the visual tree and configure it
        if (FindVisualChild<UniformGrid>(GridItems) is UniformGrid panel)
        {
            panel.Columns = _columns;
            panel.Rows = _rows;
            
            // Generate items to fill the grid
            var items = new List<int>(_columns * _rows);
            for (int i = 0; i < _columns * _rows; i++) items.Add(i);
            GridItems.ItemsSource = items;
        }
    }

    private T? FindVisualChild<T>(DependencyObject? obj) where T : DependencyObject
    {
        if (obj == null) return null;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(obj, i);
            if (child is T t)
                return t;
            
            if (FindVisualChild<T>(child) is T childOfChild)
                return childOfChild;
        }
        return null;
    }
}
