# DebugEngineHost.Stub

⚠️ **This project is a contract / reference assembly. It is never loaded
at runtime.** The method bodies here are stubs — they do not describe
what the host actually does.

If you want to know what a `Host*` API actually does, read the **real**
implementations instead:

- `src/DebugEngineHost` — Visual Studio implementation.
- `src/DebugEngineHost.VSCode` — VS Code / OpenDebugAD7 implementation.

If you are adding or changing a host API, you must update all three
projects (`DebugEngineHost.Stub`, `DebugEngineHost`,
`DebugEngineHost.VSCode`) so their public surfaces stay identical. See
[`docs/DebugEngineHost-for-AI.md`](../../docs/DebugEngineHost-for-AI.md)
for the full rules and rationale.
