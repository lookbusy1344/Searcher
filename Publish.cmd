@echo off
echo Cleaning...
dotnet clean --verbosity minimal
echo Publishing single file...

dotnet publish Searcher.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
cd .\bin\Release\net7.0-windows\win-x64\publish
