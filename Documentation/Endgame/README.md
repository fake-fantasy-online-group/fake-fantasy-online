# Gameplay workbench integration

## Play entry

The native world host negotiates `endgame.v1` after character admission. Transient loading/storage or send failures retry after a two-second cooldown, with at most three hello attempts per connection. A missing response (including a read-only hello interrupted by a map transfer) waits 15 seconds before entering that cooldown; disconnect clears the timer and retry budget. A failed refresh after successful negotiation preserves the accepted renderer. This is separate from manual mutation retries. There is no F8 shortcut, global endgame launcher, or cross-module tab strip. Star runes use the original **镶嵌炉** drag-to-empty-hole flow, with no standalone insertion/extraction workbench; the original doorway behind **左慈** leads to the **周天鼎** in 商会; **龙城八仙使者** opens **演劫洞天**. Trial NPCs retain key-use advice. An older server has no added dialogue entries and sends no endgame mutations.

- **仙遗与宿阵装备**: inspect, equip and unequip through the existing native bag and character equipment UI. Native tooltips retain rolled attributes, requirements, lore and ordered sockets. Occupied server slots **14–25** are accessible through the character window's compact **其他装备** control, borrowing only the doll area and reusing native slot actions; there is no duplicate equipment dashboard or unique encyclopedia.
- **星篆镶嵌**: open the original 镶嵌炉, drag bag equipment into its equipment slot, then drag a bag star rune into an open empty socket. The existing native furnace packet and authoritative socket receipt are retained. Invalid/stale bag inputs, unopened holes and occupied holes cannot submit. There is no extra 星篆 insertion/extraction dialog or plugin removal action. Native owned-rune tooltips and formation metadata remain.
- **周天鼎**: enter the existing 商会 interior through the native doorway behind 左慈, then talk to the dedicated 周天鼎 (original NPC sprite 903). Its 12-slot container accepts owned bag instances and exact stackable quantities, with the existing icon/tooltip path. It never loads or shows recipe/operation/output lists. The server matches `cubeInputs` against database rules; identical-input variants of one operation are equally likely, with no random roll during preview. 问鼎 reports matched result/risk and copper, then 炼化 executes the frozen quote atomically. Placing, removing, closing and reconnecting never move physical items. See [container contract](CUBE-CONTAINER.md).
- **演劫洞天**: 四村试炼小仙接取试炼；完成领钥后，在龙城八仙使者对话中选择演劫洞天。队友须在队长附近。Choose difficulty **1–150 阶**, with direct numeric input and ±1/±10 controls. Difficulty is separate from the run’s **第 N 层**. The server’s optional maximum/unlock limits constrain fresh entry. Joining an invitation preserves its run ID; fresh entry leaves the run ID empty. The HUD shows server progress, time, map/floor, current objectives, elite aura colors/affixes and globe markers. Runs expose an exact decimal-string seed and generator version. The optional floor exit is marked in the native world; the run opener can approach it and choose **前往下一层** to skip remaining packs. This preserves difficulty, accumulated progress and the run timer; uncollected globes are not credited. Advance sends the observed run ID and one-based floor index, blocks repeated clicks while uncertain, and uses the normal native map transition. It is unavailable during transitions and the guardian phase. Circle/ring/finite-line attack telegraphs draw translucent danger areas, clear boundaries, ability names, dodge hints and local countdowns from server-authored map pixels. Ring centers remain clear. Warnings expire locally or disappear on the authoritative update; they do not apply damage. Phase changes show a short non-modal notice; restored snapshots do not replay transitions. The HUD opens result review without claiming automatically and yields input to the open panel. Move within 75 pixels of a globe for the server's automatic pickup. Claim is enabled only by the server's `claimable` flag. Interrupted paid runs can expose compensation. Leaving requires a second click.

Close/Escape clears selections and quotes but keeps an uncertain mutation. **查询原请求** resends the same immutable request ID and parameters, never a new purchase or reroll. Disconnect/title return clears visible state. A bounded, process-memory retry journal restores only the same endpoint/account/server-issued persistent character ID's uncertain request after a new hello; it never automatically executes it. **The journal does not survive quitting the client process.** Server receipts remain authoritative.

## Addons and external agents

The existing shared Game API now projects this same native owner through `endgame`, `endgame.items`, `endgame.catalog` and `endgame.rift`. Guarded `endgame.*` actions cover refresh/catalog pages, sockets, server quotes and execution, rift entry/advance/claim/leave, exact uncertain-request retry, and native equip/unequip. Decimal UID/revision strings, ordered rune arrays, complete-snapshot publication, current run IDs and server-authoritative receipts retain their native semantics. See [the endgame API guide](../Plugins/ENDGAME.md). The external bridge remains explicit opt-in and loopback-only.

## Pairing and installation

Use this client branch with the matching server `feature/daoist-endgame` implementation, endgame migration/catalog data and server feature configuration. Existing native packets retain their layouts. The new sidecar is request opcode `0x10d0`, response `0x80d0`, then little-endian U16 schema **1**, U16 UTF-8 byte length and JSON, limited to **60 KiB**. The DTO authority is the paired server's `server/internal/domain/endgame_contract.go`.

UIDs and revisions are decimal strings; snapshot/run/quote IDs and catalog cursors are bounded strings. Native bag tabs are zero-based 0–3 and bag slots are page-local. Snapshots have zero-based chunk indices, at most 32 item rows per chunk and 128 chunks (4096 rows). Only a complete ordered snapshot replaces the immutable view. Catalog pages use an empty first/terminal cursor and are bounded to 4096 rows per catalog. Invalid, mixed, stale or incomplete snapshots cannot partially mutate displayed inventory.

The independent workbenches use existing `ArtworkSkin`, `NpcChatter.CnFont`, item icons, compose artwork and external asset paths. No asset bundle or external resource copy is included. Install the same external art/font resources required by the existing client. `.meta` files accompany every new Assets source. Their shared server-binding component is attached at runtime by `NativeNetClientHost`; no scene edit is needed.

JSON parsing lives once in Foundation. Existing plugin API/host JSON entry points remain forwarding compatibility facades; no assembly cycle or second codec was added.

## Verification

Run from the repository root:

```sh
FFO_MONO_ROOT=/path/to/MonoBleedingEdge Tests/Endgame/run-contracts.sh
FFO_MONO_ROOT=/path/to/MonoBleedingEdge Tests/Endgame/run-ui-contract.sh
FFO_MONO_ROOT=/path/to/MonoBleedingEdge Tests/Endgame/run-world-entry-contract.sh
FFO_MONO_ROOT=/path/to/MonoBleedingEdge Tests/Endgame/run-module-contract.sh
FFO_MONO_ROOT=/path/to/MonoBleedingEdge Tests/Endgame/run-loot-context-contract.sh
```

Both also accept the existing `UNITY_EDITOR_DIR` convention. These scripts run managed tests only and do not launch Unity or build a Player.

- Codec/state contracts execute actual production Foundation, endgame Application and wire codec sources. Text-only fixtures were generated by the paired server's production Go `EncodeEndgame` (`server/cmd/endgame-fixtures`), including positive 64-bit UIDs above 2^53, max-int64 rune UIDs, tab 0, equipped slot 25, two-chunk publication, empty snapshots, catalog and mutation receipts. Additional audit fixtures cover active formations with no items.
- Panel contracts execute all actual `EndgamePanel` partials against explicitly documented deterministic Unity/artwork/native substitutes. They cover commands, quote-only execution, material display, repeated clicks, uncertain retry, close, invitation polling, claim/reentry/closed transitions and disconnect. They do not establish pixel layout, native rendering, collision, live networking or game balance.
- Source contracts verify actual gateway/bootstrap/tooltip hooks and Unity metadata. They supplement, rather than replace, compilation.
- The cloud rebuild compiled the complete original Foundation, Domain, Application, Protocol, PluginApi, MoonSharp and PluginHost assemblies with official Roslyn 4.8 and Mono 6.8 references, without stubs. The entire modified gateway is included. Unity-dependent assembly compilation and rendered/Player validation are separate checks and were not established by those seven assemblies.

Existing plugin regression scripts and `Tests/UiArtwork/run-artwork-contract.sh` remain applicable. Test fixtures contain synthetic game data, not account credentials.

### Reproduce the complete managed and host checks

Prerequisites: a working Mono runtime plus `mcs.exe`, .NET Framework 4.7.2 reference assemblies (including Facades), and Roslyn `csc.exe` supporting C# 9. The cloud check used official Debian bookworm Mono 6.8 packages (including `mono-devel` reference assemblies) and the official NuGet `Microsoft.Net.Compilers.Toolset` 4.8.0 package (`tasks/net472/csc.exe`). Dependencies include System.Xml.Linq, System.Net.Http and System.Runtime.Serialization. No compiler/runtime binary is committed. `FFO_MONO_ROOT` must expose `bin/mono` and `lib/mono/4.5/mcs.exe`; an existing Unity Editor's MonoBleedingEdge directory is compatible, but the scripts do not start it. Older `mcs` alone cannot compile the existing C# 9 Application sources.

```sh
export FFO_MONO_ROOT=/path/to/MonoBleedingEdge
export FFO_CSC=/path/to/Microsoft.Net.Compilers.Toolset/tasks/net472/csc.exe
export FFO_FRAMEWORK_DIR=/path/to/mono/4.7.2-api
export FFO_COMPILE_DIR=/tmp/ffo-managed-compile
Tests/Endgame/run-managed-compile.sh
Tests/Endgame/run-host-contract.sh
python3 Tests/Endgame/compare-go-dto.py /path/to/paired-server/server/internal/domain/endgame_contract.go
```

The managed compile script records exact source lists and compiler logs in `FFO_COMPILE_DIR`. It compiles all 643 actual sources across seven assemblies (5 Foundation, 1 Domain, 147 Application, 221 Protocol, 10 PluginApi, 248 MoonSharp, 11 PluginHost), with no substitutes. Host integration then compiles the actual `NativeNetClientHost` and its actual interface against those assemblies; only Unity and presentation capabilities are substituted. It executes the real gateway's connected/host-thread gates, native encrypted send queue, main receive dispatch, receipt retry, reconnect scope and title fence using a local loopback connection. It does not connect to an external server or start networking background loops.

Recorded final counts: **116** codec/state/production-Go-fixture assertions, **26** actual-panel action assertions with Unity substitutes, **38** actual host/gateway/crypto integration assertions, **80** source/metadata checks and **112** Go/client DTO field-type checks. Existing plugin checks also passed: 723 contracts, 38 actions, 39 addon UI, 58 real-Lua runtime and 83 transport assertions plus Python client integration; existing artwork checks passed 32 assertions.

`Fixtures/full-fields.*` was emitted by production `protocol.EncodeEndgame` with every response DTO field populated. To regenerate it, copy `Tests/Endgame/generate-full-fixture.go` into a temporary command directory under the paired server's `server/cmd`, run that directory with the absolute client `Tests/Endgame/Fixtures` output path, then remove the temporary directory. The other base fixtures are generated by the server's `cmd/endgame-fixtures`; the prefixed audit fixtures came from its protocol-storage audit generator. All fixtures are synthetic text JSON/hex.

Remaining verification: full Unity-dependent Presentation/Bootstrap/Editor assembly compilation with real Unity references, rendered layout/art/font availability, a live client/server play session and a Player build were **not run**. The panel/host substitutes do not establish those outcomes.

Profession requirements are native bitmasks (1 战士, 2 剑客, 4 刺客, 8 药师, 16 术士), not ordinal class IDs. A zero mask keeps the native-restriction fallback. Legacy servers can show the supplied unique potency as 图鉴基准 or owned 道意. Negotiated native-stat servers suppress that universal multiplier label because only explicitly variable properties roll; the displayed properties already contain the actual values. Snapshot material `count` is the wallet total; quote material `count` is required and `owned` is available.

## World integration pass

See [design, official Diablo references and validation](WORLD-INTEGRATION.md). This pass changes client presentation only; there is no migration or wire schema change. Native tooltip, dialogue and artwork capabilities are reused. Exact tier-unlock and per-run reward summaries are not exposed by the existing DTO, so the UI does not fabricate them.

## Seeded multi-floor validation

The extended DTO keeps `tier` (1–150) independent of one-based `floorIndex`, preserves a full uint64 `seed` as text, and accepts omitted floor fields from older fixtures. The paired server emits optional `generatorVersion`, `mapName`, `canAdvance`, `transitioning`, `exitX`/`exitY`, `maxTier`/`unlockedTier`, aura `affixes` and `telegraphs`. Danger geometry uses `shape` (`circle`, `ring`, `line`), locked map-pixel coordinates, outer/inner radii, and `remainingMS` until the server-owned impact. For a line, radius is its half-width and ends are flat.

Focused checks additionally cover tier 150 versus invalid 151, max-uint64 seed preservation/rejection, independent floor guards, exact uncertain advance retry, double-click/close flows, restored/advanced-floor notices, server unlock ceilings, transition/guardian gating, bounded API telegraph paging, all three rendered-warning code paths, local expiry and transition suppression. The tests execute production panel/API code against deterministic Unity/native substitutes. They are **not** rendered Unity validation or a live play/balance test. Existing installed artwork/font resources are reused; no new external asset is required.

### Native equipment tooltip presentation

Owned unique and formed runeword tooltips reuse `UITip` and its line protocol. The single centered name is replaced in-place; durability, native requirement rows, intrinsic attributes, and the socket-count row stay in their native order. Legacy unique headers are folded into the native attribute/lore sections, rather than appending a second equipment card. Runeword names omit the leading 宿阵· category prefix. The ordered composition is centered on its own line immediately below the native equipment-slot heading using only each rune’s first character without separators, such as 角亢. Its line-scoped center marker does not change the white, left-aligned intrinsic attributes. Socket indexes, repeated runes, native spacing, and full standalone rune names are preserved. Runeword affixes remain in the attribute section; authored lore remains in the description section. Equipment comparison uses the same presentation path.

Unique and runeword names, extra attributes and the existing socket-count row use a dedicated dark-gold quality token (5, RGB 183/155/98). Native intrinsic/base attributes remain white and left-aligned, including explicit quality-0 white rows. Red requirements and disabled-status markers retain their native colors. Existing quality 4 remains yellow for ordinary equipment; unformed socketed equipment keeps its original presentation.

Do not infer suppressed unique powers from two matching item IDs. Server unique effects select the strongest qualifying trait by kind in each combat context and cap combined channels; the snapshot does not carry per-trait suppression. Similarly, a formation's `active=false` and the old aggregate reason can mean unmet requirements or durability, not duplication. Only the explicit server reason `同类宿阵优先级未满足` displays the concise `不生效`; active and bag items show no status commentary. Formation recipe/eligibility data remain server-authoritative.

The optional `endgame.native-stats.v1` hello capability transfers stat-block ownership to the server without adding any closed-schema JSON keys. New clients advertise it. Session treats the base capability as transport opt-in and forwards the original hello to the scene rather than acknowledging it itself; the scene echoes the requested native-stat capability only after successfully refreshing native bag/equipment descriptions, and then sends the sidecar snapshot. The event sink may omit an unchanged inventory delta on repeated negotiation. Only after this negotiation does the client stop adding catalog/DTO reference stats to native rows. Older servers keep the legacy overlay; older clients receive the legacy native layout. Reset, failed hello, retry and reconnect clear or gate the capability correctly.

For negotiated special equipment, the server combines actual rune-card and recipe extras with identical attribute/mode/global scope into one row. Local enhancement percentages stay separate; their actual effect is already present in the white base numbers and is never added again as a flat extra. Signed values and percentages stay authoritative. Broken, ineligible and suppressed equipment still displays potential item properties, with the concise authoritative inactivity badge where applicable. No client text-parsing arithmetic is used.

Validation uses the real panel source against deterministic Unity substitutes, including native line-order/deduplication/dark-gold contracts. It is not visual validation, and no Unity Editor or Player build is performed.

Tooltip regression coverage also retains native refinement suffixes, resolves owned formations beyond the first catalog page, anchors socketless unique stats before suit/skill/lore sections, preserves native section/status colors, and distinguishes local equipment enhancement from global attribute percentages. Legacy-server fallback retains independent attribute contributions until native aggregation has been negotiated. Standalone rune lore supplements native instance values without duplicating requirements or replacing those values with catalog reference stats.

Unique and formed runeword equipment show their authored lore without the original base equipment prose. The server omits only the template description while composing the native item tooltip; the client retains requirements, durability, attributes, set/skill effects and binding notices, and adds the authored unique/runeword lore through the existing overlay. The shared bag, equipped/comparison and workbench paths have managed regression coverage. Ordinary equipment, including socketed items that have not formed a runeword, keeps its original description. Formation identity controls the omission even when the item is in the bag, broken or below its activation requirements. No client prose guessing or line filtering is used.

Socket choices reuse the independent artisan workbench and authoritative cube quote surfaces: native 打孔炉 remains incremental, while cube recipe 1006 randomly opens only zero-socket equipment. The server supplies the actual capacity/range, equal probabilities and costs; the client does not calculate or predict them. Formation help describes original-base affix eligibility independently of name color, base attack/defense and later rune/refinement effects; the server remains authoritative.

### Independent module verification

See [the function-by-function entrypoint map](MODULE-ENTRYPOINTS.md). Managed source checks cover the absent F8/global/tab routes; independent NPC windows; all 66 recipe choices through 12 action families; local target/output selection; paginated metadata, failure, close and reopen; and native slots 14–25. `Tests/Endgame/run-additional-equipment-contract.py` executes the actual character-window source with deterministic Unity/network substitutes. These are source/managed contracts, not visual Unity or live-player validation. No original pregame login/selection code, wire operation, functional recipe, plugin test operation or external asset is changed by this correction.

The native-stat negotiation regression enters through the server’s real Session.OnPacket and continues through Router, Scene, eventSink and wire encoding. It checks initial legacy/new-client hellos, capability changes, bag/equipped same-scope sums, separate flat/global-percent/item-local rows, refresh-before-hello ordering, pending/storage/refresh failures, retry and rejection of optional capability without the base protocol. Client managed contracts additionally cover bounded missing-response/send-failure recovery, late success, failed rehello, reconnect reset and the centered separator-free composition. These checks do not replace the local rendered-client acceptance check.

## V5 growth and native skills

The hello advertises `endgame.growth.v5`. When accepted, `skill.ranks` provides
server-authored hard/granted/soft/effective ranks, point balance, eligibility,
blocked reasons, and exact current/next effect lines. The rank strings remain
canonical decimal text, including values beyond Int64; malformed/numeric ranks
are rejected, never capped. Only complete, non-stale rank snapshots replace the
view. The original skill window, artwork and life-skill behavior remain in place.
Its number and original tooltip level heading use the exact effective rank.
Hard-point, granted-rank and equipment-rank diagnostics remain available through
the agent API without adding replacement rows to the original card. Very long
icon labels are shortened with an ellipsis; the tooltip and agent observation
preserve all digits. Descriptions retain the original authored sentences, with
the server supplying their current numeric values instead of stale rank-5 values.

The plus control uses the authoritative investment flag, remaining point balance,
revision, and pending-operation guard. Native UI and session-coordinator upgrades
send `skill.upgrade` with `skillId`, immutable `requestId` and `expectedRevision`.
A receipt does not optimistically change rank or points: the following authoritative
rank push does that. The existing bottom skill-point label becomes “查询原请求” for
an uncertain skill investment; clicking resends that exact command. Stale or absent
rank data uses “刷新技能” in that same location. Disconnect clears the rank view,
while the existing scoped retry journal retains an uncertain investment.
The frozen 149-skill profession membership selects this behavior. Starter
13601/13602, life skills and older servers retain their original native routes.

`Tests/Endgame/run-growth-contract.sh` executes production V5 DTO/state and native
tooltip-helper code with minimal presentation substitutes. The actual host/gateway
check covers V5 wire routing and no-repeat admission; existing action tests cover
agent rank observations, investment receipts and exact retry. These checks do not
establish Unity rendering or a Player build.

The V5 sidecar also decodes typed `combatStats` from snapshot chunk0 and independent
`combat.stats` pushes. The shared agent API exposes raw server equipment channels,
aura membership and proc readiness, without tooltip parsing or local caps. Complete
snapshot publication and a combat observation version protect against stale or
interleaved updates. Tests cover Int64 numeric fields, uint32 entity IDs, decimal
source UIDs, partial snapshots, same-revision pushes, reset and read-only projection.

### V5 skill tooltip readability and training sources

The restoration baseline is client `ababca5` (before V5), specifically
`Win05.SkillDialog.BuildSkillTip`, `SkillTipText`, `Win05.SkillLevelBlock`,
`Win05.SkillDamageBonus` and `Win05.SkillTipText`. The original description block,
not just the headings, is the foundation: blue name, centered current level, full
authored prose, and active-skill `消耗:`, `技能吟唱:`, `技能间隔:` lines. The next
investment repeats its complete description and all three detail rows beneath
`下一等级`, followed by the native green requirement/passive lines, blank row,
original orange remaining-points/eligibility footer and active-skill hotbar hint.
No hard/soft/granted-rank diagnostic rows replace or precede the original prose.
Pending/stale feedback appends to the original remaining-points line.

The full current and next blocks are server-authoritative, including ranks above
20. The next-investment section disappears at hard20. An unlearned skill shows
the complete first-investment preview once under `等级: 未学习`, matching the
original layout. `nextRequiredLevel` supplies the native requirement row.
`nextChangeLines` remains accepted for protocol compatibility but is never used
by the card: unchanged effects and costs are not deleted from the next block.

One green incoming-training section is appended after the complete original card.
Only the recipient lists each source skill, its authoritative effective level
and its server-authored per-level bonus, including zero-level planning sources.
Eligible equipment levels count in the source rank when the server marks the row
`equipmentApplies=true`; equipment alone does not teach an unlearned source.
The 3–6% rate comes from the beneficiary's learning tier on the server. Legacy
hard-only rows still display their allocated points and per-point rate.
No generic equipment explanation or generated outgoing-target list is added to
the source skill. Its original authored passive description remains intact.
The same typed data is available through `endgame.skills`; older optional metadata
remains compatible. No equipment tooltip composition is changed by this fix.

Cards that fit the viewport use the original native renderer without any layout
or scale change. Oversized profession cards retain the same width, font, colors,
row order and frame, with a gray `滚轮查看` affordance and row scrolling. Scrolling
while hovering the skill reveals every original row instead of truncating the
text or shrinking the font. It takes priority over tree scrolling only when that
hovered card actually overflows. Leaving the hover, closing it, switching to a
different card or showing an item resets the scroll. Native equipment/comparison
`Render`, `Layout` and `DrawBox` bodies are byte-identical to the prior build.

`Tests/Endgame/run-skill-tooltip-contract.sh` compiles the actual growth card,
`ComposeTip`, and native `UITip` layout/render code using documented deterministic
Unity/art/font substitutes. It checks emitted rows, palette, wrapping and draw
coordinates, supplemented by starter/life routing source guards. This is a
production rendering-code contract, not a Unity screenshot, installed-font test
or Player build. `run-growth-contract.sh` covers strict additive DTO validation;
`run-game-actions.sh` covers the read-only agent projection and existing upgrade/
retry semantics.

The synthetic `skill-tooltip-all149-v5.json` fixture comes from the paired server's
actual `skillRanksForDisplay` for all 149 profession skills; the adjacent names
map comes from its verified skill catalog. `skill-tooltip-usable-v5.json` records
active/passive hint admission from the original skill inventory, rather than
pretending that all 149 skills are active. The harness executes production
`UIScale` at 1280×900/96-DPI (1×) and 1920×1080/unknown-DPI (2×), then checks actual
native draw-box bounds, complete current/next prose/detail retention, pending/stale states and
a separately labeled soft-rank layout stress case. It does not weaken the scale
to make cards fit. The `skill-ranks-synergies-v5` JSON/hex pair is emitted through
production `protocol.EncodeEndgame` and also reaches the actual client wire codec.
The server test export options are `SKILL_ALL_VIEWS_FIXTURE_PATH` with
`TestV5All149ReadableSkillViewsFixture`, and `SKILL_SYNERGY_FIXTURE_PATH` with
`TestV5TrainingSynergyRowsAreAuthoritativeIncludingUnlearnedSources`.

The original V5/V7/V9 authored fixtures remain historical compatibility inputs.
Equipment-aware synergy tests separately cover the additive decimal-string
`sourceEffectiveRank`, exact Int64 contribution validation, zero effective source
rank, every 3–6% rate, and native centered rendering without changing the original
body or adding an outgoing list. The plugin projection exposes the same source
rank and Int64 total without calculating from the receiving skill's equipment.
The separate `skill-synergy-equipped-v11.json`/`.wire.hex` pair is exported by
`TestEquipmentSynergyPostgres` via `SKILL_EQUIPMENT_SYNERGY_FIXTURE_PATH`, using
migration74 PostgreSQL catalogs and authored item2018. Its Warlock rows cover
3%, 4% and 6% beneficiary tiers, source hard3 plus equipped2 becoming effective5,
and an unlearned source at zero. The 5% display rate and Int64 boundary are
separately labeled synthetic client cases; the server covers all profession tiers.
Migration74's equipment-aware rows require the matching updated client: older
clients reject the new optional field and equipment-aware mode in their closed
DTO parser. The wire framing, schema number and opcodes are unchanged.

### Equipment skill forms V1

The client advertises `endgame.skill-variants.v1`, usable only alongside
`endgame.v1`, `endgame.growth.v5`, and `endgame.skill-behavior.v1`.
`contract.current/next.variant` is optional and, when present, contains exactly
`id`, `name`, `sourceUID` (canonical decimal string), `item`, `description`, and
`maxTargets`. Metadata is immutable and size-bounded. `typedEffects.variantId`
is server-side catalog input, not a new client item wire field.

Costs, timings, input mode and geometry continue to come from that behavior's
single server plan. Skill identity, tree membership and authored text are retained;
the existing effect rows carry the form text once per current/next block. There
is no extra equipment-form panel. Cap changes discard old rank snapshots; a
newer observed revision blocks stale behavior and form text until replacement.
An open targeting cursor re-reads its footprint and cancels if the input mode
changes. Unequipping restores ordinary behavior through the next rank snapshot.

`run-skill-variants-contract.sh` executes production DTO/state and targeting
projection with five Go-generated fixtures, capability dependency/downgrade,
lossless UID, invalid/bounded fields, same-revision equipment replacement, stale
revision, and unequip tests. `run-skill-tooltip-contract.sh` additionally renders
those forms through the existing native tooltip composition with deterministic
Unity/font substitutes. These are managed contracts, not a Unity/Player run.

Equipped tooltip identity is location + equipment slot + item ID; the snapshot retains the item’s inventory category in `tab`, which is not an equipped-slot address. Bag lookup still requires the exact tab. Character slots (including slots 14–25), comparison, workbench and generic drag previews reuse the same overlay. Managed regressions move the same UID from bag to worn across categories 0–3 and slots 1/13/25, covering legacy/native-stat negotiation, ordinary fallback, missing/stale native rows and disconnect reset. These are deterministic contracts, not rendered Unity acceptance.

Orange special effects end the equipment attribute section immediately after ordinary properties. Native effects are gathered from their prior positions and inserted beside the other attributes; sockets, stories, requirements and binding sections cannot separate the last property from its effects. Authored stories and native functional/binding text remain outside that block. The same rule applies to bag, equipped, comparison and workbench previews; regressions cover scattered native effect rows, intervening story/requirement/durability metadata, missing sockets and repeated overlays.
