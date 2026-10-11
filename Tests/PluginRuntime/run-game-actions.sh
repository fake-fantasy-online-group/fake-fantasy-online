#!/usr/bin/env bash
# Isolated compiler/test runner. Never launches Unity, a game, or a server connection.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
if [[ -n "${FFO_MONO_ROOT:-}" ]]; then MONO="$FFO_MONO_ROOT"; else : "${UNITY_EDITOR_DIR:?Set FFO_MONO_ROOT or UNITY_EDITOR_DIR}"; MONO="$UNITY_EDITOR_DIR/Data/MonoBleedingEdge"; fi
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
"$MONO/bin/mono" "$MONO/lib/mono/4.5/mcs.exe" -langversion:latest \
  -out:"$OUT/GameActionAdapterTests.exe" \
  "$ROOT"/Assets/FakeFantasyOnline/Runtime/PluginApi/*.cs \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/JsonData.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/BoundedJson.cs" \
  "$ROOT"/Assets/FakeFantasyOnline/Runtime/Application/Endgame*.cs \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/Result.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/PluginGameApi.Actions.cs" \
  "$ROOT"/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/PluginGameApi.Endgame*.cs \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/PlayerController.PluginActions.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/CombatWorld.PluginActions.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/NetClient.PluginActions.cs" \
  "$ROOT/Tests/PluginRuntime/GameActionAdapterTests.cs" \
  "$ROOT/Tests/PluginRuntime/EndgameActionAdapterTests.cs"
"$MONO/bin/mono" "$OUT/GameActionAdapterTests.exe"
