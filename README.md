# Searcher - multi-threaded file searcher

[![CodeQL](https://github.com/lookbusy1344/Searcher/actions/workflows/github-code-scanning/codeql/badge.svg)](https://github.com/lookbusy1344/Searcher/actions/workflows/github-code-scanning/codeql)

Recursively search for files containing text. Built with C#, .NET 9 and WinForms

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
  Searcher.exe --folder . --pattern *.txt,*.docx --search "hello world"
  
Search just zip files for anything containing 'hello':
  Searcher.exe -f . -p *.zip -s hello

Search txt files (including those in zips) for anything containing 'hello':
  Searcher.exe -z -f . -p *.txt -s hello
Or..
  Searcher.exe -f . -p *.txt,*.zip -s hello

Search txt files (excluding those in zips) for anything containing 'hello':
  Searcher.exe -f . -p *.txt -s hello

```

Searcher can look inside zips (use the -z option), docx and pdf files.

## Building

Requires Visual Studio 2022, or .NET 9 SDK.

```
dotnet publish -c Release
```

Or to make a single-file bundle with necessary nuget packages:

```
publish.cmd
```

## Testing

xUnit is used for testing. They can be run from Test Explorer in Visual Studio, or by running:

```
RunTests.cmd
```
