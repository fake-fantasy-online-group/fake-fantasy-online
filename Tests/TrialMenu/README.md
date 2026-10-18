# Trial guard travel menu regression

Run `python3 Tests/TrialMenu/run-trial-menu-contract.py` with the managed-test environment configured (`FFO_MONO_ROOT`, `FFO_CSC`, `FFO_FRAMEWORK_DIR`). The supported .NET adapter is also accepted. No Unity process is launched.

The runner compiles the actual `DialogUI.BuildMenu`, `OnTaskOffers`, and `AddTrialOption` methods with explicit presentation/network doubles. It checks the four village maps (37, 38, 61, 62), absent/available/active/ready/locked ordinary quest offers, repeated refresh, unrelated replies, detail-view preservation, reopening, exact `trialgo` command intent, and unsupported maps/NPCs. Run `Tests/Endgame/run-native-entry-sender-contract.py` alongside it to verify the production writer's `0x1006` payload and server-first travel behavior.

The missing row came from the client's map-37-only condition. The server already configures four claim villages in `server/migrations/000000_init-42.sql`, and `server/internal/data/trial.go` resolves each destination from its actual guard NPC:

- 37 → 40, guard (11154, 3478)
- 38 → 22, guard (3803, 4040)
- 61 → 41, guard (3341, 4135)
- 62 → 30, guard (9076, 1934)

This correction exposes the existing service in all four configured villages. It keeps the existing 5-silver label, native command, server fee/destination ownership, and quest rows. It adds no client level restriction. Trial acceptance and instance entry retain their server level/quest/party checks; paid travel to the public guard remains a separate service. The city NPC named 试炼大仙 is distinct and receives no inferred travel route.

Server regression: `go test ./internal/session -run TestNativeTrialMenuCommandsReachReadyTerminalState -count=1` from `server/`. This existing end-to-end wire test exercises the map-61 route, missing NPC context, insufficient funds, duplicate requests, authoritative landing/ready acknowledgement, party and quest failures, instance entry, and exit. It does not dynamically exercise the other three routes. Full Unity visual/runtime validation remains separate.
