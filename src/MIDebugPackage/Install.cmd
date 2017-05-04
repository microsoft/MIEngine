@echo off
setlocal

if "%~1"=="-?" goto help
if "%~1"=="/?" goto help
if "%~1"=="" goto help

if /i "%PROCESSOR_ARCHITECTURE%"=="amd64" call %SystemRoot%\SysWow64\cmd.exe /C %~dpf0 %* & goto eof
if /i NOT "%PROCESSOR_ARCHITECTURE%"=="x86" echo ERROR: Unsupported processor - script should only be run on an x86 or x64 OS & exit /b -1

set ScriptDir=%~dp0
set InstallAction=Install
set DestDir=
:ArgLoopStart
if "%~1"=="" goto ArgLoopDone
if "%~1"=="/restore" set InstallAction=RestoreBackup& goto ArgOk

set DestDir=%~1
if "%DestDir:~0,1%"=="/" echo ERROR: Unknown switch '%DestDir%'& exit /b -1
if "%DestDir:~0,1%"=="-" echo ERROR: Unknown switch '%DestDir%'& exit /b -1
goto ArgOk

:ArgOk
shift
goto :ArgLoopStart

:ArgLoopDone
if "%DestDir%"=="" echo ERROR: Destination directory must be provided.& exit /b -1
if NOT exist "%DestDir%\Common7\IDE\devenv.exe" echo ERROR: Destination directory '%DestDir%' is incorrect. Specify the root to a VS install.& exit /b -1

REM make sure we are elevated
net session >nul 2>&1
if NOT "%ERRORLEVEL%"=="0" echo ERROR: Must be called from an elevated command prompt.& exit /b -1

set BackupDir=%DestDir%\.MDDDebuggerBackup\
set MDDDebuggerDir=%DestDir%\Common7\IDE\CommonExtensions\Microsoft\MDD\Debugger\

set FilesToInstall=Microsoft.MICore.dll Microsoft.MIDebugEngine.dll Microsoft.MIDebugEngine.pkgdef Microsoft.MIDebugPackage.dll Microsoft.MIDebugPackage.pkgdef Microsoft.AndroidDebugLauncher.dll Microsoft.AndroidDebugLauncher.pkgdef Microsoft.IOSDebugLauncher.dll Microsoft.IOSDebugLauncher.pkgdef Microsoft.JDbg.dll Microsoft.DebugEngineHost.dll Microsoft.MICore.XmlSerializers.dll Microsoft.SSHDebugPS.dll Microsoft.SSHDebugPS.pkgdef

REM Add in the Facade assemblies we need to run on the desktop CLR if we are running in VS 2015. In VS 2017, Roslyn adds these, so don't add our own copy.
if not exist "%DestDir%\Common7\IDE\PrivateAssemblies\System.Diagnostics.Process.dll" set FilesToInstall=%FilesToInstall% System.Diagnostics.Process.dll System.IO.FileSystem.dll System.IO.FileSystem.Primitives.dll System.Net.Security.dll System.Net.Sockets.dll System.Reflection.TypeExtensions.dll System.Runtime.InteropServices.RuntimeInformation.dll System.Security.Cryptography.X509Certificates.dll System.Threading.Thread.dll

goto %InstallAction%

:Install
if exist "%BackupDir%" goto InstallFiles
if not exist "%MDDDebuggerDir%" goto InstallFiles
echo INFO: Backing up MDD Debugger to '%BackupDir%'.
mkdir "%BackupDir%"
set CopyError=
for /f %%f in ('dir /b "%MDDDebuggerDir%"') do call :CopyFile "%MDDDebuggerDir%\%%f" "%BackupDir%"
if NOT "%CopyError%"=="" echo ERROR: Failed to backup one or more files& echo.& exit /b -1
rem clean all files after backup
call :CleanDebuggerDir
goto InstallFiles

:RestoreBackup
if not exist "%BackupDir%" echo ERROR: No backup exists.& exit /b -1
if not exist "%MDDDebuggerDir%" mkdir "%MDDDebuggerDir%"
echo Restoring from backup
call :CleanDebuggerDir
set CopyError=
for /f %%f in ('dir /b "%BackupDir%"') do call :CopyFile "%BackupDir%\%%f" "%MDDDebuggerDir%"
if NOT "%CopyError%"=="" echo ERROR: Failed to restore one or more files& echo.& exit /b -1
call :UpdateConfiguration

echo MDD Debugger succesfully restored from backup

goto eof

:InstallFiles
if not exist "%MDDDebuggerDir%" mkdir "%MDDDebuggerDir%"
echo Installing Files
set CopyError=
for %%f in (%FilesToInstall%) do call :CopyFile "%ScriptDir%%%f" "%MDDDebuggerDir%"
if NOT "%CopyError%"=="" echo ERROR: Failed to install one or more files& echo.& exit /b -1

call :UpdateConfiguration

echo MDD Debugger succesfully installed

goto eof

rem %1 is file %2 is dest dir
rem both must be quoted prior to calling copy file
:CopyFile
echo copy %1 %2
copy /y %1 %2
if NOT "%ERRORLEVEL%"=="0" set CopyError=1
goto eof

:UpdateConfiguration
call "%DestDir%\Common7\IDE\devenv.com" /updateconfiguration
goto eof

:CleanDebuggerDir
pushd %MDDDebuggerDir%
for %%f in (*) do del %%f
popd
goto eof


:help
set X86ProgFiles=%ProgramFiles(x86)%
if "%X86ProgFiles%"=="" set X86ProgFiles=%ProgramFiles%

echo Install.cmd [^/restore] ^<dest-dir^>
echo.
echo This script should be run on the test machine and it updates the MDD debugger 
echo bits to bits from the directory where the script is.
echo.
echo Example of installing to VS 2015:
echo    %0 "%X86ProgFiles%\Microsoft Visual Studio 14.0"
echo.
echo Example of installing to VS 2017:
echo    %0 "%X86ProgFiles%\Microsoft Visual Studio\2017\Enterprise"
echo.
echo Example of restoring VS 2017:
echo    %0 /restore "%X86ProgFiles%\Microsoft Visual Studio\2017\Enterprise"
echo.

:eof
