# <img height="30" alt="icon" src="https://github.com/derac/TheGriddler/blob/master/Resources/icon.png" /> The Griddler

A lightweight window management tool for Windows that allows you to snap windows to a configurable grid using your mouse. Inspired by WindowGrid.

<img height="300" alt="usage example" src="https://github.com/user-attachments/assets/cf7fc31d-4ee2-42a8-8572-a8bf1a87d96c" />

*Drag and drop window tiling*

<img height="300" alt="settings" src="https://github.com/user-attachments/assets/12c3f9f6-a4e6-490e-aa4c-031dc3d9f60d" />

*Simple settings*

## 🚀 For Users

### How to Use
1. Put the [exe](https://github.com/derac/TheGriddler/releases/download/v1.0.0/TheGriddler.exe) anywhere you want.
1. Double click to run it.
1. While dragging a window by holding left click, right click in the grid section you want to resize from, continue dragging left click to the grid section you want to resize to and press right click again or let go of left click.
1. Double click the quickbar icon to launch settings. <img height="70" alt="{692DF108-4142-40C0-B454-73C4C36FF198}" src="https://github.com/user-attachments/assets/74883d4c-4938-4b94-a821-2df64af839a7" />
1. Settings will be saved in your `%appdata%` folder.
1. If you select the option to run at startup, it will make an entry in `shell:startup` (you can go there in file explorer).


---

## 💻 For Developers

### Prerequisites
- **Visual Studio 2022** or **VS Code**.
- **.NET 10.0 SDK**.
- Windows OS (Required for Win32 API and WPF).

### Building the Project
1.  Clone the repository.
2.  Open a terminal in the project root.
3.  Run the following commands:
    ```bash
    dotnet build
    dotnet run
    ```
4.  To build a production executable to the `publish` folder:
    ```bash
    dotnet publish -c Release -o publish --self-contained true /p:PublishSingleFile=true /p:IncludeAllContentForSelfExtract=true
    ```

---

## 📁 Project Structure

```
TheGriddler/
├── App.xaml / App.xaml.cs    # Application entry point, system tray icon, lifecycle
├── Core/                     # Core business logic and Windows API interop
│   ├── GlobalHook.cs         # Low-level mouse hook for system-wide input capture
│   ├── MainController.cs     # Orchestrates dragging detection and grid activation
│   ├── NativeMethods.cs      # P/Invoke declarations for Windows APIs
│   ├── WindowManager.cs      # Window manipulation utilities
│   └── Logger.cs             # Simple debug logging utility
├── Models/
│   └── Settings.cs           # User preferences with JSON persistence
├── Views/
│   ├── GridOverlay.xaml/.cs  # Transparent overlay that renders the snap grid
│   └── MainWindow.xaml/.cs   # Settings window UI
├── Helpers/
│   ├── Converters.cs         # WPF value converters for data binding
│   └── UiHelpers.cs          # Visual tree traversal utilities
└── Resources/
    └── icon.png / icon.ico   # Application icon
```

### Key Files Explained

| File | Purpose |
|------|---------|
| [**App.xaml.cs**](App.xaml.cs) | Initializes the app in the system tray, creates the `NotifyIcon`, handles the settings window, and manages the app lifecycle. |
| [**Core/GlobalHook.cs**](Core/GlobalHook.cs) | Installs a low-level mouse hook (`WH_MOUSE_LL`) to capture mouse events globally across all windows. Exposes events for left/right button and mouse movement. |
| [**Core/MainController.cs**](Core/MainController.cs) | The brain of the app. Listens to `GlobalHook` events, detects when a user is dragging a window + right-clicks, breaks the native drag loop, and spawns the `GridOverlay`. |
| [**Core/NativeMethods.cs**](Core/NativeMethods.cs) | Contains all P/Invoke declarations for Windows APIs used throughout the project (see below). |
| [**Core/WindowManager.cs**](Core/WindowManager.cs) | Helper methods for window operations: finding the target window, breaking drag loops, restoring minimized windows, and setting window bounds. |
| [**Models/Settings.cs**](Models/Settings.cs) | Singleton that manages user preferences (grid dimensions per monitor, colors, dark mode, run-on-startup). Persists to `%appdata%/TheGriddler/settings.json`. Implements `INotifyPropertyChanged` for reactive WPF bindings. |
| [**Views/GridOverlay.xaml.cs**](Views/GridOverlay.xaml.cs) | A transparent, click-through WPF window that covers the active monitor. Renders a grid based on the monitor's configured rows/columns and handles selection rectangle drawing. Calculates physical pixel coordinates for snapping and applies DPI compensation for cross-monitor scenarios. |
| [**Views/MainWindow.xaml.cs**](Views/MainWindow.xaml.cs) | The settings UI allowing users to configure grid dimensions per monitor, pick colors, toggle dark mode, and enable run-on-startup. |

---

## 🔧 Windows APIs Used

The Griddler relies heavily on Win32 APIs via P/Invoke to achieve system-wide window management. All API declarations are centralized in `Core/NativeMethods.cs`.

### Mouse Hooking
| API | Purpose |
|-----|---------|
| [`SetWindowsHookEx`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowshookexw) | Installs a low-level mouse hook (`WH_MOUSE_LL`) to intercept mouse events globally, even when other apps have focus. |
| [`CallNextHookEx`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-callnexthookex) | Passes the hook event to the next handler in the chain. |
| [`UnhookWindowsHookEx`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-unhookwindowshookex) | Removes the hook when the app exits. |
| [`GetAsyncKeyState`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getasynckeystate) | Polls physical button state to sync internal state with actual mouse buttons. |

### Window Discovery & Manipulation
| API | Purpose |
|-----|---------|
| [`GetForegroundWindow`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getforegroundwindow) | Gets the currently active window (the one being dragged). |
| [`WindowFromPoint`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-windowfrompoint) | Finds the window under given screen coordinates. |
| [`GetAncestor`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getancestor) | Traverses to the top-level parent window (`GA_ROOT`). |
| [`GetWindowThreadProcessId`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowthreadprocessid) | Gets the process ID owning a window (used to exclude self). |
| [`GetWindowRect`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowrect) | Gets the bounding rectangle of a window. |
| [`SetWindowPos`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos) | Moves and resizes windows to snap them to grid cells. Key flags: `SWP_NOZORDER`, `SWP_FRAMECHANGED`, `SWP_NOACTIVATE`. |
| [`ShowWindow`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindow) | Restores a maximized/minimized window before snapping (`SW_RESTORE`). |
| [`IsIconic`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-isiconic) / [`IsZoomed`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-iszoomed) | Checks if a window is minimized or maximized. |
| [`SetForegroundWindow`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow) | Brings a window to the foreground (used for context menu). |

### Drag Loop Interruption
| API | Purpose |
|-----|---------|
| [`GetGUIThreadInfo`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getguithreadinfo) | Checks if a window is in a move/size modal loop (`GUI_INMOVESIZE` flag). |
| [`SendMessage`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendmessage) | Sends `WM_CANCELMODE` to cancel modal operations and `WM_LBUTTONUP` to simulate mouse release. |
| [`ReleaseCapture`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-releasecapture) | Releases mouse capture from the window. |

### Monitor & DPI Awareness
| API | Purpose |
|-----|---------|
| [`MonitorFromWindow`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-monitorfromwindow) / [`MonitorFromPoint`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-monitorfrompoint) | Determines which monitor a window or point is on. |
| [`GetDpiForWindow`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getdpiforwindow) | Gets the DPI the window is currently using. |
| [`GetDpiForMonitor`](https://learn.microsoft.com/en-us/windows/win32/api/shellscalingapi/nf-shellscalingapi-getdpiformonitor) | Gets the effective DPI of a monitor (for multi-monitor setups). |
| [`GetWindowDpiAwarenessContext`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowdpiawarenesscontext) / [`GetAwarenessFromDpiAwarenessContext`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getawarenessfromdpiawarenesscontext) | Detects if an app is Per-Monitor DPI aware to know whether to apply DPI compensation. |
| [`EnumDisplayDevices`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumdisplaydevicesw) | Enumerates display adapters to get friendly monitor names. |

### Desktop Window Manager
| API | Purpose |
|-----|---------|
| [`DwmGetWindowAttribute`](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmgetwindowattribute) | Gets extended frame bounds (`DWMWA_EXTENDED_FRAME_BOUNDS`) for accurate window sizing. |

### Miscellaneous
| API | Purpose |
|-----|---------|
| [`DestroyIcon`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-destroyicon) | Frees GDI icon handles (used for system tray icon cleanup). |
| [`GetModuleHandle`](https://learn.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-getmodulehandlew) | Gets the current module handle for hook installation. |

---

## 🛠 Architecture & Design

### Core Flow
1. **Startup**: `App.xaml.cs` initializes `MainController` and creates a system tray icon.
2. **Hooking**: `GlobalHook` installs a `WH_MOUSE_LL` hook to monitor all mouse events.
3. **Drag Detection**: When a right-click occurs while the left button is held, `MainController` checks if the foreground window is in a move/size loop using `GetGUIThreadInfo`.
4. **Break Drag**: If confirmed, `WindowManager.BreakDragLoop` sends `WM_CANCELMODE` and `WM_LBUTTONUP` to interrupt the native drag.
5. **Overlay Display**: A `GridOverlay` window is spawned covering the current monitor, showing a configurable grid.
6. **Selection**: Mouse movement updates the selection rectangle. On completion (right-click or left release), `SnapAsync` calculates target bounds in physical pixels.
7. **Snapping**: `SetWindowPos` moves/resizes the window to the selected grid cells. DPI compensation is applied for Per-Monitor DPI aware apps.

### Design Principles
- **Separation of Concerns**: UI logic in Views, native interop in Core, state in Models.
- **Reactive Settings**: `Settings` implements `INotifyPropertyChanged` for automatic UI updates.
- **Per-Monitor Support**: Grid dimensions and DPI handling work correctly across multi-monitor setups.
- **Minimal Footprint**: Runs as a system tray app with no main window.

---

## 📜 Credits
- Created by **derac**.
- Inspired by **WindowGrid** by Joshua Wilding.

---

*Happy Gridding!*
