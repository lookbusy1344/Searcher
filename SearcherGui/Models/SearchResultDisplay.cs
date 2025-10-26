using System.IO;
using SearcherCore;

namespace SearcherGui.Models;

public class SearchResultDisplay
{
	public string FileName { get; set; } = string.Empty;
	public string FilePath { get; set; } = string.Empty;
	public int MatchCount { get; set; }

	public static SearchResultDisplay FromSingleResult(SingleResult result)
	{
		return new SearchResultDisplay {
			FileName = Path.GetFileName(result.Path),
			FilePath = result.Path,
			MatchCount = result.Result == SearchResult.Found ? 1 : 0
		};
	}
}
