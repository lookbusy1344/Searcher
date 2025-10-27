# PicoArgs Migration Design - SearcherGui

**Date:** 2025-10-27
**Status:** Approved
**Author:** Claude Code

## Overview

Migrate SearcherGui from CommandLineParser library to PicoArgs lightweight parser, achieving consistency with SearcherCli while maintaining GUI-specific features and user experience.

## Problem Statement

SearcherGui currently uses CommandLineParser while SearcherCli uses PicoArgs. This creates:
- Dependency duplication (two parsing libraries in the solution)
- Inconsistent command-line interfaces between CLI and GUI
- Maintenance burden (PicoArgs improvements don't benefit GUI)

## Goals

1. **Single source of truth:** Move PicoArgs to SearcherCore, shared by both applications
2. **Consistent parsing:** Use same parser and validation logic across CLI and GUI
3. **Preserve GUI features:** Maintain all GUI-specific options (window size, auto-close, logging)
4. **GUI-appropriate validation:** Allow lenient validation (optional search term at startup)
5. **Better error UX:** Show parsing errors in MessageBox dialog

## Design Decisions

### Decision 1: PicoArgs Location
**Choice:** Move to SearcherCore library

**Rationale:**
- Single source of truth for both SearcherCli and SearcherGui
- Easier to maintain and evolve
- Avoids code duplication or complex file linking

**Alternatives considered:**
- Keep in SearcherCli with linked file: Complex in IDE, confusing for contributors
- Duplicate in both: Maintenance burden, version drift risk

### Decision 2: GUI-Specific Options
**Choice:** Keep all current options (--width, --height, --auto-close, --log-results)

**Rationale:**
- Full backward compatibility with existing scripts/shortcuts
- Users may rely on these for automation
- No significant complexity cost

### Decision 3: Error Handling
**Choice:** Show error dialog and exit (with console fallback)

**Rationale:**
- Better UX than silent failure or console-only errors
- Users launching GUI expect GUI feedback
- Console fallback ensures errors always visible

**Implementation:** Start with Console.Error fallback, enhance with Avalonia MessageBox if needed

### Decision 4: Validation Strategy
**Choice:** GUI-specific validation (lenient at startup, validate on search)

**Rationale:**
- Interactive GUI doesn't need complete config at launch
- Users can start app and fill in search parameters via UI
- Different from CLI which requires search term upfront
- Better matches GUI user expectations

**Key difference from CLI:**
- CLI: Search term required, throws if missing
- GUI: Search term optional at startup, validates when user clicks Search

## Architecture

### File Structure Changes

```
Before:
SearcherCli/
  └── PicoArgs.cs (namespace: PicoArgs_dotnet)
SearcherGui/
  ├── GuiCliOptions.cs (with [Option] attributes)
  └── Program.cs (uses CommandLineParser)

After:
SearcherCore/
  └── PicoArgs.cs (namespace: SearcherCore)
SearcherCli/
  └── Program.cs (uses SearcherCore.PicoArgs)
SearcherGui/
  ├── GuiCliOptions.cs (simple properties, no attributes)
  └── Program.cs (uses SearcherCore.PicoArgs)
```

### GuiCliOptions Simplification

**Before (CommandLineParser):**
```csharp
[Option('f', "folder", Required = false, HelpText = "Folder to search")]
public new DirectoryInfo Folder { get; set; }
```

**After (Plain properties):**
```csharp
public new DirectoryInfo Folder { get; set; }
```

All `[Option]` attributes removed. Class becomes simple POCO inheriting from CliOptions.

### Program.cs Parsing Logic

**Key components:**

1. **Main() error handling:**
   ```csharp
   try {
       Options = ParseCommandLine(args);
       BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
   }
   catch (PicoArgsException ex) {
       ShowErrorDialog($"Command line error: {ex.Message}");
       Environment.Exit(1);
   }
   ```

2. **ParseCommandLine() method:**
   - Creates PicoArgs instance
   - Parses all options (core + GUI-specific)
   - Search term uses `GetParamOpt()` (optional, not required)
   - Validates folder path
   - Calls `pico.Finished()` to catch unexpected args

3. **ShowErrorDialog() helper:**
   - Console.Error fallback (always works)
   - Future enhancement: Avalonia MessageBox if initialization possible

### Command-Line Interface

**Core options (shared with CLI):**
- `-s, --search <text>` - Search term (optional for GUI)
- `-f, --folder <path>` - Folder to search (default: ".")
- `-p, --pattern <pattern>` - File patterns (repeatable)
- `-z, --inside-zips` - Search inside ZIP files
- `-o, --one-thread` - Single-threaded mode
- `-c, --case-sensitive` - Case-sensitive search
- `--hide-errors` - Hide error messages
- `-r, --raw` - Raw output mode

**GUI-specific options:**
- `--width <pixels>` - Initial window width (default: 1000)
- `--height <pixels>` - Initial window height (default: 600)
- `--auto-close` - Close window after search completes
- `--log-results <file>` - Log results to file for diagnostics

**Help:**
- `-h, -?, --help` - Show help and exit

## Testing Strategy

### Test Coverage Required

1. **PicoArgs unit tests** (in SearcherCore tests):
   - Verify parsing behavior
   - Edge cases and error conditions
   - Ensure no regressions from move

2. **GuiCliOptions tests:**
   - Property assignment
   - Inheritance from CliOptions
   - GUI-specific properties

3. **Program parsing tests** (new GuiProgramTests class):
   - Valid argument combinations
   - Invalid arguments throw PicoArgsException
   - Optional search term (GUI-specific)
   - GUI options parse correctly
   - Unknown arguments rejected
   - Folder path validation

4. **SearcherCli tests update:**
   - Reference SearcherCore.PicoArgs
   - Verify no behavioral changes

### Test Examples

```csharp
[Fact]
public void ParseCommandLine_ValidArgs_ReturnsOptions()
{
    var args = new[] { "-f", ".", "-s", "test", "--width", "800" };
    var opts = Program.ParseCommandLine(args);

    Assert.Equal("test", opts.Search);
    Assert.Equal(800, opts.WindowWidth);
}

[Fact]
public void ParseCommandLine_MissingSearch_AllowsEmpty()
{
    var args = new[] { "-f", "." };
    var opts = Program.ParseCommandLine(args);

    Assert.Equal("", opts.Search); // GUI allows empty
}

[Fact]
public void ParseCommandLine_UnknownArg_ThrowsPicoArgsException()
{
    var args = new[] { "--unknown", "value" };

    Assert.Throws<PicoArgsException>(() =>
        Program.ParseCommandLine(args));
}
```

## Implementation Steps

1. **Move PicoArgs.cs to SearcherCore**
   - Update namespace from `PicoArgs_dotnet` to `SearcherCore`
   - Verify SearcherCli references updated

2. **Update SearcherGui.csproj**
   - Remove CommandLineParser package reference

3. **Simplify GuiCliOptions.cs**
   - Remove all `[Option]` attributes
   - Keep properties and inheritance

4. **Rewrite Program.cs**
   - Replace CommandLineParser logic with PicoArgs
   - Implement ParseCommandLine() method
   - Add ShowErrorDialog() helper
   - Update error handling

5. **Update tests**
   - Add GuiProgramTests class
   - Update SearcherCli tests
   - Run full test suite

6. **Format and validate**
   - `dotnet format SearcherGui/SearcherGui.csproj`
   - `dotnet format SearcherCli/SearcherCli.csproj`
   - `dotnet test TestSearcher/`

## Benefits

1. **Consistency:** Both CLI and GUI use same parser
2. **Simplicity:** Remove CommandLineParser dependency, lighter weight
3. **Maintainability:** Single PicoArgs implementation to evolve
4. **Flexibility:** GUI-specific validation matches interactive use case
5. **Better UX:** Parse errors shown in dialog (not just stderr)

## Risks and Mitigations

**Risk:** Breaking existing GUI command-line users
**Mitigation:** Maintain all existing options with same flags

**Risk:** Tests fail during migration
**Mitigation:** Comprehensive test coverage before and after

**Risk:** Error dialog doesn't work pre-Avalonia init
**Mitigation:** Console.Error fallback ensures errors always visible

## Future Enhancements

1. **Enhanced error dialog:** Initialize minimal Avalonia to show MessageBox
2. **Help dialog:** Show formatted help in GUI window instead of console
3. **Config file support:** Load default options from config file
4. **Validation UI:** Show validation errors in status bar instead of blocking dialog
