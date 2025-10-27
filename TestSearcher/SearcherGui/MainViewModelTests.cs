using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SearcherGui;
using SearcherGui.Models;
using SearcherGui.ViewModels;

namespace TestSearcher.SearcherGui;

public class MainViewModelTests
{
	[Fact(DisplayName = "GUI: Constructor properly initializes with GuiCliOptions")]
	[Trait("Category", "GUI")]
	public void Constructor_InitializesWithGuiCliOptions()
	{
		var options = new GuiCliOptions {
			Folder = new DirectoryInfo("."),
			Search = "test",
			Pattern = new[] { "*.cs" }
		};

		using var vm = new MainViewModel(options);

		Assert.NotNull(vm);
		Assert.NotNull(vm.StopCommand);
	}

	[Fact(DisplayName = "GUI: Results collection starts empty")]
	[Trait("Category", "GUI")]
	public void Results_StartsEmpty()
	{
		var options = new GuiCliOptions {
			Folder = new DirectoryInfo("."),
			Search = "test"
		};

		using var vm = new MainViewModel(options);

		Assert.Empty(vm.Results);
	}

	[Fact(DisplayName = "GUI: FilesScanned starts at 0")]
	[Trait("Category", "GUI")]
	public void FilesScanned_StartsAtZero()
	{
		var options = new GuiCliOptions {
			Folder = new DirectoryInfo("."),
			Search = "test"
		};

		using var vm = new MainViewModel(options);

		Assert.Equal(0, vm.FilesScanned);
	}

	[Fact(DisplayName = "GUI: MatchesFound starts at 0")]
	[Trait("Category", "GUI")]
	public void MatchesFound_StartsAtZero()
	{
		var options = new GuiCliOptions {
			Folder = new DirectoryInfo("."),
			Search = "test"
		};

		using var vm = new MainViewModel(options);

		Assert.Equal(0, vm.MatchesFound);
	}

	[Fact(DisplayName = "GUI: IsSearching starts as false")]
	[Trait("Category", "GUI")]
	public void IsSearching_StartsAsFalse()
	{
		var options = new GuiCliOptions {
			Folder = new DirectoryInfo("."),
			Search = "test"
		};

		using var vm = new MainViewModel(options);

		Assert.False(vm.IsSearching);
	}

	[Fact(DisplayName = "GUI: StatusMessage starts as 'Ready'")]
	[Trait("Category", "GUI")]
	public void StatusMessage_StartsAsReady()
	{
		var options = new GuiCliOptions {
			Folder = new DirectoryInfo("."),
			Search = "test"
		};

		using var vm = new MainViewModel(options);

		Assert.Equal("Ready", vm.StatusMessage);
	}

	[Fact(DisplayName = "GUI: OnInitializedAsync with invalid path sets error message")]
	[Trait("Category", "GUI")]
	public async Task OnInitializedAsync_InvalidPath_SetsErrorMessage()
	{
		var options = new GuiCliOptions {
			Folder = new DirectoryInfo("/this/path/definitely/does/not/exist/12345"),
			Search = "test"
		};

		using var vm = new MainViewModel(options);
		await vm.OnInitializedAsync();

		Assert.Contains("Error", vm.StatusMessage);
		Assert.Contains("does not exist", vm.StatusMessage);
		Assert.False(vm.IsSearching);
	}

	[Fact(DisplayName = "GUI: OnInitializedAsync with valid path starts search")]
	[Trait("Category", "GUI")]
	public async Task OnInitializedAsync_ValidPath_StartsSearch()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
		Directory.CreateDirectory(tempDir);

		try {
			// Create a test file
			var testFile = Path.Combine(tempDir, "test.txt");
			await File.WriteAllTextAsync(testFile, "test content");

			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "content",
				Pattern = new[] { "*.txt" }
			};

			using var vm = new MainViewModel(options);

			// Track state changes
			var searchingStates = new List<bool>();
			vm.PropertyChanged += (sender, e) => {
				if (e.PropertyName == nameof(vm.IsSearching)) {
					searchingStates.Add(vm.IsSearching);
				}
			};

			await vm.OnInitializedAsync();

			// Search should complete
			Assert.False(vm.IsSearching);
			Assert.Contains("completed", vm.StatusMessage);

			// Should have transitioned through searching state
			Assert.Contains(true, searchingStates);
		}
		finally {
			// Cleanup
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, true);
			}
		}
	}

	[Fact(DisplayName = "GUI: Stop() method cancels search and sets IsSearching to false")]
	[Trait("Category", "GUI")]
	public void Stop_CancelsSearchAndSetsIsSearchingFalse()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
		Directory.CreateDirectory(tempDir);

		try {
			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "test",
				Pattern = new[] { "*.txt" }
			};

			using var vm = new MainViewModel(options);

			// Start the search in background
			var searchTask = vm.OnInitializedAsync();

			// Give it a moment to start
			Thread.Sleep(100);

			// Stop should work even if search hasn't really started yet
			vm.StopCommand.Execute().Subscribe();

			// StatusMessage should indicate cancellation or be completed
			Assert.False(vm.IsSearching);
		}
		finally {
			// Cleanup
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, true);
			}
		}
	}

	[Fact(DisplayName = "GUI: SearchPath property returns correct folder from options")]
	[Trait("Category", "GUI")]
	public void SearchPath_ReturnsCorrectFolderFromOptions()
	{
		var testPath = Path.GetTempPath();
		var options = new GuiCliOptions {
			Folder = new DirectoryInfo(testPath),
			Search = "test"
		};

		using var vm = new MainViewModel(options);

		Assert.Equal(testPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
					 vm.SearchPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
	}

	[Fact(DisplayName = "GUI: SearchPattern property returns correct pattern from options")]
	[Trait("Category", "GUI")]
	public void SearchPattern_ReturnsCorrectPatternFromOptions()
	{
		var options = new GuiCliOptions {
			Folder = new DirectoryInfo("."),
			Search = "test"
		};

		using var vm = new MainViewModel(options);

		// SearchPattern property should return the patterns from options
		var pattern = vm.SearchPattern;
		Assert.NotNull(pattern);
		Assert.NotEmpty(pattern);
		// Default pattern should be "*"
		Assert.Equal("*", pattern);
	}

	[Fact(DisplayName = "GUI: Multiple initializations are prevented by guard")]
	[Trait("Category", "GUI")]
	public async Task OnInitializedAsync_MultipleCallsPrevented()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
		Directory.CreateDirectory(tempDir);

		try {
			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "test",
				Pattern = new[] { "*.txt" }
			};

			using var vm = new MainViewModel(options);

			// Start first search
			var task1 = vm.OnInitializedAsync();

			// Try to start second search immediately
			var task2 = vm.OnInitializedAsync();

			await task1;
			await task2;

			// Both should complete without errors
			Assert.False(vm.IsSearching);
		}
		finally {
			// Cleanup
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, true);
			}
		}
	}

	[Fact(DisplayName = "GUI: Results are cleared on new search")]
	[Trait("Category", "GUI")]
	public async Task OnInitializedAsync_ClearsExistingResults()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}");
		Directory.CreateDirectory(tempDir);

		try {
			// Create a test file
			var testFile = Path.Combine(tempDir, "test.txt");
			await File.WriteAllTextAsync(testFile, "searchme");

			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "searchme",
				Pattern = new[] { "*.txt" }
			};

			using var vm = new MainViewModel(options);

			// First search
			await vm.OnInitializedAsync();
			var firstResultCount = vm.Results.Count;

			// Add some dummy data to simulate existing results
			vm.Results.Add(new SearchResultDisplay {
				FileName = "dummy.txt",
				FilePath = "/dummy/path",
				MatchCount = 1
			});

			// Second search should clear results
			await vm.OnInitializedAsync();

			// Results should not contain the dummy entry
			Assert.DoesNotContain(vm.Results, r => r.FileName == "dummy.txt");
		}
		finally {
			// Cleanup
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, true);
			}
		}
	}

	[Fact(DisplayName = "GUI: Dispose can be called without errors")]
	[Trait("Category", "GUI")]
	public void Dispose_CanBeCalledWithoutErrors()
	{
		var options = new GuiCliOptions {
			Folder = new DirectoryInfo("."),
			Search = "test"
		};

		var vm = new MainViewModel(options);

		// Should not throw
		vm.Dispose();
	}

	[Fact(DisplayName = "GUI: Dispose can be called multiple times (idempotent)")]
	[Trait("Category", "GUI")]
	public void Dispose_IsIdempotent()
	{
		var options = new GuiCliOptions {
			Folder = new DirectoryInfo("."),
			Search = "test"
		};

		var vm = new MainViewModel(options);

		// Should not throw on multiple calls
		vm.Dispose();
		vm.Dispose();
		vm.Dispose();
	}

	[Fact(DisplayName = "GUI: Dispose disposes log writer when present")]
	[Trait("Category", "GUI")]
	public void Dispose_DisposesLogWriter()
	{
		var logPath = Path.Combine(Path.GetTempPath(), $"test_dispose_log_{Guid.NewGuid()}.txt");

		try {
			var options = new GuiCliOptions {
				Folder = new DirectoryInfo("."),
				Search = "test",
				LogResultsFile = logPath
			};

			var vm = new MainViewModel(options);

			// Log file should be created
			Assert.True(File.Exists(logPath));

			// Dispose should close the log writer
			vm.Dispose();

			// After dispose, we should be able to delete the file
			// (which wouldn't be possible if the writer was still open)
			File.Delete(logPath);
			Assert.False(File.Exists(logPath));
		}
		finally {
			if (File.Exists(logPath)) {
				File.Delete(logPath);
			}
		}
	}

	[Fact(DisplayName = "GUI: Dispose during search cancels operation")]
	[Trait("Category", "GUI-Integration")]
	public async Task Dispose_DuringSearch_CancelsOperation()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"searcher_test_{Guid.NewGuid()}");

		try {
			Directory.CreateDirectory(tempDir);

			// Create many files to ensure search takes some time
			for (int i = 0; i < 100; i++) {
				File.WriteAllText(Path.Combine(tempDir, $"file{i}.txt"), "test content here");
			}

			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "content",
				Pattern = new[] { "*.txt" }
			};

			using var vm = new MainViewModel(options);

			// Start search in background
			var searchTask = Task.Run(async () => await vm.OnInitializedAsync());

			// Give it a moment to start
			await Task.Delay(50);

			// Dispose should cancel the search
			vm.Dispose();

			// Wait for search to complete (should be cancelled)
			await searchTask;

			// Verify search was stopped (status should indicate completion/cancellation)
			Assert.False(vm.IsSearching);
		}
		finally {
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, true);
			}
		}
	}

	[Fact(DisplayName = "GUI: Dispose after completed search works correctly")]
	[Trait("Category", "GUI-Integration")]
	public async Task Dispose_AfterCompletedSearch_WorksCorrectly()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"searcher_test_{Guid.NewGuid()}");

		try {
			Directory.CreateDirectory(tempDir);
			File.WriteAllText(Path.Combine(tempDir, "test.txt"), "search term");

			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "search",
				Pattern = new[] { "*.txt" }
			};

			using var vm = new MainViewModel(options);

			// Complete a search
			await vm.OnInitializedAsync();

			// Wait for completion
			while (vm.IsSearching) {
				await Task.Delay(10);
			}

			// Dispose after search completes
			vm.Dispose();

			// Should complete without errors
			Assert.False(vm.IsSearching);
		}
		finally {
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, true);
			}
		}
	}
}
