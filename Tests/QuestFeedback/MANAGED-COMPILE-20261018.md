# Cloud managed verification, 2026-10-18

Source revision: `5543ac0` (production changes through `8a4d707`, quest-order test `76d1ac4`, renderer fixture `5543ac0`). No production code changed during verification.

## Verified toolchain

- Official Microsoft Roslyn 4.8.0, compiler `4.8.0-7.23558.1 (e0917286)`; NuGet SHA256 `37333f4f1e2ce55e621355d6da651dc23d4cb5f94a8f76b9478816e87f110ad9`.
- Official Microsoft .NET runtime 6.0.36, SHA512 verified by the repository's pinned bootstrap script.
- Recovered `batch18-managed-toolchain-transfer.tar.gz`, SHA256 `b83e452e5ae3d8671a65981fd58f783328a47713bbf1d9eb4a09d2d48587a91d`; all 427 manifest payloads verified by size and SHA256.
- Unity 6000.4.9f1 archived real references, compiler switches, defines and generators; current source enumeration, not archived source replay.

## Commands and outcomes

After restoring the archive to `FFO_TOOLCHAINS_ROOT/prior-backups/batch18-managed-toolchain` and the official runtime/compiler to the paths documented in `Documentation/tools/DOTNET-TEST-PROFILE.md`:

```sh
source Documentation/tools/activate-dotnet-test-toolchain.sh
bash Documentation/tools/verify-dotnet-test-toolchain.sh
bash Tests/PluginRuntime/run-managed-compile.sh
python3 Documentation/tools/compile-current-presentation.py
python3 Tests/Mobile/compile-platform-boundaries.py
bash Tests/QuestFeedback/run-task-track-render-contract.sh
```

- Toolchain smoke tests: PASS actual C#9 compile/run and negative controls (invalid source and runtime exit7 remain failures).
- Core: PASS Foundation9, Domain1, Application157, Protocol223, PluginApi10 sources (400 total), no diagnostics.
- Presentation: PASS all1199 current sources, 26 warnings (obsolete APIs, possible reference comparisons, unused fields), zero errors.
- Bootstrap: PASS all5 current sources.
- Default Editor boundary script: FAIL CS0103 at `MobileHudPreview.cs:176`, `ScreenCapture` unavailable because the script reuses Presentation references and omits the ScreenCapture module.
- Supplemental Editor compile: PASS all3 current Editor sources, 9 obsolete-API warnings, when adding the already archived and hash-verified real `UnityEngine.ScreenCaptureModule.dll`:

```sh
"$FFO_DOTNET" exec "$FFO_DOTNET_CSC" /nostdlib /noconfig \
 @"${FFO_TOOLCHAINS_ROOT:-/workspace/shared/toolchains}/client/mobile-boundary-compile/Editor.rsp" \
 -r:"${FFO_TOOLCHAINS_ROOT:-/workspace/shared/toolchains}/prior-backups/batch18-managed-toolchain/references/Unity-6000.4.9f1/Resources/Scripting/Managed/UnityEngine/UnityEngine.ScreenCaptureModule.dll"
```

- Actual task-track renderer sources: PASS31 assertions under explicit Unity/font doubles on both .NET6 archived-reference profile and Mono6.12 / Mono4.7.2-api compatibility profile.

All eight present project assemblies compiled with real references once the Editor reference omission was supplied (1607 source files total). This does not mean the unmodified boundary script passed; its default Editor failure is retained above. Optional plugin framework/AI packages are absent and not compiled. No Unity Editor import, Unity execution, scene/render validation, or Player/APK build occurred.

## Evidence paths in the cloud workspace

`/workspace/shared/toolchains/client/dotnet-managed-compile/*.log`

`/workspace/shared/toolchains/client/dotnet-presentation-compile/compile.log`

`/workspace/shared/toolchains/client/dotnet-presentation-compile/source-and-profile-manifest.json`

`/workspace/shared/toolchains/client/mobile-boundary-compile/Bootstrap.log`

`/workspace/shared/toolchains/client/mobile-boundary-compile/Editor.log` (default failure)

`/workspace/shared/toolchains/client/mobile-boundary-compile/Editor-with-screen-capture.log` (supplemental pass)

`/workspace/shared/toolchains/client/dotnet-traces/` (managed adapter command/exit records)
