using System.Diagnostics;

namespace Searcher;
internal sealed class ProgressTimer
{
	// Stopwatch is a monotonic timer
	private readonly Stopwatch watch;
	private readonly int total;

	public ProgressTimer(int total)
	{
		this.watch = Stopwatch.StartNew();
		this.total = total;
	}

	public long Milliseconds => watch.ElapsedMilliseconds;

	/// <summary>
	/// Get the remaining duration in seconds
	/// </summary>
	public double GetRemainingSeconds(int current)
	{
		if (current >= total) return 0L;
		if (current <= 0) throw new Exception("Current must be greater than zero");

		var duration = watch.Elapsed.TotalSeconds;           // current duration in seconds
		var progress = current / (double)total;            // progress as a fraction
		var expectedduration = duration / progress;        // expected total duration in ms
		return expectedduration - duration;                      // remaining duration in seconds
	}

	public long GetRemainingMilliseconds(int current) => (long)(GetRemainingSeconds(current) * 1000.0);
}
