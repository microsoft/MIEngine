@echo off
setlocal

:: Change this for SxS
set VSVersion=14.0

if /i "%~1"=="" goto Help
if /i "%~1"=="-?" goto Help
if /i "%~1"=="/?" goto Help

set LoggingValue=
set Exp=0
set ServerLogging=

:ArgLoop
if /i "%~1"=="on" set LoggingValue=1& goto ArgLoopCondition
if /i "%~1"=="off" set LoggingValue=0& goto ArgLoopCondition
if /i "%~1"=="/exp" set Exp=1& goto ArgLoopCondition
if /i "%~1"=="-exp" set Exp=1& goto ArgLoopCondition
if /i "%~1"=="/version" goto SetVersion
if /i "%~1"=="-version" goto SetVersion
if /i "%~1"=="-serverlogging" goto SetServerLogging
if /i "%~1"=="/serverlogging" goto SetServerLogging
echo ERROR: Unknown argument '%~1'& exit /b -1

:SetVersion
shift
if "%~1"=="" echo ERROR: Expected version number.
set VSVersion=%~1
goto ArgLoopCondition

:SetServerLogging
REM Documentation on GDBServer command line arguments: http://www.sourceware.org/gdb/onlinedocs/gdb/Server.html
set ServerLogging=--debug
if /i "%~2"=="full" shift & set ServerLogging=--debug --remote-debug
goto ArgLoopCondition

:ArgLoopCondition
shift
if NOT "%~1"=="" goto :ArgLoop

if "%LoggingValue%"=="" echo ERROR: 'on' or 'off' must be specified.& exit /b -1
if /i NOT "%LoggingValue%"=="1" if NOT "%ServerLogging%"=="" echo ERROR: '/serverlogging' can only be used with 'on'& exit /b -1

set reg_exe=reg.exe
if /i "%PROCESSOR_ARCHITECTURE%"=="AMD64" set reg_exe=%windir%\syswow64\reg.exe

set MainRegRoot=HKLM\SOFTWARE\Microsoft\VisualStudio\%VSVersion%
set OptionalRegRoots=HKCU\SOFTWARE\Microsoft\VisualStudio\%VSVersion%_Config HKLM\SOFTWARE\Microsoft\Glass\%VSVersion%

if "%Exp%"=="1" goto :SetExpRegRoots

call :SetRegRoot %MainRegRoot%
for %%r in (%OptionalRegRoots%) do call :SetForRegRootIfExists %%r
exit /b 0

:SetExpRegRoots
reg query HKCU\Software\Microsoft\VisualStudio | findstr %VSVersion%Exp_Config > %TMP%\%VSVersion%ExpRegRoots.txt
set ExpRoots=
set ExpRootFound=
for /f "delims=" %%r in (%TMP%\%VSVersion%ExpRegRoots.txt) do set ExpRootFound=1& call set ExpRoots=%%ExpRoots%% %%r
if NOT "%ExpRootFound%"=="1" echo ERROR: Experimental instance could not be found. Experimental instance must be started before this batch script can work.& exit /b -1

for %%r in (%ExpRoots%) do call :SetRegRoot %%r
exit /b 0

:SetForRegRootIfExists
REM check if the registry root exists
call %reg_exe% query %1 /ve 1>NUL 2>NUL
if NOT %ERRORLEVEL%==0 goto eof

:SetRegRoot
call %reg_exe% add %1\Debugger /v EnableMIDebugLogger /t REG_DWORD /d %LoggingValue% /f
if not %ERRORLEVEL%==0 echo ERROR: failed to write to the registry (%1) & exit /b -1

if "%ServerLogging%"=="" goto DeleteServerLogging
goto AddServerLogging

:AddServerLogging
call %reg_exe% add %1\Debugger /v GDBServerLoggingArguments /t REG_SZ /d "%ServerLogging%" /f
if not %ERRORLEVEL%==0 echo ERROR: failed to write to the registry (%1) & exit /b -1
goto eof

:DeleteServerLogging
call %reg_exe% delete %1\Debugger /v GDBServerLoggingArguments /f 1>NUL 2>NUL
goto eof

:Help
echo SetMIDebugLogging.cmd ^<on^|off^> [/serverlogging [full]] [/exp] [/version 12.0 ^| 14.0]
echo.
echo SetMIDebugLogging.cmd is used to enable/disable logging for the Microsoft
echo MI debug engine.
echo.
echo Logging will be saved to %TMP%\Microsoft.MIDebug.log.
echo.
echo /exp              Changes the setting for the experimental instance of 
echo                   Visual Studio instead of the normal instance.
echo                   NOTE: For the experimental instance, this command must be
echo                   run after the experimental instance is started, but before
echo                   start debugging.
echo.
echo /version ^<number^> Changes the version of Visual Studio which is affected. 
echo                   Default is '%VSVersion%'.
echo.
echo /serverlogging [full] Enables logging from gdbserver. This option is only 
echo                   supported when enabling logging ('on'). 'full' logging will
echo                   turn on packet logging in addition to normal logging.
echo.
:eof
