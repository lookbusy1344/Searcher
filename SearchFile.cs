using DotNet.Globbing;
using System.IO.Compression;
using System.Xml;

namespace Searcher;

internal class SearchFile
{
	/// <summary>
	/// Wrapper to pick the correct search function
	/// </summary>
	public static bool FileContainsStringWrapper(string path, string text, IReadOnlyList<Glob> innerpatterns, StringComparison comparer, CancellationToken token)
	{
		if (path.EndsWith(".docx", CliOptions.FilenameComparison))
			return DocxContainsString(path, text, comparer);
		if (path.EndsWith(".zip", CliOptions.FilenameComparison) || Utils.IsZipArchive(path))
			return ZipContainsString(path, text, innerpatterns, comparer, token);

		return FileContainsString(path, text, comparer);
	}

	/// <summary>
	/// General function to check if a file contains a given string
	/// </summary>
	private static bool FileContainsString(string path, string text, StringComparison comparer)
	{
		if (string.IsNullOrEmpty(text)) return false;

		using var file = new StreamReader(path);
		while (!file.EndOfStream)
		{
			var line = file.ReadLine();
			if (line == null) continue;
			if (line.Contains(text, comparer)) return true;
		}

		return false;
	}

	/// <summary>
	/// Function to check if a docx contains a given string
	/// </summary>
	private static bool DocxContainsString(string path, string text, StringComparison comparer)
	{
		if (string.IsNullOrEmpty(text)) return false;
		if (!path.EndsWith(".docx", CliOptions.FilenameComparison)) throw new Exception("Not a docx file");

		using var archive = ZipFile.OpenRead(path);
		var documentEntry = archive.GetEntry("word/document.xml");
		if (documentEntry == null) return false;

		using var stream = documentEntry.Open();
		using var reader = XmlReader.Create(stream);

		while (reader.Read())
		{
			if (reader.NodeType == XmlNodeType.Text && reader.Value.Contains(text, comparer))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Wrapper around zip search to handle nested zips
	/// </summary>
	private static bool ZipContainsString(string path, string text, IReadOnlyList<Glob> innerpatterns, StringComparison comparer, CancellationToken token)
	{
		if (string.IsNullOrEmpty(text)) return false;
		try
		{
			// this is an actual zip file, so open it as archive and then use the recursive function

			using var archive = ZipFile.OpenRead(path);
			return RecursiveArchiveCheck(archive, text, innerpatterns, comparer, token);
		}
		catch (Exception)
		{
			// some error in the zip file, or a cancellation
		}

		return false;
	}

	/// <summary>
	/// Given a zip archive, loop through and check the contents. Recursively calls for nested zips
	/// </summary>
	private static bool RecursiveArchiveCheck(ZipArchive archive, string text, IReadOnlyList<Glob> innerpatterns, StringComparison comparer, CancellationToken token)
	{
		foreach (var nestedEntry in archive.Entries)
		{
			// loop through all entries in the nested zip file
			if (token.IsCancellationRequested) throw new Exception("Cancelled");

			var found = false;

			if (nestedEntry.FullName.EndsWith(".zip", CliOptions.FilenameComparison))
			{
				// its another nested zip file, we need to open it and search inside
				using var nestedArchive = new ZipArchive(nestedEntry.Open());
				found = RecursiveArchiveCheck(nestedArchive, text, innerpatterns, comparer, token);
			}
			else if (nestedEntry.FullName.EndsWith('/'))
			{
				// its a folder, we can skip it
				continue;
			}
			else
			{
				// this is an actual file, not a nested zip
				//Debug.WriteLine($"Checking {nestedEntry.Name}");

				// This fails when using FullName, because the '/' separator screws up the globbing
				if (innerpatterns.Count == 0 || innerpatterns.Any(p => p.IsMatch(nestedEntry.Name)))
					found = ArchiveEntryContainsString(nestedEntry, text, comparer, token);
			}

			if (found) return true;
		}

		return false;
	}

	/// <summary>
	/// Check an entry in a zip file for a given string
	/// </summary>
	private static bool ArchiveEntryContainsString(ZipArchiveEntry entry, string text, StringComparison comparer, CancellationToken token)
	{
		using var stream = entry.Open();
		using var file = new StreamReader(stream);
		while (!file.EndOfStream)
		{
			var line = file.ReadLine();
			if (line == null) continue;
			if (line.Contains(text, comparer)) return true;

			if (token.IsCancellationRequested) throw new Exception("Cancelled");
		}

		return false;
	}
}
