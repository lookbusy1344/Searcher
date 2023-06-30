using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace Searcher;

internal class PdfCheck
{
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
			var page = pdfDoc.GetPage(i);
			var text = PdfTextExtractor.GetTextFromPage(page); //, strategy);

			if (containsspace)
			{
				if (text.Contains("\r\n", strcomp)) text = text.Replace("\r\n", " ");
				if (text.Contains('\n', strcomp)) text = text.Replace('\n', ' ');
			}

			if (text.Contains(content, strcomp)) return true;
		}
		return false;
	}
}
