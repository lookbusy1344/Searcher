using CommandLine;

namespace Searcher;

internal class CliOptions
{
	private static readonly DirectoryInfo CurrentDir = new(".");
	public static readonly string[] DefaultPattern = new string[] { "*" };
	public const StringComparison FilenameComparison = StringComparison.OrdinalIgnoreCase;

	/// <summary>
	/// Default constructor, be case-insensitive
	/// </summary>
	public CliOptions()
	{
		CaseSensitive = false;
		OneThread = false;
	}

	private DirectoryInfo? folder;

	/// <summary>
	/// Get the string comparison to use for the search
	/// </summary>
	public StringComparison GetStringComparison() => CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

	/// <summary>
	/// Number of threads to use, according to --one-thread
	/// </summary>
	public int DegreeOfParallelism() => OneThread ? 1 : Environment.ProcessorCount;

	/// <summary>
	/// Get a string representation of the patterns
	/// </summary>
	public string GetPatterns()
	{
		if (Pattern == null || Pattern.Count == 0)
			return "*";
		else
			return string.Join(',', Pattern);
	}

	/// <summary>
	/// Folder to search. If its NULL it will return current directory
	/// </summary>
	[Option('f', "folder", Required = false, HelpText = "Folder to search", Default = null)]
	public DirectoryInfo? Folder
	{
		get
		{
			// if the folder is not set, return the current directory
			if (folder == null)
				return CurrentDir;
			else
				return folder;
		}
		set => folder = value;
	}

	/// <summary>
	/// Search pattern, eg "*.txt"
	/// </summary>
	[Option('p', "pattern", Required = false, HelpText = "File pattern", Default = null, Min = 1, Max = 20, Separator = ',')]
	public IList<string>? Pattern { get; set; }

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
