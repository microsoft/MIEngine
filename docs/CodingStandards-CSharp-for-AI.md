# C# coding conventions for MIEngine

The `.editorconfig` at the repo root is authoritative — these are the conventions an AI assistant is most likely to get wrong if it isn't reminded.

## File header

Every `.cs` file starts with the MIT header (enforced by `dotnet_diagnostic` via `file_header_template` in `.editorconfig`):

```csharp
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
```

## Naming

Enforced as **warnings** by `.editorconfig`:

| Symbol | Convention | Example |
| --- | --- | --- |
| `static` private/internal/private_protected fields | `s_camelCase` | `private static readonly object s_lock` |
| Other private/internal fields | `_camelCase` | `private readonly Debugger _debugger` |

`readonly` is preferred where possible (`dotnet_style_readonly_field = true:warning`).

## Usings

`dotnet_sort_system_directives_first = false` — `using` directives are **not** auto-sorted. Leave the existing order alone; do not run an "organize usings" pass on otherwise unrelated files.

## Indentation

- 4 spaces for `.cs` (`indent_size = 4`).
- CRLF line endings (`end_of_line = crlf`) on Windows-tracked files.
- 2 spaces for XML formats (`.csproj`, `.props`, `.targets`, `.resx`, `.natvis`, `.vsct`, `.xsd`).

## Engine-specific patterns

These aren't in `.editorconfig` but show up everywhere — follow the existing call sites:

- **MI commands go through `MICommandFactory`.** Don't build raw MI strings in MIDebugEngine. Add a method to the factory and override per debugger flavor when behavior diverges (`GdbMICommandFactory`, `LldbMICommandFactory`, …).
- **Host calls go through `DebugEngineHost`.** MIDebugEngine never references `Microsoft.VisualStudio.*` directly; use `HostLogger`, `HostMarshal`, `HostOutputWindow`, etc.
- **AD7 surface lives on `AD7*` partial classes.** Keep VS-SDK COM concerns out of the core `Debugged*` classes.
- **Worker thread discipline.** AD7 callbacks must not block. Use `Task.Run` for work and post results back via the engine's `WorkerThread` / `EngineCallback`. Mirror existing call sites; do not invent new threading patterns.
