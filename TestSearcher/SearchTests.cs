using Searcher;

namespace TestSearcher;

public partial class SearchTests
{
	private const int SearchTimeout = 2000;

	[Fact(DisplayName = "Search: No matching items", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void SearchNoMatch()
	{
		// searcher.exe -s "summer"
		// No matches

		var options = new CliOptions { Search = "summer" };
		var found = Helpers.SearchCaller(options);
		Assert.Empty(found);
	}

	[Fact(DisplayName = "GLOBS: Wrong glob - terrors of the earth", Timeout = SearchTimeout)]
	[Trait("Category", "Globs")]
	public void SearchNoMatchingGlob()
	{
		// searcher.exe -s "terrors of the earth" -p *.log,*.x
		// no results, using explicit globs that don't exist

		var options = new CliOptions
		{
			Search = "terrors of the earth",
			Pattern = new string[] { "*.log", "*.x" }
		};
		var found = Helpers.SearchCaller(options);

		Assert.Empty(found);
	}

	[Fact(DisplayName = "Search: Basic search TXT, PDF, ZIP - Terrors of the Earth", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void TerrorsOfTheEarth()
	{
		// searcher.exe -s "terrors of the earth"
		// 4 matches, the King Lear documents in different formats and 'King Lear pdf.zip'

		var expected = new string[] { "King Lear.docx", "King Lear.txt", "King Lear.pdf", "King Lear pdf.zip" };
		var options = new CliOptions { Search = "terrors of the earth" };
		var found = Helpers.SearchCaller(options);

		Assert.True(Helpers.CompareNames(expected, found));
	}

	[Fact(DisplayName = "Search: Basic + ZIP - It is the east", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void ItIsTheEast()
	{
		// searcher.exe -s "it is the east"
		// 4 matches, the Romeo & Juliet documents in different formats. And a ZIP containing R&J

		var expected = new string[] { "Macbeth and Romeo txt.zip", "Romeo and Juliet.docx", "Romeo and Juliet.txt", "Romeo and Juliet.pdf" };
		var options = new CliOptions { Search = "it is the east" };
		var found = Helpers.SearchCaller(options);

		Assert.True(Helpers.CompareNames(expected, found));
	}

	[Fact(DisplayName = "Search: Basic + ZIP - Poor player That struts", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void PoorPlayerThatStruts()
	{
		// searcher.exe -s "poor player That struts"
		// 4 matches, Macbeth documents and a ZIP

		var expected = new string[] { "Macbeth.txt", "Macbeth.docx", "Macbeth and Romeo txt.zip", "Macbeth.pdf" };
		var options = new CliOptions { Search = "poor player That struts" };
		var found = Helpers.SearchCaller(options);

		Assert.True(Helpers.CompareNames(expected, found));
	}

	[Fact(DisplayName = "Search: TXT in ZIP in ZIP - Brown fox", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void BrownFoxNestedZip()
	{
		// searcher.exe -s "brown fox"
		// 1 result, a txt file inside a ZIP, inside a ZIP

		var expected = new string[] { "Nested zip brown fox.zip" };
		var options = new CliOptions { Search = "brown fox" };
		var found = Helpers.SearchCaller(options);

		Assert.True(Helpers.CompareNames(expected, found));
	}

	[Fact(DisplayName = "Search: Single TXT file - This day", Timeout = SearchTimeout)]
	[Trait("Category", "Search")]
	public void ThisDayHenryV()
	{
		// searcher.exe -s "this day"
		// 1 result, just a basic TXT file

		var expected = new string[] { "Henry V.txt" };
		var options = new CliOptions { Search = "this day" };
		var found = Helpers.SearchCaller(options);

		Assert.True(Helpers.CompareNames(expected, found));
	}

	[Fact(DisplayName = "GLOBS: Explicit globs - terrors of the earth", Timeout = SearchTimeout)]
	[Trait("Category", "Globs")]
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
		var found = Helpers.SearchCaller(options);

		Assert.True(Helpers.CompareNames(expected, found));
	}

	[Fact(DisplayName = "ZIP: Single glob and zip", Timeout = SearchTimeout)]
	[Trait("Category", "Zip")]
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
		var found = Helpers.SearchCaller(options);

		Assert.True(Helpers.CompareNames(expected, found));
	}

	[Fact(DisplayName = "ZIP: Two globs including zip match", Timeout = SearchTimeout)]
	[Trait("Category", "Zip")]
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
		var found = Helpers.SearchCaller(options);

		Assert.True(Helpers.CompareNames(expected, found));
	}

	[Fact(DisplayName = "CASE: Wrong case, but case-insensitive", Timeout = SearchTimeout)]
	[Trait("Category", "Case")]
	public void CaseInsensitiveBasic()
	{
		// searcher.exe -s "Having some BUSINESS" -p *.txt
		// 1 result, the wrong case doesnt matter without -c

		var expected = new string[] { "Romeo and Juliet.txt" };
		var options = new CliOptions
		{
			Search = "Having some BUSINESS",
			Pattern = new string[] { "*.txt" },
			CaseSensitive = false
		};
		var found = Helpers.SearchCaller(options);

		Assert.True(Helpers.CompareNames(expected, found));
	}

	[Fact(DisplayName = "CASE: Wrong case, case-sensitive", Timeout = SearchTimeout)]
	[Trait("Category", "Case")]
	public void CaseSensitive()
	{
		// searcher.exe -s "Having some BUSINESS" -p *.txt -c
		// no result, the wrong case

		//var expected = new string[] { };
		var options = new CliOptions
		{
			Search = "Having some BUSINESS",
			Pattern = new string[] { "*.txt" },
			CaseSensitive = true
		};
		var found = Helpers.SearchCaller(options);

		Assert.Empty(found);
	}

	[Fact(DisplayName = "CASE: Correct case, case-sensitive", Timeout = SearchTimeout)]
	[Trait("Category", "Case")]
	public void CaseSensitiveMatch()
	{
		// searcher.exe -s "Having some business" -p *.docx -c
		// 1 result, we are using the correct case in this CS search

		var expected = new string[] { "Romeo and Juliet.docx" };
		var options = new CliOptions
		{
			Search = "Having some business",
			Pattern = new string[] { "*.docx" },
			CaseSensitive = true
		};
		var found = Helpers.SearchCaller(options);

		Assert.True(Helpers.CompareNames(expected, found));
	}

	[Fact(DisplayName = "CLI: Command line parsing basic", Timeout = SearchTimeout)]
	[Trait("Category", "CLI")]
	public void BasicCommandLineParse()
	{
		// searcher.exe -s "poor player That struts"
		// 4 matches, Macbeth documents and a ZIP

		var commandline = "-s \"poor player That struts\"";
		var expected = new string[] { "Macbeth.txt", "Macbeth.docx", "Macbeth and Romeo txt.zip", "Macbeth.pdf" };
		var found = Helpers.SearchCaller(Helpers.ParseCommandLine(commandline));

		Assert.True(Helpers.CompareNames(expected, found));
	}

	[Fact(DisplayName = "CLI: Command line parsing complex", Timeout = SearchTimeout)]
	[Trait("Category", "CLI")]
	public void ComplexCommandLineParse()
	{
		// searcher.exe -s "it is the east" -p *.docx,*.txt -z
		// 3 results, this time we look inside zips for DOCX and TXT files

		var commandline = "-s \"it is the east\" -p *.docx,*.txt -z";
		var expected = new string[] { "Romeo and Juliet.docx", "Macbeth and Romeo txt.zip", "Romeo and Juliet.txt" };
		var found = Helpers.SearchCaller(Helpers.ParseCommandLine(commandline));

		Assert.True(Helpers.CompareNames(expected, found));
	}

	[Fact(DisplayName = "CLI: Command line parsing with error", Timeout = SearchTimeout)]
	[Trait("Category", "CLI")]
	public void ErrorCommandLineParse()
	{
		// searcher.exe -s "it is the east" -p *.docx,*.txt -z -Q
		// this command line is an error, -Q is not a valid option

		var commandline = "-s \"it is the east\" -p *.docx,*.txt -z -Q";

		Helpers.AssertThrows<Exception>(() =>
		{
			var options = Helpers.ParseCommandLine(commandline);
		}, "Did not detect the invalid parameter(s)");
	}
}
