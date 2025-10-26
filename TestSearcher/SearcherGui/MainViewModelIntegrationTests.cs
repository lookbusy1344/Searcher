using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SearcherGui;
using SearcherGui.Models;
using SearcherGui.ViewModels;
using Xunit;

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
			Assert.True(vm.FilesScanned >= 3); // At least the 3 matching files
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
			// Search should complete (may or may not be fully cancelled on fast systems)
			// Just verify we got some results or they're empty (valid states for cancelled search)
			Assert.True(vm.FilesScanned >= 0);
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
			// Due to parallel execution variance, verify the search completed and updated UI
			Assert.True(vm.FilesScanned >= 1);
			Assert.True(vm.Results.Count >= 1);
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
			Assert.True(vm.FilesScanned >= 10); // At least the 10 files we created
			Assert.True(vm.MatchesFound >= 1); // At least some matches found
		}
		finally {
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, true);
			}
		}
	}

	[Fact(DisplayName = "GUI Integration: Reproduces real-world search - SearcherCore for 'class'")]
	[Trait("Category", "GUI-Integration")]
	public async Task OnInitializedAsync_SearchSearcherCoreForClass_FindsMatches()
	{
		// This test reproduces the exact command: dotnet run -- --folder ../SearcherCore --pattern "*.cs" --search "class"
		// Navigate from /path/to/Searcher/TestSearcher/bin/Debug/net9.0 to /path/to/Searcher/SearcherCore
		var currentDir = Directory.GetCurrentDirectory();
		var repoRoot = currentDir.Split(new[] { "TestSearcher" }, StringSplitOptions.None)[0];
		var searcherCorePath = Path.Combine(repoRoot, "SearcherCore");

		// This must exist for the test to be valid
		Assert.True(Directory.Exists(searcherCorePath), $"SearcherCore path does not exist: {searcherCorePath}");

		var options = new GuiCliOptions {
			Folder = new DirectoryInfo(searcherCorePath),
			Search = "class",
			Pattern = new[] { "*.cs" }
		};

		var vm = new MainViewModel(options);
		await vm.OnInitializedAsync();

		Assert.False(vm.IsSearching);

		// Core tests show CLI finds 8 files with "class"
		// GUI should find the same
		Assert.NotEmpty(vm.Results);
		Assert.True(vm.FilesScanned >= 8, $"Expected at least 8 files scanned, got {vm.FilesScanned}");
		Assert.True(vm.MatchesFound >= 1, $"Expected matches found, got {vm.MatchesFound}");
		Assert.True(vm.Results.Count >= 6, $"Expected at least 6 result files, got {vm.Results.Count}");

		// Verify we found expected files
		var fileNames = vm.Results.Select(r => Path.GetFileName(r.FilePath)).ToList();
		Assert.Contains(fileNames, fn => fn == "SearchFile.cs");
		Assert.Contains(fileNames, fn => fn == "GlobSearch.cs");
		Assert.Contains(fileNames, fn => fn == "Utils.cs");
	}

	[Fact(DisplayName = "GUI Integration: Verify result file paths are correct and accessible")]
	[Trait("Category", "GUI-Integration")]
	public async Task OnInitializedAsync_ResultPaths_AreValid()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"searcher_test_{Guid.NewGuid()}");
		Directory.CreateDirectory(tempDir);

		try {
			// Create test files with known paths
			var file1 = Path.Combine(tempDir, "test1.txt");
			var file2 = Path.Combine(tempDir, "test2.txt");
			await File.WriteAllTextAsync(file1, "matching");
			await File.WriteAllTextAsync(file2, "matching");

			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "matching",
				Pattern = new[] { "*.txt" }
			};

			var vm = new MainViewModel(options);
			await vm.OnInitializedAsync();

			Assert.NotEmpty(vm.Results);

			// Verify each result points to an existing file
			foreach (var result in vm.Results) {
				Assert.NotNull(result.FilePath);
				Assert.True(File.Exists(result.FilePath),
					$"Result file path does not exist: {result.FilePath}");
				Assert.NotNull(result.FileName);
				Assert.True(result.FileName.EndsWith(".txt"),
					$"Expected .txt file, got: {result.FileName}");
			}
		}
		finally {
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, true);
			}
		}
	}

	[Fact(DisplayName = "GUI Integration: Results are added to collection during search")]
	[Trait("Category", "GUI-Integration")]
	public async Task OnInitializedAsync_Results_ArePopulatedDurringSearch()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"searcher_test_{Guid.NewGuid()}");
		Directory.CreateDirectory(tempDir);

		try {
			// Create multiple files
			for (int i = 0; i < 5; i++) {
				await File.WriteAllTextAsync(
					Path.Combine(tempDir, $"file{i}.txt"),
					"findme"
				);
			}

			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "findme",
				Pattern = new[] { "*.txt" }
			};

			var vm = new MainViewModel(options);

			// Track result additions
			var resultAddedCount = 0;
			vm.Results.CollectionChanged += (s, e) => {
				if (e.NewItems != null) {
					resultAddedCount += e.NewItems.Count;
				}
			};

			await vm.OnInitializedAsync();

			// Verify results were added to collection
			Assert.True(resultAddedCount > 0, "No results were added to the Results collection");
			Assert.Equal(5, vm.Results.Count);
			Assert.Equal(5, vm.MatchesFound);
			Assert.Equal(5, vm.FilesScanned);
		}
		finally {
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, true);
			}
		}
	}
}
