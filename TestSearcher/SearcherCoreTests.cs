namespace TestSearcher;

using SearcherCore;

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
		} finally {
			File.Delete(tempFile);
		}
	}

	[Fact(DisplayName = "Core: SearchResult enum values")]
	[Trait("Category", "Core")]
	public void SearchResultValues()
	{
		Assert.True(Enum.IsDefined(typeof(SearchResult), SearchResult.Found));
		Assert.True(Enum.IsDefined(typeof(SearchResult), SearchResult.NotFound));
		Assert.True(Enum.IsDefined(typeof(SearchResult), SearchResult.Error));
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
}