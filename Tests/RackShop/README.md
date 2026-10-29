# Rack visible-icon contract

Run `python3 Tests/RackShop/run-visible-icons-contract.py` with Mono and a C# compiler
on PATH, or pass `--mono /path/to/mono --csc /path/to/mcs.exe`.

The runner compiles the production `RackShopDlg.Icons.cs` and the exact production
`SetGoods` method and `GoodsBuffer` class extracted from `ReferenceWorldReceiveOwner.RackShop.cs`, with
small deterministic Unity/network API stubs. It checks that receiving 996 rows
performs no icon reads, then exercises first/second pages, repeated draws,
cart/filter rows, shared pet icons, missing/corrupt icons, late resource readiness,
and refreshed catalog invalidation after icon-map changes or missing-icon recovery.
Source assertions cover all five normal/flag/cart render call sites.

The first page renders 12 distinct goods including the five featured rows. Later
pages may also render the original five featured rows; these are already cached.
Scrolling the cart can load up to nine additional visible goods. Icons are loaded
on demand and cached by the window; resource textures remain owned by ResLoader.
Missing icons retain the existing blank-icon presentation, with item text and
controls still usable. A decoder exception is logged once per icon/window instead
of aborting catalog receipt or window rendering.

These are compiled behavioral and source-wiring checks, not a Unity GUI run,
real-asset decode benchmark, or proof of the reported user's hang cause. Verify
cold/warm rack opening, paging/filtering, purchases and close input with real
assets in a client build before claiming the runtime incident is fixed.

## Restored shelf and tooltip foreground

Run with the repository's standard managed-test profile:

```sh
source Documentation/tools/activate-dotnet-test-toolchain.sh
python3 Tests/RackShop/run-native-shelf-tooltip-contract.py
```

Alternatively, pass `--mono <mono> --csc <Roslyn csc.exe> --framework-dir <framework-api-directory>`.
Use the verified Unity `unity-4.8-api` reference directory; older Mono `4.7.2-api`
references lack a `string.Split` overload used by the production modal renderer.
The runner explicitly references `mscorlib.dll`, `System.dll` and `System.Core.dll`
from `FFO_FRAMEWORK_DIR` (or `--framework-dir`); it does not depend on the compiler's
implicit Mono framework discovery. This compiles actual `UITip.cs` and
`PetInfoTooltip.cs` against deterministic Unity API substitutes. It checks every
frame/text draw's foreground depth, root GUI coordinates and comparisons at all
screen corners at 0.75/1/1.5/2 scale, request replacement and repaint clearing.
The same queue also executes actual `SysMsgBox.Drawing` with two overlapping
panel primitives in four callback orders; depth-priority sorting keeps tooltip
submissions last, while subsequent popups/panels set their own depth and tooltip
frame drawing preserves GUI color. Depth intentionally remains set on the
renderer component, matching native modal ownership.
Source assertions verify the removed navigation row, fixed shopping content,
modal hover suppression and preserved native close/drag controls.

This is not a real Unity UI or asset screenshot run. Validate actual overlapping
windows/popups, hover, drag, scale, repeated open/close and confirm/cancel in an
authorized playable client before calling visual behavior verified.
