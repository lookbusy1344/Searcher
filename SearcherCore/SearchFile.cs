namespace SearcherCore;

using System.IO.Compression;
using System.Xml;
using DotNet.Globbing;

/// <summary>
/// File content searching functionality
/// </summary>
public static class SearchFile
{
	private static readonly Dictionary<string, Func<string, string, IReadOnlyList<Glob>, StringComparison, CancellationToken, SearchResult>>
		FileHandlers = new(StringComparer.OrdinalIgnoreCase) {
			{ ".docx", (path, text, _, comparer, _) => DocxContainsString(path, text, comparer) },
			{ ".pdf", (path, text, _, comparer, token) => PdfCheck.CheckPdfFile(path, text, comparer, token) },
			{ ".zip", (path, text, innerpatterns, comparer, token) => CheckZipFile(path, text, innerpatterns, comparer, token) }
		};

	/// <summary>
	/// Wrapper to pick the correct search function. Special cases for docx, pdf and zip files
	/// </summary>
	public static SearchResult FileContainsStringWrapper(string path, string text, IReadOnlyList<Glob> innerpatterns, StringComparison comparer,
		CancellationToken token)
	{
		ArgumentNullException.ThrowIfNull(path);

		var extension = Path.GetExtension(path);

		// Check for specialized handlers first
		if (FileHandlers.TryGetValue(extension, out var handler)) {
			return handler(path, text, innerpatterns, comparer, token);
		}

		// Check for ZIP files by magic number (when extension isn't .zip)
		if (Utils.IsZipArchive(path)) {
			return CheckZipFile(path, text, innerpatterns, comparer, token);
		}

		// Default to general text search
		return FileContainsString(path, text, comparer);
	}

	/// <summary>
	/// General function to check if a file contains a given string
	/// </summary>
	private static SearchResult FileContainsString(string path, string text, StringComparison comparer)
	{
		if (string.IsNullOrEmpty(text)) {
			return SearchResult.NotFound;
		}

		try {
			using var file = new StreamReader(path);
			while (!file.EndOfStream) {
				var line = file.ReadLine();
				if (line == null) {
					continue;
				}

				if (line.Contains(text, comparer)) {
					return SearchResult.Found;
				}
			}
		}
		catch (Exception ex) {
			// Log exception for debugging - exceptions from StreamReader (IOException, UnauthorizedAccessException, etc.)
#if DEBUG
			System.Diagnostics.Debug.WriteLine($"Error reading file {path}: {ex.GetType().Name} - {ex.Message}");
#endif
			return SearchResult.Error;
		}

		return SearchResult.NotFound;
	}

	/// <summary>
	/// Function to check if a docx contains a given string
	/// </summary>
	private static SearchResult DocxContainsString(string path, string text, StringComparison comparer)
	{
		if (string.IsNullOrEmpty(text)) {
			return SearchResult.NotFound;
		}

		try {
			using var archive = ZipFile.OpenRead(path);
			var documentEntry = archive.GetEntry("word/document.xml");
			if (documentEntry == null) {
				return SearchResult.NotFound;
			}

			using var stream = documentEntry.Open();
			using var reader = XmlReader.Create(stream);

			while (reader.Read()) {
				if (reader.NodeType == XmlNodeType.Text && reader.Value.Contains(text, comparer)) {
					return SearchResult.Found;
				}
			}
		}
		catch (Exception ex) {
			// Log exception for debugging - exceptions from ZIP/XML libraries
#if DEBUG
			System.Diagnostics.Debug.WriteLine($"Error reading DOCX file {path}: {ex.GetType().Name} - {ex.Message}");
#endif
			return SearchResult.Error;
		}

		return SearchResult.NotFound;
	}

	/// <summary>
	/// Wrapper around zip search to handle nested zips
	/// </summary>
	private static SearchResult CheckZipFile(string path, string text, IReadOnlyList<Glob> innerpatterns, StringComparison comparer,
		CancellationToken token)
	{
		if (string.IsNullOrEmpty(text)) {
			return SearchResult.NotFound;
		}

		try {
			// this is an actual zip file, so open it as archive and then use the recursive function

			using var archive = ZipFile.OpenRead(path);
			return ZipInternals.RecursiveArchiveCheck(archive, text, innerpatterns, comparer, token) ? SearchResult.Found : SearchResult.NotFound;
		}
		catch (OperationCanceledException) {
			// this is thrown when the user cancels the search
			return SearchResult.NotFound;
		}
		catch (Exception ex) {
			// Log exception for debugging - exceptions from ZIP library
#if DEBUG
			System.Diagnostics.Debug.WriteLine($"Error reading ZIP file {path}: {ex.GetType().Name} - {ex.Message}");
#endif
			return SearchResult.Error;
		}
	}
}

/// <summary>
/// Internal ZIP file processing functionality
/// </summary>
public static class ZipInternals
{
	/// <summary>
	/// Maximum depth for nested ZIP files to prevent stack overflow from malicious or corrupted archives
	/// </summary>
	private const int MaxZipNestingDepth = 10;

	/// <summary>
	/// Given a zip archive, loop through and check the contents. Recursively calls for nested zips
	/// </summary>
	/// <param name="archive">The ZIP archive to search</param>
	/// <param name="text">Text to search for</param>
	/// <param name="innerpatterns">File patterns to match</param>
	/// <param name="comparer">String comparison method</param>
	/// <param name="token">Cancellation token</param>
	/// <param name="currentDepth">Current recursion depth (default 0)</param>
	/// <returns>True if text found, false otherwise</returns>
	public static bool RecursiveArchiveCheck(ZipArchive archive, string text, IReadOnlyList<Glob> innerpatterns, StringComparison comparer,
		CancellationToken token, int currentDepth = 0)
	{
		ArgumentNullException.ThrowIfNull(archive);
		ArgumentNullException.ThrowIfNull(innerpatterns);

		// Prevent excessive nesting that could cause stack overflow or memory exhaustion
		if (currentDepth >= MaxZipNestingDepth) {
#if DEBUG
			System.Diagnostics.Debug.WriteLine($"Maximum ZIP nesting depth ({MaxZipNestingDepth}) exceeded");
#endif
			return false;
		}

		foreach (var nestedEntry in archive.Entries) {
			// loop through all entries in the nested zip file
			token.ThrowIfCancellationRequested();

			var found = false;

			if (nestedEntry.FullName.EndsWith(".zip", CliOptions.FilenameComparison)) {
				// its another nested zip file, we need to open it and search inside
				using var nestedStream = nestedEntry.Open();
				using var nestedArchive = new ZipArchive(nestedStream);
				found = RecursiveArchiveCheck(nestedArchive, text, innerpatterns, comparer, token, currentDepth + 1);
			} else if (nestedEntry.FullName.EndsWith(".docx", CliOptions.FilenameComparison)) {
				// this is a DOCX inside a zip
				using var nestedStream = nestedEntry.Open();
				using var nestedArchive = new ZipArchive(nestedStream);
				found = DocxContainsString(nestedArchive, text, comparer);
			} else if (nestedEntry.FullName.EndsWith(".pdf", CliOptions.FilenameComparison)) {
				// this is a PDF inside a zip
				found = PdfCheck.CheckStream(nestedEntry, text, comparer, token);
			} else if (nestedEntry.FullName.EndsWith('/')) {
				// its a folder, we can skip it
				continue;
			} else {
				// this is an actual file, not a nested zip
				// This fails when using FullName, because the '/' separator screws up the globbing
				if (innerpatterns.Count == 0 || innerpatterns.Any(p => p.IsMatch(nestedEntry.Name))) {
					found = GeneralContainsString(nestedEntry, text, comparer, token);
				}
			}

			if (found) {
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Function to check if a docx inside ZIP contains a given string
	/// </summary>
	public static bool DocxContainsString(ZipArchive archive, string text, StringComparison comparer)
	{
		ArgumentNullException.ThrowIfNull(archive);
		var documentEntry = archive.GetEntry("word/document.xml");
		if (documentEntry == null) {
			return false;
		}

		using var stream = documentEntry.Open();
		using var reader = XmlReader.Create(stream);

		while (reader.Read()) {
			if (reader.NodeType == XmlNodeType.Text && reader.Value.Contains(text, comparer)) {
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Check an entry in a zip file for a given string
	/// </summary>
	public static bool GeneralContainsString(ZipArchiveEntry entry, string text, StringComparison comparer, CancellationToken token)
	{
		ArgumentNullException.ThrowIfNull(entry);
		using var stream = entry.Open();
		using var file = new StreamReader(stream);
		while (!file.EndOfStream) {
			var line = file.ReadLine();
			if (line == null) {
				continue;
			}

			if (line.Contains(text, comparer)) {
				return true;
			}

			token.ThrowIfCancellationRequested();
		}

		return false;
	}
}
