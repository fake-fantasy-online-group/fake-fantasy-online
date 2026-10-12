# MoonSharp 2.0.0 source dependency

Official upstream: https://github.com/moonsharp-devs/moonsharp
Tag: `v2.0.0.0`
Commit: `3154416e535ab96c0d52bf12e3e472985a1532f4`
License: BSD 3-clause (see LICENSE.txt; retain in source and binary distributions).

Only C# interpreter source from `src/MoonSharp.Interpreter` is vendored. Excluded: alternate `_Projects` duplicate source, assembly metadata, project/build files, tests, debugger servers, binaries, icons and other assets. The Unity assembly definition is project-owned. Interpreter source has three narrowly scoped project patches, recorded in PATCHES.diff: ScriptOptions adds optional MaximumStringLength/MaximumTableEntries (default zero preserves upstream behavior); the VM rejects concatenation over the configured bound before allocation; Table rejects additional keys over the configured per-table limit. No other upstream source files are modified.

The addon host uses explicit CoreModules with no IO, OS, loader, reflection, debug, metatables, error handlers or nested coroutine library. Entry and callbacks run as host-created coroutines with AutoYieldCounter; every frame and every callback have limits. Native helpers which run Lua callbacks (`table.sort`, string pattern helpers) are unavailable. See [the addon guide](../../../Documentation/Plugins/ADDONS.md) for capabilities and sandbox limits. MoonSharp is an in-process interpreter, not an adversarial memory-isolation boundary.

Official coroutine documentation: https://www.moonsharp.org/coroutines.html
Official sandbox guidance: https://www.moonsharp.org/sandbox.html
