namespace Searcher;

using System.Management;

internal static class DiskQuery
{
	/// <summary>
	/// Check if the disc is solid state
	/// </summary>
	public static bool IsSSD(DirectoryInfo dir)
	{
#if WINDOWS
		var driveLetter = dir.Root.FullName[..1];
		try {
			using var drive = new ManagementObject($"win32_logicaldisk.deviceid=\"{driveLetter}:\"");
			drive.Get();
			var mediatype = drive["MediaType"];
			return mediatype switch {
				uint n when n == 12 => true,
				_ => false
			};
		}
		catch (ManagementException) {
#if DEBUG
			throw;
#else
			return true;
#endif
		}
#else
		return true;
		//throw new PlatformNotSupportedException("IsSSD is only supported on Windows platforms.");
#endif
	}
}
