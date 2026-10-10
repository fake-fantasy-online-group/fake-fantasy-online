#!/usr/bin/env bash
# Standalone UI state/lifetime tests with stubs. Does not launch Unity.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${UNITY_EDITOR_DIR:?Set UNITY_EDITOR_DIR to the installed Editor directory}"
MONO="$UNITY_EDITOR_DIR/Data/MonoBleedingEdge"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
"$MONO/bin/mono" "$MONO/lib/mono/4.5/mcs.exe" -langversion:latest \
  -out:"$OUT/AddonUiTests.exe" \
  "$ROOT"/Assets/FakeFantasyOnline/Runtime/PluginApi/*.cs \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/AddonUiHost.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/AddonUiHost.Manager.cs" \
  "$ROOT/Tests/PluginRuntime/AddonUiTests.cs"
"$MONO/bin/mono" "$OUT/AddonUiTests.exe"
