# Native interaction contracts

```sh
source Documentation/tools/activate-dotnet-test-toolchain.sh
python3 Tests/NativeInteractions/run-classic-interaction-contract.py
python3 Tests/NativeInteractions/run-native-sort-contract.py
python3 Tests/NativeInteractions/run-native-controls-contract.py
```

The real-controls runner supplements the entry/action contract: it compiles the
complete production `Win05.History.cs` and extracts the unchanged actual
`ChatTabs`/history-cache declarations, `DialogUI.BuildMenu`, both
`ActivateOption` overloads, `Close`, and the native travel admission. The real
option storage/binding and family routing are compiled too. It does not replace
`BuildMenu` with a call to the new family helper.

204 assertions exercise channel clicks at three scales (including original GM
history, family aliases, empty/new GM history, and announcement dispatch), all
eight leaders, real option binding/dispatch, preserved purchase/task/leave rows,
rebuild/repeated activation, and wrong/stale NPC/map rejection. A synthetic
composition fixture executes the full menu with both family and dungeon routes;
the real dungeon reply rebuild must retain family/task/leave entries and send
only its original list/index. It is not an assertion about native NPC bindings. Unrelated NPC
services, network/window boundaries, text drawing, and Unity API calls use small
deterministic substitutes. The announcement body's content is outside scope;
its actual dispatch from `DrawHistory` is checked.

These are source-body behavioral tests, not Unity import, IMGUI composition,
PlayMode, real-network interaction, or whole-screen visual acceptance.
