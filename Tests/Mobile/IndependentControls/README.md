# Independent mobile control regressions

Run from the repository root after activating the managed toolchain:

```sh
source Documentation/tools/activate-dotnet-test-toolchain.sh
python3 Tests/Mobile/IndependentControls/run.py
```

The runner compiles the **current production sources**, never copies of their implementation.
Temporary executable files and test settings are removed automatically. No Unity process,
Player/device, live server, or real character action is used.

- `LayoutSafetyContract`: deterministic seed 53221, 4,000 safe-area/resolution changes,
  invalid values and strict parsing, clone isolation, actual temporary-file save/reload,
  corrupt UTF-8/oversized files, failure preservation, and temporary-file cleanup.
- `LayoutSessionContract`: actual common drawing/hit rectangles for 14 controls,
  8 screen dimensions × 6 UI scales, safe area and default non-overlap, six skill slots,
  actual Save/Cancel/Reset/failed-save session behavior and release-frame shield.
  The real admission gate is checked for the native gather menu, typing, registered
  windows/panels, and stale rectangles from closed inventory windows.
  Each other customizable control is placed over the joystick to test ownership exclusion.
- `PointerSafetyContract`: actual HUD, fixed-key hold/tap, editor, `ClientInput`, touch
  router, and targeting sources. Tests both input sampling orders, independent
  joystick/skill/fixed fingers, long-press cancellation, both same-frame hold-threshold
  and skill-release touch orders, canceled/interrupted gestures, exclusive ownership,
  panel blocking, edit drag, toolbar actions, and release-frame shielding.
- `FrameGateContract`: actual `PlayerController.Frame.cs` with counter dependencies,
  proving keyboard/world-pointer input is blocked while state/animation branches continue.

Boundaries are explicit: Unity geometry/input/rendering and unrelated game services are
lightweight stubs. Pointer tests count attempted fixed actions, rather than publishing
packets; the action rules are covered by the separate `run-skill-binding-contract.sh`
and `run-sitting-contract.sh` suites. The session suite compiles real model/store/layout
sources; the pointer suite stubs those session setters, so failures remain localizable.
These tests do not claim native touch/IMGUI or visual/device verification.
