@echo off
setlocal
set COMPLUS_InstallRoot=
set COMPLUS_Version=

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
if not exist "%~2\OpenDebugAD7.dll" echo ERROR: OpenDebugAD7.dll does not exist in open-debug-ad7-dir '%~2'.& exit /b -1
set OpenDebugAD7Dir=%~2

set DropDir=%~dp0..\..\bin\Debug\drop\
if not exist "%DropDir%Microsoft.MIDebugEngine.dll" echo ERROR: Microsoft.MIDebugEngine.dll has not been built & exit /b -1

if NOT "%~3"=="-d" echo ERROR: Bad command line argument. Expected '-d ^<clrdbg-dir^>'. & exit /b -1
if "%~4" == "" echo ERROR: Clrdbg binaries directory not set &exit /b -1
set CLRDBGBITSDIR=%~4
if not exist "%CLRDBGBITSDIR%\libclrdbg.dll" echo ERROR: %CLRDBGBITSDIR%\libclrdbg.dll does not exist. & exit /b -1

set DESTDIR=%~f5
if "%DESTDIR%"=="" set DESTDIR=%DefaultDestDir%

if exist "%DESTDIR%" rmdir /s /q "%DESTDIR%"
if exist "%DESTDIR%" echo ERROR: Unable to clean destination directory '%DESTDIR%' & exit /b -1

echo Installing files to %DESTDIR%
echo.

mkdir "%DESTDIR%\debugAdapters"
if NOT "%ERRORLEVEL%"=="0" echo ERROR: unable to create debug adapter directory '%DESTDIR%\debugAdapters'. &exit /b -1

pushd %~dp0CLRDependencies
if NOT "%ERRORLEVEL%"=="0" echo ERROR: Unable to find CLRDependencies directory???& exit /b -1

dotnet restore
if NOT "%ERRORLEVEL%"=="0" echo "ERROR: 'dotnet restore' failed." & exit /b -1

dotnet publish -o %DESTDIR%\debugAdapters
if NOT "%ERRORLEVEL%"=="0" echo "ERROR: 'dotnet publish' failed." & exit /b -1
popd

pushd %DESTDIR%\debugAdapters
ren dummy.exe OpenDebugAD7.exe
if NOT "%ERRORLEVEL%"=="0" echo ERROR: Unable to rename OpenDebugAD7.exe???& exit /b -1
popd

set InstallError=
for %%f in (dar.exe) do call :InstallFile "%OpenDebugAD7Dir%\%%f" debugAdapters\
for %%f in (xunit.console.netcore.exe) do call :InstallFile "%OpenDebugAD7Dir%\%%f" debugAdapters\
for %%f in (%OpenDebugAD7Dir%\*.dll) do call :InstallFile "%%f" debugAdapters\

echo.
echo Installing clrdbg bits from %CLRDBGBITSDIR%...
for %%f in (%CLRDBGBITSDIR%\*.dll) do call :InstallFile "%%f" debugAdapters\
for %%f in (%CLRDBGBITSDIR%\*.exe) do call :InstallFile "%%f" debugAdapters\
for %%f in (%CLRDBGBITSDIR%\*.vsdconfig) do call :InstallFile "%%f" debugAdapters\
for %%f in (%CLRDBGBITSDIR%\version.txt) do call :InstallFile "%%f" debugAdapters\
for /D %%d in (%CLRDBGBITSDIR%\*) do (
    echo.
    echo Installing clrdbg bits from %%d... to debugAdapters\%%~nd
    if NOT exist "%DESTDIR%\debugAdapters\%%~nd" mkdir "%DESTDIR%\debugAdapters\%%~nd
    for %%f in (%%d\*.dll) do call :InstallFile "%%f" debugAdapters\%%~nd\
)

REM NOTE: The code that deals with starting the adapter can be found in Monaco\src\vs\workbench\parts\debug\node\rawDebugSession.ts.
REM Look at getLaunchDetails.
call :CopyFile "%~dp0coreclr\package.json" package.json

for %%f in (coreclr\coreclr.ad7Engine.json) do call :InstallFile "%~dp0%%f" debugAdapters\
for %%f in (Microsoft.MICore.dll Microsoft.MIDebugEngine.dll) do call :InstallFile "%DropDir%%%f" debugAdapters\

echo.
if NOT "%InstallError%"=="" echo ERROR: Failed to copy one or more files.& exit /b -1
echo InstallToVSCode.cmd succeeded.
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
echo InstallToVSCode ^<-l^|-c^> ^<open-debug-ad7-dir^> -d ^<clrdbg-binaries^> [destination-dir]
echo.
echo This script is used to copy files needed to enable MIEngine based debugging 
echo into VS Code.
echo.
echo  -l : Create links to files instead of copying them. With this mode, it
echo    is possible to rebuild MIEngine or OpenDebugAD7 without re-running this 
echo    script.
echo  -c : Copy files to the output directory
echo  open-debug-ad7-dir : Directory which contains OpenDebugAD7.exe
echo  clrdbg-binaries: Directory which contains clrdbg binaries
echo  destination-dir: Directory to install to. By default this is:
echo     %DefaultDestDir%
echo.
echo Example: 
echo .\InstallToVSCode.cmd -l c:\dd\OpenDebugAD7\bin\Debug -d c:\dd\vs1\out\binaries\amd64chk\Debugger\x-plat\clrdbg %USERPROFILE%\.vscode-alpha\extensions\coreclr-debug
echo.

:eof
