# Gameplay workbench integration

## Play entry

The native world host negotiates `endgame.v1` after character admission. Transient loading/storage hello failures retry at two-second intervals, with at most three attempts per connection; this is separate from manual mutation retries. There is no F8 shortcut, global endgame launcher, or cross-module tab strip. Existing **巧匠/左慈** dialogue opens separate **星篆 · 镶嵌取出** and **周天鼎 · 问鼎炼化** workbenches; **妖魔结界守卫/结界使** opens **演劫洞天**. Trial NPCs retain key-use advice. An older server has no added dialogue entries and sends no endgame mutations.

- **仙遗与宿阵装备**: inspect, equip and unequip through the existing native bag and character equipment UI. Native tooltips retain rolled attributes, requirements, lore and ordered sockets. Occupied server slots **14–25** are accessible through the character window's compact **其他装备** control, borrowing only the doll area and reusing native slot actions; there is no duplicate equipment dashboard or unique encyclopedia.
- **星篆镶嵌**: use the artisan dialogue, select real equipment, a socket and a bag rune, then insert or extract. Owned rune hover details and active formation status remain. Rune/formation encyclopedia navigation and the cross-module cube shortcut are removed. Opening this workbench never requests unique or formation discovery catalogs; owned-item tooltip metadata still loads as needed.
- **周天鼎**: the artisan opens its own workbench. Choose among 12 actionable operation families. Equipment inputs are selected inside the workbench from real bag items; rune inscription/conversion has a local output chooser, preserving all 28 outputs for each operation and all 66 functional recipes. The server recipe cursor is followed automatically while this workbench is open, without player catalog pagination. A failed metadata page waits for explicit refresh; closing stops continuation and reopening resumes the cursor. For rune salvage choose one bag rune; for conversion/array insertion choose ordered bag rune UIDs; for reordering choose installed runes in the desired order. 问鼎 shows authoritative cost/result/risk; 照此投料 executes only that frozen quote. Selection changes or a new inventory snapshot invalidate it.
- **演劫洞天**: 四村试炼小仙接取试炼；完成领钥后，在妖魔结界守卫对话中选择演劫洞天。队友须在队长附近。Choose difficulty **1–150 阶**, with direct numeric input and ±1/±10 controls. Difficulty is separate from the run’s **第 N 层**. The server’s optional maximum/unlock limits constrain fresh entry. Joining an invitation preserves its run ID; fresh entry leaves the run ID empty. The HUD shows server progress, time, map/floor, current objectives, elite aura colors/affixes and globe markers. Runs expose an exact decimal-string seed and generator version. The optional floor exit is marked in the native world; the run opener can approach it and choose **前往下一层** to skip remaining packs. This preserves difficulty, accumulated progress and the run timer; uncollected globes are not credited. Advance sends the observed run ID and one-based floor index, blocks repeated clicks while uncertain, and uses the normal native map transition. It is unavailable during transitions and the guardian phase. Circle/ring/finite-line attack telegraphs draw translucent danger areas, clear boundaries, ability names, dodge hints and local countdowns from server-authored map pixels. Ring centers remain clear. Warnings expire locally or disappear on the authoritative update; they do not apply damage. Phase changes show a short non-modal notice; restored snapshots do not replay transitions. The HUD opens result review without claiming automatically and yields input to the open panel. Move within 75 pixels of a globe for the server's automatic pickup. Claim is enabled only by the server's `claimable` flag. Interrupted paid runs can expose compensation. Leaving requires a second click.

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

Owned unique and formed runeword tooltips reuse `UITip` and its line protocol. The single centered name is replaced in-place; durability, native requirement rows, intrinsic attributes, and the socket-count row stay in their native order. Legacy unique headers are folded into the native attribute/lore sections, rather than appending a second equipment card. Runeword names omit the leading 宿阵· category prefix. The ordered composition appears immediately below the native equipment-slot heading using only each rune’s first character, such as 角、亢. Socket indexes, repeated runes, native spacing, and full standalone rune names are preserved. Runeword affixes remain in the attribute section; authored lore remains in the description section. Equipment comparison uses the same presentation path.

Unique and runeword names, extra attributes and the existing socket-count row use a dedicated dark-gold quality token (5, RGB 183/155/98). Native intrinsic/base attributes remain white and left-aligned, including explicit quality-0 white rows. Red requirements and disabled-status markers retain their native colors. Existing quality 4 remains yellow for ordinary equipment; unformed socketed equipment keeps its original presentation.

Do not infer suppressed unique powers from two matching item IDs. Server unique effects select the strongest qualifying trait by kind in each combat context and cap combined channels; the snapshot does not carry per-trait suppression. Similarly, a formation's `active=false` and the old aggregate reason can mean unmet requirements or durability, not duplication. Only the explicit server reason `同类宿阵优先级未满足` displays the concise `不生效`; active and bag items show no status commentary. Formation recipe/eligibility data remain server-authoritative.

The optional `endgame.native-stats.v1` hello capability transfers stat-block ownership to the server without adding any closed-schema JSON keys. New clients advertise it; the server echoes it only when requested, after successfully refreshing native bag/equipment descriptions, and then sends the sidecar snapshot. Only after this negotiation does the client stop adding catalog/DTO reference stats to native rows. Older servers keep the legacy overlay; older clients receive the legacy native layout. Reset, failed hello, retry and reconnect clear or gate the capability correctly.

For negotiated special equipment, the server combines actual rune-card and recipe extras with identical attribute/mode/global scope into one row. Local enhancement percentages stay separate; their actual effect is already present in the white base numbers and is never added again as a flat extra. Signed values and percentages stay authoritative. Broken, ineligible and suppressed equipment still displays potential item properties, with the concise authoritative inactivity badge where applicable. No client text-parsing arithmetic is used.

Validation uses the real panel source against deterministic Unity substitutes, including native line-order/deduplication/dark-gold contracts. It is not visual validation, and no Unity Editor or Player build is performed.

Tooltip regression coverage also retains native refinement suffixes, resolves owned formations beyond the first catalog page, anchors socketless unique stats before suit/skill/lore sections, preserves native section/status colors, and distinguishes local equipment enhancement from global attribute percentages. Legacy-server fallback retains independent attribute contributions until native aggregation has been negotiated. Standalone rune lore supplements native instance values without duplicating requirements or replacing those values with catalog reference stats.

Unique and formed runeword equipment show their authored lore without the original base equipment prose. The server omits only the template description while composing the native item tooltip; the client retains requirements, durability, attributes, set/skill effects and binding notices, and adds the authored unique/runeword lore through the existing overlay. The shared bag, equipped/comparison and workbench paths have managed regression coverage. Ordinary equipment, including socketed items that have not formed a runeword, keeps its original description. Formation identity controls the omission even when the item is in the bag, broken or below its activation requirements. No client prose guessing or line filtering is used.

Socket choices reuse the independent artisan workbench and authoritative cube quote surfaces: native 打孔炉 remains incremental, while cube recipe 1006 randomly opens only zero-socket equipment. The server supplies the actual capacity/range, equal probabilities and costs; the client does not calculate or predict them. Formation help describes original-base affix eligibility independently of name color, base attack/defense and later rune/refinement effects; the server remains authoritative.

### Independent module verification

See [the function-by-function entrypoint map](MODULE-ENTRYPOINTS.md). Managed source checks cover the absent F8/global/tab routes; independent NPC windows; all 66 recipe choices through 12 action families; local target/output selection; paginated metadata, failure, close and reopen; and native slots 14–25. `Tests/Endgame/run-additional-equipment-contract.py` executes the actual character-window source with deterministic Unity/network substitutes. These are source/managed contracts, not visual Unity or live-player validation. No original pregame login/selection code, wire operation, functional recipe, plugin test operation or external asset is changed by this correction.
