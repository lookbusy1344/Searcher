using System.Diagnostics;

namespace Searcher;

/// <summary>
/// A monotonic clock that returns the current date and time, or miliseconds as a long
/// </summary>
public sealed class MonotonicDateTime
{
	/// <summary>
	/// DateTimeOffset when the clock was started (this includes the time zone)
	/// </summary>
	private readonly DateTimeOffset start;

	/// <summary>
	/// Stopwatch is a monotonic timer, but doesnt show wall-clock time
	/// </summary>
	private readonly Stopwatch watch = Stopwatch.StartNew();

	public MonotonicDateTime() => start = DateTimeOffset.Now;

	public MonotonicDateTime(DateTimeOffset dateTimeOffset) => start = dateTimeOffset;

	/// <summary>
	/// A monotonic clock that returns the current local date and time.
	/// </summary>
	public DateTimeOffset Now => start.Add(watch.Elapsed);

	/// <summary>
	/// A monotonic clock that returns the current UTC DateTime.
	/// </summary>
	public DateTime NowUTC => start.Add(watch.Elapsed).UtcDateTime;

	/// <summary>
	/// Monotonic Seconds, just from the Stopwatch
	/// </summary>
	public double Seconds => watch.Elapsed.TotalSeconds;

	/// <summary>
	/// Monotonic milliseconds, just from the Stopwatch
	/// </summary>
	public long Milliseconds => watch.ElapsedMilliseconds;

	/// <summary>
	/// Monotonic timespan, just from the Stopwatch.
	/// </summary>
	public TimeSpan TimeSpan => watch.Elapsed;

	/// <summary>
	/// Monotonic ticks, just from the Stopwatch.
	/// </summary>
	public long ElapsedTicks => watch.ElapsedTicks;
}
