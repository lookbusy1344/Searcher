# Avalonia GUI Display Issue

## Date
2025-10-26

## Problem Summary
The SearcherGui Avalonia application successfully performs searches and finds results, but **does not display them in the UI grid**. Results are correctly:
- Found by the search logic (verified in logs)
- Added to the `ObservableCollection<SearchResultDisplay>` 
- Logged to file when `--log-results` is specified

However, the DataGrid in the Avalonia window remains empty.

## Evidence

### Successful Backend Search
Log file shows 8 results found:
```
=== GUI Search Results Log ===
Started: 2025-10-26 15:48:32
Folder: /Users/johnsparrow/Documents/dev/Searcher/SearcherCore
Pattern count: 1
  Pattern[0]: '*.cs'
Pattern (formatted): *.cs
Search: class
Case Sensitive: False

[15:48:32.624] FOUND: /Users/johnsparrow/Documents/dev/Searcher/SearcherCore/CliOptions.cs
[15:48:32.696] FOUND: /Users/johnsparrow/Documents/dev/Searcher/SearcherCore/obj/Release/net9.0/osx-arm64/SearcherCore.AssemblyInfo.cs
[15:48:32.697] FOUND: /Users/johnsparrow/Documents/dev/Searcher/SearcherCore/GlobSearch.cs
[15:48:32.697] FOUND: /Users/johnsparrow/Documents/dev/Searcher/SearcherCore/PdfCheck.cs
[15:48:32.697] FOUND: /Users/johnsparrow/Documents/dev/Searcher/SearcherCore/obj/Debug/net9.0/SearcherCore.AssemblyInfo.cs
[15:48:32.697] FOUND: /Users/johnsparrow/Documents/dev/Searcher/SearcherCore/Utils.cs
[15:48:32.697] FOUND: /Users/johnsparrow/Documents/dev/Searcher/SearcherCore/SearchFile.cs
[15:48:32.697] FOUND: /Users/johnsparrow/Documents/dev/Searcher/SearcherCore/obj/Release/net9.0/SearcherCore.AssemblyInfo.cs

=== Search Completed ===
Total Results: 8
Files Scanned: 13
Matches Found: 8
```

### Unit Tests Pass
All systematic tests pass, confirming the search logic and view model work correctly:
```
Passed!  - Failed:     0, Passed:     7, Skipped:     0, Total:     7
```

Tests verify:
- Results are added to `vm.Results` collection
- File scanning works (13 files found)
- Pattern filtering works (`*.cs`)
- Search matching works (8 files contain "class")

### UI Shows Empty Grid
When running the actual Avalonia application, the window displays:
- Search parameters correctly shown
- Status message shows "Search completed in X.XXs - Found 8 matches in 8 files"
- Files Scanned: 13
- Matches Found: 8
- **But the DataGrid is completely empty**

## Root Cause Analysis

### What's Working
1. ✅ Command-line parsing (pattern correctly set to `*.cs`)
2. ✅ File scanning (13 .cs files found)
3. ✅ Search logic (8 files contain "class")
4. ✅ Results added to collection (verified in logs)
5. ✅ UI thread marshalling (using `Dispatcher.UIThread.InvokeAsync()`)
6. ✅ Waiting for async UI updates (using `Task.WhenAll()`)

### What's Failing
The DataGrid binding is not reflecting changes to the `ObservableCollection<SearchResultDisplay>`.

### Current Implementation

**MainViewModel.cs** - Search logic:
```csharp
// Search each file in parallel and collect UI update tasks
var uiUpdateTasks = new System.Collections.Concurrent.ConcurrentBag<Task>();

_ = Parallel.ForEach(files, new() { MaxDegreeOfParallelism = _options.DegreeOfParallelism, CancellationToken = ct },
    file => {
        var result = SearchFile.FileContainsStringWrapper(file, _options.Search, innerPatterns, _options.StringComparison, ct);

        if (result is SearchResult.Found or SearchResult.Error) {
            var singleResult = new SingleResult(file, result);
            var display = SearchResultDisplay.FromSingleResult(singleResult);

            var dispatcher = Avalonia.Threading.Dispatcher.UIThread;
            if (dispatcher != null && !dispatcher.CheckAccess()) {
                var task = dispatcher.InvokeAsync(() => {
                    Results.Add(display);
                    if (result == SearchResult.Found) {
                        MatchesFound++;
                    }
                    LogResult(display, result);
                }).GetTask();
                uiUpdateTasks.Add(task);
            } else {
                // Unit tests path
                Results.Add(display);
                if (result == SearchResult.Found) {
                    MatchesFound++;
                }
                LogResult(display, result);
            }
        }
    });

// Wait for all UI updates to complete
if (uiUpdateTasks.Count > 0) {
    await Task.WhenAll(uiUpdateTasks);
}
```

**MainView.axaml** - DataGrid binding:
```xml
<DataGrid x:Name="ResultsGrid"
          ItemsSource="{Binding Results}"
          CanUserReorderColumns="True"
          CanUserResizeColumns="True"
          CanUserSortColumns="True"
          IsReadOnly="True"
          AutoGenerateColumns="False">
  <DataGrid.Columns>
    <DataGridTextColumn Header="Filename" Binding="{Binding FileName}" Width="200" />
    <DataGridTextColumn Header="Path" Binding="{Binding FilePath}" Width="*" />
    <DataGridTextColumn Header="Matches" Binding="{Binding MatchCount}" Width="80" />
  </DataGrid.Columns>
</DataGrid>
```

**MainView.axaml.cs** - Initialization:
```csharp
protected override void OnOpened(EventArgs e)
{
    base.OnOpened(e);
    if (DataContext is MainViewModel vm) {
        _ = vm.OnInitializedAsync();
    }
}
```

## Theories

### Theory 1: DataContext Timing Issue
The DataGrid binding might be established before the DataContext is set, and not updating when DataContext changes.

### Theory 2: ObservableCollection Not Notifying
The `ObservableCollection` might not be raising `CollectionChanged` events properly when items are added via `Dispatcher.InvokeAsync()`.

### Theory 3: Binding Context Issue
The DataGrid's `DataContext` might not be the `MainViewModel`, or the binding path might be incorrect.

### Theory 4: Async/Dispatcher Race Condition
Despite waiting for tasks, there might be a race condition where the UI hasn't processed the collection change notifications before the window is shown.

### Theory 5: ObservableCollection Thread Affinity
Avalonia's ObservableCollection might require all modifications to happen on a specific thread, and `InvokeAsync()` might be queuing to the wrong thread.

## Attempted Solutions

1. ✅ Fixed `GuiCliOptions` property shadowing - Pattern now correctly parsed
2. ✅ Changed from `OnInitialized()` to `OnOpened()` - Search now starts
3. ✅ Made `PerformSearch` async and await UI tasks - Tasks complete
4. ✅ Used `Dispatcher.UIThread.InvokeAsync().GetTask()` - Returns proper Task
5. ❌ Results still don't display in UI

## Next Steps to Investigate

1. **Add explicit DataContext verification** - Log the DataGrid's DataContext in OnOpened
2. **Verify binding is working** - Check if other bound properties display (SearchPath, StatusMessage)
3. **Test simple synchronous add** - Try adding a dummy result directly on UI thread in OnOpened
4. **Check collection change notifications** - Subscribe to Results.CollectionChanged and log events
5. **Verify ItemsSource is set** - Check DataGrid.ItemsSource in code-behind after binding
6. **Try explicit PropertyChanged** - Manually raise PropertyChanged for Results property
7. **Test with simple list** - Replace ObservableCollection with List and manual notification
8. **Check Avalonia version compatibility** - Verify DataGrid binding works with current Avalonia version

## Comparison: CLI vs GUI

### CLI Results (Working)
```bash
cd SearcherCli
dotnet run -- --folder ../SearcherCore --pattern "*.cs" --search "class"
```
Output: 8 files found and displayed immediately

### GUI Results (Not Working)
```bash
cd SearcherGui
dotnet run -- --folder ../SearcherCore --pattern "*.cs" --search "class"
```
Output: Empty DataGrid, but status shows "Found 8 matches in 8 files"

## Files Modified

1. `SearcherGui/GuiCliOptions.cs` - Fixed property shadowing (delegates to base class)
2. `SearcherGui/ViewModels/MainViewModel.cs` - Added logging, made PerformSearch async, await UI tasks
3. `SearcherGui/Views/MainView.axaml.cs` - Changed to OnOpened event
4. `TestSearcher/SearcherGui/GuiSystematicTests.cs` - Added 7 comprehensive tests (all pass)

## Test Command

To reproduce:
```bash
cd SearcherGui
dotnet run -- --folder ../SearcherCore --pattern "*.cs" --search "class" --log-results /tmp/gui.log
# Wait 10 seconds for auto-close
cat /tmp/gui.log  # Shows 8 results found
```

To test manually (window stays open):
```bash
cd SearcherGui
dotnet run -- --folder ../SearcherCore --pattern "*.cs" --search "class"
# Window opens but DataGrid is empty despite status showing 8 matches
```
