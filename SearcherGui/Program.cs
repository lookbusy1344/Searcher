using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using SearcherCore;

namespace SearcherGui;

public static class Program
{
	/// <summary>
	/// Parsed command-line options, accessible to App and other components.
	/// Defaults to a new instance during design-time to support Avalonia designer.
	/// </summary>
	public static GuiCliOptions Options { get; private set; } = new();

	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
	public static void Main(string[] args)
	{
		try {
			Options = ParseCommandLine(args);
			BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
		}
		catch (PicoArgsException ex) {
			ShowErrorDialog($"Command line error: {ex.Message}");
			Environment.Exit(1);
		}
		catch (Exception ex) {
			ShowErrorDialog($"Error: {ex.Message}");
			Environment.Exit(1);
		}
	}

	/// <summary>
	/// Parse command-line arguments using PicoArgs
	/// </summary>
	private static GuiCliOptions ParseCommandLine(string[] args)
	{
		var pico = new PicoArgs(args);

		// Check for help first
		var help = pico.Contains("-h", "-?", "--help");
		if (help) {
			ShowHelp();
			Environment.Exit(0);
		}

		// Core search options (all optional for GUI - can start without search)
		var search = pico.GetParamOpt("-s", "--search") ?? "";
		var folder = pico.GetParamOpt("-f", "--folder") ?? ".";
		var patterns = pico.GetMultipleParams("-p", "--pattern");

		// Flags
		var insideZips = pico.Contains("-z", "--inside-zips");
		var oneThread = pico.Contains("-o", "--one-thread");
		var caseSensitive = pico.Contains("-c", "--case-sensitive");
		var hideErrors = pico.Contains("--hide-errors");
		var raw = pico.Contains("-r", "--raw");

		// GUI-specific options
		var widthStr = pico.GetParamOpt("--width") ?? "1000";
		var heightStr = pico.GetParamOpt("--height") ?? "600";
		var autoClose = pico.Contains("--auto-close");
		var logFile = pico.GetParamOpt("--log-results");

		// Parse integer values
		if (!int.TryParse(widthStr, out var width) || width <= 0) {
			throw new ArgumentException($"Invalid width value: {widthStr}");
		}
		if (!int.TryParse(heightStr, out var height) || height <= 0) {
			throw new ArgumentException($"Invalid height value: {heightStr}");
		}

		// Validate folder path
		var validatedFolder = Utils.ValidateSearchPath(folder);
		if (validatedFolder == null) {
			throw new ArgumentException($"Invalid or inaccessible folder path: {folder}");
		}

		// Check for unexpected arguments
		pico.Finished();

		return new GuiCliOptions {
			Search = search,
			Folder = new DirectoryInfo(validatedFolder),
			Pattern = patterns.AsReadOnly(),
			InsideZips = insideZips,
			OneThread = oneThread,
			CaseSensitive = caseSensitive,
			HideErrors = hideErrors,
			Raw = raw,
			WindowWidth = width,
			WindowHeight = height,
			AutoCloseOnCompletion = autoClose,
			LogResultsFile = logFile
		};
	}

	/// <summary>
	/// Show error message to user (console fallback since Avalonia not initialized)
	/// </summary>
	private static void ShowErrorDialog(string message)
	{
		Console.Error.WriteLine(message);
		Console.Error.WriteLine();
		Console.Error.WriteLine("Press any key to exit...");
		try {
			Console.ReadKey();
		}
		catch {
			// ReadKey might fail in non-interactive environments
		}
	}

	/// <summary>
	/// Display command-line help
	/// </summary>
	private static void ShowHelp()
	{
		Console.WriteLine("SearcherGui - Cross-platform text search application");
		Console.WriteLine();
		Console.WriteLine("Usage: SearcherGui [options]");
		Console.WriteLine();
		Console.WriteLine("Core Options:");
		Console.WriteLine("  -s, --search <text>        Text to search for (optional, can enter in GUI)");
		Console.WriteLine("  -f, --folder <path>        Folder to search (default: current directory)");
		Console.WriteLine("  -p, --pattern <pattern>    File patterns (can repeat: -p *.txt -p *.doc)");
		Console.WriteLine("  -z, --inside-zips          Search inside ZIP archives");
		Console.WriteLine("  -c, --case-sensitive       Case-sensitive search");
		Console.WriteLine("  -o, --one-thread           Single-threaded mode");
		Console.WriteLine("  --hide-errors              Hide error messages");
		Console.WriteLine("  -r, --raw                  Raw output mode");
		Console.WriteLine();
		Console.WriteLine("GUI Options:");
		Console.WriteLine("  --width <pixels>           Initial window width (default: 1000)");
		Console.WriteLine("  --height <pixels>          Initial window height (default: 600)");
		Console.WriteLine("  --auto-close               Close window after search completes");
		Console.WriteLine("  --log-results <file>       Log results to file");
		Console.WriteLine();
		Console.WriteLine("Help:");
		Console.WriteLine("  -h, -?, --help             Show this help message");
		Console.WriteLine();
		Console.WriteLine("Examples:");
		Console.WriteLine("  SearcherGui");
		Console.WriteLine("  SearcherGui -f /documents -s \"hello world\"");
		Console.WriteLine("  SearcherGui -f . -p *.txt -p *.md -c -s Error");
	}

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.LogToTrace();
}
