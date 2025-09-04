namespace SearcherCore;

/// <summary>
/// Result of a search operation
/// </summary>
public enum SearchResult
{
	Found,
	NotFound,
	Error
}

/// <summary>
/// A single search result
/// </summary>
/// <param name="Path">The path to the file that was searched</param>
/// <param name="Result">The result of the search</param>
public readonly record struct SingleResult(string Path, SearchResult Result);