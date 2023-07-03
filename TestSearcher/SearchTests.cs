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
		var options = new CliOptions { Search = "summer" };
		var result = SearchCaller(options);
		Assert.True(result.Length == 0);
	}

	[Fact(DisplayName = "Search: Terrors of the Earth", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void TerrorsOfTheEarth()
	{
		var expected = new string[] { "King Lear.docx", "King Lear.txt", "King Lear.pdf" };
		var options = new CliOptions { Search = "terrors of the earth" };
		var found = SearchCaller(options);

		Assert.True(CompareNames(expected, found));
	}

	[Fact(DisplayName = "Search: It is the east", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void ItIsTheEast()
	{
		var expected = new string[] { "Macbeth and Romeo txt.zip", "Romeo and Juliet.docx", "Romeo and Juliet.txt", "Romeo and Juliet.pdf" };
		var options = new CliOptions { Search = "it is the east" };
		var found = SearchCaller(options);

		Assert.True(CompareNames(expected, found));
	}

	[Fact(DisplayName = "Search: Poor player That struts", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void PoorPlayerThatStruts()
	{
		var expected = new string[] { "Macbeth.txt", "Macbeth.docx", "Macbeth and Romeo txt.zip", "Macbeth.pdf" };
		var options = new CliOptions { Search = "poor player That struts" };
		var found = SearchCaller(options);

		Assert.True(CompareNames(expected, found));
	}

	[Fact(DisplayName = "Search: Nested zip test - Brown fox", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void BrownFoxNestedZip()
	{
		var expected = new string[] { "Nested zip brown fox.zip" };
		var options = new CliOptions { Search = "brown fox" };
		var found = SearchCaller(options);

		Assert.True(CompareNames(expected, found));
	}

	[Fact(DisplayName = "Search: This day Henry V", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void ThisDayHenryV()
	{
		var expected = new string[] { "Henry V.txt" };
		var options = new CliOptions { Search = "this day" };
		var found = SearchCaller(options);

		Assert.True(CompareNames(expected, found));
	}

	[Fact(DisplayName = "Search: Explicit globs - terrors of the earth", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void TerrorsOfTheEarthGlobs()
	{
		var expected = new string[] { "King Lear.txt", "King Lear.pdf" };
		var options = new CliOptions
		{
			Search = "terrors of the earth",
			Pattern = new string[] { "*.pdf", "*.txt" }
		};
		var found = SearchCaller(options);

		Assert.True(CompareNames(expected, found));
	}




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
