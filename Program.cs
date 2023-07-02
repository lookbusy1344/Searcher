using CommandLine;
using DotNet.Globbing;

namespace Searcher;

internal static class Program
{
	/// <summary>
	///  The main entry point for the application.
	/// </summary>
	[STAThread]
	public static void Main(string[] args)
	{
		// To customize application configuration such as set high DPI settings or default font,
		// see https://aka.ms/applicationconfiguration.

		GlobOptions.Default.Evaluation.CaseInsensitive = true;

		var parsed = Parser.Default.ParseArguments<CliOptions>(args)
			.WithParsed<CliOptions>(o =>
			{
				// if not specified, pattern seems to be string[0] rather than null
				// Cant use Length so using Count() instead
				if (o.Pattern == null || o.Pattern.Count == 0)
					o.Pattern = CliOptions.DefaultPattern;

			}).WithNotParsed<CliOptions>(o =>
			{
			});

		if (parsed.Tag == ParserResultType.NotParsed)
		{
			// https://stackoverflow.com/questions/23718966/how-console-write-in-c-sharp-windows-form-application
			// workaround for console output in a WinForms app

			var info = GitVersion.VersionInfo.Get();

			_ = MainForm.AttachConsole(MainForm.ATTACH_PARENT_PROCESS);
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine($"Searcher {info.GetVersionHash(20)}");
			Console.WriteLine("Recursively search for files containing text.");
			Console.WriteLine();
			Console.WriteLine("Mandatory parameters:");
			Console.WriteLine("  --search <text>, -s <text>          Text to find");
			Console.WriteLine();
			Console.WriteLine("Optional parameters:");
			Console.WriteLine("  --folder <x>, -f <x>                Folder to search (default '.')");
			Console.WriteLine("  --pattern <x, ...>, -p <x, ...>     File patterns to match eg '*.txt,*.docx' (default '*')");
			Console.WriteLine();
			Console.WriteLine("  --inside-zips, -z                   Always search inside zip files. Implies -p *.zip");
			Console.WriteLine("  --one-thread, -o                    Don't search files in parallel");
			Console.WriteLine("  --case-sensitive, -c                Text is matched in a case-sensitive way");
			Console.WriteLine("  --hide-errors, -h                   Hide errors from the output list");
			Console.WriteLine();
			Console.WriteLine("Examples. Search current folder for txt and Word files containing 'hello world':");
			Console.WriteLine("  Searcher.exe --folder . --pattern *.txt,*.docx --search \"hello world\"");
			Console.WriteLine("Search just zip files for anything containing 'hello':");
			Console.WriteLine("  Searcher.exe -f . -p *.zip -s hello");
			Console.WriteLine("Search txt files (including those in zips) for anything containing 'hello':");
			Console.WriteLine("  Searcher.exe -z -f . -p *.txt -s hello");
			Console.WriteLine("Or..");
			Console.WriteLine("  Searcher.exe -f . -p *.txt,*.zip -s hello");
			Console.WriteLine("Search txt files (excluding those in zips) for anything containing 'hello':");
			Console.WriteLine("  Searcher.exe -f . -p *.txt -s hello");
			Console.WriteLine();
			return;
		}

		ApplicationConfiguration.Initialize();

		var form = new MainForm { cliOptions = parsed.Value };

		Application.Run(form);
	}
}