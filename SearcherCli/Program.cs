namespace SearcherCli;

using PicoArgs_dotnet;

public static class Program
{
	private static bool raw;

	private static int Main(string[] args)
	{
		try {
			var parsed = ParseCommandLine(args);

			//raw = parsed.Raw;

			if (!raw) {
				//Console.WriteLine($"ZipDir - list contents of zip files {ver.GetVersionHash(12)}");
				Console.WriteLine(parsed.OneThread ? "Single thread mode" : "Multi-thread mode");
			}

			WriteMessage($"Folder: {parsed.Folder}, pattern: {parsed.Pattern}", true);
			MainSearch.LongRunningTask(parsed);
			return 0;
		}
		catch (HelpException) {
			// --help has been requested
			//Console.WriteLine($"ZipDir - list contents of zip files {ver.GetVersionHash(20)}");
			Console.WriteLine(CommandLineMessage);
			return 0;
		}
		catch (Exception ex) {
			// any other exception
			Console.WriteLine($"ERROR: {ex.Message}\r\n");
			//Console.WriteLine($"ZipDir - list contents of zip files {ver.GetVersionHash(12)}");
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
		var openWith = pico.GetParamOpt("-w", "--open-with");
		var insideZips = pico.Contains("-z", "--inside-zips");
		var oneThread = pico.Contains("-o", "--one-thread");
		var caseSensitive = pico.Contains("-c", "--case-sensitive");
		var hideErrors = pico.Contains("-h", "--hide-errors");

		return new() {
			Search = search,
			Folder = new(folder),
			Pattern = patterns.AsReadOnly(),
			OpenWith = openWith,
			InsideZips = insideZips,
			OneThread = oneThread,
			CaseSensitive = caseSensitive,
			HideErrors = hideErrors
		};
	}

	/// <summary>
	/// Display helpful message is not in --raw mode
	/// </summary>
	internal static void WriteMessage(string msg, bool blankLine = false)
	{
		if (!raw) {
			Console.WriteLine(msg);
			if (blankLine) {
				Console.WriteLine();
			}
		}
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
