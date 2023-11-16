@echo off
call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat"
dotnet build
vstest.console.exe ".\TestSearcher\bin\Debug\net8.0-windows10.0.19041.0\TestSearcher.dll"
