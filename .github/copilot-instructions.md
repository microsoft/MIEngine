# Conventions

- Apply the code-formatting style defined in `.editorconfig`.
- This repo builds a debugger. Any `debugger_*` tool call will generally be **debugging the debugger** — `debugger_launch` will likely start another instance of Visual Studio (or a different debugger host process) where the model can drive the code in this repo.

# Documentation to read

⚠️ **CRITICAL**: Before proceeding with any task listed below, you MUST read the linked files as your FIRST action. Do NOT attempt the task until you have read and understood the documentation.

**Mandatory workflow for AI:**
1. ✅ FIRST: read all required documentation files for the task
2. ✅ SECOND: follow the workflow specified there
3. ❌ NEVER: skip to code analysis, edits, or hypothesis formation before reading the docs

| Task | Required documentation files |
| --- | --- |
| Understanding the project layout, how the layers fit together, or where new code belongs | [Architecture-for-AI.md](../docs/Architecture-for-AI.md) (and the project wiki: [Architecture-of-the-MIEngine](https://github.com/microsoft/MIEngine/wiki/Architecture-of-the-MIEngine), [Architecture-of-OpenDebugAD7](https://github.com/microsoft/MIEngine/wiki/Architecture-of-OpenDebugAD7), [Architecture-of-DebugAdapterRunner](https://github.com/microsoft/MIEngine/wiki/Architecture-of-DebugAdapterRunner)) |
| Writing or reviewing C# in this repo | [CodingStandards-CSharp-for-AI.md](../docs/CodingStandards-CSharp-for-AI.md) |
| Adding a new `DebugEngineHost` (host) API, or trying to understand what a `Host*` API actually does | [DebugEngineHost-for-AI.md](../docs/DebugEngineHost-for-AI.md) — `src/DebugEngineHost.Stub` is a **contract assembly only**; the real behavior lives in `src/DebugEngineHost` (VS) and `src/DebugEngineHost.VSCode` (VS Code), and a new API must be added to **all three** projects. |
| Building MIEngine **outside of Visual Studio** (CLI / CI / VS Code scenarios) | [Building-outside-of-VS-for-AI.md](../docs/Building-outside-of-VS-for-AI.md) |
| Running the in-process unit tests (`MICoreUnitTests`, `MIDebugEngineUnitTests`, `JDbgUnitTests`, `SSHDebugTests`) **outside of Visual Studio** | [RunningUnitTests-outside-of-VS-for-AI.md](../docs/RunningUnitTests-outside-of-VS-for-AI.md) |
| Running the end-to-end DAP tests in `test/CppTests` (against real `gdb` / `lldb-mi`) **outside of Visual Studio** | [RunningCppTests-outside-of-VS-for-AI.md](../docs/RunningCppTests-outside-of-VS-for-AI.md) |
| Capturing the MI traffic between MIEngine and `gdb`/`lldb` while reproducing a customer issue | wiki: [Logging](https://github.com/microsoft/MIEngine/wiki/Logging) (use the `Debug.MIDebugLog` Command Window verb in VS, or `src/MICore/SetMIDebugLogging.cmd on` for older flows) |

⚠️ **When working inside Visual Studio**, do **not** follow the "outside of Visual Studio" docs above. VS already provides dedicated commands for these tasks. The `*-outside-of-VS-for-AI.md` docs are for command-line, CI, and VS Code scenarios where the IDE isn't hosting the conversation.
