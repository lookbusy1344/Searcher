using System.Diagnostics;

namespace Searcher;

/// <summary>
/// A monotonic clock that returns the current date and time, or miliseconds as a long
/// </summary>
public sealed class Monotonic
{
	// record when the instance is constructed, including the timezone
	private readonly DateTimeOffset start;

	// Stopwatch is a monotonic timer
	private readonly Stopwatch watch = Stopwatch.StartNew();

	public Monotonic() => start = DateTimeOffset.Now;

	public Monotonic(DateTimeOffset dateTimeOffset) => start = dateTimeOffset;

	/// <summary>
	/// A monotonic clock that returns the current local date and time.
	/// </summary>
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	public DateTimeOffset Now() => start.Add(watch.Elapsed);

	/// <summary>
	/// A monotonic clock that returns the current UTC date and time.
	/// </summary>
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	public DateTime NowUTC() => start.Add(watch.Elapsed).UtcDateTime;

	/// <summary>
	/// Monotonic Seconds, just from the Stopwatch
	/// </summary>
	public double Seconds => watch.Elapsed.TotalSeconds;

	/// <summary>
	/// Monotonic milliseconds, just from the Stopwatch
	/// </summary>
	/// <returns>Long number of milliseconds</returns>
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
