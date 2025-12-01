# SearcherCli

A high-performance command-line text search tool that recursively searches for text inside files, including archives (ZIP), PDFs, and DOCX files.

## Features

- **Multi-format support**: Search inside text files, PDFs, DOCX documents, and ZIP archives
- **Recursive archive handling**: Supports nested ZIP files and archives containing other formats
- **Parallel processing**: Efficient multi-threaded file searching
- **Glob pattern matching**: Flexible file filtering with pattern support
- **Cross-platform**: Built on .NET 10.0, supports Windows, macOS, and Linux

## Installation

### Build from Source

```bash
cd Searcher/SearcherCli

# Build the project
dotnet build

# Or build for release (single-file bundle)
dotnet publish SearcherCli.csproj -c Release -r osx-arm64 -p:PublishSingleFile=true -p:PublishAot=false --self-contained false
```

## Usage

```bash
# Basic text search
SearcherCli --search "your search term" --folder "/path/to/search"

# Search with file pattern filtering
SearcherCli --search "TODO" --folder "src/" --pattern "*.cs"

# Search inside archives
SearcherCli --search "config" --folder "backups/" --inside-zips

# Case-sensitive search
SearcherCli --search "Error" --case-sensitive --folder "logs/"

# Get help
SearcherCli --help
```

## Command Line Options

### Mandatory Parameters
| Option | Description |
|--------|-------------|
| `--search <text>`, `-s <text>` | Text to search for (required) |

### Optional Parameters
| Option | Description |
|--------|-------------|
| `--folder <x>`, `-f <x>` | Folder to search (default: current directory) |
| `--pattern <x, ...>`, `-p <x, ...>` | File patterns to match, e.g., '*.txt,*.docx' (default: '*') |
| `--inside-zips`, `-z` | Always search inside zip files. Implies -p *.zip |
| `--one-thread`, `-o` | Don't search files in parallel |
| `--case-sensitive`, `-c` | Text is matched in a case-sensitive way |
| `--hide-errors`, `-h` | Hide errors from the output list |
| `--raw`, `-r` | Suppress all non-error messages |

## Supported File Types

- **Text files**: .txt, .cs, .js, .py, .html, .xml, .json, etc.
- **PDF files**: Extracts and searches text content
- **DOCX files**: Searches inside Word documents
- **ZIP archives**: Recursively searches files within archives
- **Nested archives**: Supports ZIP files containing other ZIP files

## Performance

SearcherCli is optimized for performance:
- **Parallel file discovery**: Custom parallel directory traversal
- **Stream-based processing**: Efficient memory usage for large files
- **Magic number detection**: Identifies file types regardless of extension

## Examples

### Search current folder for txt and Word files containing "hello world"
```bash
SearcherCli --folder . --pattern "*.txt,*.docx" --search "hello world"
```

### Search just zip files for anything containing 'hello'
```bash
SearcherCli -f . -p "*.zip" -s "hello"
```

### Search txt files (including those in zips) for anything containing "hello"
```bash
SearcherCli -z -f . -p "*.txt" -s "hello"
# Or alternatively:
SearcherCli -f . -p "*.txt,*.zip" -s "hello"
```

### Search txt files (excluding those in zips) for anything containing "hello"
```bash
SearcherCli -f . -p "*.txt" -s "hello"
```

## Dependencies

- .NET 10.0 Runtime
- SearcherCore (project reference - provides all core search functionality)
  - Inherits: iText7 (PDF processing), DotNet.Glob (pattern matching)

## Building

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Self-contained executable
dotnet publish SearcherCli.csproj -c Release -r [runtime-id] -p:PublishSingleFile=true -p:PublishAot=false --self-contained false
```

Note using AOT compilation is not supported due to dependencies in third-party libraries.

## Testing

SearcherCli is included in the main Searcher.sln along with all other projects.

```bash
# Run tests from repository root
dotnet test TestSearcher/

# Or test entire solution
dotnet test Searcher.sln
```
