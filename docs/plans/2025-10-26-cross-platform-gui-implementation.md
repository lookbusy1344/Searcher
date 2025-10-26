# Cross-Platform GUI Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a cross-platform Avalonia GUI application that reuses SearcherCore, accepts CLI arguments, auto-starts searches, and displays results with real-time updates in a responsive, non-blocking UI.

**Architecture:** New SearcherGui project using Avalonia MVVM pattern, leveraging existing SearcherCore for all search logic. Command-line arguments are parsed identically to the CLI version and passed to a ViewModel that orchestrates background search threads and UI updates. Results stream in via ObservableCollection binding as the search progresses.

**Tech Stack:** Avalonia (cross-platform UI framework), SearcherCore (search logic), CommandLineParser (CLI argument parsing), MVVM pattern, async/await with Task.Run, ObservableCollection for reactive updates, platform-specific file operations.

---

### Task 1: Create SearcherGui project structure and dependencies

**Files:**
- Create: `SearcherGui/SearcherGui.csproj`
- Create: `SearcherGui/Program.cs`
- Create: `SearcherGui/App.xaml`
- Create: `SearcherGui/App.xaml.cs`
- Modify: `Searcher.sln` (add new project reference)

**Step 1: Create the SearcherGui project file**

Run: `dotnet new avalonia.app -n SearcherGui -o SearcherGui`

This creates a basic Avalonia application structure. Then manually edit the generated `SearcherGui.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <BuiltInComInteropSupport>true</BuiltInComInteropSupport>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.0.0" />
    <PackageReference Include="Avalonia.Desktop" Version="11.0.0" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="11.0.0" />
    <PackageReference Include="Avalonia.ReactiveUI" Version="11.0.0" />
    <PackageReference Include="CommandLineParser" Version="2.9.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../SearcherCore/SearcherCore.csproj" />
  </ItemGroup>
</Project>
```

**Step 2: Verify project structure**

Run: `ls -la SearcherGui/`

Expected: Directory contains Program.cs, App.xaml, App.xaml.cs, SearcherGui.csproj

**Step 3: Commit**

```bash
git add SearcherGui/
git commit -m "chore: create SearcherGui Avalonia project structure"
```

---

### Task 2: Create command-line argument parser for GUI

**Files:**
- Create: `SearcherGui/GuiCliOptions.cs`
- Modify: `SearcherGui/Program.cs`

**Step 1: Create GuiCliOptions class**

```csharp
// SearcherGui/GuiCliOptions.cs
using CommandLine;
using SearcherCore;

namespace SearcherGui;

public class GuiCliOptions : CliOptions
{
    [Option("width", Required = false, Default = 1000, HelpText = "Initial window width")]
    public int WindowWidth { get; set; }

    [Option("height", Required = false, Default = 600, HelpText = "Initial window height")]
    public int WindowHeight { get; set; }

    [Option("auto-close", Required = false, Default = false, HelpText = "Close window after search completes")]
    public bool AutoCloseOnCompletion { get; set; }
}
```

**Step 2: Create Program.cs entry point**

```csharp
// SearcherGui/Program.cs
using Avalonia;
using CommandLine;
using SearcherGui;

public static class Program
{
    public static void Main(string[] args)
    {
        var result = Parser.Default.ParseArguments<GuiCliOptions>(args);

        result
            .WithParsed(opts => BuildAvaloniaApp(opts).StartWithClassicDesktopLifetime(args))
            .WithNotParsed(errs =>
            {
                foreach (var error in errs)
                {
                    Console.Error.WriteLine(error);
                }
                Environment.Exit(1);
            });
    }

    public static AppBuilder BuildAvaloniaApp(GuiCliOptions opts) =>
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
```

**Step 3: Verify compilation**

Run: `dotnet build SearcherGui/SearcherGui.csproj`

Expected: Clean build with no errors

**Step 4: Commit**

```bash
git add SearcherGui/GuiCliOptions.cs SearcherGui/Program.cs
git commit -m "feat: add CLI argument parsing for GUI with custom options"
```

---

### Task 3: Create ViewModel and Models

**Files:**
- Create: `SearcherGui/Models/SearchResultDisplay.cs`
- Create: `SearcherGui/ViewModels/MainViewModel.cs`

**Step 1: Create SearchResultDisplay model**

```csharp
// SearcherGui/Models/SearchResultDisplay.cs
using System.Collections.ObjectModel;
using SearcherCore;

namespace SearcherGui.Models;

public class SearchResultDisplay
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int MatchCount { get; set; }

    public static SearchResultDisplay FromSearchResult(SearchResult result)
    {
        return new SearchResultDisplay
        {
            FileName = Path.GetFileName(result.FilePath),
            FilePath = result.FilePath,
            MatchCount = result.MatchCount
        };
    }
}

public class SearchState
{
    public ObservableCollection<SearchResultDisplay> Results { get; } = new();
    public int FilesScanned { get; set; }
    public int MatchesFound { get; set; }
    public bool IsSearching { get; set; }
    public string StatusMessage { get; set; } = "Ready";
    public DateTime StartTime { get; set; }
    public CancellationTokenSource? CancellationTokenSource { get; set; }
}
```

**Step 2: Create MainViewModel**

```csharp
// SearcherGui/ViewModels/MainViewModel.cs
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using ReactiveUI;
using SearcherCore;
using SearcherGui.Models;

namespace SearcherGui.ViewModels;

public class MainViewModel : ReactiveObject
{
    private readonly GuiCliOptions _options;
    private SearchState _searchState = new();
    private int _filesScanned;
    private int _matchesFound;
    private bool _isSearching;
    private string _statusMessage = "Ready";
    private DateTime _startTime;

    public MainViewModel(GuiCliOptions options)
    {
        _options = options;
        StopCommand = ReactiveCommand.Create(Stop, this.WhenAnyValue(x => x.IsSearching));
    }

    public ObservableCollection<SearchResultDisplay> Results => _searchState.Results;

    public int FilesScanned
    {
        get => _filesScanned;
        private set => this.RaiseAndSetIfChanged(ref _filesScanned, value);
    }

    public int MatchesFound
    {
        get => _matchesFound;
        private set => this.RaiseAndSetIfChanged(ref _matchesFound, value);
    }

    public bool IsSearching
    {
        get => _isSearching;
        private set => this.RaiseAndSetIfChanged(ref _isSearching, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public string SearchPath => _options.Path ?? ".";
    public string SearchPattern => _options.IncludePatterns?.FirstOrDefault() ?? "*";

    public ReactiveCommand<Unit, Unit> StopCommand { get; }

    public async Task OnInitialized()
    {
        // Validate search path
        if (!Directory.Exists(_options.Path))
        {
            StatusMessage = $"Error: Path does not exist: {_options.Path}";
            return;
        }

        _startTime = DateTime.UtcNow;
        IsSearching = true;
        StatusMessage = "Searching...";
        FilesScanned = 0;
        MatchesFound = 0;
        Results.Clear();

        var cts = new CancellationTokenSource();
        _searchState.CancellationTokenSource = cts;

        await Task.Run(() => PerformSearch(cts.Token));

        IsSearching = false;
        var elapsed = DateTime.UtcNow - _startTime;
        StatusMessage = $"Search completed in {elapsed.TotalSeconds:F2}s - Found {MatchesFound} matches in {Results.Count} files";

        if (_options.AutoCloseOnCompletion && _options is GuiCliOptions guiOpts && guiOpts.AutoCloseOnCompletion)
        {
            // Signal window close (will be handled by view)
        }
    }

    private void PerformSearch(CancellationToken ct)
    {
        try
        {
            var glob = new GlobSearch(_options);
            var fileCount = 0;

            foreach (var file in glob.FindFiles(ct))
            {
                if (ct.IsCancellationRequested)
                    break;

                fileCount++;
                Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                {
                    FilesScanned = fileCount;
                });

                var searchFile = new SearchFile(_options);
                var (found, matches) = searchFile.FileContainsStringWrapper(file);

                if (found)
                {
                    var result = new SearchResultDisplay
                    {
                        FileName = Path.GetFileName(file),
                        FilePath = file,
                        MatchCount = matches
                    };

                    Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                    {
                        Results.Add(result);
                        MatchesFound += matches;
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
            {
                StatusMessage = $"Error: {ex.Message}";
            });
        }
    }

    private void Stop()
    {
        _searchState.CancellationTokenSource?.Cancel();
        IsSearching = false;
        StatusMessage = "Search cancelled";
    }
}
```

**Step 3: Verify no compilation errors**

Run: `dotnet build SearcherGui/SearcherGui.csproj`

Expected: Clean build

**Step 4: Commit**

```bash
git add SearcherGui/Models/ SearcherGui/ViewModels/
git commit -m "feat: add ViewModel and Models for search state management"
```

---

### Task 4: Create main UI views

**Files:**
- Modify: `SearcherGui/App.xaml`
- Modify: `SearcherGui/App.xaml.cs`
- Create: `SearcherGui/Views/MainView.xaml`
- Create: `SearcherGui/Views/MainView.xaml.cs`

**Step 1: Update App.xaml**

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="SearcherGui.App">
  <Application.Styles>
    <FluentTheme />
  </Application.Styles>
</Application>
```

**Step 2: Update App.xaml.cs**

```csharp
// SearcherGui/App.xaml.cs
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SearcherGui.Views;
using SearcherGui.ViewModels;

namespace SearcherGui;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopApplicationLifetime desktop)
        {
            var options = (GuiCliOptions)((Avalonia.Application.Current as App)?.Resources["CliOptions"] ?? new GuiCliOptions());
            desktop.MainWindow = new MainView
            {
                DataContext = new MainViewModel(options)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

Actually, we need to pass options differently. Let me revise Program.cs to inject options into App:

Update `Program.cs`:

```csharp
// SearcherGui/Program.cs
using Avalonia;
using CommandLine;
using SearcherGui;

public static class Program
{
    public static GuiCliOptions? Options { get; set; }

    public static void Main(string[] args)
    {
        var result = Parser.Default.ParseArguments<GuiCliOptions>(args);

        result
            .WithParsed(opts =>
            {
                Options = opts;
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            })
            .WithNotParsed(errs =>
            {
                foreach (var error in errs)
                {
                    Console.Error.WriteLine(error);
                }
                Environment.Exit(1);
            });
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
```

Update `App.xaml.cs`:

```csharp
// SearcherGui/App.xaml.cs
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SearcherGui.Views;
using SearcherGui.ViewModels;

namespace SearcherGui;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopApplicationLifetime desktop)
        {
            var options = Program.Options ?? new GuiCliOptions();
            var viewModel = new MainViewModel(options);
            desktop.MainWindow = new MainView
            {
                DataContext = viewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

**Step 3: Create MainView.xaml**

```xml
<!-- SearcherGui/Views/MainView.xaml -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="SearcherGui.Views.MainView"
        Title="Searcher"
        Width="1000"
        Height="600">
  <DockPanel>
    <!-- Top: Search Parameters -->
    <StackPanel DockPanel.Dock="Top" Margin="10" Spacing="5">
      <TextBlock FontWeight="Bold">Search Parameters</TextBlock>
      <Grid ColumnDefinitions="Auto,*" RowDefinitions="Auto,Auto" Margin="0,5">
        <TextBlock Grid.Row="0" Grid.Column="0" Margin="0,0,10,0">Path:</TextBlock>
        <TextBlock Grid.Row="0" Grid.Column="1" Text="{Binding SearchPath}" />

        <TextBlock Grid.Row="1" Grid.Column="0" Margin="0,0,10,0">Pattern:</TextBlock>
        <TextBlock Grid.Row="1" Grid.Column="1" Text="{Binding SearchPattern}" />
      </Grid>
    </StackPanel>

    <!-- Bottom: Status Bar -->
    <StackPanel DockPanel.Dock="Bottom" Margin="10" Spacing="5">
      <StackPanel Orientation="Horizontal" Spacing="10">
        <TextBlock Text="{Binding StatusMessage}" />
        <TextBlock Text="{Binding FilesScanned, StringFormat='Files: {0}'}" />
        <TextBlock Text="{Binding MatchesFound, StringFormat='Matches: {0}'}" />
        <Button Command="{Binding StopCommand}" IsEnabled="{Binding IsSearching}">Stop</Button>
      </StackPanel>
    </StackPanel>

    <!-- Middle: Results Grid -->
    <DataGrid ItemsSource="{Binding Results}"
              CanUserReorderColumns="True"
              CanUserResizeColumns="True"
              CanUserSortColumns="True">
      <DataGrid.Columns>
        <DataGridTextColumn Header="Filename" Binding="{Binding FileName}" Width="200" />
        <DataGridTextColumn Header="Path" Binding="{Binding FilePath}" Width="*" />
        <DataGridTextColumn Header="Matches" Binding="{Binding MatchCount}" Width="80" />
      </DataGrid.Columns>
    </DataGrid>
  </DockPanel>
</Window>
```

**Step 4: Create MainView.xaml.cs**

```csharp
// SearcherGui/Views/MainView.xaml.cs
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SearcherGui.ViewModels;

namespace SearcherGui.Views;

public partial class MainView : Window
{
    public MainView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override async void OnInitialized()
    {
        base.OnInitialized();
        if (DataContext is MainViewModel vm)
        {
            await vm.OnInitialized();
        }
    }
}
```

**Step 5: Verify compilation**

Run: `dotnet build SearcherGui/SearcherGui.csproj`

Expected: Clean build with no errors

**Step 6: Commit**

```bash
git add SearcherGui/App.xaml* SearcherGui/Views/ SearcherGui/Program.cs
git commit -m "feat: create XAML UI and wire up ViewModel initialization"
```

---

### Task 5: Implement file interaction service

**Files:**
- Create: `SearcherGui/Services/ResultInteractionService.cs`

**Step 1: Create ResultInteractionService**

```csharp
// SearcherGui/Services/ResultInteractionService.cs
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SearcherGui.Services;

public class ResultInteractionService
{
    public static bool OpenFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", filePath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", filePath);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool ShowInFolder(string filePath)
    {
        try
        {
            var folder = Path.GetDirectoryName(filePath) ?? filePath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("explorer", $"/select,{filePath}");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", new[] { "-R", filePath });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Try nautilus first, fallback to generic open
                var result = Process.Start("nautilus", folder);
                if (result == null)
                {
                    Process.Start("xdg-open", folder);
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> CopyToClipboard(string text)
    {
        try
        {
            var clipboard = Avalonia.Application.Current?.Clipboard;
            if (clipboard == null)
                return false;

            await clipboard.SetTextAsync(text);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

**Step 2: Update MainView to handle double-click and context menu**

Add to `MainView.xaml`:

```xml
<DataGrid.ContextMenu>
  <ContextMenu>
    <MenuItem Header="Open" Click="OpenFile_Click" />
    <MenuItem Header="Copy Path" Click="CopyPath_Click" />
    <MenuItem Header="Show in Folder" Click="ShowInFolder_Click" />
  </ContextMenu>
</DataGrid.ContextMenu>
```

Update `MainView.xaml.cs` to add event handlers:

```csharp
// Add using statements
using Avalonia.Controls;
using SearcherGui.Services;
using SearcherGui.Models;

// Add to MainView class
private void OpenFile_Click(object? sender, RoutedEventArgs e)
{
    var selected = ResultsGrid.SelectedItem as SearchResultDisplay;
    if (selected != null)
    {
        ResultInteractionService.OpenFile(selected.FilePath);
    }
}

private void CopyPath_Click(object? sender, RoutedEventArgs e)
{
    var selected = ResultsGrid.SelectedItem as SearchResultDisplay;
    if (selected != null)
    {
        _ = ResultInteractionService.CopyToClipboard(selected.FilePath);
    }
}

private void ShowInFolder_Click(object? sender, RoutedEventArgs e)
{
    var selected = ResultsGrid.SelectedItem as SearchResultDisplay;
    if (selected != null)
    {
        ResultInteractionService.ShowInFolder(selected.FilePath);
    }
}
```

Also add name to DataGrid in XAML:

```xml
<DataGrid x:Name="ResultsGrid" ItemsSource="{Binding Results}" ...>
```

**Step 3: Verify compilation**

Run: `dotnet build SearcherGui/SearcherGui.csproj`

Expected: Clean build

**Step 4: Commit**

```bash
git add SearcherGui/Services/ SearcherGui/Views/MainView.xaml*
git commit -m "feat: add file interaction service with open, copy path, and show folder"
```

---

### Task 6: Add project to solution and test build

**Files:**
- Modify: `Searcher.sln`

**Step 1: Add SearcherGui project to solution**

Run: `dotnet sln Searcher.sln add SearcherGui/SearcherGui.csproj`

Expected: Project added successfully

**Step 2: Run full solution build**

Run: `dotnet build Searcher.sln`

Expected: All projects build cleanly, no errors

**Step 3: Test running the GUI with sample arguments**

Run: `dotnet run -p SearcherGui/SearcherGui.csproj -- --path . --include-patterns "*.cs" --search-term "class"`

Expected: GUI window opens, begins searching, displays results as they arrive

**Step 4: Commit**

```bash
git add Searcher.sln
git commit -m "feat: add SearcherGui project to solution"
```

---

### Task 7: Add tests for ViewModel

**Files:**
- Create: `TestSearcher/SearcherGui/MainViewModelTests.cs`

**Step 1: Add test project reference to SearcherGui**

Ensure TestSearcher has reference to SearcherGui. Run:

```bash
dotnet add TestSearcher/TestSearcher.csproj reference SearcherGui/SearcherGui.csproj
```

**Step 2: Create MainViewModel tests**

```csharp
// TestSearcher/SearcherGui/MainViewModelTests.cs
using SearcherGui.ViewModels;
using SearcherGui.Models;
using SearcherCore;
using Xunit;

namespace TestSearcher.SearcherGui;

public class MainViewModelTests
{
    [Fact]
    public void Constructor_WithValidOptions_InitializesState()
    {
        var options = new GuiCliOptions { Path = ".", SearchTerm = "test" };
        var vm = new MainViewModel(options);

        Assert.NotNull(vm);
        Assert.Equal(".", vm.SearchPath);
        Assert.Equal("test", vm.SearchPattern);
        Assert.False(vm.IsSearching);
    }

    [Fact]
    public void Results_StartsEmpty()
    {
        var options = new GuiCliOptions { Path = ".", SearchTerm = "test" };
        var vm = new MainViewModel(options);

        Assert.Empty(vm.Results);
    }

    [Fact]
    public void StopCommand_DisabledWhenNotSearching()
    {
        var options = new GuiCliOptions { Path = ".", SearchTerm = "test" };
        var vm = new MainViewModel(options);

        Assert.False(vm.IsSearching);
        // StopCommand can be evaluated based on IsSearching observable
    }

    [Fact]
    public async Task OnInitialized_WithInvalidPath_SetsErrorMessage()
    {
        var options = new GuiCliOptions { Path = "/nonexistent/path", SearchTerm = "test" };
        var vm = new MainViewModel(options);

        await vm.OnInitialized();

        Assert.Contains("Error", vm.StatusMessage);
    }
}
```

**Step 3: Run tests**

Run: `dotnet test TestSearcher/TestSearcher.csproj`

Expected: All tests pass

**Step 4: Commit**

```bash
git add TestSearcher/SearcherGui/
git commit -m "test: add unit tests for MainViewModel"
```

---

### Task 8: Format code and verify project structure

**Files:**
- All modified files

**Step 1: Run code formatter**

Run: `dotnet format Searcher.sln`

Expected: Code formatted according to .editorconfig

**Step 2: Verify build and tests pass**

Run: `dotnet build Searcher.sln && dotnet test TestSearcher/`

Expected: All projects build, all tests pass

**Step 3: Final commit**

```bash
git add -A
git commit -m "chore: format code and finalize SearcherGui implementation"
```

---

## Implementation Notes

- The ViewModel uses Avalonia's dispatcher to marshal results from background threads to the UI thread
- ObservableCollection automatically triggers UI updates when results are added
- SearcherCore's existing CancellationToken support is leveraged for cancellation
- File interaction uses platform detection to call appropriate native commands
- No complex async patterns needed since SearcherCore's search methods are synchronous; Task.Run handles the backgrounding
- Tests focus on initialization and state management; functional search tests reuse SearcherCore's test suite
