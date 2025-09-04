namespace SearcherCore;

using System.Collections.Concurrent;
using DotNet.Globbing;

/// <summary>
/// File discovery using glob patterns
/// </summary>
public static class GlobSearch
{
	private static readonly EnumerationOptions diroptions = new() { IgnoreInaccessible = true };

	/// <summary>
	/// Use globs to find files recursively
	/// </summary>
	public static string[] FindFiles(string path, IReadOnlyList<Glob> globs, CancellationToken token)
	{
		var files = new List<string>(100);
		foreach (var g in globs) {
			FindFilesRecursivelyInternal(ref files, path, g, token);
		}

		return [
			.. files
				.Order()
				.Distinct()
		];
	}

	/// <summary>
	/// Inner search routine, to save need for list reallocations
	/// </summary>
	private static void FindFilesRecursivelyInternal(ref List<string> files, string path, Glob g, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		foreach (var file in Directory.GetFiles(path)) {
			if (g.IsMatch(Path.GetFileName(file))) {
				files.Add(file);
			}
		}

		foreach (var dir in Directory.GetDirectories(path, "*", diroptions)) {
			FindFilesRecursivelyInternal(ref files, dir, g, token);
		}
	}

	/// <summary>
	/// Use globs to find files recursively, in parallel
	/// </summary>
	public static string[] ParallelFindFiles(string path, IReadOnlyList<Glob> globs, int parallelthreads, Action<int>? progress,
		CancellationToken cancellationtoken)
	{
		if (parallelthreads <= 1) {
			return FindFiles(path, globs, cancellationtoken);
		}

		var count = 0;
		var results = new ConcurrentBag<List<string>>();

		var currentbuffer = new ConcurrentBag<string> { path };
		var nextbuffer = new ConcurrentBag<string>();

		// we need 2 buffers, so we can swap them. One is iterated in parallel, the other is build up for the next iteration

		while (!currentbuffer.IsEmpty) {
			cancellationtoken.ThrowIfCancellationRequested();

			count += currentbuffer.Count;
			progress?.Invoke(count);

			_ = Parallel.ForEach(currentbuffer, new() { MaxDegreeOfParallelism = parallelthreads, CancellationToken = cancellationtoken },
				(folder) => {
					cancellationtoken.ThrowIfCancellationRequested();

					// add subdirectories to the queue, to be processed in parallel on the next batch
					foreach (var dir in Directory.GetDirectories(folder, "*", diroptions)) {
						nextbuffer.Add(dir);
					}

					// now find the files that match the globs
					var candidates = Directory.GetFiles(folder);
					List<string>? found = null;
					foreach (var c in candidates) {
						cancellationtoken.ThrowIfCancellationRequested();
						var size = candidates.Length > 10 ? 10 : candidates.Length;

						var filename = Path.GetFileName(c);
						foreach (var g in globs) {
							if (g.IsMatch(filename)) {
								found ??= new(size);
								found.Add(c);
								break;
							}
						}
					}

					if (found?.Count > 0) {
						results.Add(found);
					}
				});

			// if no new folders were added, we are done
			if (nextbuffer.IsEmpty) {
				break;
			}

			currentbuffer.Clear(); // clear the processed items

			// swap the bags, so currentbuffer is now ready for the next iteration
			(nextbuffer, currentbuffer) = (currentbuffer, nextbuffer);
		}

		// flatten and sort the results
		return [
			.. results.SelectMany(s => s)
				.Order()
				.Distinct()
		];
	}

	/// <summary>
	/// OLD AND SIMPLE ROUTINE. Search for files matching the given patterns (in parallel) in the given folder
	/// </summary>
	public static string[] RecursiveFindFiles(string path, IReadOnlyList<string> outerpatterns, int parallelthreads, CancellationToken token)
	{
		var searchoptions = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };
		var results = new ConcurrentBag<string[]>();

		_ = Parallel.ForEach(outerpatterns, new() { MaxDegreeOfParallelism = parallelthreads, CancellationToken = token }, pattern => {
			if (string.IsNullOrEmpty(pattern)) {
				return;
			}

			var files = Directory.GetFiles(path, pattern, searchoptions);
			if (files.Length > 0) {
				results.Add(files);
			}
		});

		// merge the results from each task into single sorted array
		return [
			.. results
				.SelectMany(x => x)
				.Order()
				.Distinct()
		];
	}
}
