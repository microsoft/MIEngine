# DebugEngineHost (Visual Studio implementation)

This project is the **Visual Studio** implementation of
`Microsoft.DebugEngineHost.dll`. It is one of two real implementations
of the host contract defined in
[`src/DebugEngineHost.Stub`](../DebugEngineHost.Stub/README.md); the
other is [`src/DebugEngineHost.VSCode`](../DebugEngineHost.VSCode/README.md).

Behavior here is backed by `Microsoft.VisualStudio.*` APIs (settings
store, output window, COM marshaling, telemetry, wait dialog, …) and is
loaded when MIDebugEngine runs inside Visual Studio.

If you are adding or changing a host API, you must update all three
projects (`DebugEngineHost.Stub`, `DebugEngineHost`,
`DebugEngineHost.VSCode`) so their public surfaces stay identical. See
[`docs/DebugEngineHost-for-AI.md`](../../docs/DebugEngineHost-for-AI.md)
for the full rules and rationale.
