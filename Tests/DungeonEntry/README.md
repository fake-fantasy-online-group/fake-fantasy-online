# Native dungeon entry and task-admission contract

Dungeon-service lists 101..133 (except held list 114) render named NPC main-menu
choices instead of a generic TransportUI. The existing 0x1071 / 0x8049 / 0x1072
list/index authority, difficulty expansion, prices and unrelated transport stay
unchanged. List 109 changes a floor; 133 exits. List 114 remains an independent
review issue: native lookup explicitly excludes its target map 34200.

## Owner-approved task-only admission

| Map | Quest | Allowed saved states |
|---|---|---|
| 29200 异龙之巢 | 355 九转（法系） | Active 0 / Deliver 1 |
| 29400 隐魂之森 | 356 净幢（法系） | Active 0 / Deliver 1 |
| 29900 月牙塔 | 359 天塔（物理系） | Active 0 / Deliver 1 |
| 30000 月轮天经塔 | 361 凌霄（物理系） | Active 0 / Deliver 1 |

Missing/abandoned/Finished 2 hides and refuses these four entries. Client
DungeonQuestAccess uses the existing TaskClient Active/Done receipt, checks on
render and click, and invalidates stale delegates on state changes. Solo entry
no longer inherits the old generic client party check. The server independently
checks each character's persisted log at shared entry, direct transfer and
ordinary instance login restoration. No once-per-task counter was added.

Quest355 has no usable current publisher definition: 无泪公 is unplaced and the
existing disabled-task filter removes its definition, **not its saved log**.
The server now projects only a genuinely saved unfinished355 record into the
existing 0x8027 with its original task name and saved state/progress. It does not
create a definition, NPC, offer or reward. The table entry's 老道士 is also
unplaced. Admission is fixed; the task's acceptance/completion chain is not
claimed restored. See server docs/task-only-dungeon-admission.md.

Other repeatable reward dungeons are intentionally not gated by associated
quests. Existing trial233 still requires Active only. Rift server-owned handoff
and restoration keep their separate authority when reusing map templates.

## Index and lifecycle safety

The server still sends every authoritative row; the client hides ineligible
rows without renumbering them. Callbacks capture the original wire index. A
quest-state change cannot shift selection into a different dungeon or hard
variant. NPC/list/map/generation checks reject stale callbacks, and task detail
subviews are not forcibly replaced by a state receipt.

The packet has no request nonce. Cross-list/map/closed replies are discarded;
a same-list reply after re-opening the same configured NPC is indistinguishable
on the existing protocol. Do not claim per-request nonce correlation.

## Run and coverage

```sh
source /workspace/shared/client-toolchains/env.sh
python3 Tests/DungeonEntry/run-dungeon-entry-contract.py
python3 Tests/NativeInteractions/run-native-controls-contract.py
python3 Tests/TrialMenu/run-trial-menu-contract.py
python3 Tests/Endgame/run-native-entry-sender-contract.py
python3 Tests/QuestFeedback/run-quest-feedback-contract.py --mono "$FFO_MONO_ROOT/bin/mono" --csc "$FFO_CSC"
python3 Tests/NativeInteractions/run-classic-interaction-contract.py
```

Validated 2026-10-29: 394 actual entry/policy/state checks; 206 integrated
DrawHistory/BuildMenu/dispatch checks; 206 trial-menu checks; 46 native sender
checks; 35 quest-feedback checks; 64 classic-interaction checks. Actual source
owners are compiled with boundary substitutes, not reimplemented assertions
against a separate UI policy. State coverage includes missing/Active/Deliver/
Finished, abandonment/reaccept receipts, stale clicks, mixed hidden rows and
preserved difficulty indices, unrelated services and solo admission.

Full current core (418 files) and Presentation (1237 files) compile against
verified recovered Unity references. No Unity execution, live PostgreSQL,
device rendering or live-server smoke is claimed. The server focused race
suite covers its own authoritative state transitions, full saved-log load,
login restoration, direct requests, disabled355 projection, ordinary farm
party carry and existing trial/Rift entry regressions.

The 71-row fixture remains the output of real server
`WithDungeonDifficultyChoices` over 54 seeded rows: 17 appended hard variants.
It covers only lists101..133, not the two ordinary list1 rows. The fixture has
no original resource bytes and no new eligibility fields.

Development base for task policy: integrated client95b51f5/server29236d2,
containing original client36aab0ec/server6f01fdcf. Current main SHAs rechecked
2026-10-29 04:25 UTC; both unchanged. No release/version/deployment change.
