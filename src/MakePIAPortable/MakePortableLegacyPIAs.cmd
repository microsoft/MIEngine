@echo on
setlocal

if "%~1"=="" goto help
if "%~1"=="-?" goto help
if "%~1"=="/?" goto help
if "%~2"=="" goto help

set ILDAsmPath="%~1"

if exist "%~2" goto DirectoryCreated
mkdir "%~2"
if NOT "%ERRORLEVEL%"=="0" echo ERROR: Failed to create destination directory '%~2' & exit /b -1
:DirectoryCreated

set PIAOutDir=%~f2
cd /d %~dp0

set IntDir=%PIAOutDir%
if exist "%IntDir%" rmdir /s /q "%IntDir%"
if exist "%IntDir%" echo ERROR: Failed to remove %IntDir%&exit /b -1
mkdir %IntDir%

set ILAsmPath="%windir%\Microsoft.NET\Framework\v4.0.30319\ilasm.exe"

set pias=%pias% Microsoft.VisualStudio.Debugger.InteropA.dll
set pias=%pias% Microsoft.VisualStudio.Debugger.Interop.10.0.dll
set pias=%pias% Microsoft.VisualStudio.Debugger.Interop.11.0.dll
set pias=%pias% Microsoft.VisualStudio.Debugger.Interop.12.0.dll
set pias=%pias% Microsoft.VisualStudio.Debugger.Interop.15.0.dll

set PIAERROR=
for %%i in (%pias%) do call :ProcessPIA %%i
echo.
if NOT "%PIAERROR%"=="" echo ERROR Processing one or more files.& exit /b -1
echo Successfully processed PIAs.
exit /b 0

:ProcessPIA
echo.
echo Processing %1...

if exist %PIAOutDir%\%~n1.dll del %PIAOutDir%\%~n1.dll
if exist %PIAOutDir%\%~n1.dll echo ERROR: Unable to remove %PIAOutDir%\%~n1.dll & set PIAERROR=1& goto eof

%ILDAsmPath% %1 /NOBAR /out:%IntDir%\%~n1.il
if NOT "%ERRORLEVEL%"=="0" echo ERROR: Failed to disassembly %1.& set PIAERROR=1& goto eof

MakePIAPortable.exe %IntDir%\%~n1.il %IntDir%\%~n1-portable.il
if NOT "%ERRORLEVEL%"=="0" echo ERROR: Failed to fixup %1.& set PIAERROR=1& goto eof

"%ILAsmPath%" /nologo /quiet /dll %IntDir%\%~n1-portable.il /output=%PIAOutDir%\%~n1.dll /RESOURCE=%IntDir%\%~n1.res
if NOT "%ERRORLEVEL%"=="0" echo ERROR: Failed to reassemble %1.& set PIAERROR=1& goto eof

goto eof

:help
echo Syntax: MakePortableLegacyPIAs.cmd ^<ILDAsm-location^> ^<destination-dir^>
echo.
echo This script will process the legacy PIA files (Microsoft.VisualStudio.Debugger.InteropA.dll, etc) and create portable versions of them.
echo.

:eof
