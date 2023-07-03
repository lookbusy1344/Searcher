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

	[Fact(DisplayName = "Search: 3 King Lear", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void KingLear()
	{
		var expected = new string[] { "King Lear.docx", "King Lear.txt", "King Lear.pdf" };
		var options = new CliOptions { Search = "terrors of the earth" };
		var found = SearchCaller(options);

		Assert.True(found.Length == 3);
		Assert.True(expected.SequenceEqual(found));
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
		var task = searcher.TestHarness(options, false);

		task.Wait();

		if (task.IsFaulted) throw task.Exception!;
		if (task.IsCanceled) throw new Exception("Task was canceled");
		if (task.IsCompletedSuccessfully == false) throw new Exception("Task was not completed successfully");
		if (task.Result == null) throw new Exception("Task result was null");

		return GetFoundFiles(task.Result);
	}

	/// <summary>
	/// Filter out the Errors, and just return the filenames
	/// </summary>
	private static string[] GetFoundFiles(IEnumerable<SingleResult> results) => results.Where(r => r.Result == SearchResult.Found).Select(r => r.Path).ToArray();
}
