#!/usr/bin/env bash
# Pure C# contract/JSON/queue tests. Does not launch Unity or execute live gameplay.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${UNITY_EDITOR_DIR:?Set UNITY_EDITOR_DIR to the Unity Editor directory containing Data/MonoBleedingEdge}"
MONO="$UNITY_EDITOR_DIR/Data/MonoBleedingEdge"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
"$MONO/bin/mono" "$MONO/lib/mono/4.5/mcs.exe" -langversion:latest \
  -out:"$OUT/GameContractsTests.exe" \
  "$ROOT"/Assets/FakeFantasyOnline/Runtime/PluginApi/*.cs \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/JsonData.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/BoundedJson.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/PluginHost/GameJson.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/PluginHost/MainThreadGameDispatcher.cs" \
  "$ROOT/Tests/PluginRuntime/GameContractsTests.cs"
"$MONO/bin/mono" "$OUT/GameContractsTests.exe"
