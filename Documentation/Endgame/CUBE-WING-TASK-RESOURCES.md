# Cube / wing task titles with older resources

`CubeQuestTitles` is a bounded display-only fallback for IDs 1900030001 (问鼎周天) and 1900030002 (幻翼初展). It is used only when the native `TaskDB` row is absent. Existing definitions always win.

- The active list's existing display placeholder receives the title, not a new full task definition.
- Completed tasks add title labels without manufacturing definitions.
- Live and adapter progress trackers use the same two names when showing multiple changed tasks.
- Other missing tasks retain their original behavior, including the existing trial title.

This does not alter `TaskDB.All`, publisher indices, recommended quests, prerequisites, rewards, acceptance, completion, or any wire payload. There is no TQN1/new opcode or new resource format. Readable titles do not require a full external task-table replacement. Server-offered dialogue and server authority remain unchanged.

Validation: `Tests/TaskResources/run-cube-quest-titles.py` compiles the actual `TaskDlg.DisplayDef` method and title helper, checks native-row precedence, verifies unrelated IDs and inactive-task behavior, and guards completed-list wiring. `Tests/QuestFeedback/run-task-track-render-contract.sh` executes both actual tracker implementations with explicit Unity/font doubles, including the missing two-title case. These checks do not certify Unity rendering.

The earlier full-table merge experiment is retained in local archived files and the previous checkpoint Library version; it is not required or included in this production patch. No authoritative external table was replaced or published.
