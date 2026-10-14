# Independent gameplay entrypoints

This correction removes the player-facing F8/global launcher, universal tab strip and unique/rune/formation encyclopedias. It preserves server recipes, catalog metadata used by owned-item tooltips and crafting, plugin operations, native equipment interactions, and all rift progression/reward behavior. No original pregame login or character/server selection code is changed.

## Player entry and function map

| Gameplay | Native entry | Functions and scope |
| --- | --- | --- |
| Inspect/equip bag equipment | Existing inventory actions and hover | `InventoryDlg.ActivateOrdinaryItem` / existing native equip path; `EndgamePanel.BagTooltip` and `OverlayTooltip` supplement the original tooltip in place. No separate gear dashboard. |
| Inspect/remove worn equipment | Existing character equipment window | `EquipDlg.DrawSlots` and `DrawSlotAction` keep the original slots/actions. `DrawAdditionalEquipmentControl` optionally replaces the doll area with `DrawAdditionalEquipmentSlots` for occupied server slots 14–25, using the same icon/tooltip/remove/share/repair/wash/tool routes. |
| Insert a star rune | Original 镶嵌炉 | `SculpDlg.TryDropSculp` queries native equipment; `SculpDlg.TryDropRune` → `SocketUI.Inlay` submits the existing `InlayForIndependentRuntime` packet using bag positions and a zero-based empty hole. No standalone rune module, extraction button or advertised plugin removal action. |
| Cube crafting | Permanent 周天鼎 in native bag → right-click | `ItemActions` → `TryOpenCubeFromItem` → `DrawCube`. Actual bag instances/material quantities go into a 35-slot (5×7) local reservation container in the original 450×484 furnace artwork; DB recipes are matched only by the server. No recipe/operation/output chooser or catalog paging. |
| Open/join/review a rift | 龙城八仙使者 → 演劫洞天 · 查看符钥与开境 | `DialogUI.BuildMenu` → `AddRiftEntranceOption` → `TryOpenRiftFromNpc` → `DrawRift`. All existing start/join/reentry/claim/compensation/leave/advance requests remain. |
| Active-rift progress and result review | Existing active-run HUD | `DrawRiftHud`, `DrawRiftMarkers`, `DrawRiftAnnouncement` retain progress, optional floor exit, telegraphs, globes and feedback. The HUD opens only the rift view through `TryOpenRiftFromWorld`, without auto-claiming. |
| Native furnace functions | Existing 合成炉/加工炉/镶嵌炉/打孔炉 interactions | Existing `DialogUI` exact-name dispatch remains intact; `ComposeDlg` and `SculpDlg` paths are unchanged. Incremental native drilling is distinct from random zero-hole cube opening. |
| Native trial/life skills | Existing trial and artisan dialogue | Existing `AddTrialOption` and `Win05.OpenLifeLearn` remain; optional trial key-use advice stays. |

`TryOpenWorldView` is a shared lifecycle helper, not a player navigation hub. It rechecks world/negotiation, synchronizes the connection generation, clears previous module selection/quote, opens only the requested workbench, and refreshes its relevant data. NPC presentation availability does not grant server authority. Rift admission remains server NPC-checked; input-driven cube matching and execution validate the configured permanent tool and quest prerequisites on the server.

`Update` keeps Escape, disconnect/reset cleanup, active-run polling, and captured cube bag-slot/map validity. It has no F8 action. `OnGUI` has no global launcher or module tabs. `Close` clears selection and quotes while retaining any uncertain mutation identity for explicit retry.

## Input-driven cube functions

62 enabled database recipes remain available; migration83 retires 1007, 1008, 1009 and 1012. Their legacy IDs and old input combinations are rejected by the server. The player puts real items and exact material quantities into the container. Equipment type, installed sockets, selected rune order, material types/counts and the authoritative operation checks determine whether a recipe matches.

Identical-input definitions within one operation and cost form an equal-probability output pool. In particular, inscription and conversion no longer use a desired-output selector. Different-operation/cost ambiguity fails closed. Input matching does not use random values or expose recipe lists. See [the full container contract](CUBE-CONTAINER.md).

The original inventory supplies drag/drop and right-click input; no duplicate bag browser is rendered. Every visible grid cell accepts a real selection. Stack counts remain editable, grid right-click returns a reservation, and the native confirmation dialog shows the entire server result and cost before execution.

`cube.quote` carries `cubeInputs`, the current revision and no NPC entity. Its matched result/risk and copper cost are server-authored. `cube.execute` carries only frozen quote identity/revision and current portable-tool ownership. Only durable atomic success changes real inventory. Quote changes, full bags, cancel, disconnect, repeated clicks and original-request replay cannot duplicate or lose materials.

## Verification and limits

- `run-contracts.sh`: production codec/state/fixture contracts.
- `run-ui-contract.sh`: existing real-workbench action/quote/socket/rift/tooltip contracts and source guards.
- `run-world-entry-contract.sh`: world negotiation, independent NPC entry, stale/reset/out-of-world gates, Escape and absent F8.
- `run-loot-context-contract.sh`: preserved native owned-item tooltips and original-furnace guidance, with no standalone rune renderer.
- `run-native-inlay-furnace-contract.sh`: actual native equipment/rune drag handlers, unchanged writer arguments, invalid/stale/occupied-hole rejection, target switching and reopen reset.
- `run-module-contract.sh`: container selection/quantities, no recipe catalog, failed matching, close/reopen, inventory invalidation, stale item/map and reconnect cleanup. Server `TestCubeInputs*` covers historical definitions and rejects retired recipes.
- `run-additional-equipment-contract.py`: actual native character-window source with deterministic Unity/network substitutes, covering all higher slots and original action routes.

These source/managed tests do not establish rendered layout, font/art availability or live gameplay. No Unity Editor launch, cloud Player build, Mac access or asset upload was used.

## Server-owned Rift entrance

The native dialogue appends the Rift row only when the current world actor matches a negotiated `endgame.rift-entrance.v1` hello binding (`riftEntrances`: `mapId`, static database `npcId`, exact `npc`, exact `script`). The selected actor's live uint32 entity ID is sent as `npcEntityId` on `rift.start`; the database config ID is never substituted. The live spawn path preserves `map_npcs.script` unchanged (`Nbaxian` for this row); the static portrait-lookup alias `Nbaxian_ffo` is not used to normalize interaction identity. The server retains final NPC-dialog/proximity authorization. The ordinary guard's trial row and every existing NPC service remain intact.

Bindings and captured actors fail closed after hello refresh/failure, reset, map departure, actor replacement, or changing dialogs. HUD review carries no authority to create a fresh paid run; existing admitted/invited run reentry and recovery remain available through the original run-ID path. Missing bindings on a legacy server leave other endgame views available without adding a Rift entrance. The new hello field is capability-gated so older strict DTO readers remain compatible.

Agent clients discover `riftEntrances` through `endgame` or `endgame.rift`, use the existing `npc.interact` on the live actor, then invoke `endgame.rift.start`. For fresh paid creation, both native UI and the agent action read the same current dialogue identity; agents cannot supply a fabricated NPC ID or bypass native interaction. Existing admitted/invited runs do not require a new entrance interaction. The observation's `riftEntranceEntityId` is null until that identity is valid.
