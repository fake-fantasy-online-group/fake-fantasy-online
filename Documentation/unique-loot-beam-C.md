# Approved unique loot beam C

The user selected candidate C (第三种) on 2026-10-14 after reviewing the offline A/B/C animation. This is a presentation-only change for all world unique-item drops, not a rift-specific rule. Unique identity, ordinary yellow items, one-shot sound receipts, pickup/despawn, UI and protocol semantics are unchanged.

## Rendering

The reviewed C profile is now source code in `UniqueLootBeamCProfile.cs`: 540-pixel height, a 50.4-pixel bottom half-envelope falling to 1.4 at the tip, Gaussian soft edges, warm bright center, muted dark-gold veil, small ground light, and 12 sparse rising sparks. Width is `1.4 + 49 * (1-z)^1.65`. A four-second cycle pulses primary light ±3.5%. Native light filaments still come from all twenty frames of `object/DH/DH_FY_1501.gso`, at 200ms per frame; the source's union-canvas placement is preserved during remapping. No downloaded or generated artwork is added to Git.

The 450×680 approval stage is not shipped. Four shared presentation parts plus a tiny shared spark sprite render through the existing additive material. The base, center correction and pool are static; only twenty tightly cropped native-filament textures are generated. Twelve spark instances share one texture and move using immutable keyframes. These five normal cache entries total exactly 6,473,368 bytes of RGBA texture storage (6.17 MiB), shared by every simultaneous drop. Generated textures discard CPU-readable copies after upload. Temporary decoded source arrays and mapping tables are not retained.

Every layer starts and remains ten sort units below its item. The unique-only sorting flag is reset when an EffectPlayer is reused for another effect. The first visible frame already has correct spark positions and intensity. The beam is 5.4 world units high at the existing 100-pixel-per-unit convention; unlike the former native 0.65-scale column, its tall silhouette is intentional. Existing screen-space UI is untouched.

## Lifetime, startup and failure behavior

The ResLoader startup coroutine waits one frame after resource mounting, then attempts a one-time warmup. The generated entries use the existing effect budget and are not pinned. Missing or malformed source artwork produces no beam and no exception escaping into loot receive/update. The item's existing half-second retry recovers after delayed mounting or later cache eviction. Partial generation releases every staged texture and sprite. No per-drop textures or materials are created on a cache hit. All live effects borrow and protect their cached sprites through existing `CollectBorrowed` eviction checks.

Destroying/picking up one of several identical drops removes only its own effect and leaves shared immutable textures usable by the others. Identity changes, disable, scene cleanup and budget-detail invalidation retain the original cancellation paths; audio remains edge-triggered and guarded.

## Verification

Cloud-managed tests use official .NET 6.0.36, Roslyn 4.8, and the recovered Unity API references, without running Unity.

- 1,684 deterministic C profile assertions: taper endpoints, halo symmetry, root/tip intensity, four-second pulse, native sampling coordinates, all twelve spark seams and bounds.
- 90 production protocol/receipt/lifecycle assertions, including simultaneous identical drops, immutable cache reuse, initial sorting/position, missing and corrupted source recovery, cancellation and cleanup of partially generated textures.
- Source-wiring checks cover startup warmup, unique-only sorting and pool flag reset.
- Full current Presentation compile passes against real recovered Unity references (1,157 source files at this change).
- Cloud CPU generation measured about 174ms including deterministic stub upload, reduced from about 454ms by computing native sampling/taper tables once. Cache-hit attachment measured about 0.22ms. These numbers exclude real native decoding, Unity texture upload, GPU work and actual frame timing; they are not Player performance measurements.

Run `source Documentation/tools/activate-dotnet-test-toolchain.sh`, then `python3 Tests/WorldEntry/run-unique-drop-presentation-contract.py` and `python3 Documentation/tools/compile-current-presentation.py`.

Unity visual/runtime acceptance remains pending: actual color-space appearance, camera scale, bright/dark scenes, actor occlusion, overlapping drops, real GPU/CPU timing, startup timing and pickup/scene transitions. The reviewed animation is explicitly an offline preview, not a Unity recording. Additive layer quantization and subpixel bilinear spark sampling may differ by a few local pixel values from the offline combined raster, while the approved silhouette, palette, envelope, timing and source animation are retained.

Official visual-direction references: [Diablo III column-of-light loot marker](https://news.blizzard.com/en-us/article/7528695/patch-1-0-5-now-available), [Diablo III rarity glow](https://news.blizzard.com/en-gb/article/22989464/legendary-an-ancient-primal-guide), and [Diablo IV Loot Reborn](https://news.blizzard.com/en-gb/article/24077223/galvanize-your-legend-in-season-4-loot-reborn). No Blizzard art was copied into the implementation.
