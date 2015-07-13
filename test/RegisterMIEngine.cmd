@echo off
setlocal

set _GlassDir=%~dp0..\Microsoft.VisualStudio.Glass\

REM Determine the registry root first, as it is used in the help output
if not exist "%_GlassDir%glass2.exe.regroot" echo ERROR: %_GlassDir%glass2.exe.regroot not found.&exit /b -1
set RegistryRoot=
for /f "delims=" %%l in (%_GlassDir%glass2.exe.regroot) do call :ProcessRegRootLine %%l
if "%RegistryRoot%"=="" echo ERROR: Incorrect format in %_GlassDir%glass2.exe.regroot&exit /b -1
if "%RegistryRoot%"=="ERROR" echo ERROR: Incorrect format in %_GlassDir%glass2.exe.regroot&exit /b -1

if "%~1"=="-?" goto help
if "%~1"=="/?" goto help
if NOT "%~1"=="" goto help

if /i "%PROCESSOR_ARCHITECTURE%"=="amd64" call %SystemRoot%\SysWow64\cmd.exe /C "%~dpf0" %* & goto eof
if /i NOT "%PROCESSOR_ARCHITECTURE%"=="x86" echo ERROR: Unsupported processor - script should only be run on an x86 or x64 OS & exit /b -1

set ERROR=

if exist %tmp%\MIEngine.reg del %tmp%\MIEngine.reg
call %_GlassDir%GlassRegGen.exe %~dp0MIEngine.regdef %RegistryRoot% %tmp%\MIEngine.reg
if NOT "%ERRORLEVEL%"=="0" set ERROR=1& goto Done

call reg.exe import %tmp%\MIEngine.reg
if NOT "%ERRORLEVEL%"=="0" set ERROR=1

:Done
if "%Error%"=="" echo RegisterMIEngine.cmd completed successfully.&echo.& goto eof
echo RegisterMIEngine.cmd FAILED.&echo.&exit /b -1

goto eof

:ProcessRegRootLine
set line=%1
if "%line%"=="" goto eof
if "%line:~0,1%"=="#" goto eof
if NOT "%RegistryRoot%"=="" set RegistryRoot=ERROR& goto eof
set RegistryRoot=%line%
goto eof

:Help
echo RegisterMIEngine.cmd
echo.
echo This script is used to register glass2.exe for use with the Concord debug
echo engine. It must be run on a computer with Visual Studio 11.0 is already 
echo installed.
echo.
echo Registry keys are written under HKLM\%RegistryRoot%. Delete 
echo this key to uninstall.
echo.
echo This script must be run as an administrator.
echo.

:eof
