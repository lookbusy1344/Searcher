using System.Collections.Generic;
using System.IO;
using CommandLine;
using SearcherCore;

namespace SearcherGui;

/// <summary>
/// GUI-specific command-line options that extend the core search options
/// </summary>
public class GuiCliOptions : CliOptions
{
	// Re-declare inherited properties with [Option] attributes to expose them to CommandLineParser
	// These must properly update the base class properties

	[Option('f', "folder", Required = false, HelpText = "Folder to search")]
	public new DirectoryInfo Folder
	{
		get => base.Folder;
		set => base.Folder = value;
	}

	[Option('p', "pattern", Required = false, Separator = ',', HelpText = "File patterns to match (comma-separated)")]
	public new IReadOnlyList<string> Pattern
	{
		get => base.Pattern;
		set => base.Pattern = value;
	}

	[Option('s', "search", Required = false, Default = "", HelpText = "Text to search for")]
	public new string Search
	{
		get => base.Search;
		set => base.Search = value;
	}

	[Option('c', "case-sensitive", Required = false, Default = false, HelpText = "Case sensitive search")]
	public new bool CaseSensitive
	{
		get => base.CaseSensitive;
		set => base.CaseSensitive = value;
	}

	[Option('o', "one-thread", Required = false, Default = false, HelpText = "Single-threaded search")]
	public new bool OneThread
	{
		get => base.OneThread;
		set => base.OneThread = value;
	}

	[Option('z', "inside-zips", Required = false, Default = false, HelpText = "Search inside ZIP files")]
	public new bool InsideZips
	{
		get => base.InsideZips;
		set => base.InsideZips = value;
	}

	[Option("hide-errors", Required = false, Default = false, HelpText = "Hide errors from output")]
	public new bool HideErrors
	{
		get => base.HideErrors;
		set => base.HideErrors = value;
	}

	[Option('r', "raw", Required = false, Default = false, HelpText = "Raw output mode")]
	public new bool Raw
	{
		get => base.Raw;
		set => base.Raw = value;
	}

	// GUI-specific options

	[Option("width", Required = false, Default = 1000, HelpText = "Initial window width")]
	public int WindowWidth { get; set; } = 1000;

	[Option("height", Required = false, Default = 600, HelpText = "Initial window height")]
	public int WindowHeight { get; set; } = 600;

	[Option("auto-close", Required = false, Default = false, HelpText = "Close window after search completes")]
	public bool AutoCloseOnCompletion { get; set; } = false;

	[Option("log-results", Required = false, HelpText = "Log results to specified file for diagnostics")]
	public string? LogResultsFile { get; set; }
}
