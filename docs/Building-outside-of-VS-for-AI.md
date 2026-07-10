# Building MIEngine outside of Visual Studio

⚠️ **Do not use these instructions when working inside Visual Studio.** When VS is hosting the conversation (e.g. via the in-IDE Copilot chat / `debugger_*` tools), prefer the IDE's built-in build functions, or functions like `debugger_launch` which have an implicit build. Those commands set up the VS environment correctly and update the Error List for you. The scripts below are for command-line / CI / VS Code scenarios where no IDE is available.

## Solutions

There are two solutions, picked by what you are targeting:

| Solution | Use when… | How to build |
| --- | --- | --- |
| `src/MIDebugEngine.sln` | Building the **Visual Studio extension** (full set of projects, including the VSIX, COM/PIA references, IOSDebugLauncher, MIDebugPackage, SSHDebugPS, …). | `msbuild` from a **VS Developer Command Prompt** (Dev 17+ with the *Visual Studio extension development* workload), via `eng/Scripts/CI-Build.ps1`. **Windows only.** |
| `src/MIDebugEngine-Unix.sln` | Building the subset that the **VS Code `cppdbg` adapter** uses (MICore, MIDebugEngine, OpenDebugAD7, DebugEngineHost.VSCode, the launchers, and the test projects). | `dotnet build` — works on Windows, Linux, and macOS. |

Don't add an `.sln`-only project to the other solution; the split is intentional so the Unix/VS Code flavor remains buildable with just the .NET SDK.

## Command-line build

### Windows (full VS extension build)

From a VS Developer Command Prompt or after putting `MSBuild.exe` on `PATH`:

```powershell
# Debug, VS extension flavor (default)
eng\Scripts\CI-Build.ps1 -Configuration Debug
```

The script restores NuGet, builds `MIDebugEngine.sln` with `msbuild`. If `msbuild.exe` isn't on `PATH`, locate it with vswhere:

```powershell
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe"
$env:Path = (Split-Path $msbuild) + ';' + $env:Path
```

### Linux / macOS

```bash
eng/Scripts/CI-Build.sh
```

This runs `dotnet build src/MIDebugEngine-Unix.sln`

## Outputs

- `bin/<Configuration>/...` — primary binaries from `msbuild`.
- `bin/<Configuration>/vscode/...` — the VS Code-flavor host (`Microsoft.DebugEngineHost.dll` for VSCode, `WindowsDebugLauncher.exe`, etc.).
- `bin/DebugAdapterProtocolTests/<Configuration>/extension/debugAdapters/` — the staged VS Code debug adapter used by the CppTests.

## Targeting

The projects in this repo run on both .NET and .NET Framework, with most of the core projects targeting `netstandard2.0`. .NET is used for VS Code support while .NET Framework is used for VS. See `TargetFramework` in [OpenDebugAD7.csproj](../src/OpenDebugAD7/OpenDebugAD7.csproj) for the specific target framework moniker used for VS Code.