using DotNet.Globbing;
using Microsoft.Win32;
using System.Diagnostics;

namespace Searcher;

internal class Utils
{
	private static readonly byte[] magicNumberZip = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
	private static readonly string[] textFileTypes = new string[] { ".txt", ".log", ".md", ".cs", ".rs", ".js", ".html" };
	private static readonly string[] wordFileTypes = new string[] { ".docx", ".doc", ".rtf" };
	private static readonly string pathToWord = GetWordPath();

	/// <summary>
	/// Check the magic number of a file to see if it is a zip archive
	/// </summary>
	public static bool IsZipArchive(string filePath)
	{
		try
		{
			var fileBytes = new byte[magicNumberZip.Length];

			using var file = new FileStream(filePath, FileMode.Open, FileAccess.Read);
			file.Read(fileBytes, 0, fileBytes.Length);

			return fileBytes.SequenceEqual(magicNumberZip);
		}
		catch (Exception)
		{
			// some error in the zip file
			return false;
		}
	}

	/// <summary>
	/// Open a file using the default program for that file type
	/// </summary>
	public static void OpenFile(string path)
	{
		var extension = Path.GetExtension(path).ToLower();

		if (textFileTypes.Contains(extension))
			Process.Start("notepad.exe", path);
		else if (wordFileTypes.Contains(extension))
			Process.Start(pathToWord, path);
		else if (extension == ".zip")
			Process.Start("explorer.exe", path);
		else
		{
			// Open file using default program
			Process.Start(path);
		}
	}

	/// <summary>
	/// Use the registry to find the path to MS Word, or a hardcoded path if not found
	/// </summary>
	private static string GetWordPath()
	{
		var keyName = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\Winword.exe";
		var winwordPath = (string?)Registry.GetValue(keyName, "Path", null);
		if (winwordPath == null)
			return "C:\\Program Files\\Microsoft Office\\root\\Office16\\winword.exe";
		else
			return Path.Combine(winwordPath, "Winword.exe");
	}

	/// <summary>
	/// take the provided patterns, and add *.zip if needed. This is used for searching for files outside zips
	/// </summary>
	public static IReadOnlyList<Glob> ProcessOuterPatterns(IList<string> patterns, bool includezips)
	{
		var needzip = false;
		if (includezips)
			needzip = !patterns.Any(pat => string.Equals(pat, "*.zip", CliOptions.FilenameComparison));

		var results = patterns
			.Select(pat => Glob.Parse(pat))
			.ToList();

		if (needzip)
			results.Add(Glob.Parse("*.zip"));

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
		foreach (var pattern in p)
		{
			copyPatterns.Add(pattern);
			if (string.Equals(pattern, "*.zip", CliOptions.FilenameComparison)) haszip = true;
		}

		// if we need to, add the zip pattern
		if (includezips && !haszip)
			copyPatterns.Add("*.zip");

		return copyPatterns;
	}
	/// <summary>
	/// This is used for searching for files inside zips, and returns globs
	/// </summary>
	public static IReadOnlyList<Glob> ProcessInnerPatterns(IList<string> patterns)
	{
		return patterns.Where(pat => !pat.EndsWith(".zip", CliOptions.FilenameComparison))
			.Select(pat => Glob.Parse(pat))
			.ToList();
	}
}

/// <summary>
/// Extension methods for List Views
/// </summary>
public static class ListViewExtensions
{
	// https://stackoverflow.com/questions/442817/c-sharp-flickering-listview-on-update

	/// <summary>
	/// Sets the double buffered property of a list view to the specified value
	/// </summary>
	/// <param name="listView">The List view</param>
	/// <param name="doubleBuffered">Double Buffered or not</param>
	public static void SetDoubleBuffered(this System.Windows.Forms.ListView listView, bool doubleBuffered = true) =>
		listView?.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(listView, doubleBuffered, null);
}
