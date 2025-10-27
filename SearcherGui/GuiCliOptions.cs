using System.Collections.Generic;
using System.IO;
using SearcherCore;

namespace SearcherGui;

/// <summary>
/// GUI-specific command-line options that extend the core search options
/// </summary>
public class GuiCliOptions : CliOptions
{
	// Inherited properties from CliOptions (no attributes needed)
	// - Folder
	// - Pattern
	// - Search
	// - CaseSensitive
	// - OneThread
	// - InsideZips
	// - HideErrors
	// - Raw

	// GUI-specific options

	/// <summary>
	/// Initial window width in pixels
	/// </summary>
	public int WindowWidth { get; set; } = 1000;

	/// <summary>
	/// Initial window height in pixels
	/// </summary>
	public int WindowHeight { get; set; } = 600;

	/// <summary>
	/// Close window after search completes
	/// </summary>
	public bool AutoCloseOnCompletion { get; set; }

	/// <summary>
	/// Log results to specified file for diagnostics
	/// </summary>
	public string? LogResultsFile { get; set; }
}
