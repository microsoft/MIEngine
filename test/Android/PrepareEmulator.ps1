[CmdletBinding()]
Param(
    [switch]$killRunningEmulator
)

$xdeProcess = Get-Process | where {$_ -like "*XDE*"}
if ($xdeProcess.count -gt 0)
{
    if ($killRunningEmulator)
    {
        <# Kill emulator if already started, otherwise the hanging issue is more likely to repro. #>
        $xdeProcess | Stop-Process
    }
    else 
    {
        Write-Output "Emulator already started"
        exit 0
    }
}

$emulatorPath = ${Env:\ProgramFiles(x86)} + '/Microsoft Emulator Manager/1.0'

if (!(Test-Path $emulatorPath))
{
    Write-Error "Emulator manager is not installed at $emulatorPath"
    exit 1
}

$deviceList = & "$emulatorPath/emulatorcmd.exe" list /sku:Android /type:device

<# try to use Lollipop emulator if there is one #>
$device = $deviceList | where {$_ -match ".*(Lollipop).*" } | Select-Object -First 1
if (!$device) 
{
    $device = $deviceList | where {$_ -match ".*(Marshmallow|KitKat).*" } | Select-Object -First 1

    if (!$device)
    {
        Write-Error 'Can not find an valid emulator device'
        exit 1    
    }
}

$adbPath = ${Env:\ProgramFiles(x86)} + '/Android/android-sdk/platform-tools'

if (!(Test-Path "$adbPath/adb.exe"))
{
    Write-Error "Adb.exe does not exist under $adbPath"
    exit 1
}

& "$adbPath/adb.exe" kill-server

<# launch emulator #>
Write-Output "Starting device: $device"

$deviceId = $device.Substring(0, $device.IndexOf(' '))

pushd $emulatorPath
start-process powershell -argumentlist ".\emulatorcmd.exe launch /sku:android /id:$deviceId"
popd

<# wait for emulator to be ready #>
$timeout = new-timespan -Minutes 3
$stopwatch = [diagnostics.stopwatch]::StartNew()

do {    
    Start-Sleep -s 2    
    $result = & "$adbPath/adb.exe" shell getprop init.svc.bootanim 2>&1
    echo $result
} while (($result -ne 'running') -and ($stopwatch.elapsed -lt $timeout))

if ($result -ne 'running')
{
    Write-Error "Failed to start emulator"
    exit 1
}

<# wait a few more seconds to ensure the emulator is fully booted #>
Start-Sleep -s 10

exit 0