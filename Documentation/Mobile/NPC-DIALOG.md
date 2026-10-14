# Mobile NPC dialogue

The mobile-only `DialogUI` branch uses the existing NPC window/selection/close artwork. Desktop geometry and callbacks are unchanged. The new layout stays within the top-origin safe area; close and page controls remain visible, and option hit rows are 44 presentation-scale logical points.

- Option pages retain the original `optLabels`/`optActs` index and call the existing `ActivateOption(int)` once. No task opcode, task mutation, offer state, or reward rule changes.
- Greeting/task text pages use the same glyph advance as the existing rich-text renderer. `/Cnn` colors carry to subsequent pages. Text is not truncated to the old fixed body height.
- NPC/content/action-list changes reset pages and pending presses. Resize cancels pending presses and rebuilds text pages. Dragged presses and duplicate same-frame dispatches do not invoke actions. Canceled touches, multiple fingers, focus loss and application pause cancel pending presses; fresh input is accepted after touch clearance. The same original action has a mobile-only 350ms double-tap guard, after which an intentional retry is allowed without inventing task pending/acknowledgement state.
- The visible NPC dialog owns mobile world input globally through the existing modal predicate. Its mobile GUI consumes pointer down, drag, release, and scroll events, including outside-panel pointer events.

## Verified in managed execution

Run after `source Documentation/tools/activate-dotnet-test-toolchain.sh`:

- `python3 Tests/Mobile/run-npc-dialog-contract.py`: 521 checks, including safe-area bounds, long text preservation/colors, all 50 original option indices across pages, same-frame duplicate suppression, stale presses after offer refresh, NPC page reset, drag cancellation, existing Close routing, cross-frame double-tap suppression/later retry, cancellation/multitouch/focus/pause, and stale release after resuming.
- `bash Tests/Mobile/run-layout-contract.sh`: 325 checks, including global modal/world-input suppression.
- `python3 Documentation/tools/compile-current-presentation.py`: complete Presentation compilation against real recovered Unity references, 1183 sources at time of verification.

These checks do not establish Unity rendering, phone touch delivery, safe-area/orientation behavior on a device, or real task/server outcomes. Device QA should open a many-option NPC, page through all options, read a long colored task description and reward, cancel a drag, close from each page, and exercise accept/complete/leave through the authoritative server flow.
