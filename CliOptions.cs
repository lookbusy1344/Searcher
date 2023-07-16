using CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace Searcher;

public class CliOptions
{
	private static readonly DirectoryInfo CurrentDir = new(".");
	private static readonly string[] DefaultPattern = new string[] { "*" };
	public const StringComparison FilenameComparison = StringComparison.OrdinalIgnoreCase;

	/// <summary>
	/// Default constructor, be case-insensitive
	/// </summary>
	public CliOptions()
	{
		CaseSensitive = false;
		OneThread = false;
	}

	/// <summary>
	/// Get the string comparison to use for the search
	/// </summary>
	public StringComparison StringComparison => CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

	/// <summary>
	/// Number of threads to use, according to --one-thread
	/// </summary>
	public int DegreeOfParallelism() => OneThread ? 1 : Environment.ProcessorCount;

	/// <summary>
	/// Are any patterns defined?
	/// </summary>
	[MemberNotNullWhen(false, nameof(pattern))] // if we return false, then pattern is not null. This tells the compiler that
	private bool IsPatternEmpty => pattern is null || pattern.Count == 0;

	/// <summary>
	/// Get a string representation of the patterns
	/// </summary>
	public string GetPatterns() => IsPatternEmpty ? "*" : string.Join(',', Pattern);

	/// <summary>
	/// Folder to search. If its NULL it will return current directory
	/// </summary>
	[Option('f', "folder", Required = false, HelpText = "Folder to search", Default = null)]
	public DirectoryInfo Folder
	{
		get => folder ?? CurrentDir;        // if the folder is not set, return the current directory
		set => folder = value;
	}

	/// <summary>
	/// Backing field for folder. This can be null but the property never will be
	/// </summary>
	private DirectoryInfo? folder;

#pragma warning disable CA2227 // Collection properties should be read only
	/// <summary>
	/// Search pattern, eg "*.txt"
	/// </summary>
	[Option('p', "pattern", Required = false, HelpText = "File pattern", Default = null, Min = 1, Max = 20, Separator = ',')]
	public IList<string> Pattern
	{
		get => IsPatternEmpty ? DefaultPattern : pattern;
		set => pattern = value;
	}
#pragma warning restore CA2227 // Collection properties should be read only

	/// <summary>
	/// Backing field for pattern. This can be null but the property never will be
	/// </summary>
	private IList<string>? pattern;

	/// <summary>
	/// Search text eg "hello world"
	/// </summary>
	[Option('s', "search", Required = true, HelpText = "Search text")]
	public string? Search { get; set; }

	/// <summary>
	/// true if the search should be case sensitive; false otherwise
	/// </summary>
	[Option('c', "case-sensitive", Required = false, HelpText = "Search case-sensitive", Default = false)]
	public bool CaseSensitive { get; set; }

	/// <summary>
	/// true if the search should be case sensitive; false otherwise
	/// </summary>
	[Option('o', "one-thread", Required = false, HelpText = "Just use a single thread", Default = false)]
	public bool OneThread { get; set; }

	/// <summary>
	/// Always search inside zips
	/// </summary>
	[Option('z', "inside-zips", Required = false, HelpText = "Always search inside zips", Default = false)]
	public bool InsideZips { get; set; }

	/// <summary>
	/// Hide errors in output list
	/// </summary>
	[Option('h', "hide-errors", Required = false, HelpText = "Hide errors in output list", Default = false)]
	public bool HideErrors { get; set; }
}
