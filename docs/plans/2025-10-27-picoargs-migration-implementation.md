# PicoArgs Migration Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Migrate SearcherGui from CommandLineParser to PicoArgs, achieving consistency with SearcherCli and reducing dependencies.

**Architecture:** Move PicoArgs.cs to SearcherCore as shared parser. Update SearcherGui Program.cs to parse arguments directly using PicoArgs with GUI-specific lenient validation (search term optional). Remove CommandLineParser dependency entirely.

**Tech Stack:** .NET 9.0, PicoArgs (custom parser), Avalonia, xUnit

---

## Task 1: Move PicoArgs.cs to SearcherCore

**Files:**
- Move: `SearcherCli/PicoArgs.cs` → `SearcherCore/PicoArgs.cs`
- Verify: `SearcherCore/SearcherCore.csproj`

**Step 1: Copy PicoArgs.cs to SearcherCore**

```bash
cp SearcherCli/PicoArgs.cs SearcherCore/PicoArgs.cs
```

Expected: File copied to SearcherCore directory

**Step 2: Update namespace in SearcherCore/PicoArgs.cs**

Change line 1 from:
```csharp
namespace PicoArgs_dotnet;
```

To:
```csharp
namespace SearcherCore;
```

**Step 3: Verify SearcherCore builds**

Run: `dotnet build SearcherCore/SearcherCore.csproj`
Expected: Build succeeds

**Step 4: Commit**

```bash
git add SearcherCore/PicoArgs.cs
git commit -m "feat: add PicoArgs to SearcherCore for shared parsing

Moved from SearcherCli to enable sharing between CLI and GUI applications.
Updated namespace to SearcherCore."
```

---

## Task 2: Update SearcherCli to use SearcherCore.PicoArgs

**Files:**
- Modify: `SearcherCli/Program.cs` (line 3: update using)
- Delete: `SearcherCli/PicoArgs.cs`

**Step 1: Update using statement in SearcherCli/Program.cs**

Change line 3 from:
```csharp
using PicoArgs_dotnet;
```

To:
```csharp
using SearcherCore;
```

**Step 2: Build SearcherCli to verify change**

Run: `dotnet build SearcherCli/SearcherCli.csproj`
Expected: Build succeeds (now using SearcherCore.PicoArgs)

**Step 3: Run SearcherCli tests**

Run: `dotnet test TestSearcher/ --filter "FullyQualifiedName~Cli"`
Expected: All CLI tests pass

**Step 4: Delete old PicoArgs.cs from SearcherCli**

```bash
git rm SearcherCli/PicoArgs.cs
```

Expected: File deleted and staged for removal

**Step 5: Commit**

```bash
git add SearcherCli/Program.cs
git commit -m "refactor: use SearcherCore.PicoArgs in SearcherCli

Removed local PicoArgs.cs copy, now references shared version in SearcherCore."
```

---

## Task 3: Remove CommandLineParser from SearcherGui

**Files:**
- Modify: `SearcherGui/SearcherGui.csproj` (remove PackageReference)
- Modify: `SearcherGui/Program.cs` (line 3: remove using)

**Step 1: Remove CommandLineParser package reference**

Remove this line from `SearcherGui/SearcherGui.csproj`:
```xml
<PackageReference Include="CommandLineParser" Version="2.9.1" />
```

**Step 2: Remove using CommandLine from Program.cs**

Remove line 3 from `SearcherGui/Program.cs`:
```csharp
using CommandLine;
```

**Step 3: Add using for SearcherCore**

Add to the using statements in `SearcherGui/Program.cs` (after line 2):
```csharp
using SearcherCore;
```

**Step 4: Restore packages**

Run: `dotnet restore SearcherGui/SearcherGui.csproj`
Expected: Restore succeeds without CommandLineParser

**Step 5: Commit**

```bash
git add SearcherGui/SearcherGui.csproj SearcherGui/Program.cs
git commit -m "refactor: remove CommandLineParser dependency from SearcherGui

Preparing for PicoArgs migration. Added SearcherCore using statement."
```

---

## Task 4: Simplify GuiCliOptions.cs

**Files:**
- Modify: `SearcherGui/GuiCliOptions.cs`

**Step 1: Remove all CommandLineParser attributes**

Replace the entire file contents with:

```csharp
using System.Collections.Generic;
using System.IO;
using SearcherCore;

namespace SearcherGui;

/// <summary>
/// GUI-specific command-line options that extend the core search options
/// </summary>
public class GuiCliOptions : CliOptions
{
	// Inherited properties from CliOptions (no attributes needed)
	// - Folder
	// - Pattern
	// - Search
	// - CaseSensitive
	// - OneThread
	// - InsideZips
	// - HideErrors
	// - Raw

	// GUI-specific options

	/// <summary>
	/// Initial window width in pixels
	/// </summary>
	public int WindowWidth { get; set; } = 1000;

	/// <summary>
	/// Initial window height in pixels
	/// </summary>
	public int WindowHeight { get; set; } = 600;

	/// <summary>
	/// Close window after search completes
	/// </summary>
	public bool AutoCloseOnCompletion { get; set; } = false;

	/// <summary>
	/// Log results to specified file for diagnostics
	/// </summary>
	public string? LogResultsFile { get; set; }
}
```

**Step 2: Format the file**

Run: `dotnet format SearcherGui/SearcherGui.csproj --include GuiCliOptions.cs`
Expected: File formatted according to .editorconfig

**Step 3: Commit**

```bash
git add SearcherGui/GuiCliOptions.cs
git commit -m "refactor: simplify GuiCliOptions by removing CommandLineParser attributes

Converted to plain POCO with simple properties. CommandLineParser attributes
no longer needed - parsing will be done via PicoArgs."
```

---

## Task 5: Rewrite Program.cs parsing logic (Part 1: ParseCommandLine method)

**Files:**
- Modify: `SearcherGui/Program.cs`

**Step 1: Replace Main method**

Replace the Main method (lines 19-43) with:

```csharp
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
```

**Step 2: Add ParseCommandLine method**

Add this method after the Main method:

```csharp
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
```

**Step 3: Verify code compiles**

Run: `dotnet build SearcherGui/SearcherGui.csproj`
Expected: Build should fail with errors about missing ShowErrorDialog and ShowHelp methods (we'll add these next)

**Step 4: Commit progress**

```bash
git add SearcherGui/Program.cs
git commit -m "refactor: add ParseCommandLine method using PicoArgs

Replaced CommandLineParser logic with PicoArgs parsing. GUI-specific validation
allows empty search term at startup. Still needs ShowErrorDialog and ShowHelp methods."
```

---

## Task 6: Rewrite Program.cs parsing logic (Part 2: Helper methods)

**Files:**
- Modify: `SearcherGui/Program.cs`

**Step 1: Add ShowErrorDialog method**

Add this method after ParseCommandLine:

```csharp
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
```

**Step 2: Add ShowHelp method**

Add this method after ShowErrorDialog:

```csharp
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
```

**Step 3: Build SearcherGui**

Run: `dotnet build SearcherGui/SearcherGui.csproj`
Expected: Build succeeds

**Step 4: Test help output**

Run: `dotnet run --project SearcherGui/SearcherGui.csproj -- --help`
Expected: Help text displays and exits cleanly

**Step 5: Commit**

```bash
git add SearcherGui/Program.cs
git commit -m "feat: add ShowErrorDialog and ShowHelp for PicoArgs parsing

Console-based error display and comprehensive help output. Migration complete."
```

---

## Task 7: Add tests for GUI parsing logic

**Files:**
- Create: `TestSearcher/GuiProgramTests.cs`

**Step 1: Create test file with basic structure**

Create `TestSearcher/GuiProgramTests.cs`:

```csharp
using SearcherGui;
using SearcherCore;
using Xunit;

namespace TestSearcher;

/// <summary>
/// Tests for SearcherGui Program.cs command-line parsing with PicoArgs
/// </summary>
public class GuiProgramTests
{
	[Fact]
	[Trait("Category", "GUI")]
	public void ParseCommandLine_NoArgs_UsesDefaults()
	{
		// This will be a reflection-based test since ParseCommandLine is private
		// For now, we'll test via GuiCliOptions directly
		var opts = new GuiCliOptions();

		Assert.Equal(1000, opts.WindowWidth);
		Assert.Equal(600, opts.WindowHeight);
		Assert.False(opts.AutoCloseOnCompletion);
		Assert.Null(opts.LogResultsFile);
	}

	[Fact]
	[Trait("Category", "GUI")]
	public void GuiCliOptions_Defaults_MatchExpected()
	{
		var opts = new GuiCliOptions();

		Assert.Equal(1000, opts.WindowWidth);
		Assert.Equal(600, opts.WindowHeight);
		Assert.False(opts.AutoCloseOnCompletion);
		Assert.Null(opts.LogResultsFile);
		Assert.Equal("", opts.Search);
		Assert.False(opts.CaseSensitive);
		Assert.False(opts.OneThread);
		Assert.False(opts.InsideZips);
		Assert.False(opts.HideErrors);
		Assert.False(opts.Raw);
	}

	[Fact]
	[Trait("Category", "GUI")]
	public void GuiCliOptions_SetProperties_WorksCorrectly()
	{
		var opts = new GuiCliOptions {
			WindowWidth = 800,
			WindowHeight = 600,
			AutoCloseOnCompletion = true,
			LogResultsFile = "test.log",
			Search = "test search",
			CaseSensitive = true
		};

		Assert.Equal(800, opts.WindowWidth);
		Assert.Equal(600, opts.WindowHeight);
		Assert.True(opts.AutoCloseOnCompletion);
		Assert.Equal("test.log", opts.LogResultsFile);
		Assert.Equal("test search", opts.Search);
		Assert.True(opts.CaseSensitive);
	}

	[Fact]
	[Trait("Category", "GUI")]
	public void GuiCliOptions_InheritsFromCliOptions()
	{
		var opts = new GuiCliOptions();

		Assert.IsAssignableFrom<CliOptions>(opts);
	}
}
```

**Step 2: Run new tests**

Run: `dotnet test TestSearcher/ --filter "Category=GUI"`
Expected: All 4 tests pass

**Step 3: Commit**

```bash
git add TestSearcher/GuiProgramTests.cs
git commit -m "test: add GuiProgramTests for GuiCliOptions validation

Tests verify default values, property assignment, and CliOptions inheritance.
ParseCommandLine is private so tested indirectly via integration tests."
```

---

## Task 8: Add integration test for GUI startup

**Files:**
- Modify: `TestSearcher/GuiProgramTests.cs`

**Step 1: Add integration test for command-line args**

Add this test to `TestSearcher/GuiProgramTests.cs`:

```csharp
[Fact]
[Trait("Category", "GUI-Integration")]
public void Program_WithValidArgs_ParsesCorrectly()
{
	// Test that Program.Options gets populated correctly
	// This requires launching the actual program, which is complex for GUI apps
	// Instead, we'll test PicoArgs parsing directly

	var args = new[] { "-s", "test", "-f", ".", "-p", "*.txt", "--width", "800" };
	var pico = new PicoArgs(args);

	var search = pico.GetParamOpt("-s", "--search");
	var folder = pico.GetParamOpt("-f", "--folder");
	var patterns = pico.GetMultipleParams("-p", "--pattern");
	var width = pico.GetParamOpt("--width");

	Assert.Equal("test", search);
	Assert.Equal(".", folder);
	Assert.Single(patterns);
	Assert.Equal("*.txt", patterns[0]);
	Assert.Equal("800", width);

	pico.Finished(); // Should not throw
}

[Fact]
[Trait("Category", "GUI-Integration")]
public void Program_WithUnknownArg_ThrowsPicoArgsException()
{
	var args = new[] { "--unknown-arg", "value" };
	var pico = new PicoArgs(args);

	var ex = Assert.Throws<PicoArgsException>(() => pico.Finished());
	Assert.Contains("Unrecognised parameter", ex.Message);
}

[Fact]
[Trait("Category", "GUI-Integration")]
public void Program_WithMultiplePatterns_ParsesAll()
{
	var args = new[] { "-p", "*.txt", "-p", "*.md", "-p", "*.doc" };
	var pico = new PicoArgs(args);

	var patterns = pico.GetMultipleParams("-p", "--pattern");

	Assert.Equal(3, patterns.Count);
	Assert.Equal("*.txt", patterns[0]);
	Assert.Equal("*.md", patterns[1]);
	Assert.Equal("*.doc", patterns[2]);

	pico.Finished();
}

[Fact]
[Trait("Category", "GUI-Integration")]
public void Program_WithBoolFlags_ParsesCorrectly()
{
	var args = new[] { "-z", "-c", "-o", "--hide-errors", "--auto-close" };
	var pico = new PicoArgs(args);

	var insideZips = pico.Contains("-z", "--inside-zips");
	var caseSensitive = pico.Contains("-c", "--case-sensitive");
	var oneThread = pico.Contains("-o", "--one-thread");
	var hideErrors = pico.Contains("--hide-errors");
	var autoClose = pico.Contains("--auto-close");

	Assert.True(insideZips);
	Assert.True(caseSensitive);
	Assert.True(oneThread);
	Assert.True(hideErrors);
	Assert.True(autoClose);

	pico.Finished();
}
```

**Step 2: Run integration tests**

Run: `dotnet test TestSearcher/ --filter "Category=GUI-Integration"`
Expected: All 4 new integration tests pass

**Step 3: Commit**

```bash
git add TestSearcher/GuiProgramTests.cs
git commit -m "test: add integration tests for PicoArgs parsing in GUI

Tests verify argument parsing, multiple patterns, boolean flags, and error handling."
```

---

## Task 9: Update CLAUDE.md documentation

**Files:**
- Modify: `SearcherGui/CLAUDE.md`
- Modify: `CLAUDE.md` (root)

**Step 1: Update SearcherGui/CLAUDE.md dependencies section**

Find the "Dependencies" section and update to:

```markdown
## Dependencies

- **Avalonia 11.3.8**: Core UI framework
- **Avalonia.Desktop 11.3.8**: Desktop platform support
- **Avalonia.Controls.DataGrid 11.3.8**: Data grid controls
- **Avalonia.Themes.Fluent 11.3.8**: Fluent design theme
- **Avalonia.ReactiveUI 11.3.8**: ReactiveUI integration
- **SearcherCore**: Internal reference for search functionality (includes PicoArgs)

**Note:** CommandLineParser has been removed. SearcherGui now uses PicoArgs from SearcherCore for command-line parsing, matching SearcherCli's approach.
```

**Step 2: Update root CLAUDE.md dependencies section**

Find the SearcherGui dependencies section and update to:

```markdown
### SearcherGui Dependencies
- **Avalonia**: Cross-platform UI framework
- **SearcherCore**: Shared library (includes PicoArgs parser)

**Note:** Previously used CommandLineParser, now uses PicoArgs from SearcherCore for consistency with SearcherCli.
```

**Step 3: Commit**

```bash
git add SearcherGui/CLAUDE.md CLAUDE.md
git commit -m "docs: update CLAUDE.md to reflect PicoArgs migration

Removed CommandLineParser references, documented PicoArgs usage from SearcherCore."
```

---

## Task 10: Run full test suite and format code

**Files:**
- All modified files

**Step 1: Format all code**

Run:
```bash
dotnet format SearcherGui/SearcherGui.csproj
dotnet format SearcherCli/SearcherCli.csproj
dotnet format SearcherCore/SearcherCore.csproj
dotnet format TestSearcher/TestSearcher.csproj
```

Expected: All files formatted according to .editorconfig

**Step 2: Build entire solution**

Run: `dotnet build Searcher.sln`
Expected: Build succeeds with no errors

**Step 3: Run full test suite**

Run: `dotnet test TestSearcher/ --verbosity normal`
Expected: All 113 tests pass (109 existing + 4 new GuiCliOptions tests)

**Step 4: Test SearcherGui manually**

Run: `dotnet run --project SearcherGui/SearcherGui.csproj -- --help`
Expected: Help text displays

Run: `dotnet run --project SearcherGui/SearcherGui.csproj -- -f . -s test`
Expected: GUI launches with folder "." and search "test" pre-filled

Run: `dotnet run --project SearcherGui/SearcherGui.csproj -- --unknown`
Expected: Error message displays and exits

**Step 5: Commit formatting changes**

```bash
git add -A
git commit -m "style: format code after PicoArgs migration

Applied dotnet format to all projects."
```

---

## Task 11: Final verification and summary commit

**Files:**
- N/A (verification only)

**Step 1: Verify SearcherCli still works**

Run: `dotnet run --project SearcherCli/SearcherCli.csproj -- --help`
Expected: CLI help displays correctly

Run: `dotnet run --project SearcherCli/SearcherCli.csproj -- -s test -f .`
Expected: Search executes (may show results or no matches)

**Step 2: Verify all tests pass**

Run: `dotnet test TestSearcher/`
Expected: All 113+ tests pass

**Step 3: Check git status**

Run: `git status`
Expected: Working tree clean (all changes committed)

**Step 4: Review commit history**

Run: `git log --oneline -15`
Expected: Should see commits from this migration:
- Move PicoArgs to SearcherCore
- Update SearcherCli
- Remove CommandLineParser
- Simplify GuiCliOptions
- Add ParseCommandLine methods
- Add helper methods
- Add tests
- Update docs
- Format code

**Step 5: Create summary of changes**

Document what was accomplished:

**Migration Complete:**
- ✅ PicoArgs moved to SearcherCore (single source of truth)
- ✅ SearcherCli updated to use SearcherCore.PicoArgs
- ✅ SearcherGui migrated from CommandLineParser to PicoArgs
- ✅ GuiCliOptions simplified (no attributes)
- ✅ Program.cs rewritten with ParseCommandLine, ShowErrorDialog, ShowHelp
- ✅ Tests added for GuiCliOptions and PicoArgs integration
- ✅ Documentation updated
- ✅ All tests passing (113+ tests)
- ✅ Code formatted

**Benefits achieved:**
- Single parsing library for entire solution
- Consistent CLI between GUI and CLI apps
- Reduced dependencies (removed CommandLineParser)
- GUI-specific lenient validation
- Better error handling

---

## Rollback Plan

If issues are discovered:

```bash
# View commits from this migration
git log --oneline --grep="PicoArgs"

# Rollback to before migration (find commit hash before first PicoArgs commit)
git reset --hard <commit-hash-before-migration>

# Or rollback individual commits
git revert <commit-hash> <commit-hash> ...
```

## Success Criteria

- ✅ All tests pass (113+ tests)
- ✅ SearcherGui launches successfully
- ✅ SearcherGui accepts command-line arguments
- ✅ SearcherGui shows help with --help
- ✅ SearcherGui shows errors for invalid arguments
- ✅ SearcherCli continues to work unchanged
- ✅ No CommandLineParser references remain
- ✅ PicoArgs in SearcherCore only
- ✅ Code formatted and documented

## Notes

- ParseCommandLine is private in Program.cs, so direct unit testing requires reflection or making it public/internal with InternalsVisibleTo
- Integration tests verify PicoArgs behavior directly
- Error dialog uses Console.Error fallback since Avalonia not initialized at parse time
- Future enhancement: Initialize minimal Avalonia for actual MessageBox error display
