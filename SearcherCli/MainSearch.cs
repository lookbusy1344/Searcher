namespace SearcherCli;

internal static class MainSearch
{
	public static void LongRunningTask(CliOptions config)
	{
		var parallelthreads = config.DegreeOfParallelism;
		var filescount = 0;
		var modulo = 20;

		using var cts = new CancellationTokenSource();

		try {
			if (string.IsNullOrWhiteSpace(config.Search)) {
				throw new OperationCanceledException();
			}

			// outerpatterns are the physical files to find, and may include .zip files even if not given as a pattern, when -z is selected
			// innerpatterns are for searching inside zip files. May be an empty list for everything, but explicitly doesnt include .zip
			var innerpatterns = Utils.ProcessInnerPatterns(config.Pattern);
			var outerpatterns = Utils.ProcessOuterPatterns(config.Pattern, config.InsideZips);
			if (outerpatterns.Count == 0) {
				throw new("No pattern specified");
			}

			// Parallel routine for searching folders
			//var sw = Stopwatch.StartNew();
			var files = GlobSearch.ParallelFindFiles(config.Folder.FullName, outerpatterns, parallelthreads, null, cts.Token);
			//Debug.WriteLine($"Found {files.Length} files in {sw.ElapsedMilliseconds}ms");

			// Work out a reasonable update frequency for the progress bar
			filescount = files.Length;
			modulo = Utils.CalculateModulo(filescount);

			// var progresstimer = new ProgressTimer(filescount);
			// var counter = new SafeCounter();

			// search the files in parallel
			_ = Parallel.ForEach(files, new() { MaxDegreeOfParallelism = parallelthreads, CancellationToken = cts.Token }, file => {
				// Search the file for the search string
				var found = SearchFile.FileContainsStringWrapper(file, config.Search, innerpatterns, config.StringComparison, cts.Token);
				if (found is SearchResult.Found or SearchResult.Error) {
					ShowResult(new(file, found));
				}

				// when the task completes, update completed counter. This is thread safe
				//var currentcount = counter.Increment();
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
}
