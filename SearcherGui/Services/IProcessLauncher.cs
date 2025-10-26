using System.Diagnostics;

namespace SearcherGui.Services;

/// <summary>
/// Interface for launching external processes, enabling dependency injection and testing.
/// </summary>
public interface IProcessLauncher
{
	/// <summary>
	/// Starts a process with the specified filename and arguments.
	/// </summary>
	/// <param name="fileName">The process or command to execute.</param>
	/// <param name="arguments">The arguments to pass to the process.</param>
	/// <returns>The started process, or null if the process could not be started.</returns>
	Process? Start(string fileName, string arguments);

	/// <summary>
	/// Starts a process with the specified filename and argument array.
	/// </summary>
	/// <param name="fileName">The process or command to execute.</param>
	/// <param name="argumentList">The arguments to pass to the process.</param>
	/// <returns>The started process, or null if the process could not be started.</returns>
	Process? Start(string fileName, string[] argumentList);

	/// <summary>
	/// Starts a process with the specified ProcessStartInfo.
	/// </summary>
	/// <param name="startInfo">The ProcessStartInfo containing process configuration.</param>
	/// <returns>The started process, or null if the process could not be started.</returns>
	Process? Start(ProcessStartInfo startInfo);
}
