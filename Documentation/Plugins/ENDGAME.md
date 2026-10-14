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

The observation domains are:

- `endgame`: availability, API instance ID, connection generation, exact inventory
  `expectedRevision`, published `snapshotId`/`snapshotRevision`, snapshot state,
  error/message, pending immutable request,
  current server quote, material wallet, active formations, rift summary and
  catalog load/cursor state.
- `endgame.items`: UID-bearing items, native location/tab/slot, rolled stats,
  unique traits, ordered sockets, binding and lock flags. Filters are `uid`,
  `location`, `tab`, `itemId` and `name`.
- `endgame.catalog`: requires `catalog`, one of `uniques`, `runes`, or `formations`. Cube recipe lists are not exposed. Returns already-loaded rows plus `loaded`, `requested`, `hasMore`
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
| `endgame.cube.quote` | `expectedRevision`, `cubeInputs`; legacy optional `itemUids` (reordering disabled) | Owned portable 周天鼎 input match/preview only |
| `endgame.cube.execute` | `expectedRevision`, `quoteId` | Execute exact current unexpired server quote through the same live workbench |
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
respects observed nonzero `maxTier`/`unlockedTier` limits. Opaque IDs/cursors are at most 128 characters. Cube actions never accept recipe IDs or ask for recipe catalogs.

`cubeInputs` contains 1–12 selected inputs. An instance is exactly `{uid:"...",count:1}`; a stackable material is exactly `{itemId:123,count:4}`. UID strings are canonical Int64 values; quantities are positive Int32 values. Unknown fields, duplicate identities and mixed forms are rejected. Inputs must belong to the current authoritative bag snapshot and not exceed owned amounts. These nested objects are frozen for request/retry identity.

`itemUids` is an optional ordered array of at most five distinct positive UID strings for already installed runes on selected equipment. Loose bag runes are ordered by their position in `cubeInputs`. Both orders are significant for retry equality. Installed-socket reordering is disabled by server migration83; retaining this field does not enable the retired operation.

Own the database-configured permanent 周天鼎 in an unlocked native bag slot. `endgame.cube-inputs.v1`, `cubeInputsV1` and `cubeItemAvailable` confirm capability and observed tool availability; `cubeItemId` reports its currently usable ID (zero when unavailable). The legacy `cubeWorkbenchEntityId` field remains present as null. No NPC interaction or open panel is needed for quote/execute, and neither request carries an NPC entity. The server independently checks current ownership and eligibility before quoting or committing. Tool loss/locking or a stale inventory snapshot disables the adapter. Exact request replay remains unchanged.

Locked or stale items cannot mutate. Equipping an unbound item needs explicit
`allowBind:true`; trade blocks native equipment activation. The underlying native
equipment owner still validates inventory identity and session state.

## A safe progression loop

1. Discover availability and observe `endgame`, inventory/items and relevant
   catalogs. Request a missing snapshot/catalog explicitly, then observe again.
2. Choose actual bag inputs and quantities, socket order, item locks and current revision.
   Persist one chosen action and unique request ID before sending.
3. For cube work, request `endgame.cube.quote` and wait for its correlated server
   response. Inspect `endgame.quote`, including matched costs, expiry
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

## V5 skill progression

With negotiated `endgame.growth.v5`, observe `endgame.skills` (optional `skillId`,
`offset`, `limit`). It returns `skillRanksRevision`, `expectedRevision`,
`formulaVersion`, `hardCap`, `playerCap`, `petCap`, `unspentPoints`, and paged
`skillRanks`. Each row includes `skillId`, `hardRank`, `grantRank`, `softRank`,
`effectiveRank`, `canCast`, `canInvest`, `blockedReasons`, `contributions`,
`currentEffectLines`, `nextEffectLines`, `nextEffectiveRank`, `nextRequiredLevel`,
`trainingSynergies`, `nativeModifierLines`, and `nextChangeLines`. Keep granted/soft/effective ranks and
contribution levels as strings; no gameplay cap or numeric narrowing is applied.
Current/next effect text retains the complete original authored prose and native
cost/cast/interval labels, with server-authoritative numeric values. When provided, `nextEffectiveRank`
identifies the next hard-point investment preview and `nextRequiredLevel` is its
player-level requirement. The native tooltip hides this investment section at
hard20; current effective levels still include equipment/grants. Neither preview
text nor a level requirement replaces `canInvest` and the revision/pending guards.
`nextEffectLines` always retains the full next-rank block, including unchanged
prose and detail rows. Optional `nextChangeLines` remains protocol-compatible
metadata; the native card ignores it and never substitutes changed-only summaries.
Unlearned cards show the complete first-investment block once. Incoming training
sources are appended after the original card only on the receiving skill.

Each `trainingSynergies` row provides `sourceId`, `sourceName`, `effect`,
`effectLabel`, `perPointValue`, `unit` (`percent_bp` or `flat`),
`investedHardPoints`, `sourceEffectiveRank`, `contributionValue`, and
`equipmentApplies`. Equipment-aware rows set `equipmentApplies=true` and supply
the exact decimal-string `sourceEffectiveRank` used by the server, including
eligible equipment levels. `investedHardPoints` remains the allocated points,
not the contribution multiplier. Percentage values are basis points, so 300 means
3%; the rate is authored by the server for the receiving skill's unlock tier.
`contributionValue` is an Int64 equal to source rank times `perPointValue`.
Rows include declared unlearned sources at effective rank zero; equipment levels
alone do not grant learning. Legacy hard-only rows remain accepted with
`equipmentApplies=false`; an omitted `sourceEffectiveRank` then defaults to
`investedHardPoints`. The native card displays the source level and per-level
rate, keeping the original card body and centered incoming section.
`nativeModifierLines` separately describes the target's existing skill modifiers.
These optional fields default to empty/zero for older V5 server snapshots.

- `endgame.skill.refresh` requests a fresh `skill.ranks` view.
- `endgame.skill.upgrade` takes `expectedRevision` and `skillId`. Supply a stable
  caller `requestId` as for other endgame mutations. The adapter checks the current
  rank view and the server's `canInvest` before sending.
- `endgame.retry` queries the exact pending investment after an uncertain result.
  Never replace it with a new request ID to spend another point.

The ordinary `skills` observation retains its native learned rows and cast details,
including starter/life skills. V5 profession rows add `rankAuthority: endgame.skills`
and the complete authoritative `ranks` object. Treat the old numeric `level` as
compatibility data only; `ranks.effectiveRank` is the V5 rank authority. Native
hotbar pickup and agent cast admission use authoritative `canCast` for these profession skills.

### Authoritative equipment combat observation

`endgame.combatStats` (inside the `endgame` observation) contains the server's
`magicFind`, `physicalMitigationBP`, elemental damage/resistance/penetration/
mitigation/absorption channels, `equipmentAuras` and `procReadiness`. These are
raw aggregate equipment channels, including current friendly aura grants, before
per-enemy penetration, conditional caps and PvP damage calculations. The client
does not parse tooltips, recompute them or clamp them. Source UIDs stay decimal
strings; unsigned entity IDs and Int64 values keep their wire precision.

`combatStatsRevision`, `combatStatsVersion` and `combatStatsReceivedUtc` identify
the observed update. Aura rows include their source, rank, visual asset and lease;
proc rows include fixed rank, chance, ICD, remaining personal/global cooldown and
server `ready`. Readiness is a sampled server fact, not a local cast guarantee.
A changed `combat.stats` push refreshes this observation without requiring an
inventory change. Inventory chunk0 may also supply the view, but it publishes
only with the complete snapshot and cannot overwrite a newer runtime push.
Legacy snapshots may omit it; missing data is reported as null.
