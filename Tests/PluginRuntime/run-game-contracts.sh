#!/usr/bin/env bash
# Pure C# contract/JSON/queue tests. Does not launch Unity or execute live gameplay.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
if [[ -n "${FFO_MONO_ROOT:-}" ]]; then MONO="$FFO_MONO_ROOT"; else : "${UNITY_EDITOR_DIR:?Set FFO_MONO_ROOT or UNITY_EDITOR_DIR}"; MONO="$UNITY_EDITOR_DIR/Data/MonoBleedingEdge"; fi
: "${FFO_PLUGIN_FRAMEWORK_PACKAGE:?Set FFO_PLUGIN_FRAMEWORK_PACKAGE to the framework UPM checkout}"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
"$MONO/bin/mono" "$MONO/lib/mono/4.5/mcs.exe" -langversion:latest \
  -out:"$OUT/GameContractsTests.exe" \
  "$ROOT"/Assets/FakeFantasyOnline/Runtime/PluginApi/*.cs \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/JsonData.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/BoundedJson.cs" \
  "$FFO_PLUGIN_FRAMEWORK_PACKAGE/Runtime/Host/GameJson.cs" \
  "$FFO_PLUGIN_FRAMEWORK_PACKAGE/Runtime/Host/MainThreadGameDispatcher.cs" \
  "$ROOT/Tests/PluginRuntime/GameContractsTests.cs"
"$MONO/bin/mono" "$OUT/GameContractsTests.exe"
