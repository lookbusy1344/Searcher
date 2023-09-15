using CommandLine;
using DotNet.Globbing;

namespace Searcher;

public static class Program
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

		var parsed = ParseParams(args);

		if (parsed.Tag == ParserResultType.NotParsed)
		{
			// https://stackoverflow.com/questions/23718966/how-console-write-in-c-sharp-windows-form-application
			// workaround for console output in a WinForms app

			var info = GitVersion.VersionInfo.Get();

			_ = MainForm.AttachConsole(MainForm.ATTACH_PARENT_PROCESS);
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine($"Searcher {info.GetVersionHash(20)}");
			Console.WriteLine(CommandLineMessage);
			Console.WriteLine();
			return;
		}

		ApplicationConfiguration.Initialize();

		using var form = new MainForm { cliOptions = parsed.Value };

		Application.Run(form);
	}

	/// <summary>
	/// Parse the command line, seperated out to help with testing
	/// </summary>
	public static ParserResult<CliOptions> ParseParams(string[] args)
	{
		return Parser.Default.ParseArguments<CliOptions>(args)
			.WithParsed<CliOptions>(o => o.IsSSD = DiskQuery.IsSSD(o.Folder));
		//.WithNotParsed<CliOptions>(o =>
		//{
		//});
	}

	private const string CommandLineMessage = """
		Recursively search for files containing text.

		Mandatory parameters:
		  --search <text>, -s <text>          Text to find

		Optional parameters:
		  --folder <x>, -f <x>                Folder to search (default '.')
		  --pattern <x, ...>, -p <x, ...>     File patterns to match eg '*.txt,*.docx' (default '*')
		  --open-with <x>, -w <x>             Open files with this program instead of Notepad

		  --inside-zips, -z                   Always search inside zip files. Implies -p *.zip
		  --one-thread, -o                    Don't search files in parallel
		  --case-sensitive, -c                Text is matched in a case-sensitive way
		  --hide-errors, -h                   Hide errors from the output list

		Examples. Search current folder for txt and Word files containing "hello world":
		  Searcher.exe --folder . --pattern *.txt,*.docx --search "hello world"

		Search just zip files for anything containing 'hello':
		  Searcher.exe -f . -p *.zip -s hello

		Search txt files (including those in zips) for anything containing "hello":
		  Searcher.exe -z -f . -p *.txt -s hello
		Or..
		  Searcher.exe -f . -p *.txt,*.zip -s hello

		Search txt files (excluding those in zips) for anything containing "hello":
		  Searcher.exe -f . -p *.txt -s hello
		""";
}
