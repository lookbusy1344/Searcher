using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text.RegularExpressions;

namespace Searcher;

internal partial class PdfCheck
{
	/// <summary>
	/// Search inside a PDF file for a string
	/// </summary>
	public static bool CheckPdfForContent(string path, string content, StringComparison strcomp)
	{
		var containsspace = content.Contains(' ', strcomp);

		using var pdfReader = new PdfReader(path);
		using var pdfDoc = new PdfDocument(pdfReader);
		// var strategy = new LocationTextExtractionStrategy();
		// var strategy = new SimpleTextExtractionStrategy();

		var pages = pdfDoc.GetNumberOfPages();
		for (var i = 1; i <= pages; ++i)
		{
			// get the page text
			var page = pdfDoc.GetPage(i);
			var text = PdfTextExtractor.GetTextFromPage(page); //, strategy);

			// if we are searching for a string with spaces, replace all whitespace with a single space
			if (containsspace)
				text = AnyNumberWhitespace().Replace(text, " ");

			// does the page text contain our search string?
			if (text.Contains(content, strcomp)) return true;
		}
		return false;
	}

	/// <summary>
	/// Compiled regex for any number of whitespace characters.
	/// </summary>
	[GeneratedRegex("\\s+")]
	private static partial Regex AnyNumberWhitespace();
}
