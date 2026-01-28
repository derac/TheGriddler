# <img height="30" alt="icon" src="https://github.com/derac/TheGriddler/blob/master/Resources/icon.png" /> The Griddler

The Griddler is a lightweight window management tool for Windows that allows you to snap windows to a configurable grid using your mouse. Inspired by WindowGrid.

<img height="300" alt="usage example" src="https://github.com/user-attachments/assets/cf7fc31d-4ee2-42a8-8572-a8bf1a87d96c" />

*Drag and drop window tiling*

<img height="300" alt="settings" src="https://github.com/user-attachments/assets/12c3f9f6-a4e6-490e-aa4c-031dc3d9f60d" />

*Simple settings*

## 🚀 For Users

### How to Use
1. Put the [exe](https://github.com/derac/TheGriddler/releases/download/v1.0.0/TheGriddler.exe) anywhere you want.
1. Start it.
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

### Project Structure
- `MainController.cs`: The core engine that handles global input hooks and coordinates window snapping logic.
- `WindowManager.cs`: A wrapper around Win32 APIs for window manipulation, DPI awareness, and monitor info.
- `GridOverlay.xaml`: The WPF window that renders the grid lines and selection rectangle.
- `Settings.cs`: Manages persistent user preferences (JSON-based).
- `GlobalHook.cs`: Handles low-level mouse hooks to detect dragging and clicking outside the app.

### Key Technologies
- **WPF**: Used for the UI and overlay.
- **Win32 API**: Used for low-level window management (`SetWindowPos`, `GetWindowRect`, etc.).
- **Global Mouse Hook**: Used to intercept mouse events while other windows are focused.

---

## 🛠 Maintainability & Design
The codebase is designed with a separation of concerns:
- **UI Logic** is kept in XAML and its code-behind.
- **Native Interop** is centralized in `WindowManager.cs`.
- **Application State** is managed via the `Settings` singleton with `INotifyPropertyChanged` for reactive UI updates.

---

## 📜 Credits
- Created by **derac**.
- Inspired by **WindowGrid** by Joshua Wilding.

---

*Happy Gridding!*
