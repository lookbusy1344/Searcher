using System.Diagnostics;

namespace Searcher;
internal sealed class ProgressTimer
{
	// Stopwatch is a monotonic timer
	private readonly Stopwatch watch;
	private readonly int total;
	private double lastduration;

	public ProgressTimer(int total)
	{
		this.watch = Stopwatch.StartNew();
		this.total = total;
		this.lastduration = -1.0;
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
		var expectedduration = duration / progress;        // expected total duration in seconds

		var newexpected = expectedduration - duration;      // remaining time in seconds
		if (lastduration < 0 || newexpected - lastduration > 3.5 || newexpected < lastduration - 0.9)
		{
			// only update if the duration is significantly different (3.5 secs longer that previous calc, or 0.9 secs less)
			lastduration = newexpected;
		}

		return lastduration;
	}

	public long GetRemainingMilliseconds(int current) => (long)(GetRemainingSeconds(current) * 1000.0);

	/// <summary>
	/// Convert seconds into a human readable string
	/// </summary>
	public static string SecondsAsText(double sec)
	{
		var sign = sec < 0 ? "-" : "";
		var ts = TimeSpan.FromSeconds(Math.Abs(sec));

		return ts switch
		{
			{ TotalDays: >= 1 } => $"{sign}{ts.Days} days {ts.Hours} hrs",
			{ TotalHours: >= 1 } => $"{sign}{ts.Hours}:{ts.Minutes:00}:{ts.Seconds:00}",
			{ TotalMinutes: >= 1 } => $"{sign}{ts.Minutes:00}:{ts.Seconds:00}",
			_ => $"{sign}{ts.TotalSeconds:0.0} secs",
		};
	}
}
