using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Input.Platform;

namespace SearcherGui.Services;

public static class ResultInteractionService
{
	public static bool OpenFile(string filePath, IProcessLauncher? processLauncher = null)
	{
		processLauncher ??= new RealProcessLauncher();

		try {
			if (!File.Exists(filePath)) {
				Console.Error.WriteLine($"File not found: {filePath}");
				return false;
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				var process = processLauncher.Start(new ProcessStartInfo {
					FileName = filePath,
					UseShellExecute = true
				});
				if (process == null) {
					Console.Error.WriteLine($"Failed to start process for file: {filePath}");
					return false;
				}
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				var process = processLauncher.Start("open", filePath);
				if (process == null) {
					Console.Error.WriteLine($"Failed to start 'open' command for file: {filePath}");
					return false;
				}
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				// Linux: use try-catch instead of checking return value
				try {
					processLauncher.Start("xdg-open", filePath);
				}
				catch (Exception ex) {
					Console.Error.WriteLine($"Failed to open file with xdg-open: {ex.Message}");
					return false;
				}
			}
			return true;
		}
		catch (Exception ex) {
			Console.Error.WriteLine($"Error opening file {filePath}: {ex.Message}");
			return false;
		}
	}

	public static bool ShowInFolder(string filePath, IProcessLauncher? processLauncher = null)
	{
		processLauncher ??= new RealProcessLauncher();

		try {
			var folder = Path.GetDirectoryName(filePath) ?? filePath;

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				// Use array-based argument passing to properly escape paths with spaces
				var process = processLauncher.Start(new ProcessStartInfo {
					FileName = "explorer.exe",
					Arguments = $"/select,\"{filePath}\"",
					UseShellExecute = false
				});
				if (process == null) {
					Console.Error.WriteLine($"Failed to start explorer for file: {filePath}");
					return false;
				}
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				var process = processLauncher.Start("open", new[] { "-R", filePath });
				if (process == null) {
					Console.Error.WriteLine($"Failed to start 'open -R' for file: {filePath}");
					return false;
				}
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				// Linux: use try-catch instead of checking return value
				try {
					processLauncher.Start("nautilus", folder);
				}
				catch {
					// Fallback to xdg-open
					try {
						processLauncher.Start("xdg-open", folder);
					}
					catch (Exception ex) {
						Console.Error.WriteLine($"Failed to open folder with nautilus or xdg-open: {ex.Message}");
						return false;
					}
				}
			}
			return true;
		}
		catch (Exception ex) {
			Console.Error.WriteLine($"Error showing file in folder {filePath}: {ex.Message}");
			return false;
		}
	}

	public static async Task<bool> CopyToClipboardAsync(IClipboard? clipboard, string text)
	{
		try {
			if (clipboard == null) {
				Console.Error.WriteLine("Clipboard not available");
				return false;
			}

			await clipboard.SetTextAsync(text);
			return true;
		}
		catch (Exception ex) {
			Console.Error.WriteLine($"Error copying to clipboard: {ex.Message}");
			return false;
		}
	}
}
