@echo off
setlocal enabledelayedexpansion

where dotnet >nul 2>nul

if NOT "%ERRORLEVEL%"=="0" (
    echo dotnet needs to be installed. https://dotnet.microsoft.com/download/dotnet-core
    exit /b -1
)

dotnet script -v >nul 2>nul

if NOT "%ERRORLEVEL%"=="0" (
    echo dotnet script needs to be installed. Run 'dotnet tool install -g dotnet-script'.
    echo More Information: https://github.com/filipw/dotnet-script#net-core-global-tool
    exit /b -1
)

dotnet script %~dp0\Setup.csx %*
exit /b %ERRORLEVEL%