# Native interaction restoration (unpublished, 1.0.6 unchanged)

Base: `36aab0ec93b1912905bd1e77165a63c951e50a6c`.

- Remove the added voting tab and GM opinion/submission form. Preserve the original GM
  tab as ordinary GM chat history, other channels, announcements, protocol decoders and
  legitimate server GM commands. Preserve the default Nearby channel after removing a tab.
- Remove only the mailbox-list compose button. Native paper **3309 / 信纸 / script paper**
  enters the existing composer from the shared inventory/hotbar/mobile item-action route.
  A live matching unlocked bag row is required. Reusing paper while composing preserves
  the current draft. Opening/closing/canceling sends no mail and spends no paper. Existing
  reply, attachment and parcel behavior is retained. The paired server change consumes one
  paper transactionally for a letter (no money or attachments), with rollback on failure.
- Remove only the family-list create button. Use the original creation dialog from the
  exact main-city map/name pairs below. Opening the NPC option sends no creation request;
  the original Confirm/Cancel, 40-level/300-honor/10-gold server rules remain unchanged.
- Reuse a verified **blank native button**, not a falsely claimed dedicated sort image.
  Existing inventory and warehouse sort callbacks remain identical to the base.

## Family NPCs

| Map | Native name | Source |
| --- | --- | --- |
| 7 / qw0007 | 城主成隍 | map_npcs; native family help |
| 19 / qw0019 | 执事官徐福 | map_npcs; native family help |
| 32 / qw0032 | 玉皇大帝 | map_npcs; native family help |
| 58 / qw0058 | 监城官哈亚图 | map_npcs; native family help |
| 86 / qw0086 | 无泪城主绝聆 | map_npcs; native hint |
| 154 / qw0154 | 城主六翼羽宸 | map_npcs current main-city leader |
| 178 / qw0178 | 上古城主海煌 | map_npcs current main-city leader |
| 247 / qw0247 | 猫猫城主大王 | map_npcs current main-city leader |

Both map and exact name are checked when building and activating the option; substring
matches and same-name actors in other maps do not qualify. Repeated activation cannot
close an already-open creation form. This is UI routing, not a new anti-forgery protocol:
FamilyCreate carries only name/declaration, and the existing server has no NPC-position
admission or general last-interacted-NPC context. No new restriction is claimed.

## Sort artwork provenance

The available original package index contains 80,338 records. All 696 `res/uires/*btn.gsa`
entries were decoded for inspection (small faces viewed, map-sized entries identified by
size), plus the native ware/storedlg backgrounds and naming candidates. No dedicated
“整理” button was located. Clear/empty buttons with different baked labels were rejected.

- Reused original package path: `res/uires/festival/GetAwardBtn.gsa`.
- Decoded dimensions: **86 × 24**, four complete normal/hover/pressed/disabled frames.
- Original decoded GSA SHA-256:
  `ec01b8e06a6c0f5ca31ef75dcfd3b4566bdb5cdfdf9ab47c63fb5409b7a72fc5`.
- All four frames are blank. Runtime text is the existing 12px `BitmapFont` label “整理”.
  No source texture is edited or copied into this repository.
- The old skin used `res/uires/votedlg/buttonbk.gsa` (**79 × 39**) nine-sliced into 48 × 21.
  The new draw preserves the full 24px height, uses 9px left/right caps and only resizes
  the blank middle horizontally. It does not squeeze tall-frame borders vertically.
- Inventory face **and hit target**: `(365,40,48,24)`; warehouse `(363,38,48,24)`.
  They finish at y64/y62 respectively, before the slots at y66. There is no drawn-but-
  unclickable three-pixel strip. Existing callback timing remains unchanged.
- Missing/wrong-size frames or missing bitmap font use the existing labelled fallback.
  The shared resource-owner cache is retained; no per-event texture allocations.

Temporary decoded PNGs/contact sheets stay outside the repository. The recorded visual
comparison uses real package face pixels, **not a Unity screenshot**. The separate
`font/gbk12.png`, `.idx`, `.wid` were not materialized for the preview, so no substitute
font is portrayed as original game lettering.

## Reproduce checks

```sh
source Documentation/tools/activate-dotnet-test-toolchain.sh
python3 Tests/NativeInteractions/run-classic-interaction-contract.py
python3 Tests/NativeInteractions/run-native-sort-contract.py
export FFO_COMPILE_DIR=/tmp/native-classic-managed
export FFO_PRESENTATION_COMPILE_DIR=/tmp/native-classic-presentation
bash Tests/Endgame/run-managed-compile.sh
python3 Documentation/tools/compile-current-presentation.py
```

- 64 production entry/confirmation/cancellation assertions plus source preservation guards.
- 225 sort face/state/cache/fallback/hit-geometry assertions.
- All current core and Presentation sources compiled against recovered real Unity references.
- These are deterministic/source/compile checks, **not** Unity import, PlayMode, Player,
  real-network interaction, or whole-screen visual acceptance. No version, build, push,
  publication, live database, or user-computer operation is part of this change.
