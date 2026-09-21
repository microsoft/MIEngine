# `Microsoft.DebugEngineHost` — contract assembly + two implementations

`Microsoft.DebugEngineHost` is the shim that lets `MIDebugEngine` (and the
launchers) avoid calling `Microsoft.VisualStudio.*` directly. It is what
allows the same engine to run inside Visual Studio *and* inside the VS Code
`cppdbg` debug adapter (OpenDebugAD7).

⚠️ The host is split across **four** projects on disk. Knowing which is
which is critical before you read or change anything host-related.

| Project | What it is | When you build it |
| --- | --- | --- |
| `src/DebugEngineHost.Stub` | **Contract / reference assembly.** Defines the public shape of `Microsoft.DebugEngineHost` (types, method signatures, XML docs). Method bodies are stubs (`throw new NotImplementedException()`, `return null`, etc.) — **this assembly is never loaded at runtime by the engine.** Built only as a reference assembly so consumers can compile against the surface. | Always (part of `MIDebugEngine.sln`). |
| `src/DebugEngineHost` | **Visual Studio implementation.** The real `Microsoft.DebugEngineHost.dll` used when MIDebugEngine runs inside VS. Backed by `Microsoft.VisualStudio.*` (settings store, output window, COM marshaling, telemetry, wait dialog, …). | `MIDebugEngine.sln` only (Windows, VS Dev Cmd Prompt). |
| `src/DebugEngineHost.VSCode` | **VS Code / OpenDebugAD7 implementation.** The real `Microsoft.DebugEngineHost.dll` used when MIDebugEngine is hosted by `OpenDebugAD7` (the `cppdbg` DAP adapter). No VS / COM dependencies — pure .NET. | Both solutions (`MIDebugEngine.sln` and `MIDebugEngine-Unix.sln`). Output lands in `bin/<Configuration>/vscode/`. |
| `src/DebugEngineHost.Common` | Shared source files (e.g. `HostLogChannel.cs`) that are linked into the two real implementations so they don't drift. Not an assembly the engine references on its own. | Linked into both implementations. |

All three of `DebugEngineHost.Stub`, `DebugEngineHost`, and `DebugEngineHost.VSCode` emit an assembly
named **`Microsoft.DebugEngineHost.dll`** with the same `AssemblyVersion`
(`1.0.0`). That is intentional — the engine binds to one name and the
build wires up whichever real implementation matches the host.

## Rules for AI working on host APIs

1. **`DebugEngineHost.Stub` is a contract, not behavior.** If you want to
   know what a host API actually *does*, do **not** read `.Stub`. Open the
   matching file in `src/DebugEngineHost` (VS behavior) and/or
   `src/DebugEngineHost.VSCode` (VS Code behavior). The `.Stub` body is
   meaningless — it exists only so other projects can compile.

2. **Adding a new host API means editing all three projects.** A new
   method or type on a `Host*` class must be added to:
   - `src/DebugEngineHost.Stub/DebugEngineHost.ref.cs` (or the right `Shared` file) — declare the signature with XML docs, stub the body.
   - `src/DebugEngineHost/<HostXxx>.cs` — implement it against the VS APIs.
   - `src/DebugEngineHost.VSCode/<HostXxx>.cs` — implement it against the VS Code / OpenDebugAD7 environment.
   The three surfaces must stay **identical** (same namespace, type, name,
   parameters, return type, generic arity, accessibility). If they drift,
   one of the two hosts will fail to bind at runtime with a
   `MissingMethodException` / `TypeLoadException`.

3. **Shared helpers go in `DebugEngineHost.Common`.** If the two real
   implementations would copy/paste the same code, put it in `.Common` and
   link it into both — do **not** add it to `.Stub` (`.Stub` has no real
   code).

4. **No `Microsoft.VisualStudio.*` references from `.VSCode` or
   `.Stub`'s public surface.** The whole point of the split is that
   OpenDebugAD7 / VS Code can load the engine without any VS assemblies.
   `.Stub` may reference `Microsoft.VisualStudio.Debugger.Interop` because
   that is part of the contract (e.g. `HostMarshal` deals in `IDebug*`
   interfaces), but the `.VSCode` implementation must provide its own
   substitute behavior, not pull in VS.

5. **MIDebugEngine itself only references the contract.** It compiles
   against `.Stub`, then at runtime loads whichever real
   `Microsoft.DebugEngineHost.dll` is next to it. This is why you can't
   "just call a VS API" from MIDebugEngine — there is no VS API on the
   `.Stub` surface to call.

## Quick map of the `Host*` types

Every `Host*` file has three copies (one in each project). Common ones:

- `Host` — top-level host identity (`GetHostUIIdentifier()`).
- `HostConfigurationStore` — engine/launcher configuration lookup. VS reads from the settings store; VS Code reads from launch.json / registered options.
- `HostLogger` / `HostLogChannel` — logging plumbing (`Debug.MIDebugLog` in VS; file-based in VS Code).
- `HostMarshal` — marshals AD7 COM interface pointers (`IDebugDocumentPosition2`, etc.) across boundaries. Real COM in VS; an in-proc table in VS Code.
- `HostOutputWindow` — writes to the VS Debug Output pane vs. the DAP `output` event.
- `HostRunInTerminal` — launches a child process in an interactive terminal (VS terminal vs. DAP `runInTerminal` request).
- `HostWaitDialog` / `HostWaitLoop` — modal progress UI (VS dialog vs. DAP progress events / no-op).
- `HostTelemetry` — VS telemetry sink vs. a VS Code no-op.
- `HostNatvisProject` — natvis discovery from the loaded VS project vs. from launch.json.
- `HostDebugger` — only present in the VS implementation (it drives the VS debugger itself for nested scenarios); no `.VSCode` counterpart is needed.

When in doubt, grep for the type name across all three project folders
and read **both** real implementations — they're the source of truth.
