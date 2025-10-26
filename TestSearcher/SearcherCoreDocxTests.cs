extern alias SearcherCoreLib;

namespace TestSearcher;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Xml.Linq;
using SearcherCoreLib::SearcherCore;
using Xunit;

public class SearcherCoreDocxTests
{
	private static byte[] CreateTestDocxWithContent(string content)
	{
		// DOCX is a ZIP file containing XML files
		using var ms = new MemoryStream();
		using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true)) {
			// Create [Content_Types].xml
			var contentTypes = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
	<Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
	<Default Extension=""xml"" ContentType=""application/xml""/>
	<Override PartName=""/word/document.xml"" ContentType=""application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml""/>
</Types>";

			var ctEntry = zip.CreateEntry("[Content_Types].xml");
			using (var writer = new StreamWriter(ctEntry.Open())) {
				writer.Write(contentTypes);
			}

			// Create _rels/.rels
			var rels = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
	<Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""word/document.xml""/>
</Relationships>";

			zip.CreateEntry("_rels/.rels");
			var relsEntry = zip.CreateEntry("_rels/.rels");
			using (var writer = new StreamWriter(relsEntry.Open())) {
				writer.Write(rels);
			}

			// Create word/document.xml with the actual content
			var docXml = $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<w:document xmlns:w=""http://schemas.openxmlformats.org/wordprocessingml/2006/main"">
	<w:body>
		<w:p>
			<w:r>
				<w:t>{XmlEscape(content)}</w:t>
			</w:r>
		</w:p>
	</w:body>
</w:document>";

			var docEntry = zip.CreateEntry("word/document.xml");
			using (var writer = new StreamWriter(docEntry.Open())) {
				writer.Write(docXml);
			}
		}
		return ms.ToArray();
	}

	private static string XmlEscape(string text)
	{
		return System.Security.SecurityElement.Escape(text);
	}

	[Fact(DisplayName = "Core: DOCX search finds matching text")]
	[Trait("Category", "Core")]
	public void SearchFile_DOCX_FindsMatchingText()
	{
		var tempFile = Path.GetTempFileName();
		tempFile = Path.ChangeExtension(tempFile, ".docx");

		try {
			var docxContent = CreateTestDocxWithContent("The quick brown fox jumps");
			File.WriteAllBytes(tempFile, docxContent);

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
			if (File.Exists(tempFile)) {
				File.Delete(tempFile);
			}
		}
	}

	[Fact(DisplayName = "Core: DOCX search respects case sensitivity")]
	[Trait("Category", "Core")]
	public void SearchFile_DOCX_RespectsCaseSensitivity()
	{
		var tempFile = Path.GetTempFileName();
		tempFile = Path.ChangeExtension(tempFile, ".docx");

		try {
			var docxContent = CreateTestDocxWithContent("SearchTerm in document");
			File.WriteAllBytes(tempFile, docxContent);

			var resultCaseSensitive = SearchFile.FileContainsStringWrapper(
				tempFile,
				"SearchTerm",
				[],
				StringComparison.Ordinal,
				CancellationToken.None
			);

			var resultCaseInsensitive = SearchFile.FileContainsStringWrapper(
				tempFile,
				"searchterm",
				[],
				StringComparison.OrdinalIgnoreCase,
				CancellationToken.None
			);

			Assert.Equal(SearchResult.Found, resultCaseSensitive);
			Assert.Equal(SearchResult.Found, resultCaseInsensitive);
		}
		finally {
			if (File.Exists(tempFile)) {
				File.Delete(tempFile);
			}
		}
	}

	[Fact(DisplayName = "Core: DOCX search returns NotFound for missing text")]
	[Trait("Category", "Core")]
	public void SearchFile_DOCX_NotFoundForMissingText()
	{
		var tempFile = Path.GetTempFileName();
		tempFile = Path.ChangeExtension(tempFile, ".docx");

		try {
			var docxContent = CreateTestDocxWithContent("Some document content");
			File.WriteAllBytes(tempFile, docxContent);

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
			if (File.Exists(tempFile)) {
				File.Delete(tempFile);
			}
		}
	}

	[Fact(DisplayName = "Core: DOCX search handles empty content")]
	[Trait("Category", "Core")]
	public void SearchFile_DOCX_HandlesEmptyContent()
	{
		var tempFile = Path.GetTempFileName();
		tempFile = Path.ChangeExtension(tempFile, ".docx");

		try {
			var docxContent = CreateTestDocxWithContent("");
			File.WriteAllBytes(tempFile, docxContent);

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
			if (File.Exists(tempFile)) {
				File.Delete(tempFile);
			}
		}
	}

	[Fact(DisplayName = "Core: DOCX search handles special characters")]
	[Trait("Category", "Core")]
	public void SearchFile_DOCX_HandlesSpecialCharacters()
	{
		var tempFile = Path.GetTempFileName();
		tempFile = Path.ChangeExtension(tempFile, ".docx");

		try {
			var specialContent = "Email: test@example.com & C++ code";
			var docxContent = CreateTestDocxWithContent(specialContent);
			File.WriteAllBytes(tempFile, docxContent);

			var result = SearchFile.FileContainsStringWrapper(
				tempFile,
				"test@example.com",
				[],
				StringComparison.OrdinalIgnoreCase,
				CancellationToken.None
			);

			Assert.Equal(SearchResult.Found, result);
		}
		finally {
			if (File.Exists(tempFile)) {
				File.Delete(tempFile);
			}
		}
	}
}
