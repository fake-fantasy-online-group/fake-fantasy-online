# Name-tag atlas and monster-color contracts

Run with an installed .NET 10 SDK, without NuGet packages:

```sh
python3 Tests/UiPerformance/run-name-tag-contract.py --dotnet /path/to/dotnet
```

The runner compiles the complete current `BitmapFont.cs`,
`MonsterEntity.NameTag.cs`, `GameHud.TargetNameColor.cs` and `ISelectable.cs`
directly. It retrieves the old font and monster implementation from commit
`954f8d0e8f403abdd00802e2524ddd45ac92d390`, verifies pinned SHA-256 hashes, and
renames only their class names to run the old and current implementations together.
It verifies source hashes again after both runs. The renderer comparison checks
the actual name-tag shader's texture-times-vertex-color expression.

The second binary compiles the same, unchanged production font and monster
sources but substitutes a controllable `GameHud` color provider. This exercises
fractional-alpha fallback behavior that the current opaque-only production
color provider cannot produce.

## Intended production behavior

- Cache one atlas alpha plane, one byte per pixel. Retain only the current font
  binding, not a dictionary of names. Each actual atlas/index/width binding
  replacement discards the old plane and changes the name-tag generation.
- Recreating map entities does not invalidate an unchanged font binding.
- Bake opaque, single-line monster names with white fill and the original
  down-right black shadow. A difficulty-color change updates renderer tint,
  not the texture. Preserve the original Color32 fill quantization.
- Set renderer color only after rebuilding a sprite or changing the target
  color; unchanged frames do not repeatedly write the renderer property.
- Keep fractional-alpha monster fills on the original colored-bake route with
  white renderer tint. Tinting a whole fractional-alpha texture would incorrectly
  fade the black shadow, so this is deliberately not a generic tint optimization.
- Leave the general multi-line, title, top, bottom and icon bake behavior intact.
  Real text or font-generation changes still regenerate the affected texture.
- Publish bake-related cached state only after a successful sprite replacement,
  so a failed read/upload cannot suppress a retry or reuse the wrong tint mode.

## Cloud results, 2026-10-22

The new contract passes 1,659,312 assertions with the actual color source plus
2,884 assertions with the future-alpha substitute:

- 420 complete legacy/current pixel comparisons: Chinese, mixed ASCII, spaces,
  missing glyphs, fullwidth `｜`, scale 0 (clamped to 1), 1 and 2, multiple lines,
  tall multicolor icons, and opaque/fractional/zero fill alpha.
- Synthetic atlas alpha includes 0, 7, 8, 64, 127, 254 and 255. Separate boundary
  assertions verify source alpha 7 is skipped while 8 and 255 are retained.
  Comparisons include the original shadow offset and max-alpha overlap rules.
- 100 newly baked names perform one atlas read per binding generation rather
  than 100. Atlas-only, index-only and width-only replacement invalidate the
  cache; failed reads are retried rather than published. New map-style entity
  batches reuse an unchanged binding.
- For each of four difficulty thresholds, 100 monsters cross a band when the
  player changes from level 50 to 51. The old implementation creates 100 new
  textures, reads the atlas 100 times and calls Apply 100 times. The current
  implementation performs none of those operations, retains all 100 sprites,
  and changes each renderer tint once. Modeled shader output matches the old
  colored pixels, including black shadow opacity.
- A warmed 100-monster crossing allocates zero managed bytes in this harness.
  Stable 100-monster frames repeated 100 times do not write renderer colors.
  Friendly and same-band controls also avoid unnecessary writes and texture work.
- Real text changes rebake once and destroy superseded sprite/texture objects.
  Font replacement invalidates existing sprites even when the font remains
  ready. Failed upload/retry, visibility settings and fractional-alpha mode
  transitions preserve the expected image and successful-cache state.

Additional existing contracts were run with the installed .NET SDK compiler and
runtime adapters, without modifying their production sources:

- Map transition: 96 assertions across 25 lifecycle scenarios passed.
- Experience replay: 107 assertions and lifecycle wiring checks passed.
- Experience scene lifetime: 23 assertions passed.
- Level-up receipt/effect: 11 assertions passed.

The optional map stress suite is **not green**: both the pinned base commit and
this worktree exit 1 with identical defect/trace/summary output: 290 cases,
1,281 safety assertions, 100 same-physical-portal double-build cases and zero
retry defects. The current GameBootstrap source is byte-identical to that base
(SHA-256 `30853dd2a81c8b2331d92bd6dbd8388a73cb5017f62009a2c80aeaa7586c63ec`).
This pre-existing portal behavior was not changed by the name-tag patch.
The separate `--compare-baseline` mode could not complete because its historical
`f8d8885` object is absent from this checkout; it is not reported as passing.

## Limits and device acceptance

These are synthetic pixel and deterministic API-substitute contracts. They do
not run Unity, the real font asset, a GPU, IL2CPP, an Android build or a phone.
Counts and managed allocations establish the removed work, not a measured phone
frame-time, battery or thermal improvement. The initial 100 distinct names still
require their original 100 output textures and uploads.

Before release, use the normal Unity/device workflow to check an Android map
entry with many names, each relative-level threshold crossing, repeated map
transfers, real name changes, pet visibility, and player titles/icons. Inspect
frame-time/allocation/upload profiler captures and compare real-font screenshots
for fill, black shadow, separator and text alignment. No release/version changes,
quality or GC changes, glyph-mesh rewrite, frame queue, push or publishing are
part of this patch.
