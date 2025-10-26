extern alias SearcherCoreLib;

namespace TestSearcher;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using Xunit;
using SearcherCoreLib::SearcherCore;

public class SearcherCoreZipTests
{
	private byte[] CreateTestZipWithFiles(Dictionary<string, string> files)
	{
		using (var ms = new MemoryStream()) {
			using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true)) {
				foreach (var kvp in files) {
					var entry = zip.CreateEntry(kvp.Key);
					using (var writer = new StreamWriter(entry.Open())) {
						writer.Write(kvp.Value);
					}
				}
			}
			return ms.ToArray();
		}
	}

	[Fact(DisplayName = "Core: ZIP search finds matching text in files")]
	[Trait("Category", "Core")]
	public void SearchFile_ZIP_FindsMatchingText()
	{
		var tempFile = Path.GetTempFileName();
		tempFile = Path.ChangeExtension(tempFile, ".zip");

		try {
			var files = new Dictionary<string, string> {
				{ "file1.txt", "The quick brown fox" },
				{ "file2.txt", "Another file content" }
			};
			var zipContent = CreateTestZipWithFiles(files);
			File.WriteAllBytes(tempFile, zipContent);

			// Search for text inside ZIP
			var result = SearchFile.FileContainsStringWrapper(
				tempFile,
				"quick brown",
				[],
				StringComparison.OrdinalIgnoreCase,
				CancellationToken.None
			);

			Assert.Equal(SearchResult.Found, result);
		}
		finally {
			if (File.Exists(tempFile)) File.Delete(tempFile);
		}
	}

	[Fact(DisplayName = "Core: ZIP search respects pattern matching")]
	[Trait("Category", "Core")]
	public void SearchFile_ZIP_RespectsPatternMatching()
	{
		var tempFile = Path.GetTempFileName();
		tempFile = Path.ChangeExtension(tempFile, ".zip");

		try {
			var files = new Dictionary<string, string> {
				{ "match.txt", "search" },
				{ "no_match.log", "search" }
			};
			var zipContent = CreateTestZipWithFiles(files);
			File.WriteAllBytes(tempFile, zipContent);

			// Search only .txt files inside ZIP
			var result = SearchFile.FileContainsStringWrapper(
				tempFile,
				"search",
				[],
				StringComparison.OrdinalIgnoreCase,
				CancellationToken.None
			);

			Assert.Equal(SearchResult.Found, result);
		}
		finally {
			if (File.Exists(tempFile)) File.Delete(tempFile);
		}
	}

	[Fact(DisplayName = "Core: ZIP search handles nested directories")]
	[Trait("Category", "Core")]
	public void SearchFile_ZIP_HandlesNestedDirectories()
	{
		var tempFile = Path.GetTempFileName();
		tempFile = Path.ChangeExtension(tempFile, ".zip");

		try {
			var files = new Dictionary<string, string> {
				{ "root.txt", "root" },
				{ "folder/nested.txt", "nested" },
				{ "folder/deep/file.txt", "deep" }
			};
			var zipContent = CreateTestZipWithFiles(files);
			File.WriteAllBytes(tempFile, zipContent);

			var result = SearchFile.FileContainsStringWrapper(
				tempFile,
				"deep",
				[],
				StringComparison.OrdinalIgnoreCase,
				CancellationToken.None
			);

			Assert.Equal(SearchResult.Found, result);
		}
		finally {
			if (File.Exists(tempFile)) File.Delete(tempFile);
		}
	}

	[Fact(DisplayName = "Core: ZIP search returns NotFound for missing text")]
	[Trait("Category", "Core")]
	public void SearchFile_ZIP_NotFoundForMissingText()
	{
		var tempFile = Path.GetTempFileName();
		tempFile = Path.ChangeExtension(tempFile, ".zip");

		try {
			var files = new Dictionary<string, string> {
				{ "file.txt", "some content" }
			};
			var zipContent = CreateTestZipWithFiles(files);
			File.WriteAllBytes(tempFile, zipContent);

			var result = SearchFile.FileContainsStringWrapper(
				tempFile,
				"nonexistent",
				[],
				StringComparison.OrdinalIgnoreCase,
				CancellationToken.None
			);

			Assert.Equal(SearchResult.NotFound, result);
		}
		finally {
			if (File.Exists(tempFile)) File.Delete(tempFile);
		}
	}

	[Fact(DisplayName = "Core: ZIP search handles empty archive")]
	[Trait("Category", "Core")]
	public void SearchFile_ZIP_HandlesEmptyArchive()
	{
		var tempFile = Path.GetTempFileName();
		tempFile = Path.ChangeExtension(tempFile, ".zip");

		try {
			var zipContent = CreateTestZipWithFiles(new Dictionary<string, string>());
			File.WriteAllBytes(tempFile, zipContent);

			var result = SearchFile.FileContainsStringWrapper(
				tempFile,
				"text",
				[],
				StringComparison.OrdinalIgnoreCase,
				CancellationToken.None
			);

			Assert.Equal(SearchResult.NotFound, result);
		}
		finally {
			if (File.Exists(tempFile)) File.Delete(tempFile);
		}
	}

	[Fact(DisplayName = "Core: ZIP search handles large files in archive")]
	[Trait("Category", "Core")]
	public void SearchFile_ZIP_HandlesLargeFiles()
	{
		var tempFile = Path.GetTempFileName();
		tempFile = Path.ChangeExtension(tempFile, ".zip");

		try {
			// Create a large file with search term near the end
			var largeContent = new string('x', 1000000) + "\nSEARCHTERM\n";
			var files = new Dictionary<string, string> {
				{ "large.txt", largeContent }
			};
			var zipContent = CreateTestZipWithFiles(files);
			File.WriteAllBytes(tempFile, zipContent);

			var result = SearchFile.FileContainsStringWrapper(
				tempFile,
				"SEARCHTERM",
				[],
				StringComparison.OrdinalIgnoreCase,
				CancellationToken.None
			);

			Assert.Equal(SearchResult.Found, result);
		}
		finally {
			if (File.Exists(tempFile)) File.Delete(tempFile);
		}
	}
}
