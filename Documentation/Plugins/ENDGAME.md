# Endgame through the shared Game API

Lua addons and external agents use the same main-thread `PluginGameApi` as native
endgame play. The adapter obtains the existing `IEndgameClient` from the session;
it does not construct a second gateway, inventory, or rift state. The paired server
must negotiate `endgame.v1`. Discovery reports unavailable actions when that owner,
negotiation, complete inventory snapshot, or required server state is missing.

The existing bridge remains opt-in and loopback-only. See
[EXTERNAL_AGENTS.md](EXTERNAL_AGENTS.md) for transport setup and general action
semantics, and [the native endgame guide](../Endgame/README.md) for prerequisites.
These managed tests do not establish a live server or Unity playthrough.

## Observe first

The four observation domains are:

- `endgame`: availability, API instance ID, connection generation, exact inventory
  `expectedRevision`, published `snapshotId`/`snapshotRevision`, snapshot state,
  error/message, pending immutable request,
  current server quote, material wallet, active formations, rift summary and
  catalog load/cursor state.
- `endgame.items`: UID-bearing items, native location/tab/slot, rolled stats,
  unique traits, ordered sockets, binding and lock flags. Filters are `uid`,
  `location`, `tab`, `itemId` and `name`.
- `endgame.catalog`: requires `catalog`, one of `uniques`, `runes`, `formations`,
  or `recipes`. Returns already-loaded rows plus `loaded`, `requested`, `hasMore`
  and the server `nextCursor`. Observing does not request a page.
- `endgame.rift`: authoritative phase/progress/time, invitation/run ID, tier,
  one-based `floorIndex`, decimal-string uint64 `seed`, `generatorVersion`, `mapName`,
  `canAdvance`, `transitioning`, exit map pixels, `maxTier`/`unlockedTier`,
  keys, party eligibility, claimable flag, aura targets/affixes, globe positions and
  timed danger telegraphs.

All domains support the existing query revision/scene guards. Item/catalog/rift
lists also accept `offset` from 0 through 4096 and `limit` from 1 through 512
(default 128). Item offsets count matching rows after filters. Item/catalog pages
return `count`, `total`, `truncated` and `nextOffset`; a null continuation ends the
loaded rows. Catalog `nextCursor` requests another **server** page, whereas
`nextOffset` pages through rows already loaded in this client.

Rift `offset`/`limit` page auras, globes and telegraphs independently. Their totals and truncation
flags are separate; `nextOffset` advances until all three lists are exhausted. Aura
`targetEntityId` is the scene-qualified native monster identity accepted by
existing combat actions. Globe pickup is the server's automatic proximity rule;
`globePickupRadius` is 75 map pixels. This is not a new click-to-pickup command.
Telegraphs expose `id`, `entityId`, readable `ability`, `shape` (`circle`, `ring`,
`line`), map-pixel `x`/`y` and `endX`/`endY`, `radius`, `innerRadius` and
`remainingMS` until impact as of `receivedUtc`. Line radius is half-width with flat
ends; ring centers below the inner radius are safe. The server sends changes at
warning start/end. Local elapsed time never proves damage or a kill.

UIDs, inventory/item/rift revisions, character IDs and money are decimal **strings**.
Keep them as strings even when they look small. For example, UID
`"9007199254740993"` and rune UID `"9223372036854775807"` cannot be safely carried
through a JavaScript/Lua floating-point number. Snapshot `revision` outside `data`
is the existing observation version; action `expectedRevision` is the decimal
string inside `data`. They have different purposes.

## Discover exact action schemas

Use `describe.actions` at runtime. Every action below needs a stable caller
`requestId`; include the observed scene revision as `expectedSceneRevision`.
Unknown argument fields are rejected.

| Action | Arguments | Native route |
| --- | --- | --- |
| `endgame.refresh` | none | Snapshot request |
| `endgame.catalog.request` | `catalog`, optional `cursor` | One catalog page; omitted cursor uses the observed next cursor |
| `endgame.socket.insert` | `expectedRevision`, `targetUid`, `runeUid`, `socket` | Insert observed bag rune into observed empty socket |
| `endgame.socket.remove` | `expectedRevision`, `targetUid`, `socket` | Extract observed installed rune |
| `endgame.cube.quote` | `expectedRevision`, `recipeId`; optional `targetUid`, `runeUid`, `itemUids` | Server preview only |
| `endgame.cube.execute` | `expectedRevision`, `quoteId` | Execute exact current unexpired server quote |
| `endgame.rift.refresh` | none | Authoritative rift state request preserving the observed invitation/run ID |
| `endgame.rift.start` | `expectedRevision`, `tier`, optional `runId` | Fresh entry or exact observed invitation/run reentry |
| `endgame.rift.advance` | `expectedRevision`, `runId`, `floorIndex` | Optional guarded floor exit; server checks run opener/proximity, preserves run difficulty/progress/timer |
| `endgame.rift.claim` | `expectedRevision`, `runId` | Claim server-eligible reward or interruption compensation |
| `endgame.rift.leave` | `expectedRevision`, `runId`, `confirmLeave:true` | Explicit leave; no automatic key refund |
| `endgame.retry` | `expectedRequestId` | Resend precisely the existing immutable pending native request |
| `endgame.equipment.equip` | `expectedRevision`, `targetUid`, optional `allowBind` | Existing native equip owner |
| `endgame.equipment.unequip` | `expectedRevision`, `targetUid` | Existing native unequip owner, including slots 14–25 |

`expectedRevision`, where required, must match the current inventory revision. Mutations also need
a complete, non-loading snapshot whose `snapshotRevision` equals that revision; an
advanced mutation receipt alone cannot authorize selection from older item rows.
UID fields must be
canonical positive decimal Int64 strings. `socket` is zero-based 0–4 and must be
open in that item; insertion also requires it to be empty, extraction occupied.
Tier is 1–150 and does not change when advancing to another floor; fresh entry
respects observed nonzero `maxTier`/`unlockedTier` limits. Recipe IDs are positive Int32 values and must already be observed in
the recipe catalog. Opaque IDs/cursors are at most 128 characters.

`itemUids` is an ordered array of at most five distinct positive UID strings. Its
order is significant for both native commands and retry equality. It is the only
compound action argument accepted by the adapter. For `socket.reorder`, entries
must be installed rune UIDs on the chosen target; other selected runes must be in
the observed bag. A target-required recipe needs its observed bag target; a
recipe that does not use a target refuses an unrelated one.

Locked or stale items cannot mutate. Equipping an unbound item needs explicit
`allowBind:true`; trade blocks native equipment activation. The underlying native
equipment owner still validates inventory identity and session state.

## A safe progression loop

1. Discover availability and observe `endgame`, inventory/items and relevant
   catalogs. Request a missing snapshot/catalog explicitly, then observe again.
2. Read the actual recipe, inputs, socket order, item locks and current revision.
   Persist one chosen action and unique request ID before sending.
3. For cube work, request `endgame.cube.quote` and wait for its correlated server
   response. Inspect `endgame.quote`, including costs, material shortfall, expiry
   and description. A quote is neither a purchase nor an inventory mutation.
4. Execute only the intended exact current quote, with its observed revision.
   A new complete inventory snapshot or authoritative mutation invalidates it.
5. Observe authoritative results before making the next decision. When pending,
   reconcile that operation instead of starting another mutation.

Rift entry also requires the native NPC/key/party prerequisites. A reserved,
active, boss or completed live run uses the exact observed `runId` and tier.
Fresh entry leaves `runId` empty. Reward persistence or interruption compensation
must resolve before a new paid run. Claim requires the server's `claimable` flag;
leave requires `confirmLeave:true`. The adapter does not invent a quest solver,
combat outcome, path, refund, reward or successful admission.

For optional floor skipping, observe `canAdvance=true`, `transitioning=false` and
`phase=active`, then navigate natively to the observed exit. Call
`endgame.rift.advance` with the exact run ID and positive current `floorIndex`.
The floor guard prevents stale clicks skipping two floors; the command cannot
select another difficulty. Supply the current inventory `expectedRevision` and
wait for a complete matching inventory snapshot. This guards concurrent economic
changes, not the combat-updated rift revision. An uncertain endgame mutation
blocks it. Exit coordinates remain observable even when the opener is too far
away for `canAdvance`. Skipped enemies
and uncollected globes give no progress. The run opener/proximity check and map
transition remain server-owned. Use the same immutable retry flow if its receipt
is uncertain; never replace a timed-out request with another floor advance.

## Receipts, uncertainty and reconnection

A native submission returns `sent` and `serverOutcomeKnown:false`. Only a matching
authoritative response establishes `completed` or permanent `rejected`; request ID
and operation kind must both match. Cube execution success is `cube.result`, while
its errors use `cube.execute`. Returned result fields and `serverRevision` remain
server-authored.

The current paired server publishes snapshots without a request ID, so snapshot
refresh normally remains `sent`. Wait for a different nonempty `snapshotId`,
`hasSnapshot:true`, `snapshotLoading:false`, and matching `snapshotRevision`/
`expectedRevision` in observations. A synthetic/future server that does correlate
snapshot requests can complete refresh only after its entire ordered immutable
snapshot publishes. These are distinct regression scenarios.
Native equipment commands have no new correlated sidecar receipt and remain
`sent`; verify later inventory/equipment observations.

Transient `pending`, `storage`, `storage_unavailable` or `busy` is `unknown`, never
an invented failure. The immutable pending request stays in native state. A local
30-second receipt observation deadline also means uncertainty, not rejection.
Mutations continue accepting a later authoritative receipt. An uncorrelated
snapshot read stays `sent` with `endgame.snapshot_unconfirmed`; confirm publication
through observations rather than waiting for an invented correlated completion.
Already-submitted commands cannot be cancelled locally.

The agent's caller `requestId` is deterministically mapped to a 64-character
server ID, exposed as receipt `data.serverRequestId`. To query an uncertain
mutation, observe `endgame.pendingRequest` and call `endgame.retry` with that
exact `pendingRequest.requestId` as `expectedRequestId`, plus a new unique caller
request ID for the explicit retry action. The gateway resends the original
immutable native request and JSON. Do not replace the uncertain operation with a
new purchase/reroll request ID.

Identical retained caller requests return the same action without resending;
changed content, including different rune order, conflicts. The adapter retains
at most 1024 records and protects requests still awaiting an outcome. Native
response correlation retains 128 request IDs per connection generation. Check
`apiInstanceId`/`generation`, refresh observations after change, and respect the
native pending-request recovery rules in the endgame guide. A reset clears response
history and makes outstanding adapter receipts uncertain. It never silently
replays them or transfers an old result into a new connection.

## Managed regression checks

```sh
FFO_MONO_ROOT=/path/to/MonoBleedingEdge Tests/PluginRuntime/run-game-actions.sh
```

The runner also accepts `UNITY_EDITOR_DIR=/path/to/Unity/Editor`. It compiles the
actual action adapter, both new endgame partials, actual Application `Endgame*.cs`
and shared Foundation JSON code. Only native gameplay owners and generic
observation plumbing are substituted. Endgame filtering, paging, projections,
command validation, idempotency and receipt/state behavior execute production
source. The generic observation envelope/cache and Unity rendering are not
established by these substitutes.

The suite preserves the 38 native action assertions and adds focused endgame
regressions. See [VALIDATION.md](VALIDATION.md) for the recorded run and limitations.
No Unity/Player, external server, account login or live gameplay is started.
