# Native title and pregame adapter

`game.observe` domain `title` is available before the world exists. It reports the native page, session/loading/update/modal gates, opaque title instance, exact decimal revision, bounded server and character pages, selected identities, native numeric result code and an allowlisted result category. Native account/password and arbitrary server error text are never part of the snapshot. `characterId` is the native character name, not a numeric world entity or persistent ID.

Copy `expectedTitleInstanceId` and `expectedTitleRevision` from a fresh title observation into every title action:

- `title.submit_login`: submit credentials already present in native UI; accepts no account/password arguments. Optional `timeoutSeconds` is 1–120 (default 30).
- `title.select_server`: copy `serverId` from an observed row. This selects locally.
- `title.confirm_server`: invoke native confirmation. An endpoint change can return to Login; inspect `requiresLogin` and observe again.
- `title.select_character`: copy the exact observed `characterId`; no character creation/deletion is exposed.
- `title.enter_world`: invoke native entry for the selected character. Optional timeout has the same bounds as login.

Selection/confirmation completion is explicitly local. Login completes only on a new native successful callback. World entry completes only after the matching character appears in the connected, newly built native world. A loading gate, update requirement, blocking modal, wrong page, stale guard or missing/ambiguous identity rejects the request. Reusing the same request ID and content returns the retained receipt without resubmitting. Do not automatically retry timeouts or unknown outcomes: a submitted native operation may still finish.

For credential entry, use the separately authorized `sensitive-login` transport described in `EXTERNAL_AGENTS.md`, or the native UI. Credentials must never be placed in `game.act`, request IDs, logs, transcripts, observations or addon storage. Sensitive submission is ephemeral and produces no retained game action or action event. Title actions reject unknown arguments before action retention, with fixed error text that does not echo their names or values. Dedicated credential submission does not save preferences.

## Offline verification

Set `FFO_MONO_ROOT` to a MonoBleedingEdge directory and run:

```sh
Tests/PluginRuntime/run-game-actions.sh
bash Tests/PluginRuntime/run-native-pregame-bridge.sh
```

The adapter suite executes production title action and projection partials against deterministic native-owner substitutes. It covers stage/loading/update/modal gating, exact revision/instance guards, unknown secret argument rejection before retention, retry deduplication, native server/character selection, local confirmation, callback errors and timeout, owner replacement, native title teardown, matching/mismatched world entry, redacted bounded observations, immutable snapshots and ephemeral sensitive submission. The native suite separately exercises actual bridge and account-connection source with substitutes and source-wiring checks.

These tests do not launch Unity, connect to a server or perform a real login. Native rendering, complete Presentation compilation against Unity assemblies, installed-client integration and real-server pregame/world entry require separate verification.

## Toolkit sequence

Run against the already-enabled trusted-local bridge. The placeholders below are
copied from observations; never reuse guards after a selection or transition.

```sh
ffo-play observe title
ffo-play observe world
ffo-play sensitive-login --title-instance TITLE_INSTANCE --title-revision TITLE_REVISION --allow-actions
ffo-play observe title
```

Wait for a new successful native login result and `page: ServerSelection`. Copy
fresh guards and the chosen row's `serverId`, then submit one decision at a time:

```sh
ffo-play act title.select_server --scene SCENE_REVISION --api-instance API_INSTANCE --args '{"expectedTitleInstanceId":"TITLE_INSTANCE","expectedTitleRevision":"TITLE_REVISION","serverId":"server:INDEX"}' --allow-actions
ffo-play observe title
ffo-play observe world
ffo-play act title.confirm_server --scene SCENE_REVISION --api-instance API_INSTANCE --args '{"expectedTitleInstanceId":"TITLE_INSTANCE","expectedTitleRevision":"TITLE_REVISION"}' --allow-actions
ffo-play observe title
```

If native confirmation requires reauthentication, return to the login step with
fresh observations. On `CharacterSelection`, use the exact chosen native name:

```sh
ffo-play act title.select_character --scene SCENE_REVISION --api-instance API_INSTANCE --args '{"expectedTitleInstanceId":"TITLE_INSTANCE","expectedTitleRevision":"TITLE_REVISION","characterId":"OBSERVED_CHARACTER_NAME"}' --allow-actions
ffo-play observe title
ffo-play observe world
ffo-play act title.enter_world --scene SCENE_REVISION --api-instance API_INSTANCE --args '{"expectedTitleInstanceId":"TITLE_INSTANCE","expectedTitleRevision":"TITLE_REVISION"}' --allow-actions
ffo-play wait ACTION_ID --seconds 30
ffo-play observe world
```

Verify `title.world_entered` and current `worldReady:true`. Timeouts, unknown
receipts, loading flags or raw submission do not establish success. A changed
scene/API identity requires rebuilding all decision snapshots. Never paste a
password into any of these JSON arguments or substitute it in a shell command.
