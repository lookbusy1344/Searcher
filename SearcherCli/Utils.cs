namespace SearcherCli;

using System.Diagnostics;
using System.Linq;
using DotNet.Globbing;
using Microsoft.Win32;

/// <summary>
/// Result of a search
/// </summary>
public enum SearchResult { Found, NotFound, Error }

/// <summary>
/// A single search result
/// </summary>
public readonly record struct SingleResult(string Path, SearchResult Result);

internal static class Utils
{
	private const string TextFileOpener = "notepad.exe";
	private static ReadOnlySpan<byte> MagicNumberZip => [0x50, 0x4B, 0x03, 0x04];
	private static ReadOnlySpan<string> TextFileTypes => new string[] { ".txt", ".log", ".md", ".cs", ".rs", ".js", ".html" };
	private static ReadOnlySpan<string> WordFileTypes => new string[] { ".docx", ".doc", ".rtf" };
	private static readonly Lazy<string> pathToWord = new(GetWordPath);
	private static readonly Lazy<string> AcrobatPath = new(() => GetProgramPath("Acrobat.exe") ?? GetProgramPath("AcroRd32.exe") ?? string.Empty);

	/// <summary>
	/// Calculate a reasonable update rate, from 1 to 201 items
	/// </summary>
	public static int CalculateModulo(int count)
	{
		int modulo;
		if (count < 100) {
			return 1; // small number of files, so update progress bar every check
		}

		modulo = count / 100; // 100 or more files, so update progress bar every 1% of files

		// keep the updates between 1 and 201 items
#pragma warning disable IDE0046 // Convert to conditional expression
		if (modulo < 1) {
			return 1;
		}
#pragma warning restore IDE0046 // Convert to conditional expression

		return modulo > 201 ? 201 : modulo;
	}

	/// <summary>
	/// Check the magic number of a file to see if it is a zip archive
	/// </summary>
	public static bool IsZipArchive(string filePath)
	{
		try {
			Span<byte> fileBytes = stackalloc byte[MagicNumberZip.Length];

			using var file = new FileStream(filePath, FileMode.Open, FileAccess.Read);
			_ = file.Read(fileBytes);

			return fileBytes.SequenceEqual(MagicNumberZip);
		}
		catch (Exception) {
			// some error in the zip file
			return false;
		}
	}

	/// <summary>
	/// Open a file using the default program for that file type
	/// </summary>
	public static void OpenFile(string path, CliOptions options)
	{
		var opener = string.IsNullOrEmpty(options.OpenWith) ? TextFileOpener : options.OpenWith;
		var extension = Path.GetExtension(path).ToLower();

		path = $"\"{path}\""; // the quotes are needed if the path has spaces, in some cases

		if (TextFileTypes.Contains(extension)) {
			_ = Process.Start(opener, path);
		} else if (WordFileTypes.Contains(extension)) {
			_ = Process.Start(pathToWord.Value, path);
		} else if (extension == ".zip") {
			_ = Process.Start("explorer.exe", path);
		} else if (extension == ".pdf") {
			StartAcrobat(path);
		} else {
			// Open file using default program
			//_ = Process.Start(path);
			_ = Process.Start(opener, path);
		}
	}

	/// <summary>
	/// On my system Acrobat doesnt start automatically, so I need this
	/// </summary>
	private static void StartAcrobat(string path)
	{
		if (string.IsNullOrEmpty(AcrobatPath.Value)) {
			_ = Process.Start(path); // fallback
		} else {
			_ = Process.Start(AcrobatPath.Value, path);
		}
	}

	/// <summary>
	/// Use the registry to find the path to MS Word, or a hardcoded path if not found
	/// </summary>
	private static string GetWordPath() => "";

	/// <summary>
	/// Look in the registry to see if we can locate the path to the program
	/// </summary>
	public static string? GetProgramPath(string program) => null;

	/// <summary>
	/// take the provided patterns, and add *.zip if needed. This is used for searching for files outside zips
	/// </summary>
	public static IReadOnlyList<Glob> ProcessOuterPatterns(IReadOnlyList<string> patterns, bool includezips)
	{
		var needzip = false;
		if (includezips) {
			needzip = !patterns.Any(pat => string.Equals(pat, "*.zip", CliOptions.FilenameComparison));
		}

		var results = patterns
			.Select(pat => Glob.Parse(pat))
			.ToList();

		if (needzip) {
			results.Add(Glob.Parse("*.zip"));
		}

		return results;
	}

	/// <summary>
	/// take the provided patterns, and add *.zip if needed. This is used for searching for files outside zips
	/// </summary>
	public static IReadOnlyList<string> ProcessOuterPatternsOld(IList<string> p, bool includezips)
	{
		// *.doc, *.txt => *.doc, *.txt, *.zip

		var haszip = false;
		// duplicate the list of patterns
		var copyPatterns = new List<string>(p.Count + 1);
		foreach (var pattern in p) {
			copyPatterns.Add(pattern);
			if (string.Equals(pattern, "*.zip", CliOptions.FilenameComparison)) {
				haszip = true;
			}
		}

		// if we need to, add the zip pattern
		if (includezips && !haszip) {
			copyPatterns.Add("*.zip");
		}

		return copyPatterns;
	}

	/// <summary>
	/// This is used for searching for files inside zips, and returns globs
	/// </summary>
	public static IReadOnlyList<Glob> ProcessInnerPatterns(IReadOnlyList<string> patterns) =>
		[.. patterns.Where(pat => !pat.EndsWith(".zip", CliOptions.FilenameComparison)).Select(pat => Glob.Parse(pat))];
}
