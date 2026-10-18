# Map music lifecycle correction (2026-10-18)

## Proven defect and bounded correction

`GameBootstrap.BuildWorld` already calls `MusicManager.PlayMap(mapName)` on each world build. `MapSourceName` normalizes encoded instance IDs and custom source IDs, so music is chosen by the actual source file, not the instance ID. Same-track floors/instances intentionally keep the current playback position.

The previous `MusicManager` kept the outgoing audio when a destination mapping or OGG was missing. It also stored a selected track before loading, preventing retry after failure. The new lifecycle retires outgoing audio on a changed/unavailable selection, clears failed selections, and rejects obsolete completions using a selection generation (including A → B → A). No protocol, source-map resolution, or authored track assignments changed.

This is a defensive playback fix, not proof of the exact configuration failure in a deployed Player. A genuinely shared track still does not restart. A destination without usable music becomes silent rather than retaining unrelated music.

## Tests

Run after sourcing `Documentation/tools/activate-dotnet-test-toolchain.sh`:

- `python Tests/MapMusic/run-map-music-contract.py`: 17 passing checks using the actual complete MusicManager source and actual MapSourceName/CustomMaps methods, with deterministic Unity/network boundary substitutes. Covers entry/exit, different source floors, same source instances, shared tracks, absent mappings/files, file recovery, failed decode retry, stale completion and null map input. Fixture track assignments are synthetic and are not installed in the game.
- The identical contract with pre-fix source (`--music-source`) fails at “missing map mapping must not retain outgoing music”.
- `python Tests/WorldEntry/run-map-transition-contract.py`: 96 assertions across 25 lifecycle scenarios pass.
- `python Documentation/tools/compile-current-presentation.py`: all 1,199 current Presentation sources compile against recovered actual Unity API references.

These checks do not run Unity, decode real OGG files, exercise an audio device, build a Player/APK, or verify the user's installed bundle.

## Resource investigation still open

Original 1.6.0 manifest lists `gameassets/tables/mapmusic.json` at 7,696 bytes, MD5 `e163636ef532e21afdef011ae46d0ef3`. The currently supplied cloud resource tar contains music OGGs and PKGs but not this loose JSON. The original resource-host request returned HTTP 403. No alternative track mappings were invented.

The separate original PKG table `zipbin/ov_music.bin` has 258 rows (20-byte records after a 16-byte header). Examples: source IDs 7 → M002, 37 → M015a, 38 → M016, 88/89 → M034, 90 → M033, 91 → M032, 127–129 → M046. This table is evidence, not the runtime JSON consumed by MusicManager; it cannot establish the installed runtime mapping by itself.
