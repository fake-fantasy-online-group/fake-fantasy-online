# 周天鼎容器

## World entry

The original door behind 巧匠左慈 in 龙城 (`qw0007` proc5) leads to the existing 商会 interior (`qw0506`, map506). Migration75 places a distinct 周天鼎 NPC using verified original sprite903 at `(620,430)`, script `Nzhoutianding`. The old artisan and furnace services remain separate. `DialogUI.Cube.cs` uses the current world NPC identity, map and connection generation; its one dialogue option opens `TryOpenCubeFromNpc`. No artisan/global shortcut or recipe catalog is available in this workbench. Older servers that do not echo `endgame.cube-inputs.v1` do not expose this entry.

## Container semantics

- Thirty-five usable slots (5×7), backed by real authoritative inventory. The original 450×484 `res/uires/composedlg/composedlg.gsa` frame, native cells, title strip and red confirm/cancel/close controls are reused. Native inventory right-click reserves an item; dragging a carried item onto any empty grid cell reserves it in that exact cell. Right-click in the grid removes it. Left-click a material edits its exact positive quantity; left-click an instance removes it.
- The existing inventory opens alongside the workbench; there is no duplicate bag browser. Original title dragging, viewport scaling, modal blocking and world-click suppression remain active. Closing/resetting the cube releases its own in-flight title drag.
- Placing and removing are local reservations, not transfers into a second inventory. Close, cancel, disconnect and map/NPC changes cannot lose or duplicate items because nothing has been subtracted.
- Every completed inventory replacement invalidates the selection. An incomplete or native-only inventory replacement disables matching/execution; authoritative UID identity is required again.
- Items use `NetClient.LoadItemIcon` and owned native inventory tooltips. Installed rune ordering is optional input ordering on the selected gear, not a recipe chooser.
- No operation names, recipe list, required-material hints, output selector or automatic recipe catalog paging is shown. The player chooses actual inputs.

## Matching and execution

`cube.quote` adds the bounded `cubeInputs` field (1–35 entries; legacy 12-entry clients remain valid). Each input is either `{uid,count:1}` for an instance or `{itemId,count}` for a stackable material. The client never submits a recipe ID for this flow. Existing `itemUids` describes a requested order of already installed socket instances only.

`MatchCubeInputs` iterates the immutable `CubeRuleTable` loaded from PostgreSQL and checks exact selected material quantities, target/source shape and each existing operation's eligibility. No item ID, recipe ID, recipe fee or ingredient amount is used as a formula in the matcher. The snapshot's available material types follow the DB rules too. Startup validates their union with rune/key metadata against the client material-row and bounded byte limits.

All 66 existing recipes remain eligible. The 28 inscription definitions have identical input materials; conversion likewise has several same-input eligible outputs. These form equal-probability pools within the same operation and cost. Preview does not select an output or expose a recipe list; it reports the random-result type and fee. Ambiguity across different operations or costs fails closed.

The native 确认合成 control first requests the read-only match. Its response opens the existing confirmation dialog with the full server-authored outcome/risk and cost. Cancelling leaves every reservation intact; confirming rechecks the exact quote, inputs, revision, expiry and NPC context. A prompt held across inventory replacement, close or NPC loss cannot commit.

Execution sends only the quote identity, expected revision and the current NPC entity. The quote binds the immutable rules and catalogue snapshots; replacing either invalidates it before any new or cached candidate can execute. The server rechecks original inventory fingerprint, player conditions and workbench identity/map/dialogue/range. It selects any random output only for the candidate atomic commit. A definite storage retry retains the identical candidate and generated UID; successful persistent receipt replay precedes quote/NPC lookup, so reconnecting and retrying cannot charge again. Extra inputs, wrong counts, duplicated UIDs, locks, missing ownership and full output capacity cannot partially consume inventory.

The historic recipe-addressed wire protocol remains internal/backward-compatible. The current native container and plugin agent adapter both use exact `cubeInputs` and the dedicated live NPC context. Plugin catalog schemas/actions/observations no longer expose recipes or accept recipe-addressed shortcuts.

## Validation

- `Tests/PluginRuntime/run-game-actions.sh`: 424 total assertions, including 259 endgame adapter checks for exact inputs, bounded shapes, current NPC, hidden recipes and immutable retry identity.
- `Tests/Endgame/run-module-contract.sh`: 43 container/module checks, plus source integration assertions.
- `Tests/Endgame/run-native-cube-entry-contract.py`: 14 checks using the production inventory right-click handler and native title-drag implementation, including rejection fallthrough, native lock priority, close and viewport reset.
- `Tests/Endgame/run-ui-contract.sh`: matched risk/fee, no recipe addressing, pending/cancel and repeated-action coverage in `SocketRecipeContracts`.
- Server `TestCubeInputs*`: all66 recipes, same-input pools, configured cost changes, ambiguous operations, duplicate/foreign/locked inputs, capacity and no mutations during match.
- Server `TestEndgameCubeInputs*`: NPC binding/range, opaque nonrandom preview, durable commit, storage failure retry and reconnect receipt replay.
- Server `TestEndgameCubeInputClosedSchema`: nested keys, canonical Int64 strings, mutually exclusive identity forms and operation scope.
- `server/tests/sql/validate_zhoutian_placement.py`: isolated PostgreSQL original-map placement, idempotent replay, rollback, conflict detection and unrelated NPC/door preservation.

Real Unity world clicking/rendering is not certified by managed contracts or the resource composition preview.
