using System;
using Avalonia;
using CommandLine;

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
		var result = Parser.Default.ParseArguments<GuiCliOptions>(args);

		result
			.WithParsed(opts => {
				// Validate the folder path if provided
				var folderPath = opts.Folder?.FullName ?? ".";
				var validatedFolder = SearcherCore.Utils.ValidateSearchPath(folderPath);
				if (validatedFolder == null) {
					Console.Error.WriteLine($"Invalid or inaccessible folder path: {folderPath}");
					Environment.Exit(1);
					return;
				}
				opts.Folder = new System.IO.DirectoryInfo(validatedFolder);

				Options = opts;
				BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
			})
			.WithNotParsed(errs => {
				foreach (var error in errs) {
					Console.Error.WriteLine(error);
				}
				Environment.Exit(1);
			});
	}

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.LogToTrace();
}
