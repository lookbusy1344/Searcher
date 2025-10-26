namespace SearcherCli;

using PicoArgs_dotnet;
using SearcherCore;

public static class Program
{
	private static bool rawOutput;

	private static int Main(string[] args)
	{
		try {
			var parsed = ParseCommandLine(args);

			rawOutput = parsed.Raw;

			if (!rawOutput) {
				var git = GitVersion.VersionInfo.Get();
				Console.WriteLine($"Searcher - recursively searching inside files, including zips and pdfs ({git.GetVersionHash(8)})");
				Console.WriteLine(parsed.OneThread ? "Single thread mode" : "Multi-thread mode");
			}

			WriteMessage($"Folder: {parsed.Folder}, pattern: {parsed.GetPatterns()}, search string: \"{parsed.Search}\"", true);

			using var mainSearch = new MainSearch();
			mainSearch.Search(parsed);

			return 0;
		}
		catch (HelpException) {
			// --help has been requested
			Console.WriteLine(CommandLineMessage);
			return 0;
		}
		catch (Exception ex) {
			// any other exception
			Console.WriteLine($"ERROR: {ex.Message}\r\n");
			Console.WriteLine(CommandLineMessage);
			return 1;
		}
	}

	/// <summary>
	/// Wrap the call to PicoArgs in a using block, so it automatically throws if there are any errors
	/// </summary>
	private static CliOptions ParseCommandLine(string[] args)
	{
		using var pico = new PicoArgsDisposable(args);

		var help = pico.Contains("-h", "-?", "--help");
		if (help) {
			// if we want help, just bail here. Suppress the warning about not using other parameters
			pico.SuppressCheck = true;
			throw new HelpException();
		}

		// parse the rest of the command line
		var search = pico.GetParam("-s", "--search");
		var folder = pico.GetParamOpt("-f", "--folder") ?? ".";
		var patterns = pico.GetMultipleParams("-p", "--pattern");
		var insideZips = pico.Contains("-z", "--inside-zips");
		var oneThread = pico.Contains("-o", "--one-thread");
		var caseSensitive = pico.Contains("-c", "--case-sensitive");
		var hideErrors = pico.Contains("--hide-errors");
		var raw = pico.Contains("-r", "--raw");

		// Validate the folder path
		var validatedFolder = Utils.ValidateSearchPath(folder);
		if (validatedFolder == null) {
			throw new ArgumentException($"Invalid or inaccessible folder path: {folder}");
		}

		return new() {
			Search = search,
			Folder = new(validatedFolder),
			Pattern = patterns.AsReadOnly(),
			InsideZips = insideZips,
			OneThread = oneThread,
			CaseSensitive = caseSensitive,
			HideErrors = hideErrors,
			Raw = raw
		};
	}

	/// <summary>
	/// Display helpful message is not in --raw mode
	/// </summary>
	internal static void WriteMessage(string msg, bool blankLine = false)
	{
		if (rawOutput) {
			return;
		}

		Console.WriteLine(msg);
		if (blankLine) {
			Console.WriteLine();
		}
	}

	private const string CommandLineMessage = """
											  Recursively search for files containing text.

											  Mandatory parameters:
											    --search <text>, -s <text>          Text to find

											  Optional parameters:
											    --folder <x>, -f <x>                Folder to search (default '.')
											    --pattern <x>, -p <x>               File patterns to match eg '*.txt', can be repeated eg -p *.txt -p *.doc (default '*')

											    --inside-zips, -z                   Always search inside zip files. Implies -p *.zip
											    --one-thread, -o                    Don't search files in parallel
											    --case-sensitive, -c                Text is matched in a case-sensitive way
											    --hide-errors                       Hide errors from the output list
											    --raw, -r                           Suppress all non-error messages

											  Examples. Search current folder for txt and Word files containing "hello world":
											    Searcher --folder . --pattern *.txt,*.docx --search "hello world"

											  Search just zip files for anything containing 'hello':
											    Searcher -f . -p *.zip -s hello

											  Search txt files (including those in zips) for anything containing "hello":
											    Searcher -z -f . -p *.txt -s hello
											  Or..
											    Searcher -f . -p *.txt,*.zip -s hello

											  Search txt files (excluding those in zips) for anything containing "hello":
											    Searcher -f . -p *.txt -s hello
											  """;
}
