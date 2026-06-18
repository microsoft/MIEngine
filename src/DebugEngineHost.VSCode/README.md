# DebugEngineHost.VSCode (VS Code / OpenDebugAD7 implementation)

This project is the **VS Code** implementation of
`Microsoft.DebugEngineHost.dll`. It is one of two real implementations
of the host contract defined in
[`src/DebugEngineHost.Stub`](../DebugEngineHost.Stub/README.md); the
other is [`src/DebugEngineHost`](../DebugEngineHost/README.md) (the
Visual Studio implementation).

Behavior here has **no `Microsoft.VisualStudio.*` dependencies** — it is
pure .NET so the engine can be hosted by `OpenDebugAD7` (the `cppdbg`
debug adapter) on Windows, Linux, and macOS.

If you are adding or changing a host API, you must update all three
projects (`DebugEngineHost.Stub`, `DebugEngineHost`,
`DebugEngineHost.VSCode`) so their public surfaces stay identical. See
[`docs/DebugEngineHost-for-AI.md`](../../docs/DebugEngineHost-for-AI.md)
for the full rules and rationale.
