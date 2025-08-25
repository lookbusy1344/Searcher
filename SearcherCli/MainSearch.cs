namespace SearcherCli;

internal sealed class MainSearch : IDisposable
{
	public CancellationTokenSource CancellationToken { get; init; } = new();

	public void Search(CliOptions config)
	{
		var parallelThreads = config.DegreeOfParallelism;
		// var filesCount = 0;
		// var modulo = 20;

		var cts = CancellationToken;

		try {
			if (string.IsNullOrWhiteSpace(config.Search)) {
				throw new OperationCanceledException();
			}

			// outerPatterns are the physical files to find, and may include .zip files even if not given as a pattern, when -z is selected
			// innerPatterns are for searching inside zip files. May be an empty list for everything, but explicitly doesnt include .zip
			var innerPatterns = Utils.ProcessInnerPatterns(config.Pattern);
			var outerPatterns = Utils.ProcessOuterPatterns(config.Pattern, config.InsideZips);
			if (outerPatterns.Count == 0) {
				throw new("No pattern specified");
			}

			// Parallel routine for searching folders
			//var sw = Stopwatch.StartNew();
			var files = GlobSearch.ParallelFindFiles(config.Folder.FullName, outerPatterns, parallelThreads, null, cts.Token);
			//Debug.WriteLine($"Found {files.Length} files in {sw.ElapsedMilliseconds}ms");

			// Work out a reasonable update frequency for the progress bar
			// filesCount = files.Length;
			// modulo = Utils.CalculateModulo(filesCount);

			// var progressTimer = new ProgressTimer(filesCount);
			// var counter = new SafeCounter();

			// search the files in parallel
			_ = Parallel.ForEach(files, new() { MaxDegreeOfParallelism = parallelThreads, CancellationToken = cts.Token }, file => {
				// Search the file for the search string
				var found = SearchFile.FileContainsStringWrapper(file, config.Search, innerPatterns, config.StringComparison, cts.Token);
				if (found is SearchResult.Found or SearchResult.Error) {
					ShowResult(new(file, found));
				}

				// when the task completes, update completed counter. This is thread safe
				//var currentCount = counter.Increment();
			});
		}
		catch {
			// just ignore it
		}
	}

	/// <summary>
	/// Display a single result on screen, using a single WriteLine to avoid interleaving
	/// </summary>
	/// <param name="result"></param>
	public static void ShowResult(SingleResult result) =>
		Console.WriteLine(
			$"{result.Path}: {result.Result switch {
				SearchResult.Found => "",
				SearchResult.Error => "ERROR",
				_ => "NOT FOUND"
			}}");

	public void Dispose() => CancellationToken.Dispose();
}
