# Rift workbench: original resource reuse

The rift workbench uses its own compact 600×450 window, with one title and only the actions relevant to the current phase; rune and cube dimensions remain unchanged. The independent rift workbench and HUD reuse the resource package through `ArtworkSkin.Frames` and `ResLoader`. No external artwork, package, generated texture or audio is added to Git.

## Verified original resources

- `res/uires/common/msgboxbk.gsa`: 346×128, also used by `SysMsgBox` and `DifficultyBox`. The complete original frame is expanded with 44px corner caps. The original black center has alpha 153; the popup therefore draws those same source RGB pixels without alpha as its backing, preventing covered text and buttons from showing through. The nine original frame slices retain their alpha.
- `res/uires/gameui/intonatebarbk.gsa` and `intonatebar.gsa`: user-selected original gold frame (140×34) and green fill (116×10), with the original inset at (12,12). Both the compact HUD and workbench now use this pair. Fill destination and UV are clipped to the same authoritative ratio; no frame artwork is stretched to the former wide center HUD. A cached runtime silver-white copy maps each original pixel to neutral RGB using its maximum channel (HSV value), preserving the original bevel and alpha; no texture asset is added to Git.
- `res/uires/newcommon/downbtn.gsa`: 20×19, also used by `SysConfigDlg`; its normal/expanded frames supply the selector arrow.
- Existing `ArtworkSkin.Input`, `ArtworkSkin.Row`, and `NativeScroll.BarRows` provide the input, row selection, and scrollbar. Their existing resources remain unchanged.

Only 23,179 compressed bytes across 15 candidate resource entries were range-read for the artwork inspection. No full package download was performed.

## Compact dual-meter HUD

The HUD follows the native portrait scale (`GameHud.uiScale` when positive, otherwise `UIScale.Factor`), not the workbench's viewport-fit scale. Its frame is at logical `(80,96,140,34)`; the frame's original transparent 8px top margin places its visible edge at y=104, beside the native `善恶` row `(4,104,72,14)` and immediately below the pet vitals. Only the framed bar remains; there is no adjacent tier or remaining-time caption. The old centered dialog, permanent objective block and action buttons are removed. Hovering the slot shows progress, elapsed/total, remaining time, phase and objective; phase changes continue to use the existing bottom-right notice queue.

One slot contains two clipped fills: white elapsed-time ratio is drawn first, original green progress second. White is omitted entirely when progress is greater than or equal to time, so its alpha edges cannot bleed through an otherwise covered layer. Silver-white pixels are neutral RGB at each source pixel’s maximum channel value, retaining the original highlight/shadow contrast and alpha. This avoids a flat white rectangle; the original green texture stays unchanged. The owned white copy is reused across GUI passes and destroyed on unbind/destruction.

The selected-target panel normally occupies the same row. Only while this compact HUD is visible, its complete native rendering, name/level/HP/status rectangles, pointer hit area and tooltip positions move down 38 logical pixels. Disabling the panel/avatar, opening a modal workbench, or leaving the run restores the native position immediately. Target pointer tests use the same explicit HUD scale as rendering.

The server supplies optional `totalSeconds` from the run's own `Rules.TimedMS / 1000`; elapsed is `clamp(1 - remaining / total, 0, 1)`. No client duration constant or first-snapshot duration guess is used. Older servers without total metadata retain progress and the hover remaining-time information but do not show an invented white ratio. Only active/boss phases locally advance the countdown. Reserved runs show zero elapsed; completed/closed/reward-pending and other terminal snapshots preserve server time.

## Behavior

The dropdown offers only `1..min(150, MaxTier, UnlockedTier)`. Missing unlock metadata falls back to one; it never unlocks all 150 difficulties. Selection is clamped again when the authoritative limit changes. Opening, closing, row/wheel scrolling, Home/End/arrows/PageUp/PageDown/Enter, and Escape are supported. Popup pointer events are handled before covered workbench controls; a dismissal or selection cannot also spend a key, leave the instance, close the workbench or refresh it. Close, module changes, reconnect/generation replacement and pending mutations clear the popup.

`Tier` remains the run's difficulty; `FloorIndex` remains its internal map depth. Completed views display the frozen server `RemainingSeconds` and authoritative new `UnlockedTier`; the UI does not predict or grant unlocks. The server rule is at least one tier, otherwise one tier per complete 180 seconds remaining, capped at 150 and never reducing previous unlocks.

Completed live-instance loot is picked up in the world; the bulk claim button is omitted there. The exit NPC is mentioned in the objective, while the existing leave action stays available. The server's `Claimable` projection controls off-instance recovery; live runs permit reentry and closed runs retain recovery/compensation flows.

## Checks

Run with the cloud C# toolchain environment loaded:

```sh
bash Tests/Endgame/run-rift-hud-contract.sh
bash Tests/Endgame/run-ui-contract.sh
bash Tests/Endgame/run-module-contract.sh
python3 Tests/UiArtwork/run-native-scroll-contract.py --mono "$FFO_MONO_ROOT/bin/mono" --csc "$FFO_CSC"
git diff --check
```

The panel contracts execute the production partials against deterministic Unity/artwork substitutes, including individually selecting all 150 options, unlock contraction and missing metadata, pending requests, dismissal/click-through isolation, lifecycle reset, frozen completion display, live pickup versus recovery, original corner dimensions and progress UV clipping. The native scrollbar suite executes its production source separately.

An additional cloud-only preview compiled the production `EndgamePanel`, `ArtworkSkin` and `NativeScroll` draw paths against draw-call-recording Unity substitutes, then replayed the resulting texture rectangles/UVs using the actual decoded resource pixels. Selection, expanded dropdown, active progress and completion layouts were inspected. This caught and fixed the original center's text bleed-through. Text in that preview used an available CJK font rather than the game's bitmap atlas, and the preview loader used the first artwork frame. It is a source/artwork layout replay, not a Unity screenshot.

Unity import, PlayMode, player builds and native runtime visual acceptance have not been run in this cloud workspace.

The dual-meter cloud preview executes the production `DrawRiftHud` and artwork methods against draw-call-recording substitutes, then replays those rectangles/UVs with decoded original package pixels. It compares green 35% / elapsed 60% with green 80% / elapsed 60%, surrounded by the recovered portrait, karma row and displaced selected-target frame. The context portrait is an example original portrait, not a claim about the user's current character. Preview captions explicitly identify it as a cloud source/artwork layout replay rather than a Unity screenshot; CJK fallback text differs from the game's bitmap font. Preview files and decoded assets remain outside the repository.
