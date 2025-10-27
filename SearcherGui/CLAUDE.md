# SearcherGui - CLAUDE.md

Project-specific guidance for the SearcherGui Avalonia cross-platform desktop application.

## Overview

SearcherGui is a cross-platform desktop search application built with Avalonia, enabling recursive text search within files (including ZIP archives, PDFs, and DOCX documents). It targets .NET 9.0 and supports Windows, Linux, and macOS.

## Build Commands

```bash
# Build SearcherGui
dotnet build SearcherGui/SearcherGui.csproj

# Build for release
dotnet build SearcherGui/SearcherGui.csproj -c Release

# Build for specific platform
dotnet build SearcherGui/SearcherGui.csproj -c Release -r win-x64
dotnet build SearcherGui/SearcherGui.csproj -c Release -r linux-x64
dotnet build SearcherGui/SearcherGui.csproj -c Release -r osx-arm64

# Clean artifacts
dotnet clean SearcherGui/SearcherGui.csproj

# Format code
dotnet format SearcherGui/SearcherGui.csproj

# Restore packages
dotnet restore SearcherGui/SearcherGui.csproj
```

## Development Commands

```bash
# Run the application
dotnet run --project SearcherGui/SearcherGui.csproj

# Run with Debug configuration
dotnet run --project SearcherGui/SearcherGui.csproj -c Debug

# Run with command-line arguments
dotnet run --project SearcherGui/SearcherGui.csproj -- --help
```

## Project Structure

```
SearcherGui/
├── Program.cs                 # Application entry point, CLI parsing, Avalonia setup
├── App.axaml                  # Application-level XAML resources and styling
├── App.axaml.cs               # Application-level code-behind
├── GuiCliOptions.cs           # Command-line options specific to the GUI application
├── MainWindow.axaml           # Primary application window layout (XAML)
├── MainWindow.axaml.cs        # Window code-behind
├── ViewModels/
│   └── MainViewModel.cs       # MVVM ViewModel for main window (reactive properties, commands)
├── Views/
│   └── MainView.axaml.cs      # Custom controls and views
├── Models/
│   └── SearchResultDisplay.cs # Data model for search results display
└── Services/
    └── ResultInteractionService.cs # Service for handling result interactions
```

## Key Components

### Program.cs
- **Responsibility**: Entry point, command-line argument parsing, Avalonia initialization
- **Key Features**:
  - Parses `GuiCliOptions` from command-line arguments
  - Stores options in static `Program.Options` property for app-wide access
  - Initializes Avalonia with platform detection (`UsePlatformDetect()`)
  - Uses classic desktop lifetime for compatibility

### App.axaml / App.axaml.cs
- **Responsibility**: Application-level resources, theme initialization, root setup
- **Key Features**:
  - Fluent theme from Avalonia
  - Global resource definitions
  - Application lifecycle hooks

### MainWindow.axaml / MainWindow.axaml.cs
- **Responsibility**: Primary UI window containing search interface and results display
- **Key Features**:
  - Search input controls
  - Results ListView/DataGrid
  - Progress indicators and status messages
  - Bound to `MainViewModel` for reactive behavior

### MainViewModel.cs
- **Responsibility**: Core UI logic and state management following MVVM pattern
- **Key Features**:
  - Inherits from `ReactiveObject` (ReactiveUI)
  - Observable properties: `FilesScanned`, `MatchesFound`, `IsSearching`, `StatusMessage`, `Results`
  - Reactive commands for search and cancel operations
  - Thread-safe result collection (`ObservableCollection<SearchResultDisplay>`)
  - Integrates with `SearcherCore.GlobSearch` and `SearcherCore.SearchFile`
  - Manages `CancellationTokenSource` for cancellable long-running operations

### GuiCliOptions.cs
- **Responsibility**: Command-line options specific to the GUI application
- **Key Features**:
  - Extends or wraps `SearcherCore.CliOptions`
  - Defines GUI-specific parameters (initial directory, search pattern, etc.)
  - Uses CommandLineParser library for argument validation

### SearchResultDisplay.cs (Models)
- **Responsibility**: Data model for displaying search results in the UI
- **Key Features**:
  - Wraps `SearcherCore.SearchResult`
  - UI-friendly formatting (relative paths, highlighted matches)
  - Supports interaction tracking (double-click to open file)

### ResultInteractionService.cs (Services)
- **Responsibility**: Handles user interactions with search results
- **Key Features**:
  - Opens result files using system default handlers
  - Provides feedback on interaction outcomes
  - Platform-aware file opening

## Architecture Patterns

### MVVM with ReactiveUI
- **ViewModel**: `MainViewModel` inherits from `ReactiveObject`
- **View**: Avalonia XAML files (`MainWindow.axaml`)
- **Binding**: Data binding through XAML properties and ReactiveUI command binding
- **Reactivity**: Properties raise change notifications automatically via `RaiseAndSetIfChanged`

### Reactive Commands
- Commands defined using `ReactiveCommand.Create` and `ReactiveCommand.CreateFromTask`
- Can observe properties to enable/disable based on state (e.g., stop command only enabled when searching)

### Observable Collections
- Results stored in `ObservableCollection<SearchResultDisplay>`
- Automatically notifies UI of additions/removals during search

### Threading Model
- Long-running search operations execute on background threads via `Task`
- UI updates occur back on the main/UI thread via Avalonia's dispatcher
- `CancellationToken` enables graceful cancellation of ongoing searches

## Dependency Injection & App Initialization

- **Current Model**: Direct instantiation of `MainViewModel` with `Program.Options`
- **Data Context Setup**: Set in `MainWindow.axaml.cs` code-behind
- **Future Enhancement**: Consider Avalonia's service provider for more complex scenarios

## Dependencies

- **Avalonia 11.3.8**: Core UI framework
- **Avalonia.Desktop 11.3.8**: Desktop platform support
- **Avalonia.Controls.DataGrid 11.3.8**: Data grid controls
- **Avalonia.Themes.Fluent 11.3.8**: Fluent design theme
- **Avalonia.ReactiveUI 11.3.8**: ReactiveUI integration
- **CommandLineParser 2.9.1**: Command-line argument parsing
- **SearcherCore**: Internal reference for search functionality

## Code Style & Quality

- **Target Framework**: net9.0 (cross-platform)
- **Nullable Reference Types**: Enabled (`<Nullable>enable</Nullable>`)
- **Code Formatting**: Enforced via `.editorconfig` - always run `dotnet format SearcherGui/SearcherGui.csproj`
- **Analysis**: Comprehensive static analysis enabled (Design, Security, Performance, Reliability, Usage)
- **Threading Analyzers**: Microsoft.VisualStudio.Threading.Analyzers
- **Code Quality**: Roslynator.Analyzers

## Running Tests

Tests for SearcherGui are located in `TestSearcher/` at the repository root. The main Searcher.sln includes SearcherGui, SearcherCore, and TestSearcher.

```bash
# Run all tests from repository root (109 tests total)
dotnet test TestSearcher/

# Run with verbose output
dotnet test TestSearcher/ --verbosity normal

# Filter tests by category
dotnet test TestSearcher/ --filter "FullyQualifiedName~SearcherGui"
```

## Common Tasks

### Adding a New View
1. Create XAML file in `Views/` folder
2. Create code-behind `.cs` file in same location
3. Create corresponding ViewModel in `ViewModels/` if needed
4. Set `DataContext` binding in XAML or code-behind

### Adding a Reactive Property
1. Declare private backing field in ViewModel
2. Use property with `this.RaiseAndSetIfChanged()` method
3. Bind to UI via XAML `{Binding PropertyName}`

### Adding a Command
1. Declare `ReactiveCommand` field in ViewModel
2. Initialize in constructor with `ReactiveCommand.Create()` or `ReactiveCommand.CreateFromTask()`
3. Bind to Button via XAML `Command="{Binding CommandName}"`

### Publishing for Distribution

```bash
# Windows
dotnet publish SearcherGui/SearcherGui.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true

# Linux
dotnet publish SearcherGui/SearcherGui.csproj -c Release -r linux-x64 --self-contained false /p:PublishSingleFile=true

# macOS
dotnet publish SearcherGui/SearcherGui.csproj -c Release -r osx-arm64 --self-contained false /p:PublishSingleFile=true
```

## Debugging Tips

- **Design-Time Data**: `MainViewModel` has a default constructor that initializes `Program.Options = new()` for designer support
- **Reactive Breakpoints**: ReactiveUI's `WhenAnyValue()` allows conditional command enabling based on observable properties
- **Threading Issues**: Always ensure long-running operations are on background threads; UI updates must return to the main thread
