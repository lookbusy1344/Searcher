using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using SearcherGui;
using SearcherGui.Models;
using SearcherGui.ViewModels;

namespace TestSearcher.SearcherGui;

public class MainViewModelIntegrationTests
{
	[Fact(DisplayName = "GUI Integration: Search completes with matching files")]
	[Trait("Category", "GUI-Integration")]
	public async Task OnInitializedAsync_WithMatchingFiles_ReturnsResults()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"searcher_test_{Guid.NewGuid()}");
		Directory.CreateDirectory(tempDir);

		try {
			// Create test files with searchable content
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file1.txt"), "needle in haystack");
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file2.txt"), "another needle here");
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file3.txt"), "no match here");

			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "needle",
				Pattern = new[] { "*.txt" }
			};

			var vm = new MainViewModel(options);
			await vm.OnInitializedAsync();

			Assert.False(vm.IsSearching);
			Assert.Equal(2, vm.Results.Count);
			Assert.Equal(2, vm.MatchesFound);
			Assert.Equal(3, vm.FilesScanned);
			Assert.Contains("completed", vm.StatusMessage);
		}
		finally {
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, true);
			}
		}
	}

	[Fact(DisplayName = "GUI Integration: Search handles no matches gracefully")]
	[Trait("Category", "GUI-Integration")]
	public async Task OnInitializedAsync_WithNoMatches_ReturnsEmptyResults()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"searcher_test_{Guid.NewGuid()}");
		Directory.CreateDirectory(tempDir);

		try {
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file.txt"), "content here");

			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "xyz123notfound",
				Pattern = new[] { "*.txt" }
			};

			var vm = new MainViewModel(options);
			await vm.OnInitializedAsync();

			Assert.False(vm.IsSearching);
			Assert.Empty(vm.Results);
			Assert.Equal(0, vm.MatchesFound);
			Assert.Equal(1, vm.FilesScanned);
		}
		finally {
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, true);
			}
		}
	}

	[Fact(DisplayName = "GUI Integration: Search honors case sensitivity option")]
	[Trait("Category", "GUI-Integration")]
	public async Task OnInitializedAsync_CaseSensitive_FindsOnlyExactMatches()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"searcher_test_{Guid.NewGuid()}");
		Directory.CreateDirectory(tempDir);

		try {
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file.txt"), "Needle\nneedle\nNEEDLE");

			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "needle",
				Pattern = new[] { "*.txt" },
				CaseSensitive = true
			};

			var vm = new MainViewModel(options);
			await vm.OnInitializedAsync();

			Assert.False(vm.IsSearching);
			Assert.Single(vm.Results); // Only lowercase "needle" matches
			Assert.Equal(1, vm.MatchesFound);
		}
		finally {
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, true);
			}
		}
	}

	[Fact(DisplayName = "GUI Integration: Search handles multiple file patterns")]
	[Trait("Category", "GUI-Integration")]
	public async Task OnInitializedAsync_MultiplePatterns_SearchesAll()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"searcher_test_{Guid.NewGuid()}");
		Directory.CreateDirectory(tempDir);

		try {
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file1.txt"), "search");
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file2.md"), "search");
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file3.log"), "search");
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file4.doc"), "content");

			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "search",
				Pattern = new[] { "*.txt", "*.md", "*.log" }
			};

			var vm = new MainViewModel(options);
			await vm.OnInitializedAsync();

			Assert.Equal(3, vm.Results.Count); // Only txt, md, log files match
			Assert.Equal(4, vm.FilesScanned);
		}
		finally {
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, true);
			}
		}
	}

	[Fact(DisplayName = "GUI Integration: Search handles nested directories")]
	[Trait("Category", "GUI-Integration")]
	public async Task OnInitializedAsync_WithNestedDirs_SearchesRecursively()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"searcher_test_{Guid.NewGuid()}");
		var subDir = Path.Combine(tempDir, "subdir");
		Directory.CreateDirectory(subDir);

		try {
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file1.txt"), "found");
			await File.WriteAllTextAsync(Path.Combine(subDir, "file2.txt"), "found");
			await File.WriteAllTextAsync(Path.Combine(subDir, "file3.txt"), "not here");

			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "found",
				Pattern = new[] { "*.txt" }
			};

			var vm = new MainViewModel(options);
			await vm.OnInitializedAsync();

			Assert.Equal(2, vm.Results.Count);
			Assert.Equal(3, vm.FilesScanned); // All 3 files scanned
		}
		finally {
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, true);
			}
		}
	}

	[Fact(DisplayName = "GUI Integration: Cancel search stops processing")]
	[Trait("Category", "GUI-Integration")]
	public async Task OnInitializedAsync_CancelDuringSearch_StopsProcessing()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"searcher_test_{Guid.NewGuid()}");
		Directory.CreateDirectory(tempDir);

		try {
			// Create many files to ensure search takes time
			for (int i = 0; i < 100; i++) {
				await File.WriteAllTextAsync(
					Path.Combine(tempDir, $"file{i}.txt"),
					$"content {i} with search term"
				);
			}

			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "search",
				Pattern = new[] { "*.txt" }
			};

			var vm = new MainViewModel(options);

			// Start search and capture state changes
			var searchTask = vm.OnInitializedAsync();

			// Give search a moment to start
			await Task.Delay(50);

			// Cancel the search
			vm.StopCommand.Execute().Subscribe();

			// Wait for cancellation to complete
			await searchTask;

			Assert.False(vm.IsSearching);
			// Results should be partial (less than 100)
			Assert.True(vm.FilesScanned < 100);
		}
		finally {
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, true);
			}
		}
	}

	[Fact(DisplayName = "GUI Integration: Search updates UI thread safely")]
	[Trait("Category", "GUI-Integration")]
	public async Task OnInitializedAsync_UpdatesUIThread_WithoutCrashing()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"searcher_test_{Guid.NewGuid()}");
		Directory.CreateDirectory(tempDir);

		try {
			for (int i = 0; i < 50; i++) {
				await File.WriteAllTextAsync(
					Path.Combine(tempDir, $"file{i}.txt"),
					"test"
				);
			}

			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "test",
				Pattern = new[] { "*.txt" }
			};

			var vm = new MainViewModel(options);

			// Track if any exceptions occur during property updates
			int propertyChangedCount = 0;
			vm.PropertyChanged += (s, e) => propertyChangedCount++;

			await vm.OnInitializedAsync();

			Assert.False(vm.IsSearching);
			Assert.Equal(50, vm.FilesScanned);
			Assert.Equal(50, vm.Results.Count);
			Assert.True(propertyChangedCount > 0); // Properties updated
		}
		finally {
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, true);
			}
		}
	}

	[Fact(DisplayName = "GUI Integration: Large result set handled efficiently")]
	[Trait("Category", "GUI-Integration")]
	public async Task OnInitializedAsync_LargeResultSet_HandledEfficiently()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"searcher_test_{Guid.NewGuid()}");
		Directory.CreateDirectory(tempDir);

		try {
			// Create files with duplicated search term
			for (int i = 0; i < 10; i++) {
				await File.WriteAllTextAsync(
					Path.Combine(tempDir, $"file{i}.txt"),
					"searchterm searchterm searchterm"
				);
			}

			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "searchterm",
				Pattern = new[] { "*.txt" }
			};

			var vm = new MainViewModel(options);
			await vm.OnInitializedAsync();

			Assert.Equal(10, vm.Results.Count);
			Assert.Equal(10, vm.FilesScanned);
			Assert.True(vm.MatchesFound > 10); // Multiple matches per file
		}
		finally {
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, true);
			}
		}
	}
}
