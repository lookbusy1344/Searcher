using CommandLine;
using SearcherCore;

namespace SearcherGui;

/// <summary>
/// GUI-specific command-line options that extend the core search options
/// </summary>
public class GuiCliOptions : CliOptions
{
	[Option("width", Required = false, Default = 1000, HelpText = "Initial window width")]
	public int WindowWidth { get; set; } = 1000;

	[Option("height", Required = false, Default = 600, HelpText = "Initial window height")]
	public int WindowHeight { get; set; } = 600;

	[Option("auto-close", Required = false, Default = false, HelpText = "Close window after search completes")]
	public bool AutoCloseOnCompletion { get; set; } = false;
}
