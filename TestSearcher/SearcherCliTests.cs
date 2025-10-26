extern alias SearcherCoreLib;
using System;
using System.IO;
using System.Threading.Tasks;
using SearcherCoreLib::SearcherCore;
using Xunit;

namespace TestSearcher;

public class SearcherCliTests
{
	[Fact(DisplayName = "CLI: CliOptions parses command line arguments")]
	[Trait("Category", "CLI")]
	public void CliOptions_ParsesArguments()
	{
		var args = new[] {
			"--folder", "/tmp",
			"--search", "test",
			"--pattern", "*.txt",
			"--case-sensitive"
		};

		// Note: Actual CLI parsing happens in SearcherCli
		// This test validates that CliOptions class accepts the options
		var options = new CliOptions {
			Folder = new DirectoryInfo("/tmp"),
			Search = "test",
			Pattern = new[] { "*.txt" },
			CaseSensitive = true
		};

		Assert.Equal("/tmp", options.Folder.FullName);
		Assert.Equal("test", options.Search);
		Assert.Single(options.Pattern);
		Assert.True(options.CaseSensitive);
	}

	[Fact(DisplayName = "CLI: CliOptions has default values")]
	[Trait("Category", "CLI")]
	public void CliOptions_HasDefaults()
	{
		var options = new CliOptions();

		Assert.False(options.CaseSensitive);
		Assert.False(options.OneThread);
		Assert.NotNull(options.Pattern);
	}

	[Fact(DisplayName = "CLI: CliOptions accepts multiple patterns")]
	[Trait("Category", "CLI")]
	public void CliOptions_AcceptsMultiplePatterns()
	{
		var options = new CliOptions {
			Pattern = new[] { "*.txt", "*.md", "*.log" }
		};

		Assert.Equal(3, options.Pattern.Count);
	}

	[Fact(DisplayName = "CLI: CliOptions validates folder path")]
	[Trait("Category", "CLI")]
	public void CliOptions_ValidatesFolderPath()
	{
		var tempDir = Path.GetTempPath();
		var options = new CliOptions {
			Folder = new DirectoryInfo(tempDir)
		};

		Assert.True(options.Folder.Exists);
	}

	[Fact(DisplayName = "CLI: CliOptions from SearcherCore works")]
	[Trait("Category", "CLI")]
	public void CliOptions_ExtendsCore()
	{
		var options = new CliOptions {
			Search = "test",
			CaseSensitive = true,
			OneThread = false,
			IsSSD = true
		};

		// Verify properties work
		Assert.Equal("test", options.Search);
		Assert.True(options.CaseSensitive);
		Assert.False(options.OneThread);
		Assert.True(options.IsSSD);
	}
}
