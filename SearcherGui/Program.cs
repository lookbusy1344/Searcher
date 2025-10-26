using System;
using Avalonia;
using CommandLine;

namespace SearcherGui;

public static class Program
{
	/// <summary>
	/// Parsed command-line options, accessible to App and other components
	/// </summary>
	public static GuiCliOptions? Options { get; private set; }

	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
	public static void Main(string[] args)
	{
		var result = Parser.Default.ParseArguments<GuiCliOptions>(args);

		result
			.WithParsed(opts => {
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
