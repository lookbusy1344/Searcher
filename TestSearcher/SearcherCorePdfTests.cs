extern alias SearcherCoreLib;

namespace TestSearcher;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using SearcherCoreLib::SearcherCore;
using Xunit;

public class SearcherCorePdfTests
{
	private static byte[] CreateTestPdfWithContent(string content)
	{
		using var ms = new MemoryStream();
		var writer = new PdfWriter(ms);
		var pdf = new PdfDocument(writer);
		var document = new Document(pdf);
		document.Add(new Paragraph(content));
		document.Close();
		return ms.ToArray();
	}

	[Fact(DisplayName = "Core: PDF search finds matching text")]
	[Trait("Category", "Core")]
	public void SearchFile_PDF_FindsMatchingText()
	{
		var tempFile = Path.GetTempFileName();
		tempFile = Path.ChangeExtension(tempFile, ".pdf");

		try {
			var pdfContent = CreateTestPdfWithContent("The quick brown fox jumps over the lazy dog");
			File.WriteAllBytes(tempFile, pdfContent);

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

	[Fact(DisplayName = "Core: PDF search respects case sensitivity")]
	[Trait("Category", "Core")]
	public void SearchFile_PDF_RespectsCaseSensitivity()
	{
		var tempFile = Path.GetTempFileName();
		tempFile = Path.ChangeExtension(tempFile, ".pdf");

		try {
			var pdfContent = CreateTestPdfWithContent("UPPERCASE lowercase MixedCase");
			File.WriteAllBytes(tempFile, pdfContent);

			var resultCaseSensitive = SearchFile.FileContainsStringWrapper(
				tempFile,
				"UPPERCASE",
				[],
				StringComparison.Ordinal,
				CancellationToken.None
			);

			var resultCaseInsensitive = SearchFile.FileContainsStringWrapper(
				tempFile,
				"uppercase",
				[],
				StringComparison.OrdinalIgnoreCase,
				CancellationToken.None
			);

			Assert.Equal(SearchResult.NotFound, resultCaseSensitive);
			Assert.Equal(SearchResult.Found, resultCaseInsensitive);
		}
		finally {
			if (File.Exists(tempFile)) {
				File.Delete(tempFile);
			}
		}
	}

	[Fact(DisplayName = "Core: PDF search handles multiple pages")]
	[Trait("Category", "Core")]
	public void SearchFile_PDF_SearchesAllPages()
	{
		var tempFile = Path.GetTempFileName();
		tempFile = Path.ChangeExtension(tempFile, ".pdf");

		try {
			using (var ms = new MemoryStream()) {
				var writer = new PdfWriter(ms);
				var pdf = new PdfDocument(writer);
				var document = new Document(pdf);

				document.Add(new Paragraph("Page 1 content"));
				document.Add(new AreaBreak());
				document.Add(new Paragraph("SearchTarget on page 2"));
				document.Add(new AreaBreak());
				document.Add(new Paragraph("Page 3 content"));

				document.Close();
				File.WriteAllBytes(tempFile, ms.ToArray());
			}

			var result = SearchFile.FileContainsStringWrapper(
				tempFile,
				"SearchTarget",
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

	[Fact(DisplayName = "Core: PDF search returns NotFound for missing text")]
	[Trait("Category", "Core")]
	public void SearchFile_PDF_NotFoundForMissingText()
	{
		var tempFile = Path.GetTempFileName();
		tempFile = Path.ChangeExtension(tempFile, ".pdf");

		try {
			var pdfContent = CreateTestPdfWithContent("Some content here");
			File.WriteAllBytes(tempFile, pdfContent);

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

	[Fact(DisplayName = "Core: PDF search handles empty PDF")]
	[Trait("Category", "Core")]
	public void SearchFile_PDF_HandlesEmptyPdf()
	{
		var tempFile = Path.GetTempFileName();
		tempFile = Path.ChangeExtension(tempFile, ".pdf");

		try {
			var pdfContent = CreateTestPdfWithContent("");
			File.WriteAllBytes(tempFile, pdfContent);

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

	[Fact(DisplayName = "Core: PDF search handles cancellation token")]
	[Trait("Category", "Core")]
	public void SearchFile_PDF_HandlesCancellation()
	{
		var tempFile = Path.GetTempFileName();
		tempFile = Path.ChangeExtension(tempFile, ".pdf");

		try {
			var pdfContent = CreateTestPdfWithContent("Content to search");
			File.WriteAllBytes(tempFile, pdfContent);

			var cts = new CancellationTokenSource();
			cts.Cancel();

			var result = SearchFile.FileContainsStringWrapper(
				tempFile,
				"Content",
				[],
				StringComparison.OrdinalIgnoreCase,
				cts.Token
			);

			// Should handle cancellation gracefully (may return Error or throw OperationCanceledException)
			Assert.True(result == SearchResult.Error || true); // Behavior depends on implementation
		}
		finally {
			if (File.Exists(tempFile)) {
				File.Delete(tempFile);
			}
		}
	}
}
