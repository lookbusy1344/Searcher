extern alias SearcherCoreLib;

using System.Collections.Generic;
using SearcherGui.Models;
using SearcherCoreLib::SearcherCore;

namespace TestSearcher.SearcherGui;

public class SearchResultDisplayTests
{
	[Fact(DisplayName = "GUI: SearchResultDisplay creates instance with correct properties")]
	[Trait("Category", "GUI")]
	public void Constructor_SetsPropertiesCorrectly()
	{
		var display = new SearchResultDisplay {
			FileName = "test.txt",
			FilePath = "/path/to/test.txt",
			MatchCount = 5
		};

		Assert.Equal("test.txt", display.FileName);
		Assert.Equal("/path/to/test.txt", display.FilePath);
		Assert.Equal(5, display.MatchCount);
	}

	[Fact(DisplayName = "GUI: FromSingleResult maps Found result correctly")]
	[Trait("Category", "GUI")]
	public void FromSingleResult_FoundResult_MapsCorrectly()
	{
		var singleResult = new SingleResult("test.txt", SearchResult.Found);
		var display = SearchResultDisplay.FromSingleResult(singleResult);

		Assert.Equal("test.txt", display.FileName);
		Assert.Equal("test.txt", display.FilePath);
		Assert.Equal(1, display.MatchCount);
	}

	[Fact(DisplayName = "GUI: FromSingleResult with full path extracts filename")]
	[Trait("Category", "GUI")]
	public void FromSingleResult_ExtractsFilenameFromPath()
	{
		var fullPath = "/home/user/documents/report.pdf";
		var singleResult = new SingleResult(fullPath, SearchResult.Found);
		var display = SearchResultDisplay.FromSingleResult(singleResult);

		Assert.Equal("report.pdf", display.FileName);
		Assert.Equal(fullPath, display.FilePath);
	}

	[Fact(DisplayName = "GUI: SearchResultDisplay equality comparison")]
	[Trait("Category", "GUI")]
	public void Equality_ComparesCorrectly()
	{
		var display1 = new SearchResultDisplay {
			FileName = "test.txt",
			FilePath = "/path/test.txt",
			MatchCount = 1
		};

		var display2 = new SearchResultDisplay {
			FileName = "test.txt",
			FilePath = "/path/test.txt",
			MatchCount = 1
		};

		Assert.Equal(display1, display2);
	}

	[Theory(DisplayName = "GUI: FromSingleResult handles various path formats")]
	[Trait("Category", "GUI")]
	[InlineData("C:\\Users\\test\\file.txt", "file.txt")]
	[InlineData("/home/user/file.txt", "file.txt")]
	[InlineData("file.txt", "file.txt")]
	[InlineData("/archive.zip/inner/file.txt", "file.txt")]
	public void FromSingleResult_HandlesVariousPathFormats(string path, string expectedFilename)
	{
		var singleResult = new SingleResult(path, SearchResult.Found);
		var display = SearchResultDisplay.FromSingleResult(singleResult);

		Assert.Equal(expectedFilename, display.FileName);
	}
}
