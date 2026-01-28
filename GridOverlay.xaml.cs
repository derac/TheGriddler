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
    private int _rows;
    private int _columns;

    public bool IsSelecting => _startPos.HasValue;

    public GridOverlay(Settings settings, IntPtr targetHWnd, System.Drawing.Point startPoint)
    {
        InitializeComponent();
        _settings = settings;
        _targetHWnd = targetHWnd;


        // Log all monitors for layout debugging
        foreach (var s in System.Windows.Forms.Screen.AllScreens)
        {
            Logger.Log($"DEBUG: Monitor Detected: {s.DeviceName} '{(s.Primary ? "Primary" : "")}' Bounds={s.Bounds} WorkingArea={s.WorkingArea}");
        }

        // Position and size the overlay based on the point where the right-click happened
        var screen = System.Windows.Forms.Screen.FromPoint(startPoint);
        _physicalBounds = screen.WorkingArea;
        // Resolve per-monitor dimensions
        string friendlyName = WindowManager.GetFriendlyMonitorName(screen.DeviceName);
        var monitorConfig = _settings.GetOrCreateMonitorConfig(screen.DeviceName, friendlyName);
        _rows = monitorConfig.Rows;
        _columns = monitorConfig.Columns;

        Logger.Log($"DEBUG: GridOverlay active on screen: {screen.DeviceName} with _physicalBounds={_physicalBounds}, grid={_columns}x{_rows}");

        // 1. Set basic properties so the window exists and is associated with the right monitor.
        // WPF's Window.Left/Top usually are in SYSTEM DIUs, but user requests 1:1 mapping.
        this.Left = _physicalBounds.Left;
        this.Top = _physicalBounds.Top;
        this.Width = _physicalBounds.Width;
        this.Height = _physicalBounds.Height;

        Logger.Log($"GridOverlay Logical Setup: Left={this.Left}, Top={this.Top} | Area={_physicalBounds}");
        
        this.Loaded += (s, e) => {
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            // Position physically to the exact working area
            Logger.Log($"DEBUG: GridOverlay.Loaded physical set start: {helper.Handle:X} to {_physicalBounds}");
            WindowManager.SetWindowPos(helper.Handle, IntPtr.Zero, _physicalBounds.Left, _physicalBounds.Top, _physicalBounds.Width, _physicalBounds.Height, 0x0040 /* SWP_SHOWWINDOW */);
            Logger.Log($"DEBUG: GridOverlay Physical Set done: {_physicalBounds.Left},{_physicalBounds.Top} {_physicalBounds.Width}x{_physicalBounds.Height}");
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
        // Use the initial physical bounds to lock calculations to the starting monitor
        double relX = screenPos.X - _physicalBounds.Left;
        double relY = screenPos.Y - _physicalBounds.Top;

        // Clamp physical offsets to the initial monitor bounds
        double pX = Math.Max(0, Math.Min(_physicalBounds.Width - 1, relX));
        double pY = Math.Max(0, Math.Min(_physicalBounds.Height - 1, relY));

        // Physical cell size on the starting monitor
        double pCellW = (double)_physicalBounds.Width / _columns;
        double pCellH = (double)_physicalBounds.Height / _rows;

        col = (int)(pX / pCellW);
        row = (int)(pY / pCellH);

        Logger.Log($"DEBUG: CalculateGridPosition (Clamped): pos=({screenPos.X},{screenPos.Y}), bounds={_physicalBounds.Left},{_physicalBounds.Top} {_physicalBounds.Width}x{_physicalBounds.Height}, rel=({relX:F2},{relY:F2}), cell={pCellW:F2}x{pCellH:F2}, colrow={col},{row}");

        // Clamp indices just in case of floating point edge cases
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

        int targetColStart = Math.Min(startCol, endCol);
        int targetRowStart = Math.Min(startRow, endRow);
        int targetColEnd = targetColStart + (Math.Abs(startCol - endCol) + 1);
        int targetRowEnd = targetRowStart + (Math.Abs(startRow - endRow) + 1);

        // Calculate boundaries in PHYSICAL coordinates relative to the initial monitor
        double pX_start = _physicalBounds.Left + (targetColStart * (double)_physicalBounds.Width / _columns);
        double pX_end = _physicalBounds.Left + (targetColEnd * (double)_physicalBounds.Width / _columns);
        double pY_start = _physicalBounds.Top + (targetRowStart * (double)_physicalBounds.Height / _rows);
        double pY_end = _physicalBounds.Top + (targetRowEnd * (double)_physicalBounds.Height / _rows);

        int pX = (int)Math.Round(pX_start);
        int pY = (int)Math.Round(pY_start);
        int pWidth = (int)Math.Round(pX_end) - pX;
        int pHeight = (int)Math.Round(pY_end) - pY;

        Logger.Log($"DEBUG: SnapAsync Calc (Clamped): pBounds={_physicalBounds} | targetCells=({targetColStart},{targetRowStart}) to ({targetColEnd},{targetRowEnd}) | physicalStart=({pX_start:F2},{pY_start:F2}), physicalEnd=({pX_end:F2},{pY_end:F2}) | finalPBounds={pX},{pY} {pWidth}x{pHeight}");

        // Use the physical API to ensure accuracy
        WindowManager.SetWindowBounds(_targetHWnd, new System.Drawing.Rectangle(pX, pY, pWidth, pHeight));
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
