@echo off
setlocal

set DefaultDestDir=%USERPROFILE%\.vscode\extensions\coreclr-debug

if "%~1"=="-?" goto help
if "%~1"=="/?" goto help
if "%~1"=="-h" goto help
if "%~1"=="" goto help
if "%~2"=="" echo InstallToVSCode.cmd: ERROR: Bad command line arguments. & exit /b -1

set InstallAction=
if "%~1"=="-l" set InstallAction=LinkFile&goto InstallActionSet
if "%~1"=="/l" set InstallAction=LinkFile&goto InstallActionSet
if "%~1"=="-c" set InstallAction=CopyFile&goto InstallActionSet
if "%~1"=="/c" set InstallAction=CopyFile&goto InstallActionSet
echo ERROR: Unexpected first argument '%~1'. Expected '-l' or '-c'.& exit /b -1
:InstallActionSet

if not exist %2 echo ERROR: open-debug-ad7-dir '%~2' does not exist.& exit /b -1
if not exist "%~2\OpenDebugAD7.exe" echo ERROR: OpenDebugAD7.exe does not exist in open-debug-ad7-dir '%~2'.& exit /b -1
set OpenDebugAD7Dir=%~2

set DropDir=%~dp0..\..\bin\Debug\drop\
if not exist "%DropDir%Microsoft.MIDebugEngine.dll" echo ERROR: Microsoft.MIDebugEngine.dll has not been built & exit /b -1

set DESTDIR=%~3
if "%DESTDIR%"=="" set DESTDIR=%DefaultDestDir%

if exist "%DESTDIR%" goto DESTDIR_Done
mkdir "%DESTDIR%"
if NOT "%ERRORLEVEL%"=="0" echo ERROR: unable to create destination directory '%DESTDIR%'. &exit /b -1
:DESTDIR_Done

if exist "%DESTDIR%\debugAdapters" goto DEBUGADAPTERDIR_Done
mkdir "%DESTDIR%\debugAdapters"
if NOT "%ERRORLEVEL%"=="0" echo ERROR: unable to create debug adapter directory '%DESTDIR%\debugAdapters'. &exit /b -1
:DEBUGADAPTERDIR_Done

set InstallError=
for %%f in (OpenDebugAD7.exe Microsoft.DebugEngineHost.dll Newtonsoft.Json.dll) do call :InstallFile "%OpenDebugAD7Dir%\%%f" debugAdapters\
for %%f in (coreclr\package.json) do call :InstallFile "%~dp0%%f"
for %%f in (coreclr\coreclr.ad7Engine.json) do call :InstallFile "%~dp0%%f" debugAdapters\
for %%f in (Microsoft.MICore.dll Microsoft.MIDebugEngine.dll) do call :InstallFile "%DropDir%%%f" debugAdapters\

REM TODO: Stop doing this when we switch to running under CoreCLR
for /f %%f in ('dir /b %DropDir%System*.dll') do call :InstallFile "%DropDir%%%f" debugAdapters\

REM TODO: Add more dependencies that we need for running on CoreCLR
echo.
if NOT "%InstallError%"=="" echo ERROR: Failed to copy one or more files.& exit /b -1
echo InstallToVSCode.cmd succeeded. To complete setup, create a link or copy clrdbg next to the debug adapter. Ex:
echo.
echo    mklink /d %DESTDIR%\clrdbg C:\dd\vs\out\binaries\amd64chk\debugger\x-plat\clrdbg
echo.
exit /b 0

:InstallFile
echo Installing %~f1
goto %InstallAction%

:CopyFile
copy /y %1 "%DESTDIR%\%2">nul
if NOT "%ERRORLEVEL%"=="0" set InstallError=1& echo ERROR: Unable to copy %~nx1
goto eof

:LinkFile
if not exist "%DESTDIR%\%2%~nx1" goto LinkFile_DeleteDone
del "%DESTDIR%\%2%~nx1"
if NOT "%ERRORLEVEL%"=="0" set InstallError=1& echo ERROR: Unable to delete '%DESTDIR%\%2%~nx1'
:LinkFile_DeleteDone

mklink "%DESTDIR%\%2%~nx1" "%~f1">nul
if NOT "%ERRORLEVEL%"=="0" set InstallError=1& echo ERROR: Unable to create link for '%~nx1'
goto eof

:Help
echo InstallToVSCode ^<-l^|-c^> ^<open-debug-ad7-dir^> [destination-dir]
echo.
echo This script is used to copy files needed to enable MIEngine based debugging 
echo into VS Code.
echo.
echo  -l : Create links to files instead of copying them. With this mode, it
echo    is possible to rebuild MIEngine or OpenDebugAD7 without re-running this 
echo    script.
echo  -c : Copy files to the output directory
echo  open-debug-ad7-dir : Directory which contains OpenDebugAD7.exe
echo  destination-dir: Directory to install to. By default this is:
echo     %DefaultDestDir%
echo.

:eof