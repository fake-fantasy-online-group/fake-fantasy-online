# Client existing-mechanism audit, 2026-10-13

Scope: client `ca4187b` and server `04f9775b`, plus the working corrections
listed below. Reviewed endgame UI, item/skill/status tooltips, NPC entry points,
resource loading, and the native inventory/sidecar publication seam. No Player,
Unity rendering, live account, desktop, deployment, or push was used.

## Corrected defects

### Same-base equipment identity could cross native/sidecar generations

`EndgameClientState.Find` previously accepted any previously completed snapshot;
`EndgamePanel.Items.MatchesNative` compared only item-table ID and slot. Two
instances sharing a base ID therefore passed the check even after a new native
inventory arrived. `OverlayTooltip` then replaced the new native title with the
old instance name and added the old lore. Partial replacement snapshots also
left this path open.

The original ownership convention is already sufficient: server
`scene/inventory.go:pushInventory` publishes native rows before the corresponding
sidecar on the same ordered connection. Client
`PrivateServerGateway.PublishInventoryState` is the common native publication
point. The correction joins those existing owners instead of introducing a new
item ID, packet, or inferred balance model. Details and regression coverage are
in [INVENTORY-IDENTITY.md](INVENTORY-IDENTITY.md).

The review also traced direct native-only inventory publications in the existing
server warehouse, lock, hair, and wardrobe handlers. After each host receive
batch, a single existing read-only snapshot request recovers an unmatched native
publication; complete or assembling pushes need no extra request. Missing replies
time out once. The asynchronous retained entry fallback now uses the existing
host dispatcher before touching the Endgame collection owner.

The plugin action and observation paths use the same freshness fence. Historical
rows remain observable with an explicit `inventorySnapshotCurrent=false`; they
cannot authorize a new UID-based action. Existing immutable pending requests are
not discarded or retried automatically.

Repair locations under `Assets/FakeFantasyOnline/Runtime`: state freshness and
retired IDs in `Application/EndgameClientState.Snapshots.cs:12-107`; lookup and
timeout in `Application/EndgameClientState.cs:113-149`; host-batch recovery in
`Protocol/PrivateServerGateway.Endgame.cs:22-30`; common publication in
`Protocol/PrivateServerGateway.cs:7326-7347`; retained entry dispatch in
`Protocol/PrivateServerGateway.EnterWorldReceipt.cs:358-363`; UI selection in
`Presentation/ReferenceAssets/EndgamePanel.Items.cs:18-20`.

### Rune recovery cadence was duplicated in client code

`EndgamePanel.Tooltips.RuneStatText` special-cased item 1900028028 and attr38 to
invent a two-second recovery sentence. The server already provides
`EndgameRuneView.Description` from `AstralRuneDef.Description`, and native card
text already contains the persisted instance amount. The client now renders
ordinary attribute values plus that server description. Native actual values
still suppress reference-catalog fallback values. No recovery amount, interval,
eligibility, or balance was changed by the client correction.

## Remaining migration work, deliberately not mechanically changed

### Equipment text still has two semantic formatters

`EndgamePanel.Tooltips.cs` contains text classifiers (`IsAttributeLine`,
`IsAdditionalAttributeLine`, `IsLegacyEquipmentEffect`) and raw-stat formatting
(`StatText`, `StatName`). The server has the authoritative `scene/wash.go:affixText`
and native owned-item tooltip renderer. This already produces wording drift:
client attr23/66 uses “爆击率/魔法爆击率”, while the server uses
“暴击率/魔法暴击率”; client attr40 uses “负重上限”, server “最大负重”. More
importantly, adding a new stat/effect currently requires maintaining both
semantic classifiers and formatters.
Current classifier locations are lines 236, 263 and 288; the label map is at
451-459. The original server formatter is `scene/wash.go:29`.

Scoped migration: retain current native description as the owned-value source,
put future display labels/semantic section markers in the server's existing
native renderer or an explicitly negotiated presentation field, then limit the
legacy reconstruction path to legacy capability mode. Preserve the current
white intrinsic / gold enhanced / orange special-effect contract and cross-path
bag/equipment/comparison/workbench fixtures. Do not replace actual rolled values
with catalog reference numbers. The current patch does not claim to complete
this larger migration.

### Monster status composite does not recover a missed first icon load

In `MonsterEntity.StatusIcons.cs`, `ApplyStatusIcons` commits `siSig` before asking
`GameHud.BuffIcon` for each texture, then returns early on the same signature when
its composite object already exists. A missing first texture is baked as an empty
border; identical later status packets cannot rebuild it. `TickStatusIcons` only
rebuilds at expiry. In contrast, the existing HUD path asks `BuffIcon` each draw,
and that loader deliberately does not cache misses. This gap predates the latest
status repair; the repair correctly added icon ID and polarity to the signature
but did not close the resource-readiness case.
Concrete locations are `MonsterEntity.StatusIcons.cs:63-64` (signature commit and
early return), `:88-89` (miss skipped), and `:130-138` (expiry-only rebuild).

Scoped follow-up: track unresolved positive icon IDs and retry the composition
on a bounded interval or resource-ready generation. Keep authored zero icons
empty and retain source texture ownership in the shared loader. Add a production
composite test with missing→ready resources; existing icon tests currently test
the loader and signature independently. This was not changed in this patch.

## Existing mechanisms correctly reused

- Skill growth tooltips and targeting use the negotiated server contract through
  `Win05.GrowthSkills` and `AuthoritativeProfessionSkill`. No new client damage or
  growth formula was found in the negotiated path. `BakeDesc`/`CastReducePct` are
  original legacy presentation routines, not a new balance authority.
- `NativeSwordSkillTargeting` is an explicit legacy compatibility correction;
  negotiated profession skills take the server provider first. Deleting it would
  change approved legacy behavior rather than fix the data path.
- NPC additions reuse `DialogUI` options, native talk state, and server admission.
  Name predicates mirror the existing native menu convention. They are display
  coupling, not proof that the client grants crafting/rift permissions.
- `ArtworkSkin` scopes its borrowed texture cache to `ResLoader` and package
  ownership; rift art uses that shared loader. The unique ground beam registers
  decoded sprites in `EffectPlayer`'s existing cache/budget and releases its live
  attachment on disable/destroy. No new resource-ownership defect was confirmed.
- `NetClient.EndgameItemIcons` is a bounded last-resort compatibility fallback
  after `item_icons`, `item_icon_fix`, and server icon overrides. It is not the
  primary item mapping. It was retained; moving verified fallback metadata into
  the existing table pipeline can be considered separately without removing
  offline compatibility or replacing authored artwork.

## Verification boundary

Managed aggregate compilation passed for seven assemblies (650 source files).
Existing shell contracts passed, including UI 2,148, native skill tooltips
79,327, gateway 107, DTO/state 209, sword targeting 2,860, and full variant packet
acceptance 3,524 checks. Plugin action/projection contracts passed 366 checks,
including three new inventory-freshness checks. The plugin test's missing legacy
skill-provider substitute was repaired; production skill-provider behavior is
covered by the separate presentation acceptance suite.

The broad skill presentation acceptance script requires five server-generated
profession fixtures; its no-argument invocation is invalid. Its supported
`--variant-packets` mode passed with the existing full profession fixtures.
Fresh baseline fixture export is deferred while server changes are in progress.
The separate aggregate plugin observation runner requires installed Unity engine
assemblies and was not runnable in this cloud toolchain. These results are not a
Unity/Player visual acceptance claim.

## Paired server configuration review

This section records independent, read-only source review of the concurrent
server changes. Server implementation and its database/race test execution are
owned by their corresponding changes; no live database was accessed here.

- Migration 59 routes the 32 original base items through the ordinary item,
  description and physical-card tables. Its single DO statement detects exact
  definition conflicts before applying, retains exact rows on repetition and
  does not rewrite player/card-instance state. The deployment document correctly
  warns that binary-only rollback to the earlier code-only catalog is unsafe.
- Migration 61 feeds fees, materials, output quantities, source count and quality
  step into the actual cube planner. Existing quote fingerprint, immutable
  candidate, RNG/UID, receipt and atomic save boundaries remain. The review found
  missing client text/list/whole-page bounds and success-message suffix space;
  the rule owner added UTF-16 limits, actual JSON page/quote budgets and room for
  the longest completion suffix. No recipe price or yield was redesigned.
- Catalog 60 must validate legacy 0/5 formations as well as version 6. A bad
  legacy channel previously escaped constructor validation and could index past
  the active-channel array. The owner now validates all represented versions.
  Text/whole-response budgets were also raised as a required configuration
  acceptance check rather than assuming the seed's prose lengths are permanent.
- Skill configuration 62 needed protection/range/efficiency/attack cap anchors
  to be zero or at least the original rank-5 boundary. The original attribute
  cap of 4 remains valid. Missing members/policies also needed explicit SQL
  completeness metadata so deletion cannot silently restore legacy-only ranks.
  Both findings were accepted by the rule owner. Pointer review confirmed that
  original skill clones, status applications and passive-trigger projections
  retain the injected immutable progression catalog.
- Store query and transaction wrappers carry the same catalog into bag,
  equipment, change-set, warehouse, stall, mail and family-stash item validation.
  Memory transaction copies retain it too. Endgame commit snapshots are cloned
  before asynchronous work; no new save bypass was found. Unique versions 0/5/6
  remain selected from saved content versions without rewriting UID or roll.
- Rift/loot configuration 63 pins each run to a rule version and its LootVersion.
  Old unversioned runs resolve to published version 1, not the active version;
  supplied saved rule snapshots must match that immutable published version.
  Rule accessors copy nested arrays, and run/persistence clones copy the full
  snapshot. Ordinary scene drops explicitly receive the active loot rules, so
  an additional ItemDef loot pointer is unnecessary. Review also raised finite
  timer bounds, nonzero line length, percentage overflow and the combined live
  response budget. Final acceptance must include the owner's corresponding
  validation tests, including many uncollected globes and live monster entries.
- Shared combat-bucket tests now freeze the old outgoing and incoming arithmetic
  without calling production scaling/reduction/saturation helpers. Source-level
  review compared that oracle to the prior HEAD. Scene tests exercise the actual
  settlement path, including a 30% guard plus 30% physical aura sharing the 45%
  cap, then an independent 30% status layer. Cache injection isolates content
  numbers; those tests do not claim full database-to-equipped-item assembly.

The server was still being assembled during this review. Its final aggregate,
fresh-database and race results must be reported from the final combined tree,
not inferred from the source observations above.
