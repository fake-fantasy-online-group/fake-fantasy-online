# Elite v6: approved world visuals and verification

Status: implemented on the development feature branch. Core managed compilation and the complete managed UI contract suite have passed. V6 activation remains a separate integration decision. No resource binaries are added to Git.

## Approved native artwork

| Skill | Original resources | Presentation timing |
| --- | --- | --- |
| Golden body B | Current native actor, `magic0201` rune, `magic0080` shield | Only while the server's golden-body deadline is active. Separate rune and shield distinguish it from persistent elite gold identity. |
| Clones | Each server-spawned clone's own native actor model | Cool luminance tint and 62% alpha. The client never creates combat clones. |
| Lava C v3 | `magic4149`, `magic0420` | 4149's 38 frames at 55 ms while awaiting resolution; confirmed impact plays 0420's ten frames at 70 ms. Three-second warning is server-owned. Core visible diameter is at most 30% of blast diameter. |
| Lightning orb | `magic0216`, `magic4040` | Native orb, ten-second server lifetime. Annulus follows the supplied first pulse, pulse interval, propagation duration, radius and width. |
| Ice v3 | `magic0147`, `magic0043`, `magic0230` | Fixed first 0147 frame pulses from 0.46 to 0.80 native scale every second during the three-second warning. Confirmed impact: all nine 0147 frames at 60 ms, all eight 0043 frames at 60 ms after 180 ms, and the original 0230 ring for the first 480 ms. No animation rotation or mirroring. |
| Agility | `magic0085` and current native actor frames | Native five-frame foot ring and short, world-positioned movement afterimages. Speed stays server-owned. |
| Poison pool | `magic4145`, `magic4147` | Green, ground-projected native textures within the exact server circle; twenty-second server lifetime. |
| Ice walls | `object/SK/SK_CLY_0501.gso`, `0502`, `0503` | Three native static objects with source-textured rectangular bases at the exact server endpoints and widths; fifteen-second server lifetime. |

Magic paths use the existing `magic/magic/magicNNNN.gso` package resolver. The native-pixel fixture records all fifteen source hashes, frame counts, alpha bounds and pivot offsets.

## Geometry and lifecycle

- All new visuals are children of the actual map world root. No new viewport overlay, combat panel, labels or chat messages are added.
- Readiness requires the exact run, floor, context epoch and native root, including same-map-ID floor changes. Scene change, disconnect, authoritative removal and deadline expiry retire owned visuals.
- Explosion art requires an explicit server impact receipt. A disappearing warning or local timeout never creates an explosion. A late receipt starts at its current server-relative frame and skips elapsed frames.
- The lava peak uses the measured 150-pixel visible width; the core uses 256 pixels. Peak alpha center is corrected independently of the texture rectangle and native pivot. Camera-facing art uses the horizontal projection of the actual map circle, including rotated/isometric cameras. Ground circles and annuli stay on the real world plane.
- The electric mesh has a transparent inner safe region and exact instantaneous shell width. Server swept collision between ticks remains authoritative.
- Native additive effect and alpha-object materials are reused. Tint and mesh texture materials are private copies; there are no shared/global material edits. Decoded textures, sprites, meshes and private materials are disposed with their owner, including when a native root was destroyed first.

## Checks completed in the cloud

- Existing endgame source/metadata contracts: 213 checks passed.
- Elite source, native-pixel and independent numerical contracts: 215 checks passed.
- All fifteen native source hashes, one-direction layouts, frame counts and maximum visible-alpha widths matched the frozen fixture.
- Independent projection calculations covered radii 120/140/220, zoom factors 0.5/1/2 and four orthographic/rotated/isometric projections. The largest 128-sample circle-span error was 0.1076 pixels. Native peak-width scaling and pivot-center cancellation matched the same authoritative diameter.
- Shell script syntax and `git diff --check` passed.

These are source checks and offline native-pixel/numerical measurements. They are not Unity screenshots, gameplay footage, actual GPU rasterization measurements or execution of the C# selector.

## Actual managed compilation and execution

The official .NET 6.0.36 runtime and Roslyn 4.8 compiler executed the existing test entry points against verified Unity 4.8 API reference assemblies. The compatibility launcher is explicitly a .NET command adapter, not a Mono runtime.

- `run-managed-compile.sh`: Foundation (5 files), Domain (1), Application (157), Protocol (222), PluginApi (10), MoonSharp (248), and PluginHost (11) all compiled successfully.
- `run-ui-contract.sh`: 2,545 assertions passed in the complete existing UI suite, including the actual new renderer, asset owner, actor visual component and pure timing/geometry selector. This includes native frame boundaries, late impact receipts, no guessed detonation, repeated updates, annulus vertices, cloned tint ownership, material restoration and disposal.
- `run-rift-monster-visual-contract.sh`: 11 production name/glow lifecycle assertions passed.
- `Documentation/tools/compile-current-presentation.py`: all 1,154 current Presentation source files compiled against the hash-verified real Unity 6000.4.9f1 reference assemblies. Existing obsolete/unused warnings remain; no Unity engine was launched.
- One older test used culture-sensitive string searches for the native U+0006 color-control byte. An actual C# diagnostic demonstrated the .NET/ICU difference; those searches now use ordinal comparison while retaining the original assertions.

The UI execution uses deterministic Unity/artwork substitutes. It executes production C# logic, but does not execute a Unity engine or GPU renderer.

## Remaining validation gates

No Unity Editor, Player build, live scene, GPU rendering or in-game visual acceptance has been performed. Native alpha/blend, camera appearance, wall orientation, world occlusion and reconnect appearance still require Unity acceptance. The offline numerical results and managed contracts do not substitute for that rendering validation.

## Reproduce

Source checks without resource packages:

```sh
python3 Tests/Endgame/source_contract.py
python3 Tests/Endgame/elite_visual_source_contract.py
bash -n Tests/Endgame/run-ui-contract.sh
```

For original-pixel verification, pass `--asset-root` pointing to the native resource tree and `--decoder` pointing to the independent native decoder used for offline source inspection. Resource packages are deliberately not stored in this repository.

For actual managed validation, source `Documentation/tools/activate-dotnet-test-toolchain.sh` and run the existing `Tests/PluginRuntime/run-managed-compile.sh`, `Tests/Endgame/run-ui-contract.sh`, `Tests/Endgame/run-rift-monster-visual-contract.sh`, and authoritative hazard-contract entry point. Unity world-layer, alpha/blend, camera, wall-orientation, map-transition and reconnect acceptance is a separate gate.
