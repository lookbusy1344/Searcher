# SearcherCore

A .NET 10.0 shared library providing core file searching functionality for Searcher applications.

## Overview

SearcherCore is the foundational library that powers file content searching across different file types and formats. It provides specialized handlers for documents, PDFs, archives, and plain text files with intelligent performance optimization.

## Features

- **Multi-format Support**: Search within .docx, .pdf, .zip, and plain text files
- **Glob Pattern Matching**: Flexible file discovery using glob patterns
- **Performance Optimization**: Automatic parallelism adjustment based on storage type (SSD vs HDD)
- **Thread Safety**: Built with CancellationToken support for cooperative cancellation
- **Graceful Error Handling**: Continues operation when encountering inaccessible files

## Core Components

| Component | Purpose |
|-----------|---------|
| `SearchFile.cs` | Main file content searching with format-specific handlers |
| `GlobSearch.cs` | File discovery using glob patterns with recursive traversal |
| `PdfCheck.cs` | PDF text extraction and searching using iText7 |
| `CliOptions.cs` | Configuration and options management |
| `Utils.cs` | File type detection and utility functions |
| `SearchResult.cs` | Search result data structures |

## Dependencies

- **DotNet.Glob** - Pattern matching
- **iText7** - PDF processing
- **System.IO.Compression** - ZIP file handling

## Usage

This library is designed to be consumed by:
- SearcherGui Avalonia application
- SearcherCli command-line tool
- TestSearcher unit test project

## Building

```bash
# Build the library
dotnet build

# Release build
dotnet build -c Release

# Clean artifacts
dotnet clean

# Restore packages
dotnet restore
```

## Code Quality

The project enforces strict code quality through:
- Comprehensive Roslyn analyzers (Design, Security, Performance, Reliability, Usage)
- Microsoft.VisualStudio.Threading.Analyzers
- Roslynator.Analyzers
- RecordValueAnalyser

Always run `dotnet format` after making changes to maintain consistent formatting.

## Testing

Tests are located in the parent solution's TestSearcher project:

```bash
# Run all tests from solution root
dotnet test

# Detailed test output
dotnet test --verbosity normal
```

## Architecture Notes

- **Storage-Aware Performance**: Automatically detects SSD vs spinning disk and adjusts thread pool accordingly
- **Magic Number Detection**: Uses both file extensions and magic numbers for accurate file type identification
- **Cooperative Cancellation**: All operations support CancellationToken for responsive user experience
