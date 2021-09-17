@echo off
setlocal

if "%~1"=="/?" goto Help
if "%~1"=="-?" goto Help
if "%~1"=="-h" goto Help

@SET Args= %*
@SET Args=%Args: /= -%
powershell -NoProfile -ExecutionPolicy bypass -File %~dp0CI-Build.ps1 %Args%
@exit /B %ERRORLEVEL%
goto :EOF

:Help
call powershell -NoProfile -ExecutionPolicy ByPass -Command "get-help %~dp0CI-Build.ps1 -detailed"