@echo off
setlocal

if not defined VisualStudioVersion (
    if defined VS140COMNTOOLS (
        call "%VS140COMNTOOLS%\VsDevCmd.bat"
        goto :EnvSet
    )

    echo Error: build.cmd requires Visual Studio 2015.
    exit /b -1
)
:EnvSet

set _DeviceId=
set _Platform=
set _SdkRoot=%ProgramFiles(x86)%\Android\android-sdk
set _NdkRoot=%ProgramData%\Microsoft\AndroidNDK\android-ndk-r11c
set _LoopCount=
set _Verbose=
set _TestsToRun=

if "%~1"=="-?" goto Help
if "%~1"=="/?" goto Help

call :SetProjectRoot %~dp0..\..\

set _GlassPackageName=Microsoft.VisualStudio.Glass
set _GlassPackageVersion=1.0.0

set _GlassDir=%_ProjectRoot%%_GlassPackageName%\

:: Get Glass from NuGet
if NOT exist "%_GlassDir%glass2.exe" echo Getting Glass from NuGet.& call "%_ProjectRoot%tools\NuGet\nuget.exe" install %_GlassPackageName% -Version %_GlassPackageVersion% -ExcludeVersion -OutputDirectory %_ProjectRoot%
if NOT "%ERRORLEVEL%"=="0" echo ERROR: Failed to get Glass from NuGet.& exit /b -1

:: Ensure the project has been built
if NOT exist "%_GlassDir%Microsoft.MIDebugEngine.dll" echo The project has not been built. Building now with default settings.& call %_ProjectRoot%build.cmd
if NOT "%ERRORLEVEL%"=="0" echo ERROR: Failed to build MIEngine project.& exit /b -1

:: Copy libadb.dll to the glass directory 
xcopy /Y /D "%VSINSTALLDIR%Common7\IDE\PrivateAssemblies\libadb.dll" "%_GlassDir%"
if not "%ERRORLEVEL%"=="0" echo ERROR: Unable to copy libadb.dll from Visual Studio installation.& exit /b -1

call :EnsureGlassRegisterd
if not "%ERRORLEVEL%"=="0" exit /b -1

call :EnsureLaunchOptionsGenBuilt


:ArgLoopStart
if "%~1"=="" goto ArgLoopEnd
if /i "%~1"=="/DeviceId"    goto SetDeviceId
if /i "%~1"=="/Platform"    goto SetPlatform
if /i "%~1"=="/SdkRoot"     goto SetSdkRoot
if /i "%~1"=="/NdkRoot"     goto SetNdkRoot
if /i "%~1"=="/v"           set _Verbose=1& goto NextArg
if /i "%~1"=="/Loop"        goto SetLoop
if /i "%~1"=="/Tests"       goto SetTests
echo ERROR: Unknown argument '%~1'.&exit /b -1

:NextArg
shift /1
goto ArgLoopStart

:SetDeviceId
shift /1
set _DeviceId=%~1
goto :NextArg

:SetPlatform
shift /1
set _Platform=%~1&
goto :NextArg

:SetSdkRoot
shift /1
set _SdkRoot=%~1
goto :NextArg

:SetNdkRoot
shift /1
if "%~1" == "10" (
	set _NdkRoot=%ProgramData%\Microsoft\AndroidNDK\android-ndk-r10e
) else (
	set _NdkRoot=%~1
)
goto :NextArg

:SetLoop
shift /1
set _LoopCount=%~1
goto :NextArg

:SetTests
shift /1
if "%~1"=="" goto SetTestsDone
set _TestsToRun="%~1" %_TestsToRun%
goto SetTests
:SetTestsDone
goto ArgLoopEnd

:ArgLoopEnd

if NOT exist "%_SdkRoot%" echo ERROR: Android SDK does not exist at "%_SdkRoot%".& exit /b -1
if NOT exist "%_NdkRoot%" echo ERROR: Android NDK does not exist at "%_NdkRoot%".& exit /b -1

if "%_DeviceId%"=="" call :FindDeviceId
if "%_DeviceId%"=="" echo ERROR: DeviceId must be specified. Possible devices are:& "%_AdbExe%" devices &goto Help

set _GlassFlags=-f TestScript.xml -e ErrorLog.xml -s SessionLog.xml -err -nodefaultsetup -nodvt
if not "%_Verbose%"=="1" set _GlassFlags=%_GlassFlags% -q

set _GlassLog=
if NOT "%_Verbose%"=="1" set _GlassLog=^> glass.log

if "%_Verbose%"=="1" (
    echo DeviceId:    %_DeviceId%
    echo Platform:    %_Platform%
    echo SdkRoot:     "%_SdkRoot%"
    echo NdkRoot:     "%_NdkRoot%"
    echo Tests:       %_TestsToRun%
    echo Glass Flags: %_GlassFlags%
)

if "%_TestsToRun%"=="" goto RunAll
goto RunArgs

:FindDeviceId
if "%_Platform%"=="" set _Platform=x86
pushd %_SdkRoot%
for /f "tokens=1,6" %%i in ('platform-tools\adb devices -l ^| find /i "VS Emulator"') do (
    echo Run on device: %%i %%j
    set _DeviceId=%%i
    popd
    exit /b 0
)
exit /b -1

:RunAll
    set FAILED_TESTS=
    
    pushd %~dp0
    for /d %%t in (*) do if exist "%%t\TestScript.xml" call :RunSingleTest "%%t"
    goto ReportResults

:RunArgs
    set FAILED_TESTS=
    
    pushd %~dp0
    for %%t in (%_TestsToRun%) do call :ValidateArg %%t
    if NOT "%FAILED_TESTS%"=="" exit /b -1
    for %%t in (%_TestsToRun%) do call :RunSingleTest %%t
    goto ReportResults

:ReportResults
    echo ----------------------------------------
    if NOT "%FAILED_TESTS%"=="" goto ReportFailure
    goto ReportSuccess
    
:ReportFailure
    echo ERROR: Failures detected 'AndroidTest.cmd /DeviceId %_DeviceId% /Platform %_Platform% /Tests %FAILED_TESTS%' to rerun.
    echo.
    exit /b -1
    
:ReportSuccess
    echo All tests completed succesfully.
    echo.
    exit /b 0
    
:ValidateArg
    if not exist "%~1" echo ERROR: Test '%~1' does not exist.& set FAILED_TESTS="%~1" %FAILED_TESTS%& exit /b 0
    if not exist "%~1\TestScript.xml" echo ERROR: Test '%~1' does not have a TestScript.xml file.& set FAILED_TESTS="%~1" %FAILED_TESTS%& exit /b 0
    exit /b 0

:RunSingleTest
    if "%_LoopCount%"=="" goto RunSingleTestOnce
    set _ContinueRunningTests=true
    FOR /L %%l IN (1,1,%_LoopCount%) DO call :RunSingleTestInLoop %*
    goto EOF

:RunSingleTestInLoop
    if NOT "%_ContinueRunningTests%"=="true" goto EOF
    call :RunSingleTestOnce %*
    if NOT "%LastTestSucceeded%"=="true" set _ContinueRunningTests=false
    goto EOF

:RunSingleTestOnce
    pushd "%~1"
    echo Running '%~1'
    set LastTestSucceeded=false
    
    ::Build the app
    call msbuild /p:Platform=%_Platform%;VS_NDKRoot="%_NdkRoot%";VS_SDKRoot="%_SdkRoot%";PackageDebugSymbols=true > build.log
    if NOT "%ERRORLEVEL%"=="0" echo ERROR: Failed to build %~1. See build.log for more information.& set FAILED_TESTS="%~1" "%FAILED_TESTS%"& goto RunSingleTestDone
    
    ::Deploy the app
    call "%_SdkRoot%\platform-tools\adb.exe" -s %_DeviceId% install -r %_Platform%\Debug\%~1.apk > adb.log 2>&1
    if NOT "%ERRORLEVEL%"=="0" echo ERROR: adb failed for one reason or another.& set FAILED_TESTS="%~1" "%FAILED_TESTS%"& goto RunSingleTestDone
    
    ::Create temp directory
    if not exist temp mkdir temp
    
    ::Generate the LaunchOptions
    call %_GlassDir%LaunchOptionsGen.exe LaunchOptions.xml.template "SdkRoot=%_SdkRoot%\ " "NdkRoot=%_NdkRoot%\ " "TargetArchitecture=%_Platform%" "IntermediateDirectory=%~dp1temp\ " "AdditionalSOLibSearchPath=%~dp1%_Platform%\Debug\ " "DeviceId=%_DeviceId%"

    ::Run Glass
    call "%_GlassDir%glass2.exe" %_GlassFlags% %_GlassLog%
    if NOT "%ERRORLEVEL%"=="0" echo ERROR: Test failed. See ErrorLog.xml for more information.& set FAILED_TESTS="%~1" %FAILED_TESTS%& goto RunSingleTestDone

    set LastTestSucceeded=true
    
    :: remove installed package
    call "%_SdkRoot%\platform-tools\adb.exe" -s %_DeviceId% shell pm uninstall -k com.%~1  > adb.log
    
    :RunSingleTestDone
    popd
    exit /b 0

::These are functions called at the beginning

:EnsureGlassRegisterd
REM Check if the key exists. If not, we need to register glass.
reg query HKLM\SOFTWARE\Microsoft\glass\14.0 /v InstallDir /reg:32 1>NUL 2>NUL
if NOT "%ERRORLEVEL%"=="0" goto :RegisterGlass

REM Check if glass is currently registered at a differnt location
set CurrentGlassRegRoot=
for /f "tokens=3* skip=2" %%a in ('reg query HKLM\SOFTWARE\Microsoft\glass\14.0 /v InstallDir /reg:32') do call :SetCurrentGlassRegRoot %%a %%b %%c %%d %%e
if /i "%CurrentGlassRegRoot%"=="%_GlassDir%" goto EOF

:RegisterGlass
echo Running RegisterGlass.cmd... 
call "%_GlassDir%RegisterGlass.cmd"
if NOT "%ERRORLEVEL%"=="0" echo ERROR: Unable to register glass. Ensure that this command prompt is elevated.& goto :EOF
call "%~dp0..\RegisterMIEngine.cmd"
if NOT "%ERRORLEVEL%"=="0" echo ERROR: Unable to register MIEngine.& goto :EOF
set ERRORLEVEL=0
goto EOF

:SetCurrentGlassRegRoot
REM %1 contains the glass install root. Unfortunately, if the path to the has spaces in it,
REM these will get broken up with each chunk in a different token. Lets try to put these tokens
REM back together.
set CurrentGlassRegRoot=%1
if "%2"=="%%b" goto EOF
if "%2"=="" goto EOF
if "%2"=="d" goto EOF
set CurrentGlassRegRoot=%1 %2
if "%3"=="%%c" goto EOF
if "%3"=="" goto EOF
if "%3"=="e" goto EOF
set CurrentGlassRegRoot=%1 %2 %3
if "%4"=="%%d" goto EOF
if "%4"=="" goto EOF
set CurrentGlassRegRoot=%1 %2 %3 %4
if "%5"=="%%e" goto EOF
if "%5"=="" goto EOF
set CurrentGlassRegRoot=%1 %2 %3 %4 %5
goto EOF

:EnsureLaunchOptionsGenBuilt
if not exist %_GlassDir%LaunchOptionsGen.exe echo Building LaunchOptionsGen.exe& msbuild /p:Configuration=Release;OutDir=%_GlassDir% /v:quiet %~dp0..\LaunchOptionsGen\LaunchOptionsGen.csproj
exit /b 0

:SetProjectRoot
set _ProjectRoot=%~f1
goto EOF

:Help
echo --- MIEngine Android Test Script ---
echo Usage: androidtest.cmd /DeviceId ^<id^> /Platform ^<platform^> [/SdkRoot ^<path^>] [/NdkRoot ^<path^>] [/v] [/Loop ^<num_iterations^>] [/Tests ^<test 1^> [^<test 2^> [...]]]
echo. 
echo --- Examples --- 
echo Run All Tests:
echo androidtest.cmd /DeviceId 169.254.138.123:5555 /Platform x86 
echo.
echo Run two specific tests:
echo androidtest.cmd /DeviceId 169.254.138.123:5555 /Platform x86 /Tests Stepping Exceptions
echo.

if exist "%_SdkRoot%" "%_SdkRoot%\platform-tools\adb.exe" devices -l

:EOF