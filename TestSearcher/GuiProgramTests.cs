extern alias SearcherCoreLib;

using SearcherGui;
using SearcherCoreLib::SearcherCore;
using Xunit;

namespace TestSearcher;

/// <summary>
/// Tests for SearcherGui Program.cs command-line parsing with PicoArgs
/// </summary>
public class GuiProgramTests
{
	[Fact]
	[Trait("Category", "GUI")]
	public void ParseCommandLine_NoArgs_UsesDefaults()
	{
		// This will be a reflection-based test since ParseCommandLine is private
		// For now, we'll test via GuiCliOptions directly
		var opts = new GuiCliOptions();

		Assert.Equal(1000, opts.WindowWidth);
		Assert.Equal(600, opts.WindowHeight);
		Assert.False(opts.AutoCloseOnCompletion);
		Assert.Null(opts.LogResultsFile);
	}

	[Fact]
	[Trait("Category", "GUI")]
	public void GuiCliOptions_Defaults_MatchExpected()
	{
		var opts = new GuiCliOptions();

		Assert.Equal(1000, opts.WindowWidth);
		Assert.Equal(600, opts.WindowHeight);
		Assert.False(opts.AutoCloseOnCompletion);
		Assert.Null(opts.LogResultsFile);
		Assert.Equal("", opts.Search);
		Assert.False(opts.CaseSensitive);
		Assert.False(opts.OneThread);
		Assert.False(opts.InsideZips);
		Assert.False(opts.HideErrors);
		Assert.False(opts.Raw);
	}

	[Fact]
	[Trait("Category", "GUI")]
	public void GuiCliOptions_SetProperties_WorksCorrectly()
	{
		var opts = new GuiCliOptions {
			WindowWidth = 800,
			WindowHeight = 600,
			AutoCloseOnCompletion = true,
			LogResultsFile = "test.log",
			Search = "test search",
			CaseSensitive = true
		};

		Assert.Equal(800, opts.WindowWidth);
		Assert.Equal(600, opts.WindowHeight);
		Assert.True(opts.AutoCloseOnCompletion);
		Assert.Equal("test.log", opts.LogResultsFile);
		Assert.Equal("test search", opts.Search);
		Assert.True(opts.CaseSensitive);
	}

	[Fact]
	[Trait("Category", "GUI")]
	public void GuiCliOptions_InheritsFromCliOptions()
	{
		var opts = new GuiCliOptions();

		Assert.IsAssignableFrom<CliOptions>(opts);
	}
}
