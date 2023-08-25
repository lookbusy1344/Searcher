using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Searcher;

internal partial class PdfCheck
{
	/// <summary>
	/// Check inside a PDF zip entry for a string
	/// </summary>
	public static bool CheckStream(ZipArchiveEntry entry, string content, StringComparison strcomp, CancellationToken token)
	{
		using var stream = entry.Open();
		using var reader = new PdfReader(stream);

		var result = SearchPdfInternal(reader, content, strcomp, token);

		if (result == SearchResult.Error) throw new Exception("Error reading PDF file");
		return result == SearchResult.Found;
	}

	/// <summary>
	/// Search inside a PDF file for a string
	/// </summary>
	public static SearchResult CheckPdfFile(string path, string content, StringComparison strcomp, CancellationToken token)
	{
		try
		{
			using var pdfReader = new PdfReader(path);
			return SearchPdfInternal(pdfReader, content, strcomp, token);
		}
		catch (OperationCanceledException)
		{
			return SearchResult.NotFound;
		}
		catch
		{
			return SearchResult.Error;
		}
	}

	/// <summary>
	/// Internal helper to search inside a PdfReader object (file or stream) for a string
	/// </summary>
	private static SearchResult SearchPdfInternal(PdfReader reader, string content, StringComparison strcomp, CancellationToken token)
	{
		var containsspace = content.Contains(' ', strcomp);

		using var pdfDoc = new PdfDocument(reader);
		var pages = pdfDoc.GetNumberOfPages();

		for (var i = 1; i <= pages; ++i)
		{
			// get the page text
			var page = pdfDoc.GetPage(i);
			var text = PdfTextExtractor.GetTextFromPage(page);

			// if we are searching for a string with spaces, replace all whitespace with a single space
			if (containsspace)
				text = AnyNumberWhitespace().Replace(text, " ");

			// does the page text contain our search string?
			if (text.Contains(content, strcomp)) return SearchResult.Found;

			token.ThrowIfCancellationRequested();
		}
		return SearchResult.NotFound;
	}

	/// <summary>
	/// Compiled regex for any number of whitespace characters.
	/// </summary>
	[GeneratedRegex("\\s+")]
	private static partial Regex AnyNumberWhitespace();
}
