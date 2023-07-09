using System.Diagnostics;

namespace Searcher;

/// <summary>
/// A monotonic clock that returns the current date and time, or miliseconds as a long
/// </summary>
public sealed class Monotonic
{
	// record when the instance is constructed
	private readonly DateTime start;

	// current timezone, for UTC calculations
	private readonly TimeSpan zone;

	// Stopwatch is a monotonic timer
	private readonly Stopwatch watch;

	public Monotonic()
	{
		// record the start time and timezone, and starts the stopwatch
		watch = Stopwatch.StartNew();
		start = DateTime.Now;
		zone = TimeZoneInfo.Local.GetUtcOffset(start);
	}

	/// <summary>
	/// A monotonic clock that returns the current local date and time.
	/// </summary>
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	public DateTime Now() => start.Add(watch.Elapsed);

	/// <summary>
	/// A monotonic clock that returns the current UTC date and time.
	/// </summary>
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	public DateTime NowUTC() => start.Subtract(zone).Add(watch.Elapsed);

	/// <summary>
	/// Get the number of seconds since initialisation.
	/// </summary>
	public double Seconds => watch.Elapsed.TotalSeconds;

	/// <summary>
	/// Monotonic milliseconds - arbitrary start point, but monotonic.
	/// </summary>
	/// <returns>Long number of milliseconds</returns>
	public long Milliseconds => watch.ElapsedMilliseconds;
}
