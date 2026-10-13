# Independent gameplay entrypoints

This correction removes the player-facing F8/global launcher, universal tab strip and unique/rune/formation encyclopedias. It preserves server recipes, catalog metadata used by owned-item tooltips and crafting, plugin operations, native equipment interactions, and all rift progression/reward behavior. No original pregame login or character/server selection code is changed.

## Player entry and function map

| Gameplay | Native entry | Functions and scope |
| --- | --- | --- |
| Inspect/equip bag equipment | Existing inventory actions and hover | `InventoryDlg.ActivateOrdinaryItem` / existing native equip path; `EndgamePanel.BagTooltip` and `OverlayTooltip` supplement the original tooltip in place. No separate gear dashboard. |
| Inspect/remove worn equipment | Existing character equipment window | `EquipDlg.DrawSlots` and `DrawSlotAction` keep the original slots/actions. `DrawAdditionalEquipmentControl` optionally replaces the doll area with `DrawAdditionalEquipmentSlots` for occupied server slots 14–25, using the same icon/tooltip/remove/share/repair/wash/tool routes. |
| Insert/extract a star rune | 巧匠/左慈 → 星篆 · 镶嵌取出 | `DialogUI.BuildMenu` → `OpenEndgameView(Runes)` → `TryOpenRunesFromWorld` → `DrawRunes`. Select real owned equipment, socket and bag rune. Emits `socket.insert` / `socket.remove`. No cube shortcut or catalog browser. |
| Cube crafting | 巧匠/左慈 → 周天鼎 · 问鼎炼化 | `DialogUI.BuildMenu` → `OpenEndgameView(Cube)` → `TryOpenCubeFromWorld` → `DrawCube`. `DrawCubeChoices` offers operation families and local equipment/output pickers. `ChooseRecipe` clears old quote/input order. `LoadRecipeMetadata` follows the server cursor while open. |
| Open/join/review a rift | 龙城八仙使者 → 演劫洞天 · 查看符钥与开境 | `DialogUI.BuildMenu` → `AddRiftEntranceOption` → `TryOpenRiftFromNpc` → `DrawRift`. All existing start/join/reentry/claim/compensation/leave/advance requests remain. |
| Active-rift progress and result review | Existing active-run HUD | `DrawRiftHud`, `DrawRiftMarkers`, `DrawRiftAnnouncement` retain progress, optional floor exit, telegraphs, globes and feedback. The HUD opens only the rift view through `TryOpenRiftFromWorld`, without auto-claiming. |
| Native furnace functions | Existing 合成炉/加工炉/镶嵌炉/打孔炉 interactions | Existing `DialogUI` exact-name dispatch remains intact; `ComposeDlg` and `SculpDlg` paths are unchanged. Incremental native drilling is distinct from random zero-hole cube opening. |
| Native trial/life skills | Existing trial and artisan dialogue | Existing `AddTrialOption` and `Win05.OpenLifeLearn` remain; optional trial key-use advice stays. |

`TryOpenWorldView` is a shared lifecycle helper, not a player navigation hub. It rechecks world/negotiation, synchronizes the connection generation, clears previous module selection/quote, opens only the requested workbench, and refreshes its relevant data. NPC presentation availability does not grant server authority. Rift admission remains server NPC-checked; existing socket/cube authority checks are unchanged.

`Update` keeps Escape, disconnect/reset cleanup, active-run polling, and cube metadata continuation. It has no F8 action. `OnGUI` has no global launcher or module tabs. `Close` clears selection and quotes while retaining any uncertain mutation identity for explicit retry.

## All cube functions remain usable

All routes below start in the independent 周天鼎 workbench. The ten singular operations are direct choices; inscription and conversion each have one action plus a local chooser for all 28 server-defined outputs. This retains all 66 recipe IDs without displaying a 66-row recipe encyclopedia.

| Action | Recipe ID | Operation | Selected input |
| --- | --- | --- | --- |
| 解器归元 | 1001 | `salvage.normal` | Bag equipment UID |
| 仙遗化韵 | 1002 | `salvage.unique` | Bag unique UID |
| 星篆归尘 | 1003 | `salvage.rune` | One bag rune UID |
| 开窍引星 | 1006 | `socket.open` | Zero-hole bag equipment UID; server supplies capacity, odds and cost |
| 归星还篆 | 1007 | `socket.extract` | Bag equipment UID; returns installed cards |
| 移星换斗 | 1008 | `socket.reorder` | Bag equipment UID and ordered installed rune UIDs |
| 布宿成阵 | 1009 | `socket.array` | Bag equipment UID and ordered bag rune UIDs |
| 淬器成真 | 1010 | `unique.awaken` | Bag equipment UID |
| 返本重铸 | 1011 | `unique.reforge` | Bag unique UID |
| 炼真守一 | 1012 | `unique.temper` | Bag unique UID |
| 凝髓成篆 | 11001–11028 | `rune.inscribe` | Desired output selected locally; wallet materials remain server-owned |
| 同象易宿 | 12001–12028 | `rune.convert` | Desired output and three identical bag rune UIDs; server validates same quadrant/different destination |

Every operation uses `cube.quote` with the selected inputs and current revision. The displayed authoritative quote gives required/owned materials, money, result/risk description and expiration. `cube.execute` sends only the frozen quote identity/revision. No recipe, economic cost, authorization rule or mutation packet changes in this UI correction.

Opening or changing an input/output never performs crafting. Repeated execution remains blocked while uncertain. The existing explicit original-request retry keeps the exact nonce and parameters. Failed recipe metadata waits for explicit Refresh; closing stops further pages and reopening resumes the server cursor.

## Verification and limits

- `run-contracts.sh`: production codec/state/fixture contracts.
- `run-ui-contract.sh`: existing real-workbench action/quote/socket/rift/tooltip contracts and source guards.
- `run-world-entry-contract.sh`: world negotiation, independent NPC entry, stale/reset/out-of-world gates, Escape and absent F8.
- `run-loot-context-contract.sh`: preserved native owned-item tooltips and no encyclopedia/cross-module buttons.
- `run-module-contract.sh`: all 66 choices through 12 action families, exact metadata cursors, failure/refresh/close/reopen, local selection, and module isolation.
- `run-additional-equipment-contract.py`: actual native character-window source with deterministic Unity/network substitutes, covering all higher slots and original action routes.

These source/managed tests do not establish rendered layout, font/art availability or live gameplay. No Unity Editor launch, cloud Player build, Mac access or asset upload was used.

## Server-owned Rift entrance

The native dialogue appends the Rift row only when the current world actor matches a negotiated `endgame.rift-entrance.v1` hello binding (`riftEntrances`: `mapId`, static database `npcId`, exact `npc`, exact `script`). The selected actor's live uint32 entity ID is sent as `npcEntityId` on `rift.start`; the database config ID is never substituted. The live spawn path preserves `map_npcs.script` unchanged (`Nbaxian` for this row); the static portrait-lookup alias `Nbaxian_ffo` is not used to normalize interaction identity. The server retains final NPC-dialog/proximity authorization. The ordinary guard's trial row and every existing NPC service remain intact.

Bindings and captured actors fail closed after hello refresh/failure, reset, map departure, actor replacement, or changing dialogs. HUD review carries no authority to create a fresh paid run; existing admitted/invited run reentry and recovery remain available through the original run-ID path. Missing bindings on a legacy server leave other endgame views available without adding a Rift entrance. The new hello field is capability-gated so older strict DTO readers remain compatible.

Agent clients discover `riftEntrances` through `endgame` or `endgame.rift`, use the existing `npc.interact` on the live actor, then invoke `endgame.rift.start`. For fresh paid creation, both native UI and the agent action read the same current dialogue identity; agents cannot supply a fabricated NPC ID or bypass native interaction. Existing admitted/invited runs do not require a new entrance interaction. The observation's `riftEntranceEntityId` is null until that identity is valid.
