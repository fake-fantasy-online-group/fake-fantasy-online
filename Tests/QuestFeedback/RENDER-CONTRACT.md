# Task-track source render contract

First compile the engine-free assemblies using `Tests/PluginRuntime/run-managed-compile.sh`, then run `bash Tests/QuestFeedback/run-task-track-render-contract.sh` with the same `FFO_MONO_ROOT`, `FFO_CSC`, and `FFO_COMPILE_DIR`. The core runner supplies `framework.rsp` and `FakeFantasyOnline.Application.dll`; the managed host must also resolve its runtime dependencies.

This compiles the complete current `PlayerTaskTrack.cs`, `ReferenceWorldPlayerTaskTrack.cs`, and shared `TaskPromptLayout.cs` against the actual compiled Application assembly. Explicit Unity camera, screen, time, and font doubles record drawing calls. Thirty-one assertions cover wrapping, horizontal/top bounds, preserved text, expiration, invalid inputs, authoritative snapshot progress, and signature cleanup.

This is source-level behavior verification under test doubles, not real Unity API compatibility, rendered screenshots, an Editor import, or a Player build.
