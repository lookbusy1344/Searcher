# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Searcher is a C# WinForms application that recursively searches for text inside files, including archives (ZIP), PDFs, and DOCX files. It uses .NET 9.0 targeting Windows and supports parallel processing for performance.

**Project Structure:**
- **Root directory**: Contains the main WinForms application
- **SearcherCli/**: Console version of the application
- **TestSearcher/**: xUnit test project

## Build Commands

```bash
# Build the entire solution
dotnet build

# Build for release (single-file bundle)
Publish.cmd

# Build console version specifically
dotnet build SearcherCli/SearcherCli.csproj

# Run the console application
dotnet run --project SearcherCli -- --help
```

## Test Commands

```bash
# Run tests (Windows batch script)
RunTests.cmd

# Run tests using dotnet (modern approach)
dotnet test TestSearcher/

# Build and run tests
dotnet build && dotnet test TestSearcher/
```

## Development Commands

The project uses extensive code analysis and formatting rules defined in `.editorconfig`.

```bash
# Restore packages for entire solution
dotnet restore

# Clean build artifacts
dotnet clean

# Format code (relies on editorconfig settings)
dotnet format

# IMPORTANT: Always run 'dotnet format' after making code changes to ensure consistent formatting

# Publish on Windows (single file, framework-dependent):
dotnet publish SearcherCli.csproj -r win-arm64 -c Release -p:PublishSingleFile=true --self-contained false

# Publish on Apple Silicon (single file, framework-dependent):
dotnet publish SearcherCli.csproj -r osx-arm64 -c Release -p:PublishSingleFile=true --self-contained false
```

## Project Architecture

### Main Application Components (Root Directory)

- **Program.cs**: Entry point for WinForms application
- **MainForm.cs/.Designer.cs**: Main UI form with search interface
- **CliOptions.cs**: Shared configuration class with search parameters
- **SearchFile.cs**: Core file content searching logic for different file types
- **GlobSearch.cs**: Parallel file discovery using glob patterns
- **Utils.cs**: Utility functions for pattern processing and file operations
- **PdfCheck.cs**: PDF-specific search implementation using iText7
- **DiskQuery.cs**: Hardware detection for performance optimization
- **SafeCounter.cs**, **ProgressTimer.cs**, **MonotonicDateTime.cs**: Performance and threading utilities

### Console Application Components (SearcherCli/)

- **Program.cs**: Console entry point with command line parsing
- **MainSearch.cs**: Main search orchestrator for CLI version
- **PicoArgs.cs**: Custom lightweight command line argument parser
- Shares core components with main application via file references

### Key Architectural Patterns

1. **Dual Interface Architecture**: Both WinForms GUI and Console CLI share core search logic
2. **File Type Routing**: Different search strategies based on file extension (.txt, .docx, .pdf, .zip)
3. **Parallel Processing**: Uses `Parallel.ForEach` for both file discovery and content searching
4. **Recursive ZIP Handling**: Supports nested ZIP files and archives containing other formats
5. **Stream-based Processing**: Efficient memory usage when reading large files
6. **Hardware-Aware Performance**: Detects SSD vs HDD to adjust parallelism automatically

### Search Flow

1. Parse command line arguments (CLI) or form inputs (GUI) into `CliOptions`
2. Process file patterns for outer files vs inner archive files
3. Use `GlobSearch.ParallelFindFiles()` to discover matching files
4. Apply parallel search using `SearchFile.FileContainsStringWrapper()`
5. Route to specific handlers based on file type (text/DOCX/PDF/ZIP)
6. Display results with error handling

### Performance Considerations

- **SSD Detection**: `DiskQuery.cs` adjusts parallelism based on storage type (full cores for SSD, half for HDD)
- **Memory Efficiency**: Stream-based reading, minimal string allocations
- **Parallel File Discovery**: Custom parallel directory traversal algorithm
- **Magic Number Detection**: Uses file signatures to identify ZIP files regardless of extension
- **Thread-Safe Counters**: `SafeCounter.cs` for progress tracking across threads

## Dependencies

Key NuGet packages:
- **DotNet.Glob**: For file pattern matching
- **itext7**: For PDF text extraction
- **System.Management**: For hardware detection
- **xunit**: For unit testing (TestSearcher only)

**Note**: SearcherCli uses a custom lightweight command line parser (`PicoArgs.cs`) instead of external packages for argument parsing.

## Configuration

The project uses comprehensive code analysis with multiple analyzers:
- Microsoft.VisualStudio.Threading.Analyzers
- Roslynator.Analyzers
- lookbusy1344.RecordValueAnalyser

Code style is enforced via `.editorconfig` with specific rules for C# formatting, naming conventions, and analysis severity levels. The configuration includes extensive Roslynator rules for code quality.
