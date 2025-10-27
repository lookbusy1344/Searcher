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

	[Fact]
	[Trait("Category", "GUI-Integration")]
	public void Program_WithValidArgs_ParsesCorrectly()
	{
		// Test that Program.Options gets populated correctly
		// This requires launching the actual program, which is complex for GUI apps
		// Instead, we'll test PicoArgs parsing directly

		var args = new[] { "-s", "test", "-f", ".", "-p", "*.txt", "--width", "800" };
		var pico = new PicoArgs(args);

		var search = pico.GetParamOpt("-s", "--search");
		var folder = pico.GetParamOpt("-f", "--folder");
		var patterns = pico.GetMultipleParams("-p", "--pattern");
		var width = pico.GetParamOpt("--width");

		Assert.Equal("test", search);
		Assert.Equal(".", folder);
		Assert.Single(patterns);
		Assert.Equal("*.txt", patterns[0]);
		Assert.Equal("800", width);

		pico.Finished(); // Should not throw
	}

	[Fact]
	[Trait("Category", "GUI-Integration")]
	public void Program_WithUnknownArg_ThrowsPicoArgsException()
	{
		var args = new[] { "--unknown-arg", "value" };
		var pico = new PicoArgs(args);

		var ex = Assert.Throws<PicoArgsException>(() => pico.Finished());
		Assert.Contains("Unrecognised parameter", ex.Message);
	}

	[Fact]
	[Trait("Category", "GUI-Integration")]
	public void Program_WithMultiplePatterns_ParsesAll()
	{
		var args = new[] { "-p", "*.txt", "-p", "*.md", "-p", "*.doc" };
		var pico = new PicoArgs(args);

		var patterns = pico.GetMultipleParams("-p", "--pattern");

		Assert.Equal(3, patterns.Count);
		Assert.Equal("*.txt", patterns[0]);
		Assert.Equal("*.md", patterns[1]);
		Assert.Equal("*.doc", patterns[2]);

		pico.Finished();
	}

	[Fact]
	[Trait("Category", "GUI-Integration")]
	public void Program_WithBoolFlags_ParsesCorrectly()
	{
		var args = new[] { "-z", "-c", "-o", "--hide-errors", "--auto-close" };
		var pico = new PicoArgs(args);

		var insideZips = pico.Contains("-z", "--inside-zips");
		var caseSensitive = pico.Contains("-c", "--case-sensitive");
		var oneThread = pico.Contains("-o", "--one-thread");
		var hideErrors = pico.Contains("--hide-errors");
		var autoClose = pico.Contains("--auto-close");

		Assert.True(insideZips);
		Assert.True(caseSensitive);
		Assert.True(oneThread);
		Assert.True(hideErrors);
		Assert.True(autoClose);

		pico.Finished();
	}
}
