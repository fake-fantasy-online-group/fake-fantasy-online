# Rift workbench: original resource reuse

The rift workbench uses its own compact 600×450 window, with one title and only the actions relevant to the current phase; rune and cube dimensions remain unchanged. The independent rift workbench and HUD reuse the resource package through `ArtworkSkin.Frames` and `ResLoader`. No external artwork, package, generated texture or audio is added to Git.

## Verified original resources

- `res/uires/common/msgboxbk.gsa`: 346×128, also used by `SysMsgBox` and `DifficultyBox`. The complete original frame is expanded with 44px corner caps. The original black center has alpha 153; the popup therefore draws those same source RGB pixels without alpha as its backing, preventing covered text and buttons from showing through. The nine original frame slices retain their alpha.
- `res/uires/statusbar/expbarbk.gsa` and `expbar.gsa`: matching 800×6 track/fill, already used by `GameHud`. Progress clips the fill's UV and destination width by the authoritative ratio. It preserves the six-pixel logical height and draws the progress numbers separately.
- `res/uires/newcommon/downbtn.gsa`: 20×19, also used by `SysConfigDlg`; its normal/expanded frames supply the selector arrow.
- Existing `ArtworkSkin.Input`, `ArtworkSkin.Row`, and `NativeScroll.BarRows` provide the input, row selection, and scrollbar. Their existing resources remain unchanged.

Only 23,179 compressed bytes across 15 candidate resource entries were range-read for the artwork inspection. No full package download was performed.

## Behavior

The dropdown offers only `1..min(150, MaxTier, UnlockedTier)`. Missing unlock metadata falls back to one; it never unlocks all 150 difficulties. Selection is clamped again when the authoritative limit changes. Opening, closing, row/wheel scrolling, Home/End/arrows/PageUp/PageDown/Enter, and Escape are supported. Popup pointer events are handled before covered workbench controls; a dismissal or selection cannot also spend a key, leave the instance, close the workbench or refresh it. Close, module changes, reconnect/generation replacement and pending mutations clear the popup.

`Tier` remains the run's difficulty; `FloorIndex` remains its internal map depth. Completed views display the frozen server `RemainingSeconds` and authoritative new `UnlockedTier`; the UI does not predict or grant unlocks. The server rule is at least one tier, otherwise one tier per complete 180 seconds remaining, capped at 150 and never reducing previous unlocks.

Completed live-instance loot is picked up in the world; the bulk claim button is omitted there. The exit NPC is mentioned in the objective, while the existing leave action stays available. The server's `Claimable` projection controls off-instance recovery; live runs permit reentry and closed runs retain recovery/compensation flows.

## Checks

Run with the cloud C# toolchain environment loaded:

```sh
bash Tests/Endgame/run-ui-contract.sh
bash Tests/Endgame/run-module-contract.sh
python3 Tests/UiArtwork/run-native-scroll-contract.py --mono "$FFO_MONO_ROOT/bin/mono" --csc "$FFO_CSC"
git diff --check
```

The panel contracts execute the production partials against deterministic Unity/artwork substitutes, including individually selecting all 150 options, unlock contraction and missing metadata, pending requests, dismissal/click-through isolation, lifecycle reset, frozen completion display, live pickup versus recovery, original corner dimensions and progress UV clipping. The native scrollbar suite executes its production source separately.

An additional cloud-only preview compiled the production `EndgamePanel`, `ArtworkSkin` and `NativeScroll` draw paths against draw-call-recording Unity substitutes, then replayed the resulting texture rectangles/UVs using the actual decoded resource pixels. Selection, expanded dropdown, active progress and completion layouts were inspected. This caught and fixed the original center's text bleed-through. Text in that preview used an available CJK font rather than the game's bitmap atlas, and the preview loader used the first artwork frame. It is a source/artwork layout replay, not a Unity screenshot.

Unity import, PlayMode, player builds and native runtime visual acceptance have not been run in this cloud workspace.
