namespace TestSearcher;

using Searcher;

public class MonotonicTest
{
	[Fact(DisplayName = "Monotonic: Milliseconds from zero start")]
	[Trait("Category", "Monotonic")]
	public void MonotonicZeroStart()
	{
		var m = new MonotonicDateTime();
		var dur = m.Milliseconds;

		// we expect the diff to be within 3ms of the requested duration
		Assert.True(dur is >= 0 and <= 3);
	}

	[Fact(DisplayName = "Monotonic: should run at the same rate as wall clock")]
	[Trait("Category", "Monotonic")]
	public void MonotonicTimeSpan()
	{
		//var dt = DateTime.UtcNow;
		var m = new MonotonicDateTime();

		Thread.Sleep(500);

		var mtime = m.NowUTC;
		var diff = mtime - DateTime.UtcNow;
		var dur = Math.Abs(diff.TotalMilliseconds);

		// we expect the diff to be within 10ms of the requested duration
		Assert.True(dur is >= 0 and <= 10);
	}

	[Theory(DisplayName = "Monotonic: Sleep Test with different durations")]
	[Trait("Category", "Monotonic")]
	[InlineData(2)]
	[InlineData(100)]
	[InlineData(560)]
	public void MonotonicSleepTest(int dur)
	{
		var m = new MonotonicDateTime();

		Thread.Sleep(dur);

		var diff = m.Milliseconds - dur;

		// we expect the diff to be within 50ms of the requested duration
		Assert.True(diff is >= 0 and <= 50);
	}
}
