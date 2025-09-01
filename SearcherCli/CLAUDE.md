# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SearcherCli is a C# console application that recursively searches for text inside files, including archives (ZIP), PDFs, and DOCX files. It uses .NET 9.0 with AOT compilation and supports parallel processing for performance.

## Build Commands

```bash
# Build the project
dotnet build

# Build for release with AOT
dotnet publish -c Release

# Run the application
dotnet run -- --help
```

## Development Commands

The project uses extensive code analysis and formatting rules defined in `.editorconfig`. No specific test framework is configured in this project.

```bash
# Restore packages
dotnet restore

# Clean build artifacts
dotnet clean

# Format code (relies on editorconfig settings)
# No specific formatter command - code style enforced via build analysis
```

## Project Architecture

### Core Components

- **Program.cs**: Entry point with command line parsing using PicoArgs
- **CliOptions.cs**: Configuration class with search parameters and parallelism settings
- **MainSearch.cs**: Main search orchestrator that coordinates file discovery and content searching
- **SearchFile.cs**: Handles different file types (text, DOCX, PDF, ZIP) with appropriate search strategies
- **GlobSearch.cs**: Parallel file discovery using glob patterns
- **Utils.cs**: Utility functions for pattern processing and file type detection
- **PdfCheck.cs**: PDF-specific search implementation using iText7
- **PicoArgs.cs**: Custom command line argument parser

### Key Architectural Patterns

1. **File Type Routing**: Different search strategies based on file extension (.txt, .docx, .pdf, .zip)
2. **Parallel Processing**: Uses `Parallel.ForEach` for both file discovery and content searching
3. **Recursive ZIP Handling**: Supports nested ZIP files and archives containing other formats
4. **Stream-based Processing**: Efficient memory usage when reading large files
5. **Cancellation Support**: All long-running operations support cancellation tokens

### Search Flow

1. Parse command line arguments into `CliOptions`
2. Process file patterns for outer files vs inner archive files
3. Use `GlobSearch.ParallelFindFiles()` to discover matching files
4. Apply parallel search using `SearchFile.FileContainsStringWrapper()`
5. Route to specific handlers based on file type
6. Display results with error handling

### Performance Considerations

- **SSD Detection**: Adjusts parallelism based on storage type (full cores for SSD, half for HDD)
- **Memory Efficiency**: Stream-based reading, minimal string allocations
- **Parallel File Discovery**: Custom parallel directory traversal algorithm
- **Magic Number Detection**: Uses file signatures to identify ZIP files regardless of extension

## Configuration

The project uses comprehensive code analysis with multiple analyzers:
- Microsoft.VisualStudio.Threading.Analyzers
- Roslynator.Analyzers  
- lookbusy1344.RecordValueAnalyser

Code style is enforced via `.editorconfig` with specific rules for C# formatting, naming conventions, and analysis severity levels.