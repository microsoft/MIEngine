param(
[ValidateSet("Debug", "Release")]
[Alias("c")]
[string]$Configuration="Debug"
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