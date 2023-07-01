@echo off
echo Cleaning...
dotnet clean
echo Publishing single file...
rem dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
dotnet publish Searcher.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
cd .\bin\Release\net7.0-windows\win-x64\publish
