# 太清演劫录 client integration

## Play entry

The native world host negotiates `endgame.v1` after character admission. Transient loading/storage hello failures retry at two-second intervals, with at most three attempts per connection; this is separate from manual mutation retries. The existing **妖魔结界守卫** dialogue opens **演劫洞天**, and **巧匠/左慈** opens **周天鼎**. Trial NPCs offer optional key-use advice. **F8** or **演劫 · F8** remains a portable convenience entry. An older server displays a disabled entry rather than sending mutations.

- **仙遗**: choose a real bag/equipped item; inspect gold name, server trait, rolled stats and ordered sockets. Equip and unequip use existing native commands. Equipped slots **14–25** appear here even though the original paperdoll has only 13 slots. 图鉴 opens the paged unique catalog.
- **星篆 · 宿阵**: select equipment, a socket and a rune from the bag, then insert or extract. Owned runes appear with real stats/lore on hover. Rune and formation catalogs are explicit opt-in views; formation discovery shows ordered runes, level/channel/slot requirements, bonuses and bag-rune availability. Current active formations are separate from the discovery catalog.
- **周天鼎**: choose a recipe and real inputs. For rune salvage choose one bag rune; for conversion/array insertion choose ordered bag rune UIDs; for reordering choose the installed runes in the desired order. 问鼎 is a server quote, showing costs and the result/risk description. 照此投料 executes that quote; selection changes or a new inventory snapshot invalidate it. A quote is not a success or an inventory change.
- **演劫洞天**: 四村试炼小仙接取试炼；完成领钥后，在妖魔结界守卫对话中选择演劫洞天。队友须在队长附近。Choose tier 1–50. Joining an invitation preserves its run ID; fresh entry leaves the run ID empty. The HUD shows server progress, time, current objectives, elite aura colors and globe markers. Phase changes show a short non-modal notice; restored snapshots do not replay transitions. The HUD opens result review without claiming automatically and yields input to the open panel. Move within 75 pixels of a globe for the server's automatic pickup. Claim is enabled only by the server's `claimable` flag. Interrupted paid runs can expose compensation. Leaving requires a second click.

Close/Escape clears selections and quotes but keeps an uncertain mutation. **查询原请求** resends the same immutable request ID and parameters, never a new purchase or reroll. Disconnect/title return clears visible state. A bounded, process-memory retry journal restores only the same endpoint/account/server-issued persistent character ID's uncertain request after a new hello; it never automatically executes it. **The journal does not survive quitting the client process.** Server receipts remain authoritative.

## Addons and external agents

The existing shared Game API now projects this same native owner through `endgame`, `endgame.items`, `endgame.catalog` and `endgame.rift`. Guarded `endgame.*` actions cover refresh/catalog pages, sockets, server quotes and execution, rift entry/claim/leave, exact uncertain-request retry, and native equip/unequip. Decimal UID/revision strings, ordered rune arrays, complete-snapshot publication, current run IDs and server-authoritative receipts retain their native semantics. See [the endgame API guide](../Plugins/ENDGAME.md). The external bridge remains explicit opt-in and loopback-only.

## Pairing and installation

Use this client branch with the matching server `feature/daoist-endgame` implementation, endgame migration/catalog data and server feature configuration. Existing native packets retain their layouts. The new sidecar is request opcode `0x10d0`, response `0x80d0`, then little-endian U16 schema **1**, U16 UTF-8 byte length and JSON, limited to **60 KiB**. The DTO authority is the paired server's `server/internal/domain/endgame_contract.go`.

UIDs and revisions are decimal strings; snapshot/run/quote IDs and catalog cursors are bounded strings. Native bag tabs are zero-based 0–3 and bag slots are page-local. Snapshots have zero-based chunk indices, at most 32 item rows per chunk and 128 chunks (4096 rows). Only a complete ordered snapshot replaces the immutable view. Catalog pages use an empty first/terminal cursor and are bounded to 4096 rows per catalog. Invalid, mixed, stale or incomplete snapshots cannot partially mutate displayed inventory.

The new panel uses existing `ArtworkSkin`, `NpcChatter.CnFont`, item icons, compose artwork and external asset paths. No asset bundle or external resource copy is included. Install the same external art/font resources required by the existing client. `.meta` files accompany every new Assets source. The panel is attached at runtime by `NativeNetClientHost`; no scene edit is needed.

JSON parsing lives once in Foundation. Existing plugin API/host JSON entry points remain forwarding compatibility facades; no assembly cycle or second codec was added.

## Verification

Run from the repository root:

```sh
FFO_MONO_ROOT=/path/to/MonoBleedingEdge Tests/Endgame/run-contracts.sh
FFO_MONO_ROOT=/path/to/MonoBleedingEdge Tests/Endgame/run-ui-contract.sh
FFO_MONO_ROOT=/path/to/MonoBleedingEdge Tests/Endgame/run-world-entry-contract.sh
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

The managed compile script records exact source lists and compiler logs in `FFO_COMPILE_DIR`. It compiles all 642 actual sources across seven assemblies (5 Foundation, 1 Domain, 147 Application, 221 Protocol, 9 PluginApi, 248 MoonSharp, 11 PluginHost), with no substitutes. Host integration then compiles the actual `NativeNetClientHost` and its actual interface against those assemblies; only Unity and presentation capabilities are substituted. It executes the real gateway's connected/host-thread gates, native encrypted send queue, main receive dispatch, receipt retry, reconnect scope and title fence using a local loopback connection. It does not connect to an external server or start networking background loops.

Recorded final counts: **116** codec/state/production-Go-fixture assertions, **26** actual-panel action assertions with Unity substitutes, **38** actual host/gateway/crypto integration assertions, **80** source/metadata checks and **112** Go/client DTO field-type checks. Existing plugin checks also passed: 723 contracts, 38 actions, 39 addon UI, 58 real-Lua runtime and 83 transport assertions plus Python client integration; existing artwork checks passed 32 assertions.

`Fixtures/full-fields.*` was emitted by production `protocol.EncodeEndgame` with every response DTO field populated. To regenerate it, copy `Tests/Endgame/generate-full-fixture.go` into a temporary command directory under the paired server's `server/cmd`, run that directory with the absolute client `Tests/Endgame/Fixtures` output path, then remove the temporary directory. The other base fixtures are generated by the server's `cmd/endgame-fixtures`; the prefixed audit fixtures came from its protocol-storage audit generator. All fixtures are synthetic text JSON/hex.

Remaining verification: full Unity-dependent Presentation/Bootstrap/Editor assembly compilation with real Unity references, rendered layout/art/font availability, a live client/server play session and a Player build were **not run**. The panel/host substitutes do not establish those outcomes.

Profession requirements are native bitmasks (1 战士, 2 剑客, 4 刺客, 8 药师, 16 术士), not ordinal class IDs. A zero mask keeps the native-restriction fallback. Unique catalog potency is labeled 图鉴基准 when supplied; owned UID-bearing items show the actual server roll. Snapshot material `count` is the wallet total; quote material `count` is required and `owned` is available.

## World integration pass

See [design, official Diablo references and validation](WORLD-INTEGRATION.md). This pass changes client presentation only; there is no migration or wire schema change. Native tooltip, dialogue and artwork capabilities are reused. Exact tier-unlock and per-run reward summaries are not exposed by the existing DTO, so the UI does not fabricate them.
