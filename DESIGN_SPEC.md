# WindowGridRedux - Design & Specification

## Overview
WindowGridRedux is a window management utility for Windows that allows users to quickly resize and position windows using a grid-based overlay. The user activates the grid by holding the Right Mouse Button (or a configured hotkey) and dragging to select a region on the screen. The target window (the one under the cursor) is then "snapped" to fill that physical region.

## Core Workflow
1.  **Activation**: 
    *   `GlobalHook` intercepts `RightMouseButtonDown`.
    *   `MainController` determines the target window under the cursor (`WindowManager.GetTargetWindow`).
    *   The OS drag loop is broken (`WindowManager.BreakDragLoop`) to take control from the Windows shell.
2.  **Selection**:
    *   A transparent `GridOverlay` window is spawned on the correct monitor.
    *   The overlay renders a grid (e.g., 12x12).
    *   As the user drags the mouse, a selection rectangle is drawn.
3.  **Snap**:
    *   On `RightMouseButtonUp`, the physical bounds of the selected grid cells are calculated.
    *   `WindowManager` applies these bounds to the target window using Win32 APIs.

## Technical Architecture

### Tech Stack
*   **Framework**: .NET 10.0
*   **UI System**: WPF (Windows Presentation Foundation)
*   **OS Integration**: Win32 API (P/Invoke) via `user32.dll` and `dwmapi.dll`.

### Key Components
*   **`MainController.cs`**: The central brain. It manages the application state (`Idle` vs `Dragging`), initializes the `GlobalHook`, and coordinates the interactions between the user's input and the window management logic.
*   **`WindowManager.cs`**: A static utility class containing all Win32 interop logic. It is responsible for:
    *   Getting window bounds in *physical pixels*.
    *   Handling DPI scaling calculations.
    *   Performing the actual `SetWindowPos` to move/resize windows.
*   **`GridOverlay.xaml.cs`**: The visual grid. It calculates the grid cells based on the monitor's physical resolution and handles the drawing of the blue selection rectangle.
*   **`GlobalHook.cs`**: Installs low-level mouse and keyboard hooks (`WH_MOUSE_LL`, `WH_KEYBOARD_LL`) to detect interactions even when the app is not in focus.

## DPI Awareness & Coordinate Systems (Critical)
This is the most complex part of the application due to how Windows handles high-DPI monitors and "Per-Monitor V2" awareness.

### The Problem
*   **WPF Coordinates**: Logical Units (1/96th of an inch). Scaled by system DPI.
*   **Win32 Coordinates**: Physical Pixels.
*   **Desktop Window Manager (DWM)**: Can "virtualize" windows that aren't DPI aware, lying to them about their size.
*   **Cross-Monitor Movement**: When a window moves between monitors with different DPIs, Windows sends `WM_DPICHANGED`. Applications might resize themselves automatically in response, causing race conditions with our snap logic.

### The Solution Strategy
1.  **Strict Physical Positioning**: We rely on `System.Windows.Forms.Screen` (or Win32 `GetDpiForMonitor`) to get the *exact physical pixel bounds* of the monitors. The `GridOverlay` is positioned using `SetWindowPos` to match these physical bounds exactly, ensuring the grid draws pixel-perfectly over the screen.
2.  **Border Compensation**: `WindowManager.GetWindowBorders` compares `DwmGetWindowAttribute` (visible frame) vs `GetWindowRect` (window rect) to determine the invisible shadow/border offsets. This ensures clearly aligned snaps without gaps.
3.  **Transient DPI Lag Handling**: 
    *   When moving a window to a new monitor, there is a delay where `GetWindowRect` might return coordinates from the *old* DPI scale, or the window might resize itself (Native scaling) mid-operation.
    *   **Fix**: We calculate an `InputScale` by comparing the window's actual size to the requested target size.
        *   If `InputScale` is `~1.0` and `ScaleFactor` is `1.0`, the specific window is behaving normally.
        *   If the window seemingly resizes (e.g. `InputScale` becomes `1.5` or `0.67`) but `ScaleFactor` says it shouldn't be virtualized (it's `1.0`), we detect this as a **Transient DPI Reaction**. In this case, we Force `InputScale = 1.0` to re-assert the intended physical dimensions, overriding the OS's temporary resize attempt.

### Future Maintenance
If you encounter "wrong size" or "wrong position" bugs:
1.  Check `debug.log`.
2.  Look at the `GetWindowBorders` log.
    *   `wRect`: The window rect as reported by Win32.
    *   `fRect`: The visible frame (DWM).
    *   `scale`: The detected virtualization scale (Frame / Window).
3.  Look at `SetWindowBoundsPhysical`.
    *   `InputScale`: The ratio of actual window size vs target size. If this isn't 1.0, the window is fighting our sizing commands (or is virtualized).

## Deployment
*   **Single File**: The app is currently compiled as a single file exe (though not strictly required).
*   **Manifest**: `app.manifest` defines `dpiAwareness` as `PerMonitorV2`. This is mandatory. Removing this will break the physical coordinate logic.
