# Running the in-process unit tests outside of Visual Studio

⚠️ **Do not use these instructions when working inside Visual Studio.** Use VS **Test Explorer** (or the `debugger_run_tests` tool if available) to discover, run, and debug these tests — that path attaches the VS debugger and updates the Test Explorer UI. The commands below are for command-line / CI scenarios.

This page covers the four pure .NET unit-test assemblies. For end-to-end DAP tests against a real `gdb`/`lldb-mi`, see [RunningCppTests-outside-of-VS-for-AI.md](RunningCppTests-outside-of-VS-for-AI.md).

## The unit-test assemblies

| Project | What it tests |
| --- | --- |
| `MICoreUnitTests` | MI parser, transports, command factories. |
| `MIDebugEngineUnitTests` | AD7 wrappers, expression evaluation helpers, and other in-engine logic. |
| `JDbgUnitTests` | JDWP client used by `AndroidDebugLauncher`. |
| `SSHDebugTests` | SSHDebugPS port supplier and related SSH helpers. |

All four are built by `MIDebugEngine.sln` (Windows) and most are also built by `MIDebugEngine-Unix.sln`.

## Prerequisites

- A successful build (see [Building-outside-of-VS-for-AI.md](Building-outside-of-VS-for-AI.md)).
- No native toolchain is required — these are managed-only tests.

## Running the full suite (Windows / VSTest)

This matches what GitHub Actions does. From a VS Developer Command Prompt:

```powershell
vstest.console.exe `
  bin\Debug\MICoreUnitTests.dll `
  bin\Debug\JDbgUnitTests.dll `
  bin\Debug\SSHDebugTests.dll `
  bin\Debug\MIDebugEngineUnitTests.dll
```

## Running a single test

```powershell
vstest.console.exe bin\Debug\MICoreUnitTests.dll /Tests:Namespace.ClassName.MethodName
```

`/Tests:` does substring matching, so you can pass just the method name if it is unique.

For the projects that are also in the Unix solution, `dotnet test` works too:

```powershell
dotnet test src\MICoreUnitTests\MICoreUnitTests.csproj --filter "FullyQualifiedName~Namespace.ClassName.MethodName"
```
