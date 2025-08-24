namespace SearcherCli;

using System.Diagnostics.CodeAnalysis;
using CommandLine;

public class CliOptions
{
	private static readonly DirectoryInfo CurrentDir = new(".");
	private static readonly IReadOnlyList<string> DefaultPattern = ["*"];
	public const StringComparison FilenameComparison = StringComparison.OrdinalIgnoreCase;

	/// <summary>
	/// Default constructor, be case-insensitive
	/// </summary>
	public CliOptions()
	{
		CaseSensitive = false;
		OneThread = false;
		IsSSD = true;
		Search = string.Empty;
	}

	/// <summary>
	/// Get the string comparison to use for the search
	/// </summary>
	public StringComparison StringComparison => CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

	/// <summary>
	/// Number of threads to use, according to --one-thread
	/// </summary>
	public int DegreeOfParallelism => OneThread ? 1 : GetMaxParallelism();

	/// <summary>
	/// Get the max allowed parallelism, which is number of cores or half the cores if its a spinning disk
	/// </summary>
	private int GetMaxParallelism()
	{
		var p = Environment.ProcessorCount;
		if (IsSSD) {
			return p;
		}

		// spinning disk, so use half the cores. 
		p /= 2;

		return Math.Max(p, 1);
	}

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
		get => folder ?? CurrentDir; // if the folder is not set, return the current directory
		set => folder = value;
	}

	/// <summary>
	/// Backing field for folder. This can be null but the property never will be
	/// </summary>
	private DirectoryInfo? folder;

	/// <summary>
	/// Search pattern, eg "*.txt"
	/// </summary>
	[Option('p', "pattern", Required = false, HelpText = "File pattern", Default = null, Min = 1, Max = 20, Separator = ',')]
	public IReadOnlyList<string> Pattern
	{
		get => IsPatternEmpty ? DefaultPattern : pattern;
		set => pattern = value;
	}

	/// <summary>
	/// Backing field for pattern. This can be null but the property never will be
	/// </summary>
	private IReadOnlyList<string>? pattern;

	/// <summary>
	/// Search text eg "hello world"
	/// </summary>
	[Option('s', "search", Required = true, HelpText = "Search text")]
	public string Search { get; set; }

	[Option('w', "open-with", Required = false, HelpText = "App to open apps", Default = null)]
	public string? OpenWith { get; set; }

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

	public bool IsSSD { get; set; }
}
