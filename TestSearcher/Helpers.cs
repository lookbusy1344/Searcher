using CommandLine;
using Searcher;
using System.Text.RegularExpressions;

namespace TestSearcher;

internal static partial class Helpers
{
	private const string SearchPath = @"C:\Users\JohnT\Documents\Visual Studio\Projects\Searcher\TestDocs";

	/// <summary>
	/// Try the action, and assert that it throws the expected exception
	/// </summary>
	public static void AssertThrows<E>(Action action, string errmsg) where E : Exception
	{
		var result = CheckThrows<E>(action);
		if (!result)
			Assert.Fail(errmsg);
		else
			Assert.True(true);
	}

	/// <summary>
	/// Try the action, and if it throws the expected exception, return true
	/// </summary>
	public static bool CheckThrows<E>(Action action) where E : Exception
	{
		try
		{
			action();
		}
		catch (E)
		{
			// expected exception was thrown, test passed
			return true;
		}
		catch
		{
			// some other exception was thrown, test failed
			return false;
		}

		// no exception was thrown, test failed
		return false;
	}

	/// <summary>
	/// Take a single string and parse it into a CliOptions instance
	/// </summary>
	public static CliOptions ParseCommandLine(string s)
	{
		var items = SplitParams(s);
		var parsed = Searcher.Program.ParseParams(items);
		if (parsed.Tag == ParserResultType.NotParsed)
			throw new Exception($"Failed to parse command line: {s}");

		return parsed.Value;
	}

	/// <summary>
	/// Split a single string into a string array, respecting quotes and spaces
	/// </summary>
	private static string[] SplitParams(string s) => SplitOnSpacesRespctQuotes().Split(s).Where(i => i != "\"").ToArray();

	/// <summary>
	/// Helper to set up the instance, run the test, and return the results
	/// </summary>
	public static string[] SearchCaller(CliOptions options)
	{
		// default testing options
		options.Folder = new DirectoryInfo(SearchPath);
		options.Pattern ??= ["*"];

		using var searcher = new MainForm();
		var task = searcher.TestHarnessAsync(options);

		task.Wait();

		if (task.IsFaulted) throw task.Exception!;
		if (task.IsCanceled) throw new Exception("Task was canceled");
		if (!task.IsCompletedSuccessfully) throw new Exception("Task was not completed successfully");
		if (task.Result == null) throw new Exception("Task result was null");

		return task.Result.Where(r => r.Result == SearchResult.Found)
			.Select(r => Path.GetFileName(r.Path))
			.ToArray();
	}

	/// <summary>
	/// Compare the lengths, and sort and compare the arrays
	/// </summary>
	public static bool CompareNames(string[] a, string[] b)
	{
		if (a.Length != b.Length) return false;

		return a.OrderBy(s => s)
			.SequenceEqual(b.OrderBy(s => s));
	}

	[GeneratedRegex("(?<=\")(.*?)(?=\")|\\s+")]
	private static partial Regex SplitOnSpacesRespctQuotes();
}
