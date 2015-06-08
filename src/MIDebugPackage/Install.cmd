@echo off
setlocal

if "%~1"=="-?" goto help
if "%~1"=="/?" goto help

if /i "%PROCESSOR_ARCHITECTURE%"=="amd64" call %SystemRoot%\SysWow64\cmd.exe /C "%~dpf0" %* & goto eof
if /i NOT "%PROCESSOR_ARCHITECTURE%"=="x86" echo ERROR: Unsupported processor - script should only be run on an x86 or x64 OS & exit /b -1

REM make sure we are elevated
net session >nul 2>&1
if NOT "%ERRORLEVEL%"=="0" echo ERROR: Must be called from an elevated command prompt.& exit /b -1

set VSVersion=14.0
set BackupDir=%LOCALAPPDATA%\Microsoft\VisualStudio\%VSVersion%\MDDDebuggerBackup\
set MDDDebuggerDir=%ProgramFiles%\Microsoft Visual Studio 14.0\Common7\IDE\CommonExtensions\Microsoft\MDD\Debugger\
if not exist "%MDDDebuggerDir%" echo ERROR: Dev14 C++ MDD Tools are not installed on this computer.& exit /b -1

set FilesToInstall=Microsoft.MICore.dll Microsoft.MIDebugEngine.dll Microsoft.MIDebugEngine.pkgdef Microsoft.MIDebugPackage.dll Microsoft.MIDebugPackage.pkgdef Microsoft.AndroidDebugLauncher.dll Microsoft.AndroidDebugLauncher.pkgdef Microsoft.IOSDebugLauncher.dll Microsoft.IOSDebugLauncher.pkgdef Microsoft.JDbg.dll

if "%~1"=="/restore" goto RestoreBackup

goto Backup

:Backup
if exist "%BackupDir%" goto InstallFiles
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
echo Restoring from backup
call :CleanDebuggerDir
set CopyError=
for /f %%f in ('dir /b "%BackupDir%"') do call :CopyFile "%BackupDir%\%%f" "%MDDDebuggerDir%"
if NOT "%CopyError%"=="" echo ERROR: Failed to restore one or more files& echo.& exit /b -1
call :DeleteConfigRegistry

echo MDD Debugger succesfully restored from backup

goto eof

:InstallFiles
echo Installing Files
set CopyError=
for %%f in (%FilesToInstall%) do call :CopyFile "%~dp0%%f" "%MDDDebuggerDir%"
if NOT "%CopyError%"=="" echo ERROR: Failed to install one or more files& echo.& exit /b -1

call :DeleteConfigRegistry

echo MDD Debugger succesfully installed

goto eof

rem %1 is file %2 is dest dir
rem both must be quoted prior to calling copy file
:CopyFile
echo copy %1 %2
copy /y %1 %2
if NOT "%ERRORLEVEL%"=="0" set CopyError=1
goto eof

:DeleteConfigRegistry
reg delete HKCU\Software\Microsoft\VisualStudio\%VSVersion%_Config /f
goto eof

:CleanDebuggerDir
pushd %MDDDebuggerDir%
for %%f in (*) do del %%f
popd
goto eof


:help
echo Install.cmd [^/restore]
echo.
echo This script should be run on the test machine and it updates the MDD debugger 
echo bits to bits from the directory where the script is.
echo.

:eof
