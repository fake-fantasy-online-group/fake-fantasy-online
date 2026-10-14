# Mobile landscape presentation

This is an opt-in mobile presentation branch over the original IMGUI game. Desktop entry bodies remain the default. `MobileClientPresentation.Enabled` is the only mode switch: mobile platform detection or explicit `MobileClientPresentation.Simulate = true` for an in-editor harness. Simulation is not persisted, and is false by default.

## Presentation and shared actions

- `GameHud.GuiOwner` chooses the mobile view once. `GameHud.Mobile` retains original avatar/health/mana, target actions, buffs, experience, raid/zone status and minimap resources; no replacement artwork is generated.
- The safe-area layout puts the movement stick at lower left, a larger attack control and four hotbar slots at lower right. Eight pages expose the original 30 hotbar entries. Unassigned final-page slots are inert. The original usability, cooldown, pet, item and skill admission remains in `GameHud.UseHotSlot`.
- A skill touch owns its finger until release or cancellation. Targeted skills begin preview on down, aim relative to the player from thumb displacement, and confirm on release; instant, single-target, pet and item actions wait for release. Dragging to the marked cancel area, OS touch cancellation, focus/pause, missing finger, orientation/safe-area change, or opening a modal cancels without releasing the skill.
- The map uses the full safe viewport for its original compact/full-map drawing. The four mobile map controls dispatch directly to original zoom, world-map and diagram actions. Original IMGUI map buttons are disabled while rendering this branch, preventing duplicate mouse-synthesis clicks. The compact map stays visible. Hover-only gate tips remain a desktop interaction, and legacy world-map dialogs still require device usability validation.
- Function buttons use the original control textures and activation functions. The expanded grid sits left of the map. Function expansion and expanded chat are mutually exclusive. Chat is compact at bottom center by default; expansion is centered. Chronological system and ordinary messages share the existing character-specific channel filter. This does not re-enable disabled message producers. Existing text/share guards and send rules are reused, and a reported keyboard occlusion area is respected.
- Legacy visible dialogs block mobile world and skill input across the screen until migrated. Plugin hit testing takes the actual pointer position. Mobile inventory registers its own panel. A touch that began on UI is never reclassified as a world touch after dragging away.

## Item workflows

- While trading, the mobile inventory detail action explicitly adds the selected stack or withdraws an offered stack. Split a stack first to trade only part of it. The desktop held-item drop and mobile button share lock/binding/fused-item checks and the existing offer sender. Own locking or either final confirmation blocks changes; the other side locking alone keeps the original editable-side semantics. Delayed fused-item approval is invalidated by a bag receipt, a new trade, changed partner or changed item.
- In the cube, mobile taps select a reserved cell. The explicit “取回选中” button releases that reservation and invalidates the quote without moving or spending inventory. Material quantity editing remains available; pending requests and confirmation dialogs block removal. Desktop left/right-click behavior stays available.
- Repair selection has a mobile “取消修理选物” control above the bag. It calls the original cancellation action and clears its input region immediately.

## Tests

From the repository with the managed toolchain activated:

```sh
source Documentation/tools/activate-dotnet-test-toolchain.sh
bash Tests/Mobile/run-layout-contract.sh
bash Tests/Mobile/run-chat-contract.sh
bash Tests/Mobile/run-touch-pointer-contract.sh
bash Tests/Mobile/run-item-gesture-contract.sh
bash Tests/Mobile/run-bag-offer-contract.sh
bash Tests/Mobile/run-repair-pick-contract.sh
bash Tests/Endgame/run-ui-contract.sh
bash Tests/Endgame/run-managed-compile.sh
python Documentation/tools/compile-current-presentation.py
```

The layout contract compiles the actual layout source against deterministic Unity data stubs, checks safe bounds and non-overlapping controls at four landscape aspect ratios, centered expanded chat, registered-panel ownership, and legacy modal gates. This is geometry/contract coverage, not Unity rendering.

## Still requires Unity and device acceptance

No Android installation, Unity scene execution, touch-device run, or iOS build is established by the managed checks. Verify actual original texture appearance, touch target physical size, IMGUI function/chat button synthesis, two-finger movement plus skill gestures, interruption/reentry, keyboard/Chinese composition and keyboard-area reporting, map zoom/expansion/return, NPC dialogs, inventory actions, camera and movement synchronization, and desktop regression in the running client. Landscape is the supported initial layout. Existing non-inventory dialogs have safe input blocking but have not all been relaid out for mobile.
