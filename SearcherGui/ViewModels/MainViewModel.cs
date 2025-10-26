using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using SearcherCore;
using SearcherGui.Models;

namespace SearcherGui.ViewModels;

public class MainViewModel : ReactiveObject
{
	private readonly GuiCliOptions _options;
	private readonly ObservableCollection<SearchResultDisplay> _results = new();
	private CancellationTokenSource? _cancellationTokenSource;
	private StreamWriter? _logWriter;
	private int _filesScanned;
	private int _matchesFound;
	private bool _isSearching;
	private string _statusMessage = "Ready";
	private DateTime _startTime;

	public MainViewModel(GuiCliOptions options)
	{
		_options = options;
		StopCommand = ReactiveCommand.Create(Stop, this.WhenAnyValue(x => x.IsSearching));
		
		if (!string.IsNullOrEmpty(options.LogResultsFile)) {
			try {
				_logWriter = new StreamWriter(options.LogResultsFile, false);
				_logWriter.WriteLine($"=== GUI Search Results Log ===");
				_logWriter.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
				_logWriter.WriteLine($"Folder: {options.Folder.FullName}");
				_logWriter.WriteLine($"Pattern count: {options.Pattern.Count}");
				for (int i = 0; i < options.Pattern.Count; i++) {
					_logWriter.WriteLine($"  Pattern[{i}]: '{options.Pattern[i]}'");
				}
				_logWriter.WriteLine($"Pattern (formatted): {options.GetPatterns()}");
				_logWriter.WriteLine($"Search: {options.Search}");
				_logWriter.WriteLine($"Case Sensitive: {options.CaseSensitive}");
				_logWriter.WriteLine();
				_logWriter.Flush();
			}
			catch (Exception ex) {
				Console.Error.WriteLine($"Failed to create log file: {ex.Message}");
			}
		}
	}

	public ObservableCollection<SearchResultDisplay> Results => _results;

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

	public string SearchPath => _options.Folder.FullName;
	public string SearchPattern => _options.GetPatterns();

	public ReactiveCommand<Unit, Unit> StopCommand { get; }

	public async Task OnInitializedAsync()
	{
		// Guard against race condition
		if (IsSearching) {
			return;
		}

		// Validate search path
		if (!Directory.Exists(_options.Folder.FullName)) {
			StatusMessage = $"Error: Path does not exist: {_options.Folder.FullName}";
			return;
		}

		_startTime = DateTime.UtcNow;
		IsSearching = true;
		StatusMessage = "Searching...";
		FilesScanned = 0;
		MatchesFound = 0;
		Results.Clear();

		var cts = new CancellationTokenSource();
		_cancellationTokenSource = cts;

		try {
			await Task.Run(() => PerformSearch(cts.Token));
		}
		finally {
			cts?.Dispose();
			_cancellationTokenSource = null;
		}

		IsSearching = false;
		var elapsed = DateTime.UtcNow - _startTime;
		StatusMessage = $"Search completed in {elapsed.TotalSeconds:F2}s - Found {MatchesFound} matches in {Results.Count} files";
		CloseLog();

		if (_options.AutoCloseOnCompletion || !string.IsNullOrEmpty(_options.LogResultsFile)) {
			// Close after delay when logging or auto-close enabled
			var delayMs = !string.IsNullOrEmpty(_options.LogResultsFile) ? 10000 : 0;
			await Task.Delay(delayMs);
			await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
				if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
					desktop.MainWindow?.Close();
				}
			});
		}
	}

	private void PerformSearch(CancellationToken ct)
	{
		try {
			// Process patterns for outer and inner searches
			var innerPatterns = Utils.ProcessInnerPatterns(_options.Pattern);
			var outerPatterns = Utils.ProcessOuterPatterns(_options.Pattern, _options.InsideZips);

			// Find all files matching the patterns
			var files = GlobSearch.ParallelFindFiles(_options.Folder.FullName, outerPatterns, _options.DegreeOfParallelism, null, ct);

			// Update file count
			if (Avalonia.Threading.Dispatcher.UIThread != null) {
				Avalonia.Threading.Dispatcher.UIThread.Invoke(() => {
					FilesScanned = files.Length;
				});
			} else {
				FilesScanned = files.Length;
			}

			// Search each file in parallel
			_ = Parallel.ForEach(files, new() { MaxDegreeOfParallelism = _options.DegreeOfParallelism, CancellationToken = ct },
				file => {
					if (ct.IsCancellationRequested) {
						return;
					}

					// Search the file for the search string
					var result = SearchFile.FileContainsStringWrapper(file, _options.Search, innerPatterns, _options.StringComparison, ct);

					if (result is SearchResult.Found or SearchResult.Error) {
						var singleResult = new SingleResult(file, result);
						var display = SearchResultDisplay.FromSingleResult(singleResult);

						if (Avalonia.Threading.Dispatcher.UIThread != null) {
							Avalonia.Threading.Dispatcher.UIThread.Invoke(() => {
								Results.Add(display);
								if (result == SearchResult.Found) {
									MatchesFound++;
								}
								LogResult(display, result);
							});
						} else {
							// No dispatcher available (e.g., during unit tests)
							Results.Add(display);
							if (result == SearchResult.Found) {
								MatchesFound++;
							}
							LogResult(display, result);
						}
					}
				});
		}
		catch (Exception ex) {
			var message = $"Error: {ex.Message}";
			if (Avalonia.Threading.Dispatcher.UIThread != null) {
				Avalonia.Threading.Dispatcher.UIThread.Invoke(() => {
					StatusMessage = message;
				});
			} else {
				StatusMessage = message;
			}
		}
	}

	private void LogResult(SearchResultDisplay display, SearchResult result)
	{
		if (_logWriter == null) return;
		
		try {
			_logWriter.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {(result == SearchResult.Found ? "FOUND" : "ERROR")}: {display.FilePath}");
			_logWriter.Flush();
		}
		catch {
			// Ignore logging errors
		}
	}

	private void Stop()
	{
		_cancellationTokenSource?.Cancel();
		IsSearching = false;
		StatusMessage = "Search cancelled";
		CloseLog();
	}

	private void CloseLog()
	{
		if (_logWriter == null) return;

		try {
			_logWriter.WriteLine();
			_logWriter.WriteLine($"=== Search Completed ===");
			_logWriter.WriteLine($"Total Results: {Results.Count}");
			_logWriter.WriteLine($"Files Scanned: {FilesScanned}");
			_logWriter.WriteLine($"Matches Found: {MatchesFound}");
			_logWriter.WriteLine($"Finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
			_logWriter.Close();
			_logWriter = null;
		}
		catch {
			// Ignore logging errors
		}
	}
}
