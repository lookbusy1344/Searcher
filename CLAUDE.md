# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Searcher is a C# WinForms application for recursively searching text inside files, including archives (ZIP), PDFs, and DOCX files. It targets .NET 9.0 for Windows and uses parallel processing for performance optimization.

**Project Structure:**
- **Root directory**: Main WinForms GUI application (Searcher.csproj)
- **SearcherCore/**: Shared .NET library containing core search functionality
- **TestSearcher/**: xUnit test project
- **SearcherCli/**: Console version of the application (separate solution)

## Build Commands

```bash
# Build the entire solution
dotnet build

# Build for release
dotnet build -c Release

# Build with specific platform
dotnet build -c Release -p:Platform=x64

# Clean build artifacts
dotnet clean

# Restore packages
dotnet restore

# Format code (critical - always run after changes)
dotnet format

# Publish single-file Windows executable
dotnet publish Searcher.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true

# Alternative publish using batch script
Publish.cmd
```

## Test Commands

```bash
# Run tests using dotnet
dotnet test TestSearcher/

# Run tests with detailed output
dotnet test TestSearcher/ --verbosity normal

# Build then test
dotnet build && dotnet test TestSearcher/

# Run tests using Windows batch script (uses vstest.console.exe)
RunTests.cmd
```

## Development Commands

```bash
# Run the WinForms application
dotnet run

# Run with specific configuration
dotnet run -c Debug

# Check for vulnerabilities (Windows)
CheckVul.cmd
```

## Project Architecture

### Main WinForms Application Components

- **Program.cs**: Application entry point with Windows Forms initialization
- **MainForm.cs/.Designer.cs**: Primary UI form with search interface, results display, and file opening functionality
- **CliOptions.cs**: Configuration class shared with SearcherCore for search parameters
- **ListViewExtensions.cs**: UI helper extensions for ListView controls

### Core Search Components (via SearcherCore reference)

- **SearchFile.cs**: Main file content searching with format-specific handlers (.txt, .docx, .pdf, .zip)
- **GlobSearch.cs**: Parallel file discovery using glob patterns with recursive directory traversal
- **PdfCheck.cs**: PDF text extraction and searching using iText7 library
- **Utils.cs**: File type detection, pattern processing, and utility functions
- **SearchResult.cs**: Data structure for search results
- **DiskQuery.cs**: Hardware detection for SSD/HDD-based parallelism optimization

### Key Architectural Patterns

1. **Shared Core Architecture**: WinForms GUI uses SearcherCore library for all search logic
2. **File Type Routing**: Different search strategies based on file extension and magic number detection
3. **Parallel Processing**: Uses `Parallel.ForEach` with hardware-aware thread management
4. **Stream-based Processing**: Memory-efficient handling of large files
5. **Recursive Archive Support**: Handles nested ZIP files and mixed file types within archives
6. **Hardware Optimization**: Automatic parallelism adjustment based on storage type (SSD vs HDD)

### Search Workflow

1. User enters search criteria in WinForms interface
2. Parameters converted to `CliOptions` configuration object
3. `GlobSearch.ParallelFindFiles()` discovers matching files using patterns
4. `SearchFile.FileContainsStringWrapper()` performs parallel content search
5. Results displayed in ListView with double-click to open files
6. Error handling for inaccessible files and unsupported formats

### Performance Features

- **Storage-Aware Threading**: Full CPU cores for SSD, half cores for spinning disks
- **Magic Number Detection**: Identifies ZIP files regardless of extension
- **Memory Efficiency**: Stream-based reading with minimal allocations
- **Thread-Safe Counters**: Progress tracking across parallel operations
- **Cancellation Support**: Cooperative cancellation via CancellationToken

## Dependencies

Key NuGet packages:
- **DotNet.Glob**: File pattern matching and globbing
- **itext7**: PDF text extraction and processing
- **CommandLineParser**: Command line argument parsing (WinForms uses this for consistency)
- **System.Management**: Hardware detection for performance optimization

## Code Analysis and Quality

The project uses comprehensive static analysis:
- **Analysis Modes**: All modes enabled (Design, Security, Performance, Reliability, Usage)
- **Microsoft.VisualStudio.Threading.Analyzers**: Threading best practices
- **Roslynator.Analyzers**: Code quality and style enforcement
- **lookbusy1344.RecordValueAnalyser**: Record type analysis

## Configuration

- **Target Framework**: net9.0-windows10.0.26100.0 (Windows-specific)
- **Code Style**: Enforced via comprehensive `.editorconfig` with specific C# formatting rules
- **Unsafe Code**: Enabled for performance-critical operations
- **Git Integration**: Automatic source revision ID embedding via git describe

**Critical**: Always run `dotnet format` after code changes to maintain consistent formatting and style compliance.
