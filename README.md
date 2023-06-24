# Searcher

C# / .NET 7 project to recursively search a folder, looking for files containing a given string.

## Parameters

```
Searcher.exe [-f <folder>] [-p <patterns>] -s <text to find> [-z]
```

For example:

```
Searcher.exe -f . -p *.txt,*.log -s "Hardware"
Searcher.exe -f "C:\docs" -p *.docx -s "Hardware" -z

```

Searcher can look inside zips (use the -z option), and docx files.
