@echo off
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
 echo Instale o .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
 pause
 exit /b 1
)
dotnet publish src\KayBrowser.csproj -c Release -r win-x64 --self-contained true -p:EnableCompressionInSingleFile=true -o dist
if errorlevel 1 (pause & exit /b 1)
echo Pronto: dist\KayBrowser.exe
pause
