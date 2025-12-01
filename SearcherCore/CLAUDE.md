# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Structure

SearcherCore is a shared .NET 10.0 library that provides core functionality for Searcher applications. It's part of a larger solution that includes:
- SearcherGui Avalonia application (cross-platform GUI)
- SearcherCli command-line application (cross-platform CLI)
- TestSearcher unit test project

## Core Architecture

The library contains specialized search functionality across different file types:

- **CliOptions.cs**: Configuration and options management for search operations, including parallelism control based on storage type (SSD vs spinning disk)
- **SearchFile.cs**: Main file content searching with special handling for .docx, .pdf, and .zip files
- **GlobSearch.cs**: File discovery using glob patterns with recursive directory traversal
- **PdfCheck.cs**: PDF-specific text extraction and searching using iText7
- **Utils.cs**: Utility functions for file type detection and common operations
- **SearchResult.cs**: Search result data structure

Key dependencies:
- DotNet.Glob for pattern matching
- iText7 for PDF processing
- Built-in System.IO.Compression for ZIP files

## Build Commands

```bash
# Build the library
dotnet build

# Build in release mode
dotnet build -c Release

# Clean build artifacts
dotnet clean

# Restore packages
dotnet restore

# Format code after changes
dotnet format
```

## Testing

Tests are located in the TestSearcher project in the parent solution. From the solution root:

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run specific test project
dotnet test TestSearcher/TestSearcher.csproj
```

## Code Style

The project uses comprehensive .editorconfig rules with:
- Opening braces on same line for code blocks (not methods/types)
- UTF-8 encoding with final newlines
- Comprehensive Roslyn analyzers enabled (all analysis modes: Design, Security, Performance, Reliability, Usage)
- Specific C# formatting preferences defined

Code analyzers include:
- Microsoft.VisualStudio.Threading.Analyzers
- Roslynator.Analyzers  
- lookbusy1344.RecordValueAnalyser

**Important**: Always run `dotnet format` after making code changes to ensure consistent formatting. Use LF line endings for all markdown files.

## Important Implementation Details

- **Thread Safety**: Uses CancellationToken throughout for cooperative cancellation
- **Performance**: Automatically adjusts parallelism based on storage type (SSD gets full cores, spinning disk gets half)
- **File Type Detection**: Uses both extension checking and magic number detection for ZIP files
- **Error Handling**: Graceful degradation with inaccessible files and directories
