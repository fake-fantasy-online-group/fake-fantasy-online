# Native dungeon entry presentation contract

Change: existing server-owned dungeon-service lists 101..133 (except 114) appear
as named NPC main-menu choices rather than a generic TransportUI. Original
0x1071 / 0x8049 / 0x1072 list/index authority remains unchanged. List 109 is an
in-instance floor choice and 133 is an exit. Ordinary transport lists remain
ordinary. List 114 is deliberately retained for owner review because the native
lookup explicitly excludes its target map 34200.

No server production code, task eligibility, difficulty mapping, SQL or resource
file is changed. The six existing `dungeonhard` commands remain separate. For
unbound NPCs the original native table callback is byte-for-byte unchanged.

## Run

```sh
source /workspace/shared/client-toolchains/env.sh
python3 Tests/DungeonEntry/run-dungeon-entry-contract.py
python3 Tests/TrialMenu/run-trial-menu-contract.py
python3 Tests/Endgame/run-native-entry-sender-contract.py
```

The first command compiles the actual new DialogUI partial, actual menu travel
segment, actual unchanged table callback, actual lookup and actual transport
state types. Only Unity, socket and surrounding UI owners are substitutes.
301 assertions cover all 71 audited list/index choices, 17 hard variants,
ordinary transport, held list 114, exit wording, price rendering, double click,
closed dialogue, old NPC reply, map transition, re-opened stale delegate,
retained task subview and original table-only party gate. Quest states are
characterized by real server tests, not simulated by string labels in this UI
harness. TrialMenu has neutral dungeon-helper substitutes because it tests
unrelated trial services with curTrans=0; this suite owns the actual helpers.

`AuthoritativeRouteFixture.cs` was generated from actual server
`domain.WithDungeonDifficultyChoices` output against the 54 explicit seeded
rows in service lists 101..133 at server 6f01fdcf. Its 71 row indices, labels,
map IDs, prices and 17 hard flags were individually checked against the audit
CSV. It does not contain original resource bytes. In particular, two ordinary
list-1 SQL rows must never be counted or rendered as dungeon-service routes.

Base: client 36aab0ec93b1912905bd1e77165a63c951e50a6c (1.0.6), server
6f01fdcf0051ff6b204acc60f73ea96de2a25df4. Source references:

- server/migrations/000000_init-0.sql:8664–8925: 54 routes + exact NPC bindings.
- server/internal/domain/dungeon_transport.go:7–58: existing difficulty expansion.
- server/internal/game/scene/transport.go:10–121: source/NPC/context/price/clear gates.
- server/internal/game/scene/dungeon.go:26–78,321–374: party/solo/trial gates.
- server/internal/domain/quest.go:31–37,382+,584+: exact quest state semantics.
- server/migrations/000000_init-32.sql:51189–51195: four solo quest narratives.
- server/migrations/000000_init-42.sql:16–23 and init-52.sql:228: trial233, four entrances,12–100.
- DungeonEntrances.Lookup.cs:10: map34200 explicitly excluded.
- DialogUI.Travel.cs:50–67: unchanged table callback.

Validation performed: complete current core compilation (418 sources) and
Presentation compilation (1234 sources) with verified recovered Unity references;
301 dungeon assertions; 206 adjacent trial-menu assertions; 46 native production
sender assertions. No Unity execution, device rendering or live-server smoke is
claimed. Existing native sender harness verifies actual opcode writers; this
suite verifies the new call sites use those writers, not a local map switch.

The transport packet has no request nonce. Cross-list/cross-map/closed replies
are discarded, and stale click delegates have a generation guard. A same-list
reply after re-opening the same configured NPC is indistinguishable on the
existing protocol; it is safe only because the current authoritative list/index
configuration is the same. Do not claim per-request nonce correlation.
