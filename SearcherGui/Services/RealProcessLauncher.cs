using System.Diagnostics;

namespace SearcherGui.Services;

/// <summary>
/// Default implementation of IProcessLauncher that delegates to System.Diagnostics.Process.
/// </summary>
public sealed class RealProcessLauncher : IProcessLauncher
{
	public Process? Start(string fileName, string arguments)
	{
		return Process.Start(fileName, arguments);
	}

	public Process? Start(string fileName, string[] argumentList)
	{
		return Process.Start(fileName, argumentList);
	}

	public Process? Start(ProcessStartInfo startInfo)
	{
		return Process.Start(startInfo);
	}
}
