namespace Searcher;

using System.IO.Compression;
using System.Xml;
using DotNet.Globbing;

internal static class SearchFile
{
	/// <summary>
	/// Wrapper to pick the correct search function. Special cases for docx, pdf and zip files
	/// </summary>
	public static SearchResult FileContainsStringWrapper(string path, string text, IReadOnlyList<Glob> innerpatterns, StringComparison comparer, CancellationToken token)
	{
#pragma warning disable IDE0046 // Convert to conditional expression
		if (path.EndsWith(".docx", CliOptions.FilenameComparison)) {
			return DocxContainsString(path, text, comparer);
		}

		if (path.EndsWith(".pdf", CliOptions.FilenameComparison)) {
			return PdfCheck.CheckPdfFile(path, text, comparer, token);
		}
#pragma warning restore IDE0046 // Convert to conditional expression

		return path.EndsWith(".zip", CliOptions.FilenameComparison) || Utils.IsZipArchive(path)
			? CheckZipFile(path, text, innerpatterns, comparer, token)
			: FileContainsString(path, text, comparer);
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
		catch {
			// exceptions are not thrown by my code here, but potentially by the StreamReader
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

		if (!path.EndsWith(".docx", CliOptions.FilenameComparison)) {
			throw new Exception("Not a docx file");
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
		catch {
			// exceptions are not thrown by my code here, but potentially by libraries
			return SearchResult.Error;
		}

		return SearchResult.NotFound;
	}

	/// <summary>
	/// Wrapper around zip search to handle nested zips
	/// </summary>
	private static SearchResult CheckZipFile(string path, string text, IReadOnlyList<Glob> innerpatterns, StringComparison comparer, CancellationToken token)
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
		catch {
			return SearchResult.Error;
		}
	}
}

internal static class ZipInternals
{
	/// <summary>
	/// Given a zip archive, loop through and check the contents. Recursively calls for nested zips
	/// </summary>
	public static bool RecursiveArchiveCheck(ZipArchive archive, string text, IReadOnlyList<Glob> innerpatterns, StringComparison comparer, CancellationToken token)
	{
		foreach (var nestedEntry in archive.Entries) {
			// loop through all entries in the nested zip file
			token.ThrowIfCancellationRequested();

			var found = false;

			if (nestedEntry.FullName.EndsWith(".zip", CliOptions.FilenameComparison)) {
				// its another nested zip file, we need to open it and search inside
				using var nestedStream = nestedEntry.Open();
				using var nestedArchive = new ZipArchive(nestedStream);
				found = RecursiveArchiveCheck(nestedArchive, text, innerpatterns, comparer, token);
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
				//Debug.WriteLine($"Checking {nestedEntry.Name}");

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
