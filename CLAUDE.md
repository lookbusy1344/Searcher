# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Searcher is a fully cross-platform (Windows, macOS, Linux) search application for recursively searching text inside files, including archives (ZIP), PDFs, and DOCX files. It targets .NET 9.0 and uses parallel processing for performance optimization. The project uses only cross-platform technologies and has no platform-specific implementations.

**Project Structure:**
- **SearcherGui/**: Cross-platform Avalonia GUI application (primary focus) - runs on Windows, macOS, and Linux
- **SearcherCore/**: Shared .NET library containing core search functionality
- **SearcherCli/**: Cross-platform console version of the application
- **TestSearcher/**: Cross-platform xUnit test project (tests SearcherGui and SearcherCore, runs on Windows/macOS/Linux)

## Build Commands

```bash
# Build SearcherGui (Avalonia) and SearcherCli
dotnet build SearcherGui/SearcherGui.csproj
dotnet build SearcherCli/SearcherCli.csproj

# Build for release
dotnet build SearcherGui/SearcherGui.csproj -c Release
dotnet build SearcherCli/SearcherCli.csproj -c Release

# Build with specific platform (cross-platform)
dotnet build SearcherGui/SearcherGui.csproj -c Release -r linux-x64
dotnet build SearcherGui/SearcherGui.csproj -c Release -r osx-arm64
dotnet build SearcherGui/SearcherGui.csproj -c Release -r win-x64

# Clean build artifacts
dotnet clean

# Restore packages
dotnet restore

# Format code (critical - always run after changes)
dotnet format SearcherGui/SearcherGui.csproj
dotnet format SearcherCli/SearcherCli.csproj

# Publish SearcherGui as single-file executable
dotnet publish SearcherGui/SearcherGui.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
dotnet publish SearcherGui/SearcherGui.csproj -c Release -r linux-x64 --self-contained false /p:PublishSingleFile=true
dotnet publish SearcherGui/SearcherGui.csproj -c Release -r osx-arm64 --self-contained false /p:PublishSingleFile=true
```

## Test Commands

```bash
# Run tests for SearcherGui and SearcherCli
dotnet test TestSearcher/

# Run tests with detailed output
dotnet test TestSearcher/ --verbosity normal

# Build then test
dotnet build && dotnet test TestSearcher/
```

## Development Commands

```bash
# Run SearcherGui (Avalonia application)
dotnet run --project SearcherGui/SearcherGui.csproj

# Run with specific configuration
dotnet run --project SearcherGui/SearcherGui.csproj -c Debug

# Run SearcherCli
dotnet run --project SearcherCli/SearcherCli.csproj -- [options]
```

## Project Architecture

### SearcherGui (Avalonia) Components

- **Program.cs**: Application entry point with Avalonia initialization
- **App.axaml/App.axaml.cs**: Application root and resource definitions
- **MainWindow.axaml/MainWindow.xaml.cs**: Primary UI window with search interface and results display
- **MainViewModel.cs**: MVVM ViewModel handling search logic and state management
- **ViewModels/**: Other view models for UI components
- **Views/**: Avalonia XAML-based UI components

### SearcherCli (Console) Components

- **Program.cs**: Console application entry point with argument parsing
- **CliOptions.cs**: Command-line configuration class extending SearcherCore.CliOptions
- **Console output handling**: Results formatted for terminal display

### Core Search Components (via SearcherCore reference)

- **CliOptions.cs**: Base configuration class for search parameters (in SearcherCore)
- **SearchFile.cs**: Main file content searching with format-specific handlers (.txt, .docx, .pdf, .zip)
- **GlobSearch.cs**: Parallel file discovery using glob patterns with recursive directory traversal
- **PdfCheck.cs**: PDF text extraction and searching using iText7 library
- **Utils.cs**: File type detection, pattern processing, and utility functions
- **SearchResult.cs**: Data structure for search results

### Key Architectural Patterns

1. **Shared Core Architecture**: Both SearcherGui and SearcherCli use SearcherCore library for all search logic
2. **MVVM Pattern**: SearcherGui uses MVVM with ViewModels for UI state management
3. **File Type Routing**: Different search strategies based on file extension and magic number detection
4. **Parallel Processing**: Uses `Parallel.ForEach` with hardware-aware thread management
5. **Stream-based Processing**: Memory-efficient handling of large files
6. **Recursive Archive Support**: Handles nested ZIP files and mixed file types within archives
7. **Hardware Optimization**: Automatic parallelism adjustment based on storage type (SSD vs HDD)

### Search Workflow

1. User enters search criteria via SearcherGui Avalonia UI or SearcherCli command line
2. Parameters converted to base `CliOptions` configuration object
3. `GlobSearch.ParallelFindFiles()` discovers matching files using patterns
4. `SearchFile.FileContainsStringWrapper()` performs parallel content search
5. Results displayed in GUI ListView or console output
6. Error handling for inaccessible files and unsupported formats

### Performance Features

- **Storage-Aware Threading**: Full CPU cores for SSD, half cores for spinning disks
- **Magic Number Detection**: Identifies ZIP files regardless of extension
- **Memory Efficiency**: Stream-based reading with minimal allocations
- **Thread-Safe Counters**: Progress tracking across parallel operations
- **Cancellation Support**: Cooperative cancellation via CancellationToken

## Dependencies

### SearcherGui Dependencies
- **Avalonia**: Cross-platform UI framework
- **CommandLineParser**: Command line argument parsing

### SearcherCli Dependencies
- **CommandLineParser**: Command line argument parsing

### SearcherCore Dependencies
- **DotNet.Glob**: File pattern matching and globbing
- **itext7**: PDF text extraction and processing
- **System.Management**: Hardware detection for performance optimization

## Code Analysis and Quality

The project uses comprehensive static analysis:
- **Analysis Modes**: All modes enabled (Design, Security, Performance, Reliability, Usage)
- **Microsoft.VisualStudio.Threading.Analyzers**: Threading best practices
- **Roslynator.Analyzers**: Code quality and style enforcement
- **lookbusy1344.RecordValueAnalyser**: Record type analysis

## Configuration

- **Target Framework**: net9.0 (fully cross-platform)
- **SearcherGui**: Windows, macOS, and Linux via Avalonia (no platform-specific code)
- **SearcherCli**: Cross-platform console application (all platforms)
- **TestSearcher**: Cross-platform test suite (Windows/macOS/Linux)
- **Code Style**: Enforced via comprehensive `.editorconfig` with specific C# formatting rules
- **Unsafe Code**: Enabled in SearcherCore for performance-critical operations
- **Git Integration**: Automatic source revision ID embedding via git describe

**Critical**: Always run `dotnet format SearcherGui/SearcherGui.csproj` and `dotnet format SearcherCli/SearcherCli.csproj` after code changes to maintain consistent formatting and style compliance.
