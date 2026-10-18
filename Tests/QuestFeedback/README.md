# Quest feedback regression

Run `python3 Tests/QuestFeedback/run-quest-feedback-contract.py --mono /path/to/mono --csc /path/to/mcs.exe`.
The runner compiles the actual task state/command owners, completion receipt adapter,
and prompt layout helper against small deterministic presentation stubs.

Checks cover authoritative hand-in/removal/step advance, unchanged/rejected snapshots,
no transport, unrelated requests, replies after reopening or switching NPC, and a
synchronous receipt. Sending a command alone does not hide the dialog or fabricate
quest completion. Completion closes only the same offer detail that issued the request.

Prompt checks cover long Chinese text, explicit newlines, mixed-width text, surrogate
pairs, narrow viewports, replaced messages, scale/width changes, and cached repeat GUI
passes. Both live and immutable-snapshot renderers use the same measured rows.

This is a product correction to the recovered presentation: the original detail owner
stays open on Complete and ignores offer refresh while in a subview. The added close is
triggered by authoritative task-log change, without changing wire or task rules.
The red prompt retains its original color, five-second lifetime and overhead anchor;
only wrapping and screen-edge placement change.

These tests do not launch Unity or prove real-game visual behavior. A client build must
still verify long red prompts near screen edges and successive/nonterminal/repeatable
NPC hand-ins with actual resources and server receipts.

