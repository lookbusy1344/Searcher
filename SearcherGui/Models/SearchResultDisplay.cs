using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
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

public class SearchState
{
	public ObservableCollection<SearchResultDisplay> Results { get; } = new();
	public int FilesScanned { get; set; }
	public int MatchesFound { get; set; }
	public bool IsSearching { get; set; }
	public string StatusMessage { get; set; } = "Ready";
	public DateTime StartTime { get; set; }
	public CancellationTokenSource? CancellationTokenSource { get; set; }
}
