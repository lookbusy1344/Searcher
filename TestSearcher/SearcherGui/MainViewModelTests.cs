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

		var vm = new MainViewModel(options);

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

		var vm = new MainViewModel(options);

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

		var vm = new MainViewModel(options);

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

		var vm = new MainViewModel(options);

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

		var vm = new MainViewModel(options);

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

		var vm = new MainViewModel(options);

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

		var vm = new MainViewModel(options);
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

			var vm = new MainViewModel(options);

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

			var vm = new MainViewModel(options);

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

		var vm = new MainViewModel(options);

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

		var vm = new MainViewModel(options);

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

			var vm = new MainViewModel(options);

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

			var vm = new MainViewModel(options);

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
}
