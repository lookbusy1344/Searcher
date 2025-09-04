# SearcherCli

A high-performance command-line text search tool that recursively searches for text inside files, including archives (ZIP), PDFs, and DOCX files.

## Features

- **Multi-format support**: Search inside text files, PDFs, DOCX documents, and ZIP archives
- **Recursive archive handling**: Supports nested ZIP files and archives containing other formats
- **Parallel processing**: Hardware-aware performance optimization (SSD vs HDD detection)
- **Glob pattern matching**: Flexible file filtering with pattern support
- **Cross-platform**: Built on .NET 9.0, supports Windows, macOS, and Linux

## Installation

### Build from Source

```bash
cd Searcher/SearcherCli

# Build the project
dotnet build

# Or build for release (single-file bundle)
dotnet publish -r win-x64 -c Release -p:PublishSingleFile=true --self-contained true
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
| `--open-with <x>`, `-w <x>` | Open files with this program instead of Notepad |
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
- **Automatic hardware detection**: Adjusts parallelism based on SSD vs HDD
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

- .NET 9.0 Runtime
- iText7 (for PDF processing)
- DotNet.Glob (for pattern matching)

## Building

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Self-contained executable
dotnet publish -r [runtime-id] -c Release -p:PublishSingleFile=true --self-contained true
```

## Testing

```bash
dotnet test ../TestSearcher/
```
