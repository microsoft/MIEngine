@echo off
setlocal

if /i "%~1"=="" goto Help
if /i "%~1"=="-?" goto Help
if /i "%~1"=="/?" goto Help

set LoggingValue=
set Exp=0
set ServerLogging=
set VSRootDir=
set MIEngineRelativeDir=Common7\IDE\CommonExtensions\Microsoft\MDD\Debugger
set MIEngineRelativePath=%MIEngineRelativeDir%\Microsoft.MIDebugEngine.dll

:ArgLoop
if /i "%~1"=="on" set LoggingValue=1& goto ArgLoopCondition
if /i "%~1"=="off" set LoggingValue=0& goto ArgLoopCondition
if /i "%~1"=="/VSRootDir" goto SetVSRoot
if /i "%~1"=="-VSRootDir" goto SetVSRoot
if /i "%~1"=="-serverlogging" goto SetServerLogging
if /i "%~1"=="/serverlogging" goto SetServerLogging
echo ERROR: Unknown argument '%~1'& exit /b -1

:SetVSRoot
shift
if "%~1"=="" echo ERROR: Expected version number.
set VSRootDir=%~1
if not exist "%VSRootDir%" echo ERROR: '/VSRootDir' value '%VSRootDir%' does not exist & exit /b -1
if not exist "%VSRootDir%\%MIEngineRelativePath%" echo ERROR: '/VSRootDir' value '%VSRootDir%' does not contain MIEngine (%VSRootDir%\%MIEngineRelativePath%) & exit /b -1
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

set SetLoggingError=
if NOT "%VSRootDir%"=="" call :SetLogging "%VSRootDir%" & goto Done

set ProgRoot=%ProgramFiles(x86)%
if "%ProgRoot%"=="" set ProgRoot=%ProgramFiles%

set VSVersionFound=
set MIEngineFound=
call :TryVSPath "%ProgRoot%\Microsoft Visual Studio 14.0"
call :TryVSPath "%ProgRoot%\Microsoft Visual Studio\VS15Preview"
if "%VSVersionFound%"=="" echo ERROR: Visual Studio 2015+ is not installed, or not installed to the default location. Use '/VSRootDir' to specify the directory. & exit /b -1
if "%MIEngineFound%"=="" echo ERROR: The found version(s) of Visual Studio do not have the MIEngine installed. & exit /b -1
goto Done

:Done
    echo.
    if NOT "%SetLoggingError%"=="" exit /b -1
    echo SetMIDebugLogging.cmd succeeded. Restart Visual Studio to take effect.
    if "%LoggingValue%"=="1" echo Logging will be saved to %TMP%\Microsoft.MIDebug.log.
    exit /b 0

:TryVSPath
    REM Arg1: path to VS Root

    if NOT "%SetLoggingError%"=="" goto :EOF
    if not exist "%~1" goto :EOF
    set VSVersionFound=1
    if not exist "%~1\%MIEngineRelativePath%" goto :EOF
    set MIEngineFound=1
    goto SetLogging

:SetLogging
    REM Arg1: path to VS Root
    set PkgDefFile=%~1\%MIEngineRelativeDir%\logging.pkgdef

    if NOT exist "%PkgDefFile%" goto SetLogging_NoPkgDef
        del "%PkgDefFile%"
        if NOT exist "%PkgDefFile%" goto SetLogging_NoPkgDef
        echo ERROR: Failed to remove "%PkgDefFile%". Ensure this script is run as an administrator.
        set SetLoggingError=1
        goto :EOF
    :SetLogging_NoPkgDef

    if "%LoggingValue%"=="0" goto UpdateConfiguration

    :EnableLogging
        echo [$RootKey$\Debugger]> "%PkgDefFile%"
            if exist "%PkgDefFile%" goto EnableLogging_PkgDefCreated
            echo ERROR: Failed to create "%PkgDefFile%". Ensure this script is run as an administrator.
            set SetLoggingError=1
            goto :EOF
        :EnableLogging_PkgDefCreated

        echo "Logging"=dword:00000001>> "%PkgDefFile%"
        if NOT "%ServerLogging%"=="" echo "GDBServerLoggingArguments"="%ServerLogging%">> "%PkgDefFile%"

    :UpdateConfiguration
    echo Setting logging for %1
    call "%~1\Common7\IDE\devenv.com" /updateconfiguration
    if "%ERRORLEVEL%"=="0" goto :EOF
    echo ERROR: '"%~1\Common7\IDE\devenv.com" /updateconfiguration' failed with error %ERRORLEVEL%. 
    set SetLoggingError=1
    goto :EOF

:Help
echo SetMIDebugLogging.cmd ^<on^|off^> [/serverlogging [full]] [/VSRootDir ^<value^>]
echo.
echo SetMIDebugLogging.cmd is used to enable/disable logging for the Microsoft
echo MI debug engine.
echo.
echo Logging will be saved to %TMP%\Microsoft.MIDebug.log.
echo.
echo /serverlogging [full] Enables logging from gdbserver. This option is only 
echo                   supported when enabling logging ('on'). 'full' logging will
echo                   turn on packet logging in addition to normal logging.
echo.
echo /VSRootDir ^<value^> sets the path to the root of Visual Studio 
echo                   (ex: C:\Program Files (x86)\Microsoft Visual Studio 14.0)
echo.
:eof
