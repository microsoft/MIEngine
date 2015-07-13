@echo off 
setlocal

if "%_DeviceId%"=="" echo ERROR: DeviceId not set. This should be set by androidtest.cmd.& exit /b -1
if not exist "%_SdkRoot%\platform-tools\adb.exe" echo ERROR: Cannot find adb.exe.& exit /b -1

set _adb=%_SdkRoot%\platform-tools\adb.exe

"%_adb%" -s "%_DeviceId%" shell am force-stop com.Attach/android
:: there is no way to check if this returns an error code AFAIK. https://code.google.com/p/android/issues/detail?id=3254
:: if the am start fails, the test will fail.

exit /b 0