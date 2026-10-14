# 周天鼎容器

## Portable item entry

The trial quest leads from 试炼小仙 through a completed Greater Rift to 巧匠左慈, who awards a permanent 周天鼎 bag item. There is no scene cube NPC. Right-click its actual bag slot to open the existing 35-cell panel; opening does not send a consumable-use request or start an item cooldown. The server's ordinary UseItem handler also refuses to consume this permanent tool.

The existing hello `result` string map supplies database-owned `cubeItemId`, `cubeNPC` and `cubeLore`. No tool identity or story is hardcoded in the client. Missing/invalid configuration disables entry. Native inventory icon/avatar loading uses the server item definition and installed resources, with no new uploaded art. 左慈 offers one server-authored lore passage at a time, cycling the supplied passages, and `cube.recover` recovery; ordinary first acquisition remains the task protocol. Recovery includes current NPC entity, expected revision and immutable request ID; the server checks quest eligibility, real interaction/range and existing bag/storage ownership. Pending recovery exposes 查询寻回结果 at the artisan and retries the same immutable request instead of issuing another recovery.

## Container semantics

- Thirty-five usable slots (5×7), backed by real authoritative inventory. The original 450×484 `res/uires/composedlg/composedlg.gsa` frame, native cells, title strip and red confirm/cancel/close controls are reused. Native inventory right-click reserves an item; dragging a carried item onto any empty grid cell reserves it in that exact cell. Right-click in the grid removes it. Left-click a material edits its exact positive quantity; left-click an instance removes it.
- The existing inventory opens alongside the workbench; there is no duplicate bag browser. Original title dragging, viewport scaling, modal blocking and world-click suppression remain active. Closing/resetting the cube releases its own in-flight title drag.
- Placing and removing are local reservations, not transfers into a second inventory. Close, cancel, disconnect and map/tool changes cannot lose or duplicate items because nothing has been subtracted.
- Every completed inventory replacement invalidates the selection. An incomplete or native-only inventory replacement disables matching/execution; authoritative UID identity is required again.
- Items use `NetClient.LoadItemIcon` and owned native inventory tooltips. Installed rune ordering is optional input ordering on the selected gear, not a recipe chooser.
- No operation names, recipe list, required-material hints, output selector or automatic recipe catalog paging is shown. The player chooses actual inputs.

## Matching and execution

`cube.quote` adds the bounded `cubeInputs` field (1–35 entries; legacy 12-entry clients remain valid). Each input is either `{uid,count:1}` for an instance or `{itemId,count}` for a stackable material. The client never submits a recipe ID for this flow. The legacy `itemUids` field remains parseable for compatibility; installed-rune reorder no longer has an enabled recipe.

`MatchCubeInputs` iterates the immutable `CubeRuleTable` loaded from PostgreSQL and checks exact selected material quantities, target/source shape and each existing operation's eligibility. No item ID, recipe ID, recipe fee or ingredient amount is used as a formula in the matcher. The snapshot's available material types follow the DB rules too. Startup validates their union with rune/key metadata against the client material-row and bounded byte limits.

62 enabled database recipes remain eligible across eight operation families. Server migration83 disables 1007 (归星还篆), 1008 (移星换斗), 1009 (布宿成阵), and 1012 (炼真守一); historical rows and saved items are preserved. Native socketing still installs runes one at a time and checks formation on the final rune. The 28 inscription definitions have identical input materials; conversion likewise has several same-input eligible outputs. These form equal-probability pools within the same operation and cost. Preview does not select an output or expose a recipe list; it reports the random-result type and fee. Ambiguity across different operations or costs fails closed.

The native 确认合成 control first requests the read-only match. Its response opens the existing confirmation dialog with the full server-authored outcome/risk and cost. Cancelling leaves every reservation intact; confirming rechecks the exact quote, inputs, revision, expiry and portable-item context. A prompt held across inventory replacement, close, map change or tool loss cannot commit.

Execution sends only the quote identity and expected revision, with no NPC identity. The server independently rechecks the configured bound permanent tool in the bag and player prerequisites for quote and execution. Client presentation never grants authority. The quote binds the immutable rules and catalogue snapshots; replacing either invalidates it before execution. Original inventory fingerprints, locks, capacity and costs are rechecked before atomic commit. A definite storage retry retains the identical candidate and generated UID; persistent receipt replay preserves exact-request idempotency across reconnects.

The native container closes and invalidates reservations/quote on map departure, connection generation change, changed configured tool, loss, movement or locking of the captured bag slot. Existing pending transactions remain immutable and can be retried. The tool itself cannot be reserved as an ingredient. The historic recipe-addressed wire remains internal/backward-compatible. The container and plugin adapter both send exact `cubeInputs`; neither exposes a recipe catalogue or relies on a scene workbench.

## Validation

- `Tests/PluginRuntime/run-game-actions.sh`: endgame adapter checks for exact inputs, bounded shapes, current portable tool, hidden recipes and immutable retry identity.
- `Tests/Endgame/run-module-contract.sh`: container/module checks, plus source integration assertions.
- `Tests/Endgame/run-native-cube-entry-contract.py`: checks using the production inventory right-click handler and native title-drag implementation, including rejection fallthrough, native lock priority, close and viewport reset.
- `Tests/Endgame/run-ui-contract.sh`: matched risk/fee, no recipe addressing, pending/cancel and repeated-action coverage in `SocketRecipeContracts`.
- Server `TestCubeInputs*`: historical recipe mechanics plus retired-recipe rejection, same-input pools, configured cost changes, ambiguous operations, duplicate/foreign/locked inputs, capacity and no mutations during match.
- Server `TestEndgameCubeInputs*`: portable item ownership, opaque nonrandom preview, durable commit, storage failure retry and reconnect receipt replay.
- Server `TestEndgameCubeInputClosedSchema`: nested keys, canonical Int64 strings, mutually exclusive identity forms and operation scope.
- Server portable cube quest/recovery tests verify initial award, lost-tool recovery, storage ownership and NPC prerequisite guards.

Real Unity world clicking/rendering is not certified by managed contracts or the resource composition preview.
