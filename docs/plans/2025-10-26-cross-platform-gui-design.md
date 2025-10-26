# Cross-Platform GUI Implementation Design

**Goal:** Build a cross-platform Avalonia GUI application that reuses SearcherCore to provide an interactive search interface for Windows, Linux, and macOS.

**Architecture:** Simple MVVM pattern with Avalonia, accepting same CLI arguments as the CLI version. The GUI auto-starts searches with parameters from command line, displays results in real-time as they arrive, and provides user interactions (open file, copy path, show in folder) through a responsive, non-blocking UI powered by background threading.

**Tech Stack:** Avalonia (cross-platform UI), SearcherCore (search logic), MVVM pattern, async/await with Task.Run, ObservableCollection for reactive updates.

---

## Component Overview

### SearcherGui Project Structure
- **SearcherGui.csproj** - New .NET 9.0 cross-platform Avalonia project
- **App.xaml / App.xaml.cs** - Application entry point with DI setup
- **MainView.xaml / MainView.xaml.cs** - Main window UI definition
- **MainViewModel.cs** - ViewModel handling search state and UI logic
- **CliArgumentParser.cs** - Command-line argument parsing using CommandLineParser library
- **ResultInteractionService.cs** - Platform-specific file operations (open, copy, show in folder)
- **Models/SearchResultDisplay.cs** - ViewModel-friendly result representation

### UI Layout

Three-section window:

1. **Top Section**: Read-only display of search parameters
   - Search path, pattern, file type filters
   - Shows what the search is executing with

2. **Middle Section**: Results DataGrid
   - Columns: Filename, File Path, Match Count
   - Sortable, real-time updates as results arrive
   - Bound to ObservableCollection<SearchResultDisplay>
   - Double-click opens file, right-click shows context menu

3. **Bottom Section**: Status Bar
   - Progress display: "X files scanned, Y matches found"
   - Elapsed time counter
   - Stop button to cancel ongoing search
   - Search status (Running/Completed/Cancelled)

### Threading & Async Model

**Search Execution:**
- Called in `MainViewModel.OnInitialized()` using `Task.Run(() => PerformSearch())`
- Delegates to SearcherCore's `GlobSearch.ParallelFindFiles()` and `SearchFile.FileContainsStringWrapper()`
- Results stream in as they're found, not batched at completion
- Background thread holds no UI context

**Result Marshalling:**
- Results bubble up from SearcherCore on background thread
- Each result is wrapped in `Avalonia.Threading.Dispatcher.UIThread.InvokeAsync()` before adding to ObservableCollection
- Status updates (files scanned count) use similar dispatcher pattern
- Keeps UI responsive even during large searches

**Cancellation:**
- CancellationToken created in ViewModel and passed through search chain
- Stop button sets token cancellation
- SearcherCore already respects cancellation tokens

### File Interaction Service

**Platform Detection:**
- Windows: `Process.Start(filePath)` for file open, `explorer /select,filePath` for show in folder
- macOS: `open filePath` for file open, `open -R filePath` for show in folder
- Linux: `xdg-open filePath` for file open, file manager via `nautilus` or fallback to `xdg-open` with folder

**Clipboard Operations:**
- Copy file path uses Avalonia's `Clipboard.SetTextAsync()`
- Works identically across platforms

**Error Handling:**
- File not found: Display user-friendly error in status bar
- Missing handler: Gracefully report and continue
- Permission denied: Log and allow user to retry

### Command-Line Integration

**Argument Parsing:**
- Reuse `CommandLineParser` library (already in SearcherCore)
- Create `GuiCliOptions` extending SearcherCore's `CliOptions`
- Add GUI-specific options (e.g., window size, auto-close on completion)
- Parse in `Program.cs` before creating ViewModel

**Startup Flow:**
1. Parse command-line arguments into `GuiCliOptions`
2. Create MainWindow and inject options into MainViewModel
3. ViewModel receives options in constructor
4. OnInitialized() starts search immediately with provided parameters
5. Results populate as search progresses

### Error Handling & Edge Cases

- **No results found**: Show message in status bar, no errors logged
- **Invalid search path**: Show error dialog before search starts
- **Inaccessible files**: Log warnings per SearcherCore behavior, continue searching
- **Search cancelled**: Clear results, reset UI to initial state
- **Archive processing fails**: SearcherCore handles gracefully, continue with next file

---

## Implementation Notes

- Keep ViewModel logic focused on UI state and coordination
- SearcherCore handles all business logic (no duplication)
- Use Avalonia's data binding extensively to minimize code-behind
- Platform detection uses `RuntimeInformation.IsOSPlatform()` from System.Runtime.InteropServices
- No external process launching for common scenarios (use framework APIs where possible)
