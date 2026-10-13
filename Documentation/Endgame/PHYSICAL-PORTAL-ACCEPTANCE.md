# Rift physical portal repair

The next-floor action is available by walking into a scene portal. The modal and HUD no longer provide next-floor buttons. Existing durable leave and completion-NPC behavior are unchanged.

## Fixed failure modes

- A native polygon edge previously latched even when hello/context, run information, inventory snapshot, or another mutation prevented sending `rift.advance`. Production-source reproduction fails on the original code when readiness arrives while the player stays inside. The new one-shot intent waits only while still in the gate, binds the current connection/run/floor, and sends once. Leaving, reset, a changed floor, guardian/terminal phase, or a transition cancels it. Sent requests, including rejections and uncertain outcomes, never automatically retry. Unknown context that resolves public resumes the original native callback once; initial spawn-inside suppression is preserved.
- The seeded `ExitX/Y` marker previously required clicking the next-floor button. Its existing server-authoritative exit now has a 45-native-pixel physical walk-in trigger, independent of native polygons or callbacks. Initial observation inside the portal, reconnect, and same-map floor replacement cannot chain transfers. The client still uses the existing `rift.advance` request and the server's position, opener, readiness, phase, floor identity, revision, and persistence checks.
- Ordinary `LoadTeleports` intentionally rejects missing public destinations. Reusing its filtered result for rifts incorrectly removed valid gate geometry although rifts never use those destinations. A separate source-only `map_teleports` polygon query populates private rift geometry. Public destinations, scripts, dungeon admission, and ordinary portal filtering are unchanged.

A read-only test against an idle migration-73 PostgreSQL fixture confirms ordinary loading remains 463 portals with 71 missing-destination rows. Rift-only geometry covers 245 source maps and retains extra authored polygons on maps 346, 347, 349, 350, and 961. These prove loader behavior, not the identity of the user's reported portal. The unavailable current client teleport JSON was not claimed equivalent to SQL.

## Verification

Run from the corresponding checkout after enabling its managed toolchain:

- `python3 Tests/Endgame/run-rift-portal-contract.py`: 49 production client assertions, including deferred native edges, authoritative public release, 45px seeded walk-in, no native list/callback, rejection, floor/reset cancellation, completion, and public/reconnect suppression.
- `python3 Tests/Endgame/run-rift-routing-race-contract.py`: 189 production native-state race assertions and 119 plugin routing assertions.
- `python3 Tests/Endgame/run-rift-reconnect-busy-contract.py --fixture <export>/hello-busy-1.payload.bin --admission-fixtures <export>`: 59 assertions replaying fresh actual server admission/context bytes.
- `go test ./internal/game/scene ./internal/session -run 'TestRiftNativePortal|TestRiftSceneContext|TestRiftPublicExitContext|TestRiftLegacyClient|TestRiftLeave|TestRiftFloor|TestRiftFreshReconnect|TestRiftHello|TestRift.*Completion' -count=1`: actual scene floor handoff, persistence, same-map public exit, completion, reconnect, and session admission.
- `go test ./internal/data -run TestRiftPortalAreas -count=1`: independent geometry and unchanged ordinary rejection. The optional actual-loader case requires `FANTASY_RIFT_PORTAL_PG_DATA` pointing only to an idle disposable migrated fixture, plus `PG_BIN`; it uses single-user read-only transactions and starts no listener.
- Ordinary map lifecycle: 96 assertions; production native position sender: 17 assertions.

The existing ordinary `run-map-transition-contract.py --stress` remains a known failing check: baseline and repaired source both produce 290 cases, 1,281 safety assertions, 100 first-portal double-build scenarios, and zero retry defects. This work does not claim that unrelated lifecycle stress defect is repaired or that the aggregate check passes.

These are source, managed-runtime, native scene-actor, and read-only database tests. No Unity Player, live rendered-map gameplay, user computer, deployment, or production-server test was performed.
