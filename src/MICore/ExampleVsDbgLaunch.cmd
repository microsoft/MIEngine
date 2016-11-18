REM
REM This is an example script for starting a project on Windows and attaching to it with vsdbg. 
REM Various paths at the top of the script may need to be changed. 
REM 
REM See ExampleVsDbgLaunchOptions.Windows.xml for more information.
REM 

setlocal
set vsdbg=C:\dd\VSPro_VBCS\binaries\amd64chk\Debugger\x-plat\Windows\vsdbg.exe
set ProjectName=AspNet5Con
set ProjectDir=C:\proj\AspNet5Con\src\AspNet5Con
set dnx=%USERPROFILE%\.dnx\runtimes\dnx-coreclr-win-x64.1.0.0-beta4-11296\bin\dnx.exe

if not exist "%vsdbg%" echo ERROR: %vsdbg% does not exist.>&2 & exit /b -1
if not exist "%dnx%" echo ERROR: %dnx% does not exist.>&2 & exit /b -1

start %dnx% --appbase %ProjectDir% "Microsoft.Framework.ApplicationHost"  --configuration Debug "%ProjectName%"
if NOT "%ERRORLEVEL%"=="0" echo ERROR: Failed to start dnx.>&2 & exit /b -1

REM NOTE: There may be more than once and we want the last pid
set target_pid=
for /f "skip=3 tokens=2" %%p in ('tasklist /FI "IMAGENAME eq dnx.exe"') do set target_pid=%%p

if "%target_pid%"=="" echo ERROR: Failed to find dnx.exe process.>&2 & exit /b -1

%vsdbg% --interpreter=mi --attach %target_pid%
