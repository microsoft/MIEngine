@echo off
setlocal

REM Copyright (c) Microsoft. All rights reserved.
REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

if "%~3"=="" goto help
if NOT "%~4"=="" goto help
goto Run
:Help
echo syntax: ValidateDesignerFile.cmd ^<path_to_designer_file^> ^<path_to_candidate_file^> ^<intermediate_directory^>
echo.
echo ValidateDesignerFile.cmd will determine if the designer file and the candidate 
echo file are the same (excluding full line c-style comments and whitespace). 
echo If they are different the designer file will be updated.
echo.
exit /b -1

:Run
if not exist "%2" echo ERROR: %2 does not exist & exit /b -1
if not exist "%1" goto :UpdateFile
findstr /v /r /c:"^ *//" %1 | findstr /v /c:"[System.CodeDom.Compiler.GeneratedCodeAttribute(" > "%~3\%~nx1-nocomments"
findstr /v /r /c:"^ *//" %2 | findstr /v /c:"[System.CodeDom.Compiler.GeneratedCodeAttribute(" > "%~3\%~nx2-nocomments"

fc /W "%~3\%~nx1-nocomments" "%~3\%~nx2-nocomments"
if not "%ERRORLEVEL%"=="0" goto UpdateFile

echo %1 is already up to date.
exit /b 0

:UpdateFile
echo Updating %1
copy /y %2 %1
if NOT %ERRORLEVEL%==0 echo ERROR: Unable to update %1. >&2 & exit /b -1

exit /b 0