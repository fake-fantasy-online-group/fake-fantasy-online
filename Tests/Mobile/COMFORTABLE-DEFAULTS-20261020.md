# Comfortable mobile defaults, 2026-10-20

## Scope and reference

Source-only change based on main `0672ddf6cbee81c682c89f1312c29cb7a563297d`.
No APK, Player build, Unity process, deployment, server address, or desktop HUD change.

Reference phone: landscape 2616 x 1212; measured system safe area
`(104, 0, 2512, 1212)`. This matches the reference screenshot; Unity's
`Screen.safeArea` was not sampled directly. The focused test supplies that safe
area to the production conversion and uses UI scale `1212 / 540`.

- Preserve the measured attack size, `1.3344183` times its original 96-unit
  baseline. The resulting touch width is approximately 287.52 physical pixels.
- Keep joystick, mount and meditate defaults unchanged. Reference joystick
  touch width is approximately 359.11 physical pixels.
- Align only Skill0/Skill1, Skill3/Skill5 and the two potions to their respective
  midpoint Y. Maximum displacement is 2.83 physical pixels. Other captured
  centers are reproduced within 0.002 pixels in the geometry contract.
- Keep the existing right/bottom anchoring and UI-scale adaptation. On narrower
  safe areas, uniformly fit only missing/default right-thumb controls beside
  chat. Under the current scale cap, the smallest fit factor is about 0.876741;
  pickup and group-wheel touch targets retain the existing 44-unit minimum.
- Preserve the original size baselines, v1 file format, and every saved custom
  center/scale. Do not seed defaults into the saved file. Reset is an editing
  preview until Save; saved Reset remains `version=1` without override entries.

These are touch rectangles, not artwork diameters. The .65 layout scale bound
alone is not a collision guarantee; compact-fit safety is also protected by
regression tests against the existing UI-scale cap.

## Verification completed in the cloud

The documented .NET 6.0.36 / Roslyn 4.8 profile compiled production C# against
geometry/application substitutes for contract execution. This is not Mono,
Unity rendering, Unity import, an APK build, or an on-device acceptance pass.

- `run-comfortable-layout-contract.sh`: 11,810 assertions.
- `run-layout-contract.sh`: 52,921 assertions, including the existing portrait
  regression cases; no new portrait behavior was introduced.
- `run-control-layout-contract.sh`: 350 checks.
- `IndependentControls/run.py`: 20,068 layout-safety, 6,872 edit-session,
  96 pointer, and 11 frame-gate assertions.
- `run-panel-input-contract.sh`: 24 checks.
- `run-group-wheel-contract.sh`: 241 checks.
- `run-settings-scale-contract.sh`: 3,220 checks.
- Core-only managed compilation: Foundation, Domain, Application, Protocol,
  and PluginApi passed (410 actual source files).
- Full current Presentation compilation: 1,221 actual source files, verified
  recovered Unity reference assemblies and archived compiler configuration.
- ClientBuild static Python suite: 24 tests. Shell syntax and `git diff --check`
  passed. Build test fixtures are not a built or installed application.

The layout suite's existing 200-unit cancel-travel assertion now allows 0.01
logical units of floating-point tolerance; an exact 200-unit result calculated
as 200.0000216 previously failed. No production cancel behavior changed.

## Repeat checks

```sh
source Documentation/tools/activate-dotnet-test-toolchain.sh
bash Tests/Mobile/run-comfortable-layout-contract.sh
bash Tests/Mobile/run-layout-contract.sh
bash Tests/Mobile/run-control-layout-contract.sh
python3 Tests/Mobile/IndependentControls/run.py
```

## Device verification when a future build is authorized

1. Preserve a backup of the current layout. Launch the new client on the same
   phone/orientation with the same UI scale; existing custom positions and
   sizes must remain unchanged.
2. Enter layout editing and preview Reset. Check the lower skill pair, upper
   skill pair and potion pair align; attack and joystick retain their measured
   sizes and centers. Check pickup, skill and group-wheel artwork/hit behavior.
3. Cancel Reset and verify the previous custom layout is restored.
4. Preview Reset again and Save only if desired. Restart and verify the new
   defaults persist without synthetic override entries.
5. Drag the default attack button before resizing it; it must not shrink.
   Confirm the wheel, aiming/cancel, chat, notch and system gesture edges on
   the real device. A fresh installation should show these same defaults.
