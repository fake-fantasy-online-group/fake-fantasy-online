# UI artwork harmony

This feature restores artwork-backed styling to the 26 UI groups below. It reuses external resource packages through ResLoader; no image, audio or package data is included in this change. Existing UI actions and current wording are preserved, including cases where an older image contains outdated text.

## Scope

1. 邮箱写信与退回
2. 家族创建刷新邮件
3. 背包仓库整理
4. 仓库存取金额
5. 聊天投票公告及GM按钮
6. 商城导航分类
7. 购物篮按钮
8. 洗炼融合宠物工坊
9. 摆摊下架
10. 装备称号箭头
11. 默认输入框
12. 删除角色确认
13. 塔狱难度
14. 家族仓库
15. 充值返利
16. 试衣间
17. 回收特价限时
18. 账号安全改名弹窗
19. 乱斗五窗
20. 聊天选取复制
21. 登录界面
22. 空角色槽
23. 聊天辅助
24. 系统设置
25. 宠物觉醒
26. 家族输入与文本菜单

## Shared skin

`ArtworkSkin` caches resource-owned textures per loader/package. Blank four-state button artwork is sliced to retain corners, current labels are drawn separately, inputs use the existing edit frame, and panels use the existing nine-piece frame with an opaque backing beneath translucent center artwork. Existing complete/return button images and baked input wells remain intact.

## Reproducible source contracts

Set `UNITY_EDITOR_DIR` to the installed Unity Editor directory (the directory containing `Data`), then run:

```sh
bash Tests/UiArtwork/run-artwork-contract.sh
python3 Tests/UiArtwork/inventory_social_regression.py --repo .
python3 Tests/UiArtwork/run-native-scroll-contract.py --repo . --unity-editor "$UNITY_EDITOR_DIR"
```

The contract executable compiles the production ArtworkSkin source against small deterministic Unity API stubs. It checks resource caching, loader/package changes, corner-preserving geometry, state selection, GUI color restoration, long-label fitting, click capture/release, cancellation, keyboard focus and disabled controls. This is not a Unity runtime or PlayMode test.

## Validation boundary

Standalone C# compilation and source/geometry regression checks do not establish runtime visual acceptance. Unity import, PlayMode and Player builds were intentionally not run for this task. Resource previews were inspected locally; the resources themselves are not part of this repository.

Inventory/social regression checks compare the baseline action and callback blocks and test layout geometry. NativeScroll contracts execute the production scrollbar source against API stubs, including dual-column drag isolation. Neither suite simulates the Unity event pipeline or establishes visual acceptance.

## Recorded checks (2026-10-10)

- All 26 implementation groups completed and cross-reviewed at source level.
- All 9 assemblies compiled against the installed Unity references: 0 errors, 208 existing warnings.
- 32 ArtworkSkin deterministic contract assertions passed.
- 200 inventory/social source and geometry assertions passed.
- 26 NativeScroll production-source contract assertions passed.
- All literal GSA resource paths in changed production files exist in the local package index.
- No external resource files were added to Git.

These results cover source and deterministic contracts only, not runtime appearance or a playable client build.

## Title button refinement (2026-10-11)

Title text buttons use the existing `gbk12` bitmap font at native 1× logical
scale instead of 1.15×. Existing centered, outlined, width-fitted label drawing
and the title frame's resolution scaling remain unchanged. Single-image title
buttons retain their baked labels, dimensions, hit targets, and `GUI.Button`
action timing, with a draw-only hover, pressed, and disabled tint. The new
single-image draw helper restores the caller's GUI color and does not allocate
textures or load additional resources.

Validation: 41 deterministic ArtworkSkin contract assertions pass against Unity
API stubs, including nine new native-size/single-image feedback assertions.
The previously supplied reference screenshots and external font/button textures
are unavailable in this workspace. No visual comparison, Unity import, PlayMode,
or Player build was performed for this refinement. The 12-pixel point-filtered
font basis comes from `BitmapFont.cs`; it is source evidence, not a screenshot.

## Original pregame restoration (2026-10-11)

The title/login and character-selection portions of the harmonization above have
been reverted at the user's request. The authoritative baseline is
`99c800c6f0c369a231733b292887d12ad7132803`, the parent of the first 26-group
harmonization commit `7b295bf9774990525fe7fbdfd8bccd72b9b2c789`.
All 22 original title source files at that baseline are also identical to the
initial source release `f8d8885`; this is not merely a rollback of the later
`d9cbf21` button refinement.

Restored visual sources:
- `TitleFlow.LoginGui.cs`: original input wells, 15px bold GUI button font,
  procedural rounded gold-bordered textures/styles, registration/recovery/appeal
  panels, resolution/frame-rate lists and original form-button fallback behavior.
- `TitleFlow.LoginMembers.cs`: original button style, announcement panel/scroll
  indicator, hint box, checkbox fallback and form rows.
- `TitleFlow.VersionGui.cs`: original update-gate panel and text styles.
- `TitleFlow.CharacterSelection.cs`: original empty-slot outline/text and
  character-delete confirmation appearance.
- `RecoveredTitleLoginSurface.cs`: original image-button rendering without the
  subsequently added hover/pressed tint.

Server-selection, character-creation, title resources, layout, sprite helpers and
other unchanged title drawing sources are checked against the same baseline.
The unrelated in-game harmonization and endgame artwork remain intact.

The only allowed differences in the visual sources are plugin owner registration,
opaque input-edit revisions and timeout notifications. `TitleFlow.NativeGui.cs`
retains its non-drawing GUI-state cleanup guard and shared server-binding helper;
its actual draw sequence is checked against the original after inlining that
helper. Login connection and lifecycle files retain the plugin functional hooks.

Reproduce the source proof (requires baseline Git history):

```sh
python3 Tests/UiArtwork/pregame_original_regression.py
bash Tests/PluginRuntime/run-native-pregame-bridge.sh
```

Recorded checks: **42** provenance/equivalence checks across **22** original title
source files, **32** native bridge execution checks plus its source invariants,
**41** shared artwork contracts, and **200** unrelated inventory/social regression
checks passed. The C# checks use deterministic test substitutes. No Unity import,
PlayMode, rendered screenshot comparison, Mac operation, Player build or external
resource modification was performed. Source restoration is established; runtime
visual acceptance remains unverified.

## Current skill icon identity correction (2026-10-12)

Original tree resources contain grayscale pictures that can differ from the
current skill icon. User runtime feedback clarified that the current dynamic
icon must be retained, rather than the baked picture. The earlier change that
skipped the dynamic overlay for unlearned nodes was therefore the wrong layer
choice and is superseded here.

Confirmed resource/contract mappings: assassin 10213 (`othertree3`, slot 37,11,
item7049, 陷阱：爆弹) and 10204 (`maintree3`, slot 9,8, item5023, 暗器修炼).
Both live resources are 30×30 and fully opaque, but the renderer does not assume
this for all icons. `DrawSkillIcon` preserves the outer 2 logical pixels of the
36-pixel original tree node and replaces only its inner 32×32 well. A neutral
#111 opaque inner backing prevents obsolete artwork leaking through transparent
pixels, aspect-fit margins, or a missing texture. The current icon is fitted
inside that well at identical positions in both states: RGB brightness 55% with
alpha 1 when unlearned, full color when learned. Missing icons leave an empty
inner well instead of presenting a different skill. Missing trees retain the
36×36 fallback area because there is no authored rim to preserve.

No original resources are changed. Arrow/outer-rim pixels, node hit areas,
coordinates, upgrade commands, tooltips, and gameplay stay unchanged. The V5
`CanCast` field used here is validated as effective rank > 0, including passives;
it is distinct from active-cast admission.

Reproduce with the cloud C# toolchain environment set:

```sh
python3 Tests/UiArtwork/run-skill-icon-contract.py
```

403 assertions execute extracted production `Fit` and `DrawSkillIcon` methods:
missing/present trees and icons, learned/unlearned transitions, 0.5–2× scales,
scroll offsets, square/wide/tall images, opaque/half/fully transparent pixels,
old-background isolation, four original rim edges and the connecting arrow,
and restored apothecary nodes 10316/10318/10321. Source guards protect caller
wiring and passive rank semantics.

Evidence was decoded using actual repository GSA/DIMG sources against texture
upload substitutes; four assassin/apothecary tree images and both current item
icons were inspected. Archive SHA-256:
`9025faeb1d1ccc4e5976c1105d6041a3319a556b72f20d9451bae110a186f408`.
External resources and decoded images remain outside the repository. The
before/unlearned/learned preview is a source pixel composite, not a game
screenshot. No Unity import, PlayMode, Player build, or Mac UI operation was run.
