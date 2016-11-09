<#
.SYNOPSIS
Downloads the given $Version of clrdbg for the given $RuntimeID and installs it to the given $InstallPath

.DESCRIPTION
The following script will generate a project.json and NuGet.config and use dotnet restore and publish to install clrdbg, the .NET Core Debugger

.PARAMETER Version
Specifies the version of clrdbg to install. Can be 'latest', VS2015U2, or a specific version string i.e. 14.0.25406-preview-3044032

.PARAMETER RuntimeID
Specifies the .NET Runtime ID of the clrdbg that will be downloaded. Example: ubuntu.14.04-x64. Defaults to the Runtime ID of the current machine.

.Parameter InstallPath
Specifies the path where clrdbg will be installed. Defaults to the directory containing this script.

.INPUTS
None. You cannot pipe inputs to GetClrDbg.

.EXAMPLE
C:\PS> .\GetClrDbg.ps1 -Version latest -RuntimeID ubuntu.14.04-x64 -InstallPath .\clrdbg

.LINK
https://github.com/Microsoft/MIEngine
#>

Param (
    [Parameter(Mandatory=$true, ParameterSetName="ByName")]
    [string]
    [ValidateSet("latest", "VS2015U2")]
    $Version,

    [Parameter(Mandatory=$true, ParameterSetName="ByNumber")]
    [string]
    [ValidatePattern("\d+\.\d+\.\d+.*")]
    $VersionNumber,

    [string]$RuntimeID,
    [string]$InstallPath = (Split-Path -Path $MyInvocation.MyCommand.Definition)
)

function GetDotNetRuntimeID() {
    $ridLine = dotnet --info | findstr "RID"
    
    if ([System.String]::IsNullOrEmpty($ridLine)) {
        throw [System.Exception] "Unable to determine runtime from dotnet --info. Make sure dotnet cli is up to date on this machine"
    }

    $rid = $ridLine.Split(":")[1].Trim();

    if ([System.String]::IsNullOrEmpty($rid)) {
        throw [System.Exception] "Unable to determine runtime from dotnet --info. Make sure dotnet cli is up to date on this machine"
    }

    return $rid
}

# Produces project.json in the current directory
function GenerateProjectJson([string] $version, [string]$runtimeID) {
    $projectJson = 
"{
    `"dependencies`": {
       `"Microsoft.VisualStudio.clrdbg`": `"$version`"
    },
    `"frameworks`": {
        `"netcoreapp1.0`": {
          `"imports`": [ `"dnxcore50`", `"portable-net45+win8`" ]
       }
   },
   `"runtimes`": {
      `"$runtimeID`": {}
   }
}"

    $projectJson | Out-File -Encoding utf8 project.json
}

# Produces NuGet.config in the current directory
function GenerateNuGetConfig() {
    $nugetConfig = 
"<?xml version=`"1.0`" encoding=`"utf-8`"?>
<configuration>
  <packageSources>
      <clear />
      <add key=`"api.nuget.org`" value=`"https://api.nuget.org/v3/index.json`" />
  </packageSources>
</configuration>"

    $nugetConfig | Out-File -Encoding utf8 NuGet.config
}

function IsProjectJsonSupported() {
    # Last preview2 release version is 3133 and starting preview3 only csproj files were supported.
    $dotnetVersion = dotnet --version
    return (($dotnetVersion.Split("-")[2] -as [int]) -le 3133)
}

if ($Version -eq "latest") {
    $VersionNumber = "15.0.25904-preview-3404276"
} elseif ($Version -eq "vs2015u2") {
    $VersionNumber = "15.0.25904-preview-3404276" 
}
Write-Host "Info: Using clrdbg version '$VersionNumber'"

if (-not $RuntimeID) {
    $RuntimeID = GetDotNetRuntimeID
}
Write-Host "Info: Using Runtime ID '$RuntimeID'"

# create the temp folder if it does not exist
$GuidString = [System.Guid]::NewGuid()
$TempPath = Join-Path -Path $env:TEMP -ChildPath $GuidString
if (-not (Test-Path -Path $TempPath -PathType Container)) {
    New-Item -ItemType Directory -Force -Path $TempPath
}
$TempPath = Resolve-Path -Path $TempPath -ErrorAction Stop

# if we were given a relative path, assume its relative to the script directory and create an absolute path
if (-not([System.IO.Path]::IsPathRooted($InstallPath))) {
    $InstallPath = Join-Path -Path (Split-Path -Path $MyInvocation.MyCommand.Definition) -ChildPath $InstallPath
}

Push-Location $TempPath -ErrorAction Stop

if (IsProjectJsonSupported) {
    # Legacy support till supported versions are available as zip.
    Write-Host "Info: Generating project.json"
    GenerateProjectJson $VersionNumber $RuntimeID
    
    Write-Host "Info: Generating NuGet.config"
    GenerateNuGetConfig
    
    Write-Host "Info: Executing dotnet restore"
    dotnet restore
    
    Write-Host "Info: Executing dotnet publish"
    dotnet publish -r $RuntimeID -o $InstallPath
} else {
    $target = ($VersionNumber + '-' + $RuntimeID).Replace('.','-')
    $url = "https://vsdebugger.azureedge.net/clrdbg-" + $target + "/clrdbg.zip"
    Write-Host "Downloading: " $url

    $zipfile = Join-Path $TempPath "clrdbg.zip"
    Invoke-WebRequest -uri $url -o $zipfile
    Add-Type -assembly "System.IO.Compression.FileSystem"
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $InstallPath)
}

Pop-Location

Remove-Item -Path $TempPath -Recurse -Force

Write-Host "Successfully installed clrdbg at '$InstallPath'"