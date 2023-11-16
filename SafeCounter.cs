namespace Searcher;

/// <summary>
/// This is a thread safe Int32 counter using Interlocked.Increment etc
/// </summary>
internal sealed class SafeCounter
{
	/// <summary>
	/// The counter, cannot be accessed directly in a thread safe way
	/// </summary>
	private int counter;    // this class exists to wrap this field and prevent direct access

	/// <summary>
	/// Get the current value of the counter
	/// </summary>
	public int Value => counter;    // no need for Interlocked here, Int32 is atomic on both 32 and 64 bit

	/// <summary>
	/// Create a new counter with an optional start value
	/// </summary>
	/// <param name="startvalue">The initial value</param>
	public SafeCounter(int startvalue = 0) => counter = startvalue;

	/// <summary>
	/// Increment the counter in a thread safe way. Lock free
	/// </summary>
	/// <returns>The incremented value</returns>
	public int Increment() => Interlocked.Increment(ref counter);

	/// <summary>
	/// Decrement the counter in a thread safe way. Lock free
	/// </summary>
	/// <returns>The decremented value</returns>
	public int Decrement() => Interlocked.Decrement(ref counter);

	/// <summary>
	/// Reset the counter in a thread safe way. Lock free
	/// </summary>
	/// <returns>The original value</returns>
	public int Reset(int newvalue = 0) => Interlocked.Exchange(ref counter, newvalue);
}
