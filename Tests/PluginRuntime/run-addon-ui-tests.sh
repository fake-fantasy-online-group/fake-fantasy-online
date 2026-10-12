#!/usr/bin/env bash
# Standalone UI state/lifetime tests with stubs. Does not launch Unity.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
if [[ -n "${FFO_MONO_ROOT:-}" ]]; then MONO="$FFO_MONO_ROOT"; else : "${UNITY_EDITOR_DIR:?Set FFO_MONO_ROOT or UNITY_EDITOR_DIR}"; MONO="$UNITY_EDITOR_DIR/Data/MonoBleedingEdge"; fi
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
"$MONO/bin/mono" "$MONO/lib/mono/4.5/mcs.exe" -langversion:latest \
  -out:"$OUT/AddonUiTests.exe" \
  "$ROOT"/Assets/FakeFantasyOnline/Runtime/PluginApi/*.cs \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/JsonData.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/AddonUiHost.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/AddonUiHost.Manager.cs" \
  "$ROOT/Tests/PluginRuntime/AddonUiTests.cs"
"$MONO/bin/mono" "$OUT/AddonUiTests.exe"
