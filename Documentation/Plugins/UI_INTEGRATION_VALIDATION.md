# UI and plugin integration validation

2026-10-12, cloud-only isolated integration, based on published `cfb7f22`.

## Scope

Preserves the initial artwork UI and Lua/external-agent runtime. Adds native title/login/server/character selection, restores the original pregame UI, and exposes ordinary native party commands. Bounded JSON utilities are shared through Foundation. Skill icon changes only cover obsolete baked pictures and draw the current icon using the existing `currentLevel > 0` learned check. Tooltip rows retain their authored centered alignment using the existing UITip protocol.

No new gameplay modules, skill definitions, balance values, equipment variants, gameplay workbenches, binary artwork, scenes, prefabs, resources, or third-party dependencies are included by these changes. Application, Domain and Protocol source trees remain identical to `cfb7f22`.

## Passed

- Full source compilation of all seven engine-free assemblies: Foundation 5, Domain 1, Application 137, Protocol 219, PluginApi 10, MoonSharp 248, PluginHost 11; **631 actual source files**.
- Shared game contracts: 723 assertions.
- Lua addon runtime: 58 checks using the actual interpreter.
- Addon UI lifetime/state: 39 assertions; bootstrap/source integration: 13.
- External agent transport: 100 assertions and Python client/CLI integration.
- Game action adapter: 165 assertions total, including 59 title adapter and 31 party projection assertions. Native substitutes are used; no gameplay executes.
- Native pregame bridge: 32 execution checks plus source invariants.
- Original pregame equivalence: 42 checks over 22 files.
- Inventory/social UI source and geometry: 200 assertions.
- Artwork/input contract: 32 assertions.
- Native scrolling: 26 production-source assertions.
- Skill icon geometry/draw contract: 403 production-method assertions.
- Tooltip composition: 3 production-method assertions (center/color order, empty input, authored whitespace).
- Whitespace diff validation and test shell syntax checks.

## Commands

Set up Mono/Roslyn with `FFO_MONO_ROOT`, `FFO_CSC` and `FFO_FRAMEWORK_DIR`.

Run `Tests/PluginRuntime/run-managed-compile.sh` with `FFO_COMPILE_DIR` outside Assets, then the PluginRuntime shell runners except the aggregate observation runner described below. UI Python runners live in `Tests/UiArtwork`; pass `--mono "$FFO_MONO_ROOT/bin/mono" --csc "$FFO_CSC"` to the native-scroll runner. Run `Tests/UiArtwork/run-artwork-contract.sh` separately.

## Not run / limits

The full compiled-Presentation `run-game-observations.sh` suite is blocked: this cloud environment has no Unity Editor managed references or compiled Presentation assembly. Its updated title/party discovery count and party cache/schema tests are not reported as executed. Focused production title/party projection tests do not replace the full observation-envelope/cache suite.

No Unity Editor/Player build, real rendering, live login, server connection, gameplay, or local-computer operation was performed. Visual acceptance and live flows remain a separate check on the user's authorized Unity environment.
