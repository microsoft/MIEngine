@echo off
setlocal

REM Copyright (c) Microsoft. All rights reserved.
REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

if "%~2"=="" goto help
if NOT "%~3"=="" goto help
goto Run
:Help
echo syntax: PostProcessXsdOutput.cmd ^<path_to_xsd_exe_generated_file^> ^<ouput_file_path^>
echo.
echo PostProcessXsdOutput.cmd will remove attributes from the xsd generated file which make
echo it incompatible with portable class library projects.
echo.
exit /b -1

:Run
findstr /v /c:"[System.SerializableAttribute()]" /c:"[System.ComponentModel.DesignerCategoryAttribute(" %1 > %2
exit /b %ERRORLEVEL%
