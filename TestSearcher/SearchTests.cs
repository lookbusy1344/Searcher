using Searcher;

namespace TestSearcher;

public class SearchTests
{
	private const int SearchTimeout = 2000;
	private const string SearchPath = @"C:\Users\JohnT\Documents\Visual Studio\Projects\Searcher\TestDocs";

	[Fact(DisplayName = "Search: No matching items", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void SearchNoMatch()
	{
		// searcher.exe -s "summer"
		// No matches

		var options = new CliOptions { Search = "summer" };
		var result = SearchCaller(options);
		Assert.True(result.Length == 0);
	}

	[Fact(DisplayName = "Search: Basic search TXT, PDF, ZIP - Terrors of the Earth", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void TerrorsOfTheEarth()
	{
		// searcher.exe -s "terrors of the earth"
		// 3 matches, the King Lear documents in different formats

		var expected = new string[] { "King Lear.docx", "King Lear.txt", "King Lear.pdf" };
		var options = new CliOptions { Search = "terrors of the earth" };
		var found = SearchCaller(options);

		Assert.True(CompareNames(expected, found));
	}

	[Fact(DisplayName = "Search: Basic + ZIP - It is the east", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void ItIsTheEast()
	{
		// searcher.exe -s "it is the east"
		// 4 matches, the Romeo & Juliet documents in different formats. And a ZIP containing R&J

		var expected = new string[] { "Macbeth and Romeo txt.zip", "Romeo and Juliet.docx", "Romeo and Juliet.txt", "Romeo and Juliet.pdf" };
		var options = new CliOptions { Search = "it is the east" };
		var found = SearchCaller(options);

		Assert.True(CompareNames(expected, found));
	}

	[Fact(DisplayName = "Search: Basic + ZIP - Poor player That struts", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void PoorPlayerThatStruts()
	{
		// searcher.exe -s "poor player That struts"
		// 4 matches, Macbeth documents and a ZIP

		var expected = new string[] { "Macbeth.txt", "Macbeth.docx", "Macbeth and Romeo txt.zip", "Macbeth.pdf" };
		var options = new CliOptions { Search = "poor player That struts" };
		var found = SearchCaller(options);

		Assert.True(CompareNames(expected, found));
	}

	[Fact(DisplayName = "Search: TXT in ZIP in ZIP - Brown fox", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void BrownFoxNestedZip()
	{
		// searcher.exe -s "brown fox"
		// 1 result, a txt file inside a ZIP, inside a ZIP

		var expected = new string[] { "Nested zip brown fox.zip" };
		var options = new CliOptions { Search = "brown fox" };
		var found = SearchCaller(options);

		Assert.True(CompareNames(expected, found));
	}

	[Fact(DisplayName = "Search: Single TXT file - This day", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void ThisDayHenryV()
	{
		// searcher.exe -s "this day"
		// 1 result, just a basic TXT file

		var expected = new string[] { "Henry V.txt" };
		var options = new CliOptions { Search = "this day" };
		var found = SearchCaller(options);

		Assert.True(CompareNames(expected, found));
	}

	[Fact(DisplayName = "Search: Explicit globs - terrors of the earth", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void TerrorsOfTheEarthGlobs()
	{
		// searcher.exe -s "terrors of the earth" -p *.pdf,*.txt
		// 2 results, using explicit globs and excluding DOCX

		var expected = new string[] { "King Lear.txt", "King Lear.pdf" };
		var options = new CliOptions
		{
			Search = "terrors of the earth",
			Pattern = new string[] { "*.pdf", "*.txt" }
		};
		var found = SearchCaller(options);

		Assert.True(CompareNames(expected, found));
	}

	[Fact(DisplayName = "Search: Single glob and zip", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void SingleGlobNoZip()
	{
		// searcher.exe -s "it is the east" -p *.docx -z
		// 1 result, looking for DOCX and inside ZIPs (but there is no docx matching inside a zip)

		var expected = new string[] { "Romeo and Juliet.docx" };
		var options = new CliOptions
		{
			Search = "it is the east",
			Pattern = new string[] { "*.docx" },
			InsideZips = true
		};
		var found = SearchCaller(options);

		Assert.True(CompareNames(expected, found));
	}

	[Fact(DisplayName = "Search: Two globs including zip match", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void GlobsAndZip()
	{
		// searcher.exe -s "it is the east" -p *.docx,*.txt -z
		// 3 results, this time we look inside zips for DOCX and TXT files

		var expected = new string[] { "Romeo and Juliet.docx", "Macbeth and Romeo txt.zip", "Romeo and Juliet.txt" };
		var options = new CliOptions
		{
			Search = "it is the east",
			Pattern = new string[] { "*.docx", "*.txt" },
			InsideZips = true
		};
		var found = SearchCaller(options);

		Assert.True(CompareNames(expected, found));
	}



	// ================= HELPERS ==============================================================

	/// <summary>
	/// Helper to set up the instance, run the test, and return the results
	/// </summary>
	private static string[] SearchCaller(CliOptions options)
	{
		// default testing options
		options.Folder = new DirectoryInfo(SearchPath);
		options.Pattern ??= new List<string>() { "*" };

		var searcher = new MainForm();
		var task = searcher.TestHarness(options);

		task.Wait();

		if (task.IsFaulted) throw task.Exception!;
		if (task.IsCanceled) throw new Exception("Task was canceled");
		if (task.IsCompletedSuccessfully == false) throw new Exception("Task was not completed successfully");
		if (task.Result == null) throw new Exception("Task result was null");

		return task.Result.Where(r => r.Result == SearchResult.Found)
			.Select(r => Path.GetFileName(r.Path))
			.ToArray();
	}

	/// <summary>
	/// Compare the lengths, and sort and compare the arrays
	/// </summary>
	private static bool CompareNames(string[] a, string[] b)
	{
		if (a.Length != b.Length) return false;

		return a.OrderBy(s => s)
			.SequenceEqual(b.OrderBy(s => s));
	}
}
