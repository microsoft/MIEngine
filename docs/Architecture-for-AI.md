# MIEngine architecture for AI

MIEngine is a Visual Studio **Debug Engine** that drives debuggers speaking the GDB **Machine Interface** ("MI") protocol — primarily GDB and LLDB-MI. The same code powers the `cppdbg` debug adapter for the VS Code C/C++ extension via `OpenDebugAD7`.

## Layered design

The engine is layered. New code almost always belongs in one of these projects; before adding a class, decide which layer owns the concern.

| Layer | Project(s) | Responsibility |
| --- | --- | --- |
| Transport + MI parsing | `src/MICore` | Connections to the debuggee's debugger over local pty, SSH, pipe, serial. Owns the `MICommandFactory` family (`GdbMICommandFactory`, `LldbMICommandFactory`, `ClrdbgMICommandFactory`). All flavor-specific MI quirks live here as factory overrides. |
| AD7 implementation | `src/MIDebugEngine` | Implements the VS Core Debug Interfaces (`IDebugEngine2`, `IDebug*2`). `DebuggedProcess` is the central object owning the `MICore.Debugger`, breakpoint/threads/modules managers, and the event pump. AD7 wrapper classes (`AD7Engine`, `AD7Thread`, `AD7StackFrame`, `AD7BoundBreakpoint`, …) translate VS SDK COM calls into engine operations. |
| DAP shim | `src/OpenDebugAD7` | Hosts MIDebugEngine in-proc and exposes it as a Debug Adapter Protocol server. The entry point for the `cppdbg` adapter consumed by the VS Code C/C++ extension. |
| Host abstraction | `src/DebugEngineHost`, `src/DebugEngineHost.Common`, `src/DebugEngineHost.Stub`, `src/DebugEngineHost.VSCode` | The shim that lets MIDebugEngine avoid calling VS APIs directly. Goes through `HostLogger`, `HostMarshal`, `HostOutputWindow`, etc. The `DebugEngineHost.VSCode` variant is used by OpenDebugAD7; the in-VS build uses the COM-based host (`src/DebugEngineHost`). |
| Launchers | `src/AndroidDebugLauncher`, `src/IOSDebugLauncher`, `src/WindowsDebugLauncher` | Out-of-proc helpers spawned by the engine to start a debug session in a special environment (Android emulator, iOS device, Windows console). They communicate over stdio and implement `IPlatformAppLauncher`. |
| SSH port supplier | `src/SSHDebugPS` | A standalone VS "Port Supplier" for picking processes over SSH or Linux Docker. Independent of the engine flow. |

## Hard rules

- **No raw MI strings outside `MICore.MICommandFactory` / `Debugger.CmdAsync`.** If MIDebugEngine needs a new MI command, add a method on the factory and override per debugger flavor when behavior diverges.
- **MIDebugEngine does not depend on `Microsoft.VisualStudio.*` types directly.** Route everything through `DebugEngineHost`. This is what allows OpenDebugAD7 to host the engine on non-VS platforms.
- **Don't add `MIDebugEngine.sln`-only projects to `MIDebugEngine-Unix.sln`** (and vice versa). The split is intentional — the Unix solution must remain SDK-buildable without `msbuild`, the Windows extension workload, or COM PIAs.
- **AD7 surface goes on the `AD7*` partial class** for that interface. Keep VS-SDK COM concerns out of the core `Debugged*` classes (`DebuggedProcess`, `DebuggedThread`, `DebuggedModule`).

## Inside MICore

`MICore` itself decomposes into five concerns. When adding code there, place it in the right one rather than growing `Debugger` further:

1. **`Debugger`** — central pump that processes text from GDB/LLDB and dispatches it to consumers.
2. **`MICommandFactory`** + flavor overrides (`Gdb`, `Lldb`, `Clrdbg`) — the abstraction for "send command X". *All* MI string construction goes through here.
3. **Result parser** (`ResultValue`, etc.) — parses MI result records into typed objects.
4. **Transports** — local pty, SSH, named-pipe, serial; set up the stdin/stdout connection to the debugger.
5. **Launch options** — XML deserialization driven by `LaunchOptions.xsd` (codegenned by `tools/LaunchOptionsGen` into `LaunchOptions.cs`); also loads custom launchers.

The only enforced layering rule between MICore and MIDebugEngine is that **launchers depend on `MICore` only**. Any type a launcher needs must therefore live in MICore, not in MIDebugEngine.

## Async / threading

MIDebugEngine uses TPL `Task`s but AD7 callbacks must not block the dispatcher thread. The pattern is `Task.Run` for the work + post results back via the engine's `WorkerThread` / `EngineCallback`. Mirror existing call sites rather than inventing new threading; introducing a new pattern almost always causes deadlocks against `DebuggedProcess.WorkerThread`.

## LaunchOptions

The XML payload that `launch.json` (VS Code) or `vsdbg`/the IDE (VS) sends to the engine is described by `src/MICore/LaunchOptions.xsd`. The C# binding classes are **generated** by `tools/LaunchOptionsGen` — regenerate (`LaunchOptionsGen <xsd>`) rather than hand-editing the generated `LaunchOptions.cs`.

## Entry points worth knowing

- VS extension entry: `MIDebugPackage` registers MIDebugEngine and pulls in `MIDebugEngine.dll`.
- VS Code adapter entry: `src/OpenDebugAD7/Program.cs` — main loop reads DAP messages from stdin and dispatches to `AD7DebugSession`.
- Engine creation: `AD7Engine.LaunchSuspended` / `AD7Engine.Attach` — both end at constructing a `DebuggedProcess`.

## Tests at a glance

- Pure managed unit tests live in `MICoreUnitTests`, `MIDebugEngineUnitTests`, `JDbgUnitTests`, `SSHDebugTests`.
- End-to-end DAP tests against a real debugger live in `test/CppTests` and use the `DebugAdapterRunner` framework.

See [RunningUnitTests-outside-of-VS-for-AI.md](RunningUnitTests-outside-of-VS-for-AI.md) and [RunningCppTests-outside-of-VS-for-AI.md](RunningCppTests-outside-of-VS-for-AI.md).
