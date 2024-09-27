param(
[ValidateSet("Debug", "Release")]
[Alias("c")]
[string]$Configuration="Debug",

[ValidateSet("win-x86", "win-arm64")]
[Alias("r")]
[string]$RID="win-x86",

[ValidateSet("vs", "vscode")]
[Alias("t")]
[string]$TargetPlatform="vs"
)

$ErrorActionPreference="Stop"

$RootPath = Resolve-Path -Path "$PSScriptRoot\..\.."

$msbuildPath = (Get-Command msbuild.exe -ErrorAction Ignore).Path

if (!$msbuildPath) {
    throw "Please run the script from a developer command prompt or have msbuild.exe in your PATH"
}

msbuild /t:Restore $RootPath\src\MIDebugEngine.sln /p:Configuration=$Configuration
if ($lastexitcode -ne 0)
{
    throw "Failed to restore packages for MIDebugEngine.sln"
}
msbuild $RootPath\src\MIDebugEngine.sln /p:Configuration=$Configuration
if ($lastexitcode -ne 0)
{
    throw "Failed to build MIDebugEngine.sln"
}


if ($TargetPlatform -eq "vscode")
{
    $dotnetPath = (Get-Command dotnet.exe -ErrorAction Ignore).Path;

    if (!$dotnetPath) {
        throw "Missing .NET SDK. Please install the SDK at https://dotnet.microsoft.com/download"
    }

    dotnet publish $RootPath\src\OpenDebugAD7\OpenDebugAD7.csproj -c $Configuration -r $RID --self-contained -o $RootPath\bin\DebugAdapterProtocolTests\$Configuration\extension\debugAdapters
    if ($lastexitcode -ne 0)
    {
        throw "Failed to publish OpenDebugAD7"
    }

    Copy-Item $RootPath\bin\$Configuration\Microsoft.MIDebugEngine.dll $RootPath\bin\DebugAdapterProtocolTests\$Configuration\extension\debugAdapters/.
    Copy-Item $RootPath\bin\$Configuration\Microsoft.MICore.dll $RootPath\bin\DebugAdapterProtocolTests\$Configuration\extension\debugAdapters\.
    Copy-Item $RootPath\bin\$Configuration\vscode\WindowsDebugLauncher.exe $RootPath\bin\DebugAdapterProtocolTests\$Configuration\extension\debugAdapters\.
}