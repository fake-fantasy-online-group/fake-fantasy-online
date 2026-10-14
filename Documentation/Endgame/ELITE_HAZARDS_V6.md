# Additive v6 elite snapshot and movement contract

The existing Endgame binary envelope is unchanged: opcode `0x80d0`, schema `1`, uint16 JSON length, and the existing 60 KiB payload limit. Historical v5 JSON with no new fields remains readable. This client change does not activate server rule version 6 or migration 80.

## JSON and bounds

- A v6 Rift view supplies `mapId` and `serverNowMS`, even when its optional `hazards` array is empty. Its `runId`, `floorIndex`, `revision`, and native map ID identify the full replacement snapshot.
- At most 56 hazards: no more than 24 active-mechanism effects and 32 death-lava effects. Runtime configuration may impose a smaller budget to fit the unchanged wire envelope.
- Hazard IDs identify an effect generation, not its reusable caster entity. `sourceId` is a canonical nonzero uint32 decimal string. The stable hazard ability IDs are 2/death_lava, 3/lightning_orb, 4/ice_orb, 6/poison_pool, and 7/walls.
- `bornMS`/`expiresMS` are authoritative epoch milliseconds. Duration is at most 60,000 ms, and `remainingMS` must equal `expiresMS - serverNowMS`. An emitted effect is live at the snapshot sample. Lightning first-pulse, pulse, and propagation values are relative milliseconds.
- A wall effect contains exactly three rectangles. Every rectangle retains float64 endpoints `(x,y)`–`(x2,y2)` and full `width`, with finite absolute coordinates at most 1e7, positive width at most 1024, and positive centerline length at most 2048. IDs are unique across the complete snapshot.
- Optional aura `abilityIds` is a unique list of IDs 0–7 whose count equals the elite class. `goldenUntilMS` requires selected ability 0 and is an active server deadline at most five seconds ahead. `cloneOf` is a distinct uint32 entity ID on a class-zero aura with no elite affixes or abilities. V6 bounds are 321 base auras plus 96 clone auras.
- Optional `impacts` contains at most 40 actual resolved lava/ice blast receipts: ID, mechanism, position, radius, `atMS`, and `expiresMS`. Its lifetime is exactly 700 ms. Removing a hazard does not itself signal an explosion. Rendering owns presentation and one-shot deduplication.

## Clock, map identity, and cleanup

`EndgameRiftHazardState` anchors server time to `Stopwatch`, never the mutable local wall clock. Repeated or delayed full snapshots cannot extend the estimate, restart a fixed deadline, mutate an existing effect's geometry, or resurrect a removed effect before its original expiry. Expiry is exclusive. Effect-owner tombstones expire with the effect and are bounded by live lifetime rather than retained forever.

V6 `rift.context.result` adds the existing string-map keys `runId`, `floorIndex`, `mapId`, and `serverNowMS`. World use requires matching context and full snapshot, then `CanUseRiftHazards(mapID, nativeRoot)` binds that identity to the actual native world root. A different floor cannot bind to an old root even if the native map template is identical; a recreated PlayerController cannot bypass this state-owned guard.

The two narrow GameBootstrap hooks preserve the native map-load/coroutine order:

1. Beginning a map change suspends effects and consumes fresh context once.
2. If resource preflight rejects the change and retains the exact original root/map, rejection recovery permits that root only after another matching server context and full snapshot. The context sample must be strictly newer than both the last context and the latest snapshot at rejection; cached/equal-timestamp contexts followed by newer snapshots cannot re-enable effects. Same-millisecond recovery waits conservatively for a later authoritative sample. A client load failure cannot rewind an already confirmed newer server floor.

Reset/disconnect clears all state. Unknown context, a mismatched map, transition, terminal gameplay phase, or expired owner never supplies a temporary movement obstacle. The original permanent MASK is never edited.

## Movement integration

The native PlayerController owns the actual movement behavior. A-star node checks, adjacent-edge checks, path smoothing, exact final endpoints, and full per-frame displacement sweeps query the same rectangles. Both axes are expanded by the server's 12-unit actor clearance, with flat ends and inclusive contact. Slab clipping prevents high-speed or diagonal tunneling, including when a static MASK is absent. The scaled hypot evaluation matches Go's operation order.

A changed wall set invalidates and replans the current route. The replan retains the addon/combat movement ownership token. Non-wall hazard changes do not trigger replanning. The static terrain/bridge query and projectile line-of-sight code remain unchanged.

## Reproducible validation

- `python3 Tests/Endgame/compare-go-dto.py ../server-development/server/internal/domain/endgame_contract.go`
- `python3 Tests/Endgame/rift_hazard_source_contract.py ../server-development`
- `source Documentation/tools/activate-dotnet-test-toolchain.sh` for the verified cloud .NET test profile, or load the documented Mono/Roslyn profile
- `bash Tests/Endgame/run-rift-hazard-contract.sh`
- Existing `Tests/Endgame/*source_contract.py` and `git diff --check`

The hazard contract compiles the actual application DTO/state sources plus native Navigation, Displacement, Mask, and RiftCollision partials with deterministic Unity substitutes. It covers legacy/new schemas, malformed fields, geometry, authoritative expiry, duplicate/removal replay, owner overlap, stale map/floor/root state, failed-map recovery, path replanning, speed tunneling, MASK absence, preserved LOS, and navigation ownership. It is not a Unity rendering test.

Fixtures `elite-wire-v6*` were generated by the server's production `riftView` and `protocol.EncodeEndgame`, via `TestRiftEliteWireFixture`. Regenerate both samples by setting `RIFT_ELITE_WIRE_FIXTURE` to an output `elite-wire-v6.json` path and running that exact Go test. The metadata fixture includes real gold/clone/blast state. Compare entity/effect arrays by their IDs because Go map iteration order is unspecified. `elite-wall-geometry-v6.json` contains 20 results from the actual server `temporaryWall.pathBlocked`, generated with the checked-in Go test overlay; the C# executable compares the client geometry directly against that oracle. Run `bash Tests/Endgame/regenerate-rift-hazard-fixtures.sh ../server-development` with the Go toolchain sourced to regenerate all three samples without changing server production files.

## Executed validation (2026-10-14)

The compiler/runtime blocker was resolved using official Microsoft .NET Runtime 6.0.36 and Roslyn 4.8, with hash-verified Unity 4.8 API reference assemblies restored from the existing managed-toolchain backup. The explicit compatibility command adapter preserves compiler/test arguments, assertions and exit codes, and records compilation separately from execution. **This profile is not a Mono runtime or a Unity engine run.**

Actual C# compilation and execution passed:

- `run-rift-hazard-contract.sh`: 115 assertions, including actual Go geometry oracle comparisons, continuous native collision/path replanning, strict lifetime/snapshot parsing, same-map root isolation, failed-map recovery, and cached-context replay rejection.
- `run-contracts.sh`: 214 legacy model/state/production-wire assertions.
- `run-world-entry-contract.sh`: 52 NPC world-entry assertions.
- `run-map-transition-contract.py`: 96 assertions across 25 native lifecycle scenarios, including rejected map loads. Current and verified main-baseline `03a4a2c55a3369d740b3051a03437e6bb3bd514f` GameBootstrap sources were each compiled and executed; all nine frame/build/ready/receive flow traces matched exactly.
- `run-rift-portal-contract.py`: 49 portal/context/transfer assertions.
- `run-rift-routing-race-contract.py`: 189 native and 119 plugin race assertions.
- Shared panel runner regressions: contextual loot 39, module 43, Rift HUD 346, and scene portal 30 assertions. Five existing runners needed the real new `RiftElite*.cs` sources added to their compilation lists; the HUD runner also needed the new shared test partial. No assertion was removed or weakened.

Also passed: 256 Go/C# DTO field-type checks, 108 hazard static/actual-Go-fixture checks, existing Python source checks, Bash syntax and `git diff --check`. The production Go fixture generator and 20-case authority collision oracle were independently executed; regenerated JSON/wire samples matched by identity.

The above is focused managed validation with deterministic Unity substitutes. It does not establish a Unity Player build, live transport behavior, rendered appearance, play-mode performance, or Mono-specific runtime compatibility. The aggregate engine-free compilation and broader visual-suite results are tracked separately from this contract report; do not treat this list as a claim that every repository test ran.
