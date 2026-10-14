# Mobile skill book

Mobile presentation retains the original tree art, skill icons, authored tooltip text/colours/frame, prerequisite and growth-rank rules. Desktop click/drag and wheel paths remain in place.

- Mobile skill/study windows fit and centre within the safe area, including low-height landscape screens; their headers cannot be dragged offscreen. Rotation resets pending selection. Desktop window sizing/dragging remains unchanged.
- Drag the combat tree to scroll. A drag, canceled touch, second touch, or release outside never selects, learns, casts, or binds.
- Release a tap on a skill to open its fixed, paged original details. Return to the tree explicitly.
- Learning/upgrading requires the separate “学习 / 升级” button, using the existing eligibility and server request. The icon/plus no longer sends a mobile press-down upgrade.
- “放入快捷栏” is enabled only for a learned/castable, hotbar-compatible combat skill. It opens a paged chooser with four large buttons per page for the existing 30 slots, labelled with mobile HUD page and button numbers. Page 8 contains only buttons 1 and 2.
- Choosing a slot rechecks eligibility, uses the original kind-0 skill-book replacement and existing hotbar sync writer, and never casts or upgrades. Other slots, including item and pet slots, are untouched. Replacing a selected occupied slot follows the same original drop semantics.
- Cancel, return, close/switch window, tab change and focus/pause/disable do not commit pending binding.

Validation: `source Documentation/tools/activate-dotnet-test-toolchain.sh && Tests/Mobile/run-skill-binding-contract.sh` compiles the actual mobile Win05 actions/tree input, selection state machine, GameHud binding bridge, original hotbar replacement and sync methods with boundary doubles. It covers real safe-area window/footer preparation at 800×360, rotation/detail reset, drag/tap/cancellation, eligibility recheck, last-page mapping, replacement, untouched pet slots, repeated clicks, back/cancel/focus and explicit upgrade isolation. Full Presentation compilation is also required. These checks do not replace Unity device touch/visual testing or a live server receipt test.
