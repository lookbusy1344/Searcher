namespace Searcher;

using CommandLine;
using SearcherCore;

/// <summary>
/// Configuration options for WinForms application with command-line parser attributes
/// </summary>
public class CliOptions : SearcherCore.CliOptions
{
	/// <summary>
	/// Folder to search. If its NULL it will return current directory
	/// </summary>
	[Option('f', "folder", Required = false, HelpText = "Folder to search", Default = null)]
	public new DirectoryInfo Folder
	{
		get => base.Folder;
		set => base.Folder = value;
	}

	/// <summary>
	/// Search pattern, eg "*.txt"
	/// </summary>
	[Option('p', "pattern", Required = false, HelpText = "File pattern", Default = null, Min = 1, Max = 20, Separator = ',')]
	public new IReadOnlyList<string> Pattern
	{
		get => base.Pattern;
		set => base.Pattern = value;
	}

	/// <summary>
	/// Search text eg "hello world"
	/// </summary>
	[Option('s', "search", Required = true, HelpText = "Search text")]
	public new string Search
	{
		get => base.Search;
		set => base.Search = value;
	}

	/// <summary>
	/// App to open files with
	/// </summary>
	[Option('w', "open-with", Required = false, HelpText = "App to open apps", Default = null)]
	public new string? OpenWith
	{
		get => base.OpenWith;
		set => base.OpenWith = value;
	}

	/// <summary>
	/// true if the search should be case sensitive; false otherwise
	/// </summary>
	[Option('c', "case-sensitive", Required = false, HelpText = "Search case-sensitive", Default = false)]
	public new bool CaseSensitive
	{
		get => base.CaseSensitive;
		set => base.CaseSensitive = value;
	}

	/// <summary>
	/// true if only one thread should be used; false otherwise
	/// </summary>
	[Option('o', "one-thread", Required = false, HelpText = "Just use a single thread", Default = false)]
	public new bool OneThread
	{
		get => base.OneThread;
		set => base.OneThread = value;
	}

	/// <summary>
	/// Always search inside zip files
	/// </summary>
	[Option('z', "inside-zips", Required = false, HelpText = "Always search inside zips", Default = false)]
	public new bool InsideZips
	{
		get => base.InsideZips;
		set => base.InsideZips = value;
	}

	/// <summary>
	/// Hide errors in output list
	/// </summary>
	[Option('h', "hide-errors", Required = false, HelpText = "Hide errors in output list", Default = false)]
	public new bool HideErrors
	{
		get => base.HideErrors;
		set => base.HideErrors = value;
	}
}