namespace SearcherCore;

using System.Diagnostics;
using System.Linq;
using DotNet.Globbing;
using Microsoft.Win32;

/// <summary>
/// Utility functions for file operations and pattern processing
/// </summary>
public static class Utils
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
		ArgumentNullException.ThrowIfNull(options);
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
	/// Returns empty string on non-Windows platforms
	/// </summary>
	private static string GetWordPath()
	{
		// Return empty string on non-Windows platforms
		if (!OperatingSystem.IsWindows()) {
			return "";
		}

		const string keyName = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\Winword.exe";
		var winwordPath = (string?)Registry.GetValue(keyName, "Path", null);
		return winwordPath == null
			? "C:\\Program Files\\Microsoft Office\\root\\Office16\\winword.exe"
			: Path.Combine(winwordPath, "Winword.exe");
	}

	/// <summary>
	/// Look in the registry to see if we can locate the path to the program
	/// Returns null on non-Windows platforms
	/// </summary>
	public static string? GetProgramPath(string program)
	{
		// Return null on non-Windows platforms
		if (!OperatingSystem.IsWindows()) {
			return null;
		}

		var keyName = $"HKEY_CLASSES_ROOT\\Applications\\{program}\\shell\\Open\\command";
		var appPath = (string?)Registry.GetValue(keyName, null, null);

		if (appPath == null) {
			return null;
		}

		// may be something like
		// "C:\Program Files\Adobe\Acrobat DC\Acrobat\Acrobat.exe" "%1"
		// so we need to remove the quotes and the "%1"

		appPath = appPath.Replace("\"", "")
			.Replace("%1", "")
			.Trim();

		return !File.Exists(appPath) ? null : appPath;
	}

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
		ArgumentNullException.ThrowIfNull(p);
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

	/// <summary>
	/// Validates that a file path is safe and doesn't contain path traversal attacks
	/// </summary>
	public static bool IsValidFilePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path)) {
			return false;
		}

		try {
			// Check for path traversal attempts in the original path
			if (path.Contains("..")) {
				return false;
			}

			// Check for path traversal attempts
			var normalizedPath = Path.GetFullPath(path);

			// Check for dangerous path patterns
			var fileName = Path.GetFileName(normalizedPath);
			if (fileName.StartsWith("..") || fileName.Contains("..")) {
				return false;
			}

			// Check for reserved Windows names (even on other platforms for consistency)
			var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
			string[] reservedNames = ["CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"];

			return !reservedNames.Contains(nameWithoutExtension, StringComparer.OrdinalIgnoreCase);
		}
		catch {
			return false;
		}
	}

	/// <summary>
	/// Validates and normalizes a search folder path
	/// </summary>
	public static string? ValidateSearchPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path)) {
			return null;
		}

		try {
			var normalizedPath = Path.GetFullPath(path);
			if (!Directory.Exists(normalizedPath)) {
				return null;
			}

			// Return normalized path as-is for consistency with Path.GetFullPath()
			return normalizedPath;
		}
		catch {
			return null;
		}
	}
}
