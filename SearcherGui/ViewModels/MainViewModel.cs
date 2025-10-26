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

		if (_options.AutoCloseOnCompletion) {
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
							});
						} else {
							// No dispatcher available (e.g., during unit tests)
							Results.Add(display);
							if (result == SearchResult.Found) {
								MatchesFound++;
							}
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

	private void Stop()
	{
		_cancellationTokenSource?.Cancel();
		IsSearching = false;
		StatusMessage = "Search cancelled";
	}
}
