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

## Authored fallback follow-up

The runtime now fills absent JSON entries from the original `zipbin/ov_music.bin` via the existing `ResLoader.Read24Res` path (which reads the mounted ff21 package), joining IDs through `MapDef`'s actual authored filenames. Explicit JSON entries, including blank entries, take priority. Map ID zero, missing descriptors and empty track names do not create guessed mappings. Invalid magic/version/stride/count/filename data is rejected before any additions are published; a failed load logs once. This adds no embedded resource data or protocol fields.

Verified original input hashes:

- ov_music.bin SHA-256: `3a5b6299cb43495d2a7e179b4437833fe5155c259f91ea67c35fd984bcb7f807`
- map.def SHA-256: `b49b78be216faad30cb59578a35b88abd1e841f681a8865b0471900e13838ef9`

The real inputs yield 231 joined rows and 230 distinct authored source-map files. Map IDs 21000 and 22000 share qw0060/M001. All 230 joined soundtracks exist in the supplied 51-OGG catalog. Seven music IDs have no descriptor and are skipped (22900, 23600, 28300, 28400, 30700, 1000000, 1000001). Examples verified from original map definitions: 龙城/qw0007 → M002; 桃源西村/qw0037 → M015a; 长乐西村/qw0038 → M016; 无泪东郊/qw0088 → M034; 空灵平原四/qw0090 → M033; 百灵岛/qw0127 → M046. Dungeon instances reuse their source map's authored music.

Updated tests: 28 synthetic checks pass; 36 checks pass when `FFO_ORIGINAL_MUSIC_TABLE` and `FFO_ORIGINAL_MAP_DEF` point to the externally supplied original files. The latter executes production MusicManager parsing, production MapDef parsing and the source-file join. The test-only GBK boundary preserves ASCII IDs/files; it does not validate Chinese display-name decoding. Original binary/text resource files are not committed. The complete 1,199-source Presentation compile passes again with the fallback. No Unity/Player/audio-device QA has been performed, and the installed loose JSON remains uninspected.
