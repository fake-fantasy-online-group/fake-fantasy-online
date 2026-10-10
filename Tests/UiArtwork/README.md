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
