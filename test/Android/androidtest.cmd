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

set _GlassDir=%~dp0\..\..\glass\
if NOT exist "%_GlassDir%" echo ERROR: Glass no present at %_GlassDir%& exit /b -1

if NOT exist "%_GlassDir%Microsoft.MIDebugEngine.dll" echo ERROR: The project has not been built. Please run build.cmd from the project root or build the project inside of Visual Studio.& exit /b -1

if "%~1"=="" goto Help
if "%~1"=="-?" goto Help
if "%~1"=="/?" goto Help

call :EnsureGlassRegisterd
call :EnsureLaunchOptionsGenBuilt

set _DeviceId=%~1& shift
set _Platform=%~1& shift

if "%~1"=="/SdkRoot" shift& set _SdkRoot=%~1& shift
if "%~1"=="/NdkRoot" shift& set _NdkRoot=%~1& shift

if "%_SdkRoot%"=="" set _SdkRoot=C:\Program Files (x86)\Android\android-sdk
if NOT exist "%_SdkRoot%" echo ERROR: Android SDK does not exist in default location and was not specified on command line.& exit /b -1

if "%_NdkRoot%"=="" set _NdkRoot=C:\ProgramData\Microsoft\AndroidNDK\android-ndk-r10d
if NOT exist "%_SdkRoot%" echo ERROR: Android NDK does not exist in default location and was not specified on command line.& exit /b -1

:: Copy libadb.dll to the glass directory
copy /y "%VSINSTALLDIR%Common7\IDE\PrivateAssemblies\libadb.dll" "%_GlassDir%"
if not "%ERRORLEVEL%"=="0" echo ERROR: Unable to copy libadb.dll from Visual Studio installation.& exit /b -1

if "%~1"=="" goto RunAll

set _TestsToRun=
:BuildTestList
set _TestsToRun="%~1" %_TestsToRun%& shift
if NOT "%~1"=="" goto BuildTestList
goto RunArgs

:EnsureGlassRegisterd
reg query HKLM\SOFTWARE\Microsoft\glass\14.0 1>NUL 2>NUL
if errorlevel 1 reg query HKLM\SOFTWARE\Wow6432Node\Microsoft\glass\14.0 1>NUL 2>NUL & if errorlevel 1 echo Running RegisterGlass.cmd... & "%_GlassDir%RegisterGlass.cmd"
exit /b -1

:EnsureLaunchOptionsGenBuilt
if not exist %_GlassDir%LaunchOptionsGen.exe echo Building LaunchOptionsGen.exe& msbuild /p:Configuration=Release;OutDir=%_GlassDir% /v:quiet %~dp0..\LaunchOptionsGen\LaunchOptionsGen.csproj
exit /b 0

:RunAll
	set FAILED_TESTS=
	
	pushd %~dp0\
	
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
	echo ERROR: Failures detected 'RunTests.cmd %FAILED_TESTS%' to rerun.
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
	pushd "%~1"
	echo Running '%~1'
	
	::Build the app
	call msbuild /p:Platform=%_Platform%;VS_NDKRoot="%_NdkRoot%";VS_SDKRoot="%_SdkRoot%";PackageDebugSymbols=true > build.log
	if NOT "%ERRORLEVEL%"=="0" echo ERROR: Failed to build %~1. See build.log for more information.& set FAILED_TESTS="%~1" "%FAILED_TESTS%"& goto RunSingleTestDone
	
	::Deploy the app
	call "%_SdkRoot%\platform-tools\adb.exe" -s %_DeviceId% install -r %_Platform%\Debug\%~1.apk > adb.log
	if NOT "%ERRORLEVEL%"=="0" echo ERROR: adb failed for one reason or another.& set FAILED_TESTS="%~1" "%FAILED_TESTS%"& goto RunSingleTestDone
	
	::Create temp directory
	if not exist temp mkdir temp
	
	::Generate the LaunchOptions
	call %_GlassDir%LaunchOptionsGen.exe LaunchOptions.xml.template "SdkRoot=%_SdkRoot%\ " "NdkRoot=%_NdkRoot%\ " "TargetArchitecture=%_Platform%" "IntermediateDirectory=%~dp1temp\ " "AdditionalSOLibSearchPath=%~dp1%_Platform%\Debug\ " "DeviceId=%_DeviceId%"

	::Run Glass
	call "%_GlassDir%glass2.exe" -f TestScript.xml -e ErrorLog.xml -s SessionLog.xml -q -err -nodefaultsetup > glass2.log
	if NOT "%ERRORLEVEL%"=="0" echo ERROR: Test failed. See ErrorLog.xml for more information.& set FAILED_TESTS="%~1" %FAILED_TESTS%
	
	:RunSingleTestDone
	popd
	exit /b 0

:Help
echo testandroid.cmd ^<device id^> ^<platform^> [/SdkRoot ^<path^>] [/NdkRoot ^<path^>] [^<test 1^> [^<test 2^> [...]]]

:EOF