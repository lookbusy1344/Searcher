# Searcher - Multi-threaded File Searcher

[![CodeQL](https://github.com/lookbusy1344/Searcher/actions/workflows/github-code-scanning/codeql/badge.svg)](https://github.com/lookbusy1344/Searcher/actions/workflows/github-code-scanning/codeql)

Recursively search for files containing text. Built with C#, .NET 9 and Avalonia

## Available Versions

- **SearcherGui** (`SearcherGui/` directory): Cross-platform Avalonia GUI application
- **SearcherCli** (`SearcherCli/` directory): Command-line version that works on Windows, Mac, and Linux

## Parameters

```
Mandatory parameters:
  --search <text>, -s <text>          Text to find

Optional parameters:
  --folder <x>, -f <x>                Folder to search (default '.')
  --pattern <x, ...>, -p <x, ...>     File patterns to match eg '*.txt,*.docx' (default '*')
  --open-with <x>, -w <x>             Open files with this program instead of Notepad

  --inside-zips, -z                   Always search inside zip files. Implies -p *.zip
  --one-thread, -o                    Don't search files in parallel
  --case-sensitive, -c                Text is matched in a case-sensitive way
  --hide-errors, -h                   Hide errors in output list

```

## Examples

```
Search current folder for txt and Word files containing 'hello world':
  SearcherGui --folder . --pattern *.txt,*.docx --search "hello world"

Search just zip files for anything containing 'hello':
  SearcherGui -f . -p *.zip -s hello

Search txt files (including those in zips) for anything containing 'hello':
  SearcherGui -z -f . -p *.txt -s hello
Or..
  SearcherGui -f . -p *.txt,*.zip -s hello

Search txt files (excluding those in zips) for anything containing 'hello':
  SearcherGui -f . -p *.txt -s hello

```

Searcher can look inside zips (use the -z option), docx and pdf files.

## SearcherCli - Command Line Version

The `SearcherCli/` directory contains a cross-platform command-line version that works on Windows, Mac, and Linux. It shares the same core search functionality but provides a pure console interface.

### Building SearcherCli

```
cd SearcherCli/
dotnet build

# Or for a self-contained executable:
dotnet publish -r win-x64 -c Release -p:PublishSingleFile=true --self-contained true
# For Mac: use -r osx-x64 or -r osx-arm64
# For Linux: use -r linux-x64
```

### SearcherCli Examples

```
# Basic search
SearcherCli --search "hello world" --folder /path/to/search

# Search with patterns
SearcherCli -s "TODO" -f src/ -p "*.cs,*.js"

# Search inside archives (cross-platform)
SearcherCli -z -f . -p "*.txt" -s "config"

# Get help
SearcherCli --help
```

For detailed SearcherCli documentation, see `SearcherCli/README.md`.

## Building SearcherGui (Avalonia GUI)

Requires .NET 9 SDK.

```
# Build for current platform
dotnet build SearcherGui/SearcherGui.csproj -c Release

# Publish as single-file executable
dotnet publish SearcherGui/SearcherGui.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
# For Mac: use -r osx-arm64 or -r osx-x64
# For Linux: use -r linux-x64
```

## .NET version support

`Main` branch supports .NET 9. `dotnet8` branch for .NET 8, and .NET 7 work closed out in commit [5daec2245f](https://github.com/lookbusy1344/Searcher/tree/5daec2245f42a0d4146ba2b824bdf894f349c627)

## Testing

xUnit is used for testing. They can be run from Test Explorer in Visual Studio, or by running:

```
RunTests.cmd
```
