extern alias SearcherCoreLib;

namespace TestSearcher;

using SearcherCoreLib::SearcherCore;

public class SearcherCoreTests
{
	[Fact(DisplayName = "Core: CliOptions default settings")]
	[Trait("Category", "Core")]
	public void CliOptionsDefaults()
	{
		var options = new CliOptions();

		Assert.False(options.CaseSensitive);
		Assert.False(options.OneThread);
		Assert.True(options.IsSSD);
		Assert.Empty(options.Search);
		Assert.Equal(StringComparison.OrdinalIgnoreCase, options.StringComparison);
		Assert.Equal(["*"], options.Pattern);
	}

	[Fact(DisplayName = "Core: CliOptions case sensitivity")]
	[Trait("Category", "Core")]
	public void CliOptionsCaseSensitivity()
	{
		var options = new CliOptions { CaseSensitive = true };
		Assert.Equal(StringComparison.Ordinal, options.StringComparison);

		options.CaseSensitive = false;
		Assert.Equal(StringComparison.OrdinalIgnoreCase, options.StringComparison);
	}

	[Fact(DisplayName = "Core: CliOptions parallelism")]
	[Trait("Category", "Core")]
	public void CliOptionsParallelism()
	{
		var options = new CliOptions();
		var maxCores = Environment.ProcessorCount;

		// SSD mode should use all cores
		options.IsSSD = true;
		options.OneThread = false;
		Assert.Equal(maxCores, options.DegreeOfParallelism);

		// One thread mode
		options.OneThread = true;
		Assert.Equal(1, options.DegreeOfParallelism);

		// Spinning disk should use half cores (minimum 1)
		options.OneThread = false;
		options.IsSSD = false;
		var expected = Math.Max(maxCores / 2, 1);
		Assert.Equal(expected, options.DegreeOfParallelism);
	}

	[Fact(DisplayName = "Core: CliOptions pattern handling")]
	[Trait("Category", "Core")]
	public void CliOptionsPatterns()
	{
		var options = new CliOptions();

		// Default pattern
		Assert.Equal("*", options.GetPatterns());

		// Single pattern
		options.Pattern = ["*.txt"];
		Assert.Equal("*.txt", options.GetPatterns());

		// Multiple patterns
		options.Pattern = ["*.txt", "*.pdf"];
		Assert.Equal("*.txt,*.pdf", options.GetPatterns());
	}

	[Fact(DisplayName = "Core: SearchFile basic functionality")]
	[Trait("Category", "Core")]
	public void SearchFileBasic()
	{
		// Create a temporary test file
		var tempFile = Path.GetTempFileName();
		try {
			File.WriteAllText(tempFile, "Hello world\nThis is a test file\nWith multiple lines");

			// Test successful search
			var result = SearchFile.FileContainsStringWrapper(
				tempFile,
				"test file",
				[],
				StringComparison.OrdinalIgnoreCase,
				CancellationToken.None
			);
			Assert.Equal(SearchResult.Found, result);

			// Test case sensitivity
			result = SearchFile.FileContainsStringWrapper(
				tempFile,
				"TEST FILE",
				[],
				StringComparison.Ordinal,
				CancellationToken.None
			);
			Assert.Equal(SearchResult.NotFound, result);

			// Test not found
			result = SearchFile.FileContainsStringWrapper(
				tempFile,
				"nonexistent text",
				[],
				StringComparison.OrdinalIgnoreCase,
				CancellationToken.None
			);
			Assert.Equal(SearchResult.NotFound, result);
		}
		finally {
			File.Delete(tempFile);
		}
	}

	[Fact(DisplayName = "Core: SearchResult enum values")]
	[Trait("Category", "Core")]
	public void SearchResultValues()
	{
		Assert.True(Enum.IsDefined(SearchResult.Found));
		Assert.True(Enum.IsDefined(SearchResult.NotFound));
		Assert.True(Enum.IsDefined(SearchResult.Error));
	}

	[Fact(DisplayName = "Core: SingleResult record")]
	[Trait("Category", "Core")]
	public void SingleResultRecord()
	{
		var result = new SingleResult("test.txt", SearchResult.Found);
		Assert.Equal("test.txt", result.Path);
		Assert.Equal(SearchResult.Found, result.Result);

		// Test equality
		var result2 = new SingleResult("test.txt", SearchResult.Found);
		Assert.Equal(result, result2);

		var result3 = new SingleResult("other.txt", SearchResult.Found);
		Assert.NotEqual(result, result3);
	}

	[Theory(DisplayName = "Security: IsValidFilePath - Valid paths")]
	[Trait("Category", "Security")]
	[InlineData("C:\\temp\\file.txt", true)]
	[InlineData("/tmp/file.txt", true)]
	[InlineData("file.txt", true)]
	[InlineData("folder/subfolder/file.txt", true)]
	[InlineData("normal_file.txt", true)]
	[InlineData("file-with-dashes.txt", true)]
	[InlineData("file with spaces.txt", true)]
	public void IsValidFilePath_ValidPaths(string path, bool expected)
	{
		var result = Utils.IsValidFilePath(path);
		Assert.Equal(expected, result);
	}

	[Theory(DisplayName = "Security: IsValidFilePath - Invalid paths")]
	[Trait("Category", "Security")]
	[InlineData("../../../etc/passwd", false)]
	[InlineData("..\\..\\Windows\\System32\\config\\sam", false)]
	[InlineData("CON.txt", false)]
	[InlineData("PRN", false)]
	[InlineData("COM1.log", false)]
	[InlineData("LPT1", false)]
	[InlineData("AUX.txt", false)]
	[InlineData("NUL.dat", false)]
	[InlineData("con", false)] // case insensitive
	[InlineData("COM9.xyz", false)]
	[InlineData("LPT9.abc", false)]
	public void IsValidFilePath_InvalidPaths(string path, bool expected)
	{
		var result = Utils.IsValidFilePath(path);
		Assert.Equal(expected, result);
	}

	[Theory(DisplayName = "Security: IsValidFilePath - Edge cases")]
	[Trait("Category", "Security")]
	[InlineData("", false)]
	[InlineData("   ", false)]
	[InlineData(null, false)]
	public void IsValidFilePath_EdgeCases(string? path, bool expected)
	{
		var result = Utils.IsValidFilePath(path!);
		Assert.Equal(expected, result);
	}

	[Fact(DisplayName = "Security: IsValidFilePath - All reserved names")]
	[Trait("Category", "Security")]
	public void IsValidFilePath_AllReservedNames()
	{
		string[] reservedNames = ["CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"];

		foreach (var reserved in reservedNames) {
			// Test with and without extension
			Assert.False(Utils.IsValidFilePath(reserved), $"Reserved name {reserved} should be invalid");
			Assert.False(Utils.IsValidFilePath($"{reserved}.txt"), $"Reserved name {reserved}.txt should be invalid");
			Assert.False(Utils.IsValidFilePath(reserved.ToLower()), $"Reserved name {reserved.ToLower()} should be invalid (case insensitive)");
		}
	}

	[Fact(DisplayName = "Security: ValidateSearchPath - Valid directories")]
	[Trait("Category", "Security")]
	public void ValidateSearchPath_ValidDirectories()
	{
		// Test with current directory
		var currentDir = Directory.GetCurrentDirectory();
		var result = Utils.ValidateSearchPath(currentDir);
		Assert.Equal(Path.GetFullPath(currentDir), result);

		// Test with temp directory
		var tempDir = Path.GetTempPath();
		result = Utils.ValidateSearchPath(tempDir);
		Assert.Equal(Path.GetFullPath(tempDir), result);
	}

	[Fact(DisplayName = "Security: ValidateSearchPath - Invalid directories")]
	[Trait("Category", "Security")]
	public void ValidateSearchPath_InvalidDirectories()
	{
		// Test with non-existent directory
		var nonExistentPath = Path.Combine(Path.GetTempPath(), "non_existent_directory_12345");
		var result = Utils.ValidateSearchPath(nonExistentPath);
		Assert.Null(result);

		// Test with file instead of directory
		var tempFile = Path.GetTempFileName();
		try {
			result = Utils.ValidateSearchPath(tempFile);
			Assert.Null(result);
		}
		finally {
			File.Delete(tempFile);
		}
	}

	[Theory(DisplayName = "Security: ValidateSearchPath - Edge cases")]
	[Trait("Category", "Security")]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData(null)]
	public void ValidateSearchPath_EdgeCases(string? path)
	{
		var result = Utils.ValidateSearchPath(path!);
		Assert.Null(result);
	}

	[Fact(DisplayName = "Security: ValidateSearchPath - Path normalization")]
	[Trait("Category", "Security")]
	public void ValidateSearchPath_PathNormalization()
	{
		var currentDir = Directory.GetCurrentDirectory();

		// Test that paths are normalized (remove redundant separators, etc.)
		var pathWithRedundantSeparators = currentDir + Path.DirectorySeparatorChar + Path.DirectorySeparatorChar + "." + Path.DirectorySeparatorChar;
		var result = Utils.ValidateSearchPath(pathWithRedundantSeparators);

		// Should return normalized path
		Assert.NotNull(result);
		Assert.Equal(Path.GetFullPath(pathWithRedundantSeparators), result);
	}
}
