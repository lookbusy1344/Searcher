extern alias SearcherCoreLib;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SearcherCoreLib::SearcherCore;
using SearcherGui;
using SearcherGui.ViewModels;
using Xunit;
using Xunit.Abstractions;

namespace TestSearcher.SearcherGui;

/// <summary>
/// Systematic tests to verify GUI search functionality matches CLI/Core behavior
/// </summary>
public class GuiSystematicTests
{
	private readonly ITestOutputHelper _output;

	public GuiSystematicTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact(DisplayName = "GUI Systematic: Search SearcherCore for 'class' matches CLI results")]
	[Trait("Category", "GUI-Systematic")]
	public async Task SearchSearcherCore_ForClass_MatchesCliResults()
	{
		// Determine the SearcherCore path relative to test execution
		var currentDir = Directory.GetCurrentDirectory();
		_output.WriteLine($"Current directory: {currentDir}");

		// Find the repo root by looking for SearcherCore
		var searcherCorePath = FindSearcherCorePath(currentDir);
		_output.WriteLine($"SearcherCore path: {searcherCorePath}");

		Assert.True(Directory.Exists(searcherCorePath), $"SearcherCore path does not exist: {searcherCorePath}");

		// First, verify Core CLI behavior
		var coreOptions = new CliOptions {
			Folder = new DirectoryInfo(searcherCorePath),
			Search = "class",
			Pattern = new[] { "*.cs" }
		};

		var innerPatterns = Utils.ProcessInnerPatterns(coreOptions.Pattern);
		var outerPatterns = Utils.ProcessOuterPatterns(coreOptions.Pattern, coreOptions.InsideZips);
		var files = GlobSearch.ParallelFindFiles(coreOptions.Folder.FullName, outerPatterns, coreOptions.DegreeOfParallelism, null, default);

		_output.WriteLine($"Core: Found {files.Length} .cs files to search");
		foreach (var file in files.Take(10)) {
			_output.WriteLine($"  - {file}");
		}

		// Count matches using Core logic
		int coreMatchCount = 0;
		foreach (var file in files) {
			var result = SearchFile.FileContainsStringWrapper(file, coreOptions.Search, innerPatterns, coreOptions.StringComparison, default);
			if (result == SearchResult.Found) {
				coreMatchCount++;
				_output.WriteLine($"Core match: {Path.GetFileName(file)}");
			}
		}

		_output.WriteLine($"Core: Found {coreMatchCount} files containing 'class'");
		Assert.True(coreMatchCount > 0, "Core should find files containing 'class'");

		// Now test GUI with same parameters
		var guiOptions = new GuiCliOptions {
			Folder = new DirectoryInfo(searcherCorePath),
			Search = "class",
			Pattern = new[] { "*.cs" }
		};

		var vm = new MainViewModel(guiOptions);
		await vm.OnInitializedAsync();

		_output.WriteLine($"GUI: IsSearching = {vm.IsSearching}");
		_output.WriteLine($"GUI: StatusMessage = {vm.StatusMessage}");
		_output.WriteLine($"GUI: FilesScanned = {vm.FilesScanned}");
		_output.WriteLine($"GUI: MatchesFound = {vm.MatchesFound}");
		_output.WriteLine($"GUI: Results.Count = {vm.Results.Count}");

		// Log all results for debugging
		foreach (var result in vm.Results.Take(10)) {
			_output.WriteLine($"GUI result: {result.FileName} at {result.FilePath} (matches: {result.MatchCount})");
		}

		// Assertions
		Assert.False(vm.IsSearching, "Search should complete");
		Assert.Equal(files.Length, vm.FilesScanned); // Should scan same number of files
		Assert.Equal(coreMatchCount, vm.Results.Count); // Should find same number of results
		Assert.True(vm.MatchesFound > 0, "GUI should find matches");

		// Verify results have valid data
		foreach (var result in vm.Results) {
			Assert.NotNull(result.FilePath);
			Assert.NotEmpty(result.FilePath);
			Assert.NotNull(result.FileName);
			Assert.NotEmpty(result.FileName);
			Assert.True(File.Exists(result.FilePath), $"Result file should exist: {result.FilePath}");
		}
	}

	[Fact(DisplayName = "GUI Systematic: Empty search returns no results (expected behavior)")]
	[Trait("Category", "GUI-Systematic")]
	public async Task EmptySearch_ReturnsNoResults()
	{
		var tempDir = CreateTempTestDir();
		try {
			// Create test files
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file1.txt"), "content one");
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file2.txt"), "content two");
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file3.txt"), "content three");

			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "", // Empty search
				Pattern = new[] { "*.txt" }
			};

			var vm = new MainViewModel(options);
			await vm.OnInitializedAsync();

			_output.WriteLine($"Empty search: FilesScanned = {vm.FilesScanned}, Results = {vm.Results.Count}");

			// Empty search returns NotFound per SearchFile logic, so no results expected
			Assert.Equal(3, vm.FilesScanned);
			Assert.Empty(vm.Results);
		}
		finally {
			CleanupTempDir(tempDir);
		}
	}

	[Fact(DisplayName = "GUI Systematic: Pattern matching works correctly")]
	[Trait("Category", "GUI-Systematic")]
	public async Task PatternMatching_FiltersCorrectly()
	{
		var tempDir = CreateTempTestDir();
		try {
			// Create mixed file types
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file1.cs"), "class MyClass");
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file2.txt"), "class MyClass");
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file3.cs"), "class AnotherClass");
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file4.md"), "class Document");

			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "class",
				Pattern = new[] { "*.cs" } // Only C# files
			};

			var vm = new MainViewModel(options);
			await vm.OnInitializedAsync();

			_output.WriteLine($"Pattern *.cs: FilesScanned = {vm.FilesScanned}, Results = {vm.Results.Count}");
			foreach (var result in vm.Results) {
				_output.WriteLine($"  Found: {result.FileName}");
			}

			// Should only scan .cs files
			Assert.Equal(2, vm.FilesScanned);
			Assert.Equal(2, vm.Results.Count);
			Assert.All(vm.Results, r => Assert.EndsWith(".cs", r.FileName));
		}
		finally {
			CleanupTempDir(tempDir);
		}
	}

	[Fact(DisplayName = "GUI Systematic: Case sensitivity flag works")]
	[Trait("Category", "GUI-Systematic")]
	public async Task CaseSensitivity_WorksCorrectly()
	{
		var tempDir = CreateTempTestDir();
		try {
			await File.WriteAllTextAsync(Path.Combine(tempDir, "test.txt"), "Class class CLASS");

			// Case insensitive (default)
			var insensitiveOptions = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "class",
				Pattern = new[] { "*.txt" },
				CaseSensitive = false
			};

			var vmInsensitive = new MainViewModel(insensitiveOptions);
			await vmInsensitive.OnInitializedAsync();

			_output.WriteLine($"Case insensitive: Results = {vmInsensitive.Results.Count}");
			Assert.Single(vmInsensitive.Results); // Should find the file

			// Case sensitive
			var sensitiveOptions = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "class",
				Pattern = new[] { "*.txt" },
				CaseSensitive = true
			};

			var vmSensitive = new MainViewModel(sensitiveOptions);
			await vmSensitive.OnInitializedAsync();

			_output.WriteLine($"Case sensitive: Results = {vmSensitive.Results.Count}");
			Assert.Single(vmSensitive.Results); // Should still find it (lowercase "class" is in the file)
		}
		finally {
			CleanupTempDir(tempDir);
		}
	}

	[Fact(DisplayName = "GUI Systematic: Multiple patterns work correctly")]
	[Trait("Category", "GUI-Systematic")]
	public async Task MultiplePatterns_SearchesAllTypes()
	{
		var tempDir = CreateTempTestDir();
		try {
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file1.cs"), "search term");
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file2.txt"), "search term");
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file3.md"), "search term");
			await File.WriteAllTextAsync(Path.Combine(tempDir, "file4.log"), "different content");

			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "search term",
				Pattern = new[] { "*.cs", "*.txt", "*.md" }
			};

			var vm = new MainViewModel(options);
			await vm.OnInitializedAsync();

			_output.WriteLine($"Multiple patterns: FilesScanned = {vm.FilesScanned}, Results = {vm.Results.Count}");

			Assert.Equal(3, vm.Results.Count); // Only cs, txt, md should match
			Assert.Contains(vm.Results, r => r.FileName == "file1.cs");
			Assert.Contains(vm.Results, r => r.FileName == "file2.txt");
			Assert.Contains(vm.Results, r => r.FileName == "file3.md");
			Assert.DoesNotContain(vm.Results, r => r.FileName == "file4.log");
		}
		finally {
			CleanupTempDir(tempDir);
		}
	}

	[Fact(DisplayName = "GUI Systematic: Recursive directory search works")]
	[Trait("Category", "GUI-Systematic")]
	public async Task RecursiveSearch_FindsNestedFiles()
	{
		var tempDir = CreateTempTestDir();
		try {
			var subDir1 = Path.Combine(tempDir, "sub1");
			var subDir2 = Path.Combine(tempDir, "sub1", "sub2");
			Directory.CreateDirectory(subDir1);
			Directory.CreateDirectory(subDir2);

			await File.WriteAllTextAsync(Path.Combine(tempDir, "root.txt"), "target");
			await File.WriteAllTextAsync(Path.Combine(subDir1, "level1.txt"), "target");
			await File.WriteAllTextAsync(Path.Combine(subDir2, "level2.txt"), "target");

			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "target",
				Pattern = new[] { "*.txt" }
			};

			var vm = new MainViewModel(options);
			await vm.OnInitializedAsync();

			_output.WriteLine($"Recursive search: FilesScanned = {vm.FilesScanned}, Results = {vm.Results.Count}");

			Assert.Equal(3, vm.Results.Count);
			Assert.Contains(vm.Results, r => r.FileName == "root.txt");
			Assert.Contains(vm.Results, r => r.FileName == "level1.txt");
			Assert.Contains(vm.Results, r => r.FileName == "level2.txt");
		}
		finally {
			CleanupTempDir(tempDir);
		}
	}

	[Fact(DisplayName = "GUI Systematic: Results contain valid file paths")]
	[Trait("Category", "GUI-Systematic")]
	public async Task Results_ContainValidFilePaths()
	{
		var tempDir = CreateTempTestDir();
		try {
			var file1 = Path.Combine(tempDir, "test1.txt");
			var file2 = Path.Combine(tempDir, "test2.txt");
			await File.WriteAllTextAsync(file1, "findme");
			await File.WriteAllTextAsync(file2, "findme");

			var options = new GuiCliOptions {
				Folder = new DirectoryInfo(tempDir),
				Search = "findme",
				Pattern = new[] { "*.txt" }
			};

			var vm = new MainViewModel(options);
			await vm.OnInitializedAsync();

			Assert.Equal(2, vm.Results.Count);

			foreach (var result in vm.Results) {
				_output.WriteLine($"Checking result: FileName='{result.FileName}', FilePath='{result.FilePath}'");

				Assert.NotNull(result.FileName);
				Assert.NotEmpty(result.FileName);
				Assert.NotNull(result.FilePath);
				Assert.NotEmpty(result.FilePath);
				Assert.True(File.Exists(result.FilePath), $"File should exist: {result.FilePath}");
				Assert.Equal(Path.GetFileName(result.FilePath), result.FileName);
			}
		}
		finally {
			CleanupTempDir(tempDir);
		}
	}

	#region Helper Methods

	private static string FindSearcherCorePath(string startPath)
	{
		var dir = new DirectoryInfo(startPath);
		while (dir != null) {
			var searcherCorePath = Path.Combine(dir.FullName, "SearcherCore");
			if (Directory.Exists(searcherCorePath)) {
				return searcherCorePath;
			}
			dir = dir.Parent;
		}

		throw new DirectoryNotFoundException("Could not find SearcherCore directory");
	}

	private static string CreateTempTestDir()
	{
		var tempDir = Path.Combine(Path.GetTempPath(), $"searcher_test_{Guid.NewGuid()}");
		Directory.CreateDirectory(tempDir);
		return tempDir;
	}

	private static void CleanupTempDir(string dir)
	{
		if (Directory.Exists(dir)) {
			try {
				Directory.Delete(dir, true);
			}
			catch {
				// Best effort cleanup
			}
		}
	}

	#endregion
}
