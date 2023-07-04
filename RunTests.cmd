@echo off
call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat"
vstest.console.exe ".\TestSearcher\bin\Debug\net7.0-windows10.0.22621.0\TestSearcher.dll"
