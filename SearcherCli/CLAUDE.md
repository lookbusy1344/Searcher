# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Searcher is a C# WinForms application that recursively searches for text inside files, including archives (ZIP), PDFs, and DOCX files. It uses .NET 9.0 targeting Windows and supports parallel processing for performance.

**Project Structure:**
- **Root directory**: Contains the main WinForms application
- **SearcherCli/**: Console version of the application
- **SearcherCore/**: Shared .NET library containing core search functionality
- **TestSearcher/**: xUnit test project

## Build Commands

```bash
# Build the entire solution (cross-platform compatible)
dotnet build

# Build for release (single-file bundle)
Publish.cmd

# Build console version specifically
dotnet build SearcherCli/SearcherCli.csproj

# Build shared library specifically
dotnet build SearcherCore/SearcherCore.csproj

# Run the console application
dotnet run --project SearcherCli -- --help
```

## Test Commands

**Note**: The SearcherCli solution now includes SearcherCli, SearcherCore, and TestSearcher projects.

```bash
# Run tests (Windows batch script)
RunTests.cmd

# Run tests using dotnet (TestSearcher now included in solution)
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Build and run tests
dotnet build && dotnet test
```

## Development Commands

The project uses extensive code analysis and formatting rules defined in `.editorconfig`.

```bash
# Restore packages for entire solution
dotnet restore

# Clean build artifacts
dotnet clean

# Format code (relies on editorconfig settings)
dotnet format SearcherCli.csproj

# IMPORTANT: Always run 'dotnet format' after making code changes to ensure consistent formatting

# Publish on Windows (single file, self-contained):
dotnet publish SearcherCli.csproj -c Release -r win-arm64 -p:PublishSingleFile=true -p:PublishAot=false --self-contained false

# Publish on Apple Silicon (single file, self-contained):
dotnet publish SearcherCli.csproj -c Release -r osx-arm64 -p:PublishSingleFile=true -p:PublishAot=false --self-contained false
```

## Project Architecture

### Main WinForms Application Components (Root Directory)

- **Program.cs**: Entry point for WinForms application
- **MainForm.cs/.Designer.cs**: Main UI form with search interface
- **FormsCliOptions.cs**: WinForms-specific configuration class extending SearcherCore's CliOptions
- **DiskQuery.cs**: Hardware detection for performance optimization
- **SafeCounter.cs**, **ProgressTimer.cs**, **MonotonicDateTime.cs**: Performance and threading utilities
- **ListViewExtensions.cs**: UI helper extensions for ListView controls
- **GitVersion.cs**: Git version information integration

### Console Application Components (SearcherCli/)

- **Program.cs**: Console entry point with command line parsing
- **MainSearch.cs**: Main search orchestrator for CLI version
- **PicoArgs.cs**: Custom lightweight command line argument parser
- **GitVersion.cs**: Git version information integration (copied from root)
- References SearcherCore library for shared functionality

### Shared Library Components (SearcherCore/)

- **CliOptions.cs**: Configuration and options management for search operations
- **SearchFile.cs**: Core file content searching logic for different file types
- **GlobSearch.cs**: Parallel file discovery using glob patterns
- **PdfCheck.cs**: PDF-specific search implementation using iText7
- **Utils.cs**: Utility functions for pattern processing and file operations
- **SearchResult.cs**: Search result data structure
- Provides shared functionality for both GUI and CLI applications

### Key Architectural Patterns

1. **Dual Interface Architecture**: Both WinForms GUI and Console CLI share core search logic via SearcherCore library
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

### SearcherCli Project Dependencies
- **SearcherCore**: Shared library containing core search functionality (project reference)
- No external NuGet packages (uses custom `PicoArgs.cs` for command line parsing)

### SearcherCore Dependencies (inherited by SearcherCli)
- **DotNet.Glob**: File pattern matching and globbing
- **itext7**: PDF text extraction and processing

### Root WinForms Application Dependencies (separate)
- **CommandLineParser**: Command line argument parsing (WinForms uses this for consistency)
- **System.Management**: Hardware detection for performance optimization
- **SearcherCore**: Shared library (project reference)

### Test Dependencies
- **xunit**: Unit testing framework (TestSearcher project only)

## Configuration

The project uses comprehensive code analysis with multiple analyzers:
- Microsoft.VisualStudio.Threading.Analyzers
- Roslynator.Analyzers
- lookbusy1344.RecordValueAnalyser

Code style is enforced via `.editorconfig` with specific rules for C# formatting, naming conventions, and analysis severity levels. The configuration includes extensive Roslynator rules for code quality.
