# Searcher

Recursively search for files containing text. C#, .NET 7 and WinForms

## Parameters

```
Mandatory parameters:
  --search <text>, -s <text>          Text to find

Optional parameters:
  --folder <x>, -f <x>                Folder to search (default '.')
  --pattern <x, ...>, -p <x, ...>     File patterns to match eg '*.txt,*.docx' (default '*')

  --inside-zips, -z                   Always search inside zip files. Implies -p *.zip
  --one-thread, -o                    Don't search files in parallel
  --case-sensitive, -c                Text is matched in a case-sensitive way

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

Searcher can look inside zips (use the -z option), and docx files.

## Building

Requires Visual Studio 2022, or .NET 7 SDK.

```
dotnet publish -c Release
```