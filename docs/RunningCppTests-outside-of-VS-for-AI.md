# Running CppTests (end-to-end DAP tests) outside of Visual Studio

⚠️ **Do not use these instructions when working inside Visual Studio.** Use VS **Test Explorer** (or the `debugger_run_tests` tool if available) — it sets up the working directory and lets you attach the debugger to both the test and the spawned `OpenDebugAD7`. The command-line flow below is for CI / VS Code / headless scenarios.

`test/CppTests` drives the built `OpenDebugAD7` adapter against a real `gdb` (or `lldb-mi`) using the `DebugAdapterRunner` framework. Every test has an `[xunit.Theory]` parameterized by an `ITestSettings` discovered at runtime from `config.xml` — see [Filtering CppTests](#filtering-cpptests) below for an important quirk.

## Prerequisites

1. A VSCode-flavor build (see [Building-outside-of-VS-for-AI.md](Building-outside-of-VS-for-AI.md)):

   ```powershell
   eng\Scripts\CI-Build.ps1 -Configuration Debug -TargetPlatform vscode    # Windows
   ```

   ```bash
   eng/Scripts/CI-Build.sh                                                  # Linux / macOS
   ```

2. A native toolchain for the test debuggees:
   - **Windows:** **MSYS2 + MinGW64** (`mingw-w64-x86_64-toolchain`, providing `g++` and `gdb`). **Cygwin is not supported.** Install with `winget install --id MSYS2.MSYS2 --silent` and then, in `C:\msys64\usr\bin\bash.exe -lc "..."`:

     ```bash
     pacman -Syu --noconfirm
     pacman -S --needed --noconfirm mingw-w64-x86_64-toolchain
     ```

     Put `C:\msys64\mingw64\bin;C:\msys64\usr\bin` on `PATH`, or run `dotnet test` from `msys2_shell.cmd -mingw64`.
   - **Linux:** `gdb`, `g++`, plus `ptrace_scope=0` and a writable `core_pattern` if any test exercises core dumps:

     ```bash
     sudo apt-get install -y gdb g++
     echo 0    | sudo tee /proc/sys/kernel/yama/ptrace_scope
     echo core | sudo tee /proc/sys/kernel/core_pattern
     ```

   - **macOS:** `lldb-mi` is downloaded by `tools/DownloadLldbMI.sh`; CI does this automatically inside `eng/Scripts/CI-Test.sh`.

3. A `config.xml` next to `CppTests.dll`. Pick the right template from `bin/DebugAdapterProtocolTests/<Configuration>/CppTests/TestConfigurations/` and copy it as `config.xml`:

   | Platform / debugger | Template |
   | --- | --- |
   | Windows + MSYS2 GDB | `config_msys_gdb.xml` |
   | Linux + GDB | `config_gdb.xml` |
   | macOS + LLDB-MI | `config_lldb.xml` |
   | Windows + VsDbg | `config_vsdbg.xml` |

   ⚠️ The shipped `config_msys_gdb.xml` has hardcoded GitHub Actions paths (`D:\a\_temp\msys64\mingw64\bin\…`). Edit it to match your local install (e.g. `C:\msys64\mingw64\bin\g++.exe`, `C:\msys64\mingw64\bin\gdb.exe`) before running.

`eng/Scripts/CI-Test.sh` performs steps 2-3 automatically on Linux/macOS.

## Running

```powershell
cd bin\DebugAdapterProtocolTests\Debug\CppTests
dotnet test CppTests.dll --logger "trx;LogFileName=results.trx"
```

```bash
cd bin/DebugAdapterProtocolTests/Debug/CppTests
dotnet test CppTests.dll --logger "trx;LogFileName=results.trx"
```

## Filtering CppTests

Standard VSTest filters work:

```powershell
dotnet test CppTests.dll --filter "FullyQualifiedName~SampleTests.TestArguments"
```

**Quirk to know about — dependency ordering vs. filtering.** Many CppTests use `[DependsOnTest("OtherTest")]` (e.g. a debuggee-compile step), and the type is decorated with `[TestCaseOrderer(DependencyTestOrderer.TypeName, DependencyTestOrderer.AssemblyName)]`. The orderer (`test/DebuggerTesting/Ordering/DependencyOrderer.cs`) runs **after** VSTest's `--filter`. Historically it removed any test whose dependency wasn't in the filtered set, so a single-test filter would silently produce *"No test matches the given testcase filter"*. The orderer was updated to instead ignore missing dependencies and run the test anyway, logging a warning through xUnit's diagnostic message sink (`IMessageSink` / `DiagnosticMessage`). The warning is only surfaced when xUnit diagnostic messages are enabled — see [Seeing xUnit diagnostic messages](#seeing-xunit-diagnostic-messages) below. When adding new dependency-ordered tests:

- Don't rely on the orderer to *skip* a test when its dependency is filtered out — it will run, and may fail at runtime if the predecessor was genuinely required.
- If a predecessor produces a build artifact (e.g. a compiled debuggee), ensure the dependent test can find or rebuild that artifact when run in isolation.

## Seeing xUnit diagnostic messages

Diagnostics emitted via xUnit's `IMessageSink` (used by `DependencyTestOrderer` and other test infrastructure) are **suppressed by default**. To see them in `dotnet test` console output, in VS Test Explorer's Test output pane, or in TRX logs, drop an `xunit.runner.json` next to the test DLL with diagnostics enabled:

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "diagnosticMessages": true
}
```

For `CppTests`, place it next to `bin\DebugAdapterProtocolTests\<Configuration>\CppTests\CppTests.dll`. With `--logger "console;verbosity=detailed"`, diagnostic lines appear prefixed with `[xUnit.net …]` (e.g. `WARNING: Missing dependency for 'CppTests.Tests.SampleTests.TestArguments'; running anyway.`).

## Test-data XML

`config.xml` defines `<TestConfiguration>` entries (compiler + debugger + architecture). Every `[RequiresTestSettings]` theory is invoked once per matching configuration. To run against multiple debuggers in one pass, add multiple `<TestConfiguration>` entries.
