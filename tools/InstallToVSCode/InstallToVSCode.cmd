@echo off
setlocal
set COMPLUS_InstallRoot=
set COMPLUS_Version=
set InstallError=

if "%~1"=="-?" goto help
if "%~1"=="/?" goto help
if "%~1"=="-h" goto help
if "%~1"=="" goto help
if "%~6"=="" echo InstallToVSCode.cmd: ERROR: Bad command line arguments. & exit /b -1

set DotNetSdkRoot=%ProgramFiles%\dotnet\sdk
if /i "%PROCESSOR_ARCHITEW6432%"=="AMD64" set DotNetSdkRoot=%ProgramW6432%\dotnet\sdk
set DotNetDll=%DotNetSdkRoot%\1.0.0-preview2-1-003177\dotnet.dll
if not exist "%DotNetDll%" echo ERROR: .NET CLI preview2 must be installed.&exit /b -1

set InstallAction=
if "%~1"=="link" set InstallAction=LinkFile&goto InstallActionSet
if "%~1"=="copy" set InstallAction=CopyFile&goto InstallActionSet
echo ERROR: Unexpected first argument '%~1'. Expected 'link' or 'copy'.& exit /b -1
:InstallActionSet

set Configuration=
if "%~2"=="portable" set Configuration=Debug-PortablePDB& goto ConfigurationSet
if "%~2"=="debug" set Configuration=Debug& goto ConfigurationSet
echo ERROR: Unexpected second argument '%~2'. Expected 'portable' or 'debug'.& exit /b -1
:ConfigurationSet

set VSCodeDirName=.vscode-%3
if "%~3"=="alpha" goto VSCodeDirNameSet
if "%~3"=="insiders" goto VSCodeDirNameSet
if "%~3"=="oss-dev" goto VSCodeDirNameSet
if "%~3"=="stable" set VSCodeDirName=.vscode& goto VSCodeDirNameSet
echo ERROR: Unexpected third argument '%~3'. Expected 'oss-dev', 'alpha', 'insiders' or 'stable'.& exit /b -1
:VSCodeDirNameSet

set OpenDebugAD7Dir=%~4
set OpenDebugAD7BinDir=%OpenDebugAD7Dir%\bin\%Configuration%
if not exist %4 echo ERROR: open-debug-ad7-dir '%~4' does not exist.& exit /b -1
if not exist "%~4\src\OpenDebugAD7" echo ERROR: open-debug-ad7-dir '%~4' is invalid. Couldn't find OpenDebugAD7 sources.& exit /b -1
if not exist "%OpenDebugAD7BinDir%\OpenDebugAD7.dll" echo ERROR: %OpenDebugAD7BinDir%\OpenDebugAD7.dll does not exist.& exit /b -1

set MIEngineBinDir=%~dp0..\..\bin\%Configuration%\
if not exist "%MIEngineBinDir%Microsoft.MIDebugEngine.dll" echo ERROR: Microsoft.MIDebugEngine.dll has not been built & exit /b -1

if NOT "%~5"=="-d" echo ERROR: Bad command line argument. Expected '-d ^<vsdbg-dir^>'. & exit /b -1
if "%~6" == "" echo ERROR: VsDbg binaries directory not set &exit /b -1
set VSDBGBITSDIR=%~6
if not exist "%VSDBGBITSDIR%\vsdbg.dll" echo ERROR: %VSDBGBITSDIR%\vsdbg.dll does not exist. & exit /b -1

set DESTDIR=%USERPROFILE%\.MIEngine-VSCode-Debug
if exist "%DESTDIR%" rmdir /s /q "%DESTDIR%"
if exist "%DESTDIR%" echo ERROR: Unable to clean destination directory '%DESTDIR%' & exit /b -1
mkdir "%DESTDIR%"
if NOT "%ERRORLEVEL%"=="0" echo ERROR: unable to create directory '%DESTDIR%'. &exit /b -1

echo Installing files to %DESTDIR%
echo.

set VSCodeExtensionsRoot=%USERPROFILE%\%VSCodeDirName%\extensions
if not exist "%VSCodeExtensionsRoot%" echo ERROR: %VSCodeExtensionsRoot% does not exist& exit /b -1

set CSharpExtensionRoot=
for /d %%d in (%VSCodeExtensionsRoot%\ms-vscode.csharp-*) do call :SetCSharpExtensionRoot %%d
if NOT "%InstallError%"=="" exit /b -1
if "%CSharpExtensionRoot%"=="" echo ERROR: C# extension is not installed in VS Code. No directory matching '%VSCodeExtensionsRoot%\ms-vscode.csharp-*' found. & exit /b -1

call :SetupSymLink %CSharpExtensionRoot%\.debugger
if NOT "%InstallError%"=="" exit /b -1

mkdir "%DESTDIR%\CLRDependencies"
if NOT "%ERRORLEVEL%"=="0" echo ERROR: unable to create directory '%DESTDIR%\CLRDependencies'. &exit /b -1

xcopy /s %~dp0CLRDependencies "%DESTDIR%\CLRDependencies"
if NOT "%ERRORLEVEL%"=="0" echo ERROR: Unable to copy CLRDependencies directory???& exit /b -1

pushd "%DESTDIR%\CLRDependencies"
if NOT "%ERRORLEVEL%"=="0" echo ERROR: Unable to change to CLRDependencies directory???& exit /b -1

for /f "tokens=1 delims=" %%l in (project.json.template) do if NOT "%%l"=="@current-OS@" (echo %%l>>project.json) else (echo     "win7-x64":{}>>project.json)

dotnet "%DotNetDll%" restore
if NOT "%ERRORLEVEL%"=="0" echo "ERROR: 'dotnet restore' failed." & exit /b -1

dotnet "%DotNetDll%" publish -o %DESTDIR%
if NOT "%ERRORLEVEL%"=="0" echo "ERROR: 'dotnet publish' failed." & exit /b -1
popd

pushd %DESTDIR%
ren dummy.exe OpenDebugAD7.exe
if NOT "%ERRORLEVEL%"=="0" echo ERROR: Unable to rename OpenDebugAD7.exe???& exit /b -1
popd

for %%f in (dar.exe) do call :InstallFile "%OpenDebugAD7BinDir%\%%f"
for %%f in (xunit.console.netcore.exe) do call :InstallFile "%OpenDebugAD7BinDir%\%%f"
for %%f in (%OpenDebugAD7BinDir%\*.dll) do call :InstallFile "%%f"

echo.
echo Installing vsdbg bits from %VSDBGBITSDIR%...

REM NOTE: We ignore files that already exist. This is because we have already
REM cleaned the directory originally, and published CoreCLR files. Replacing existing
REM files will replace some of those CoreCLR files with new copies that will not work.
for %%f in (%VSDBGBITSDIR%\*.dll) do call :InstallNewFile "%%f"
for %%f in (%VSDBGBITSDIR%\*.exe) do call :InstallFile "%%f"
for %%f in (%VSDBGBITSDIR%\*.vsdconfig) do call :InstallFile "%%f"
for %%f in (%VSDBGBITSDIR%\version.txt) do call :InstallFile "%%f"
for /D %%d in (%VSDBGBITSDIR%\*) do (
    echo.
    echo Installing vsdbg bits from %%d... to %%~nd
    if NOT exist "%DESTDIR%\%%~nd" mkdir "%DESTDIR%\%%~nd
    for %%f in (%%d\*.dll) do call :InstallFile "%%f" %%~nd\
)

REM Rename vsdbg back to clrdbg
pushd %destdir%
ren vsdbg.exe clrdbg.exe
if not "%errorlevel%"=="0" echo error: unable to rename vsdbg.exe???& exit /b -1
popd

for %%f in (coreclr\coreclr.ad7Engine.json) do call :InstallFile "%~dp0%%f"
for %%f in (Microsoft.MICore.dll Microsoft.MIDebugEngine.dll) do call :InstallFile "%MIEngineBinDir%%%f"

echo.
if NOT "%InstallError%"=="" echo ERROR: Failed to copy one or more files.& exit /b -1

REM Write out an install.complete file so that the C# extension doesn't try to restore.
echo "InstallToVSCode.cmd done">%DESTDIR%\install.complete

echo InstallToVSCode.cmd succeeded.
echo.
exit /b 0

:InstallNewFile
if exist "%DESTDIR%\%2%~nx1" goto eof
goto InstallFile

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

:SetCSharpExtensionRoot
if NOT "%CSharpExtensionRoot%"=="" echo ERROR: Multiple C# extensions found under %VSCodeExtensionsRoot%& set InstallError=1& goto eof
set CSharpExtensionRoot=%~1
goto eof

:SetupSymLink
REM Assume that if the debugAdapaters directory is already a symlink, it is probably going to the right spot
if "%~a1"=="d-------l--" goto eof
if NOT "%~a1"=="d----------" echo ERROR: Unexpected attributes for %1& set InstallError=1& goto eof
rmdir /s /q %1
if NOT "%ERRORLEVEL%"=="0" echo ERROR: fail to remove directory '%1'. & set InstallError=1& goto eof

mklink /d %1 "%DESTDIR%"
if NOT "%ERRORLEVEL%"=="0" echo ERROR: mklink failed. Ensure this script is running as an admin& set InstallError=1& goto eof
goto eof

:Help
echo InstallToVSCode ^<link^|copy^> ^<portable^|debug^> ^<oss-dev^|alpha^|insiders^|stable^> ^<open-debug-ad7-dir^> -d ^<vsdbg-binaries^>
echo.
echo This script is used to copy files needed to enable MIEngine based debugging 
echo into VS Code.
echo.
echo   link : Create links to files instead of copying them. With this mode, it
echo          is possible to rebuild MIEngine or OpenDebugAD7 without re-running this 
echo          script.
echo   copy : Copy files to the output directory
echo.
echo   portable: Use portable PDBs (Debug-PortablePDB solution configuration)
echo   debug: Use debug configuration
echo.
echo   alpha: Install to VSCode alpha
echo   insiders: Install to VSCode insiders
echo   stable: Install to VSCode stable
echo.
echo  open-debug-ad7-dir : Root of the OpenDebugAD7 repo
echo  vsdbg-binaries: Directory which contains vsdbg binaries
echo.
echo Example: 
echo .\InstallToVSCode.cmd link portable alpha c:\dd\OpenDebugAD7 -d c:\dd\vs1\out\binaries\amd64chk\Debugger\x-plat\vsdbg
echo.

:eof
