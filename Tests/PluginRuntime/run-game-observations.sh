#!/usr/bin/env bash
# Compiles only a managed test executable against existing parent-built assemblies.
# No Unity/Player launch, native GameObject creation, connection or gameplay action.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${UNITY_EDITOR_DIR:?Set UNITY_EDITOR_DIR to the installed Editor directory}"
: "${FFO_COMPILE_DIR:?Set FFO_COMPILE_DIR to the existing aggregate assembly directory}"
MONO="$UNITY_EDITOR_DIR/Data/MonoBleedingEdge"
ENGINE="$UNITY_EDITOR_DIR/Data/Managed/UnityEngine"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
"$MONO/bin/mono" "$MONO/lib/mono/4.5/mcs.exe" -langversion:latest \
  -r:"$MONO/lib/mono/4.7.1-api/Facades/netstandard.dll" \
  -r:"$FFO_COMPILE_DIR/FakeFantasyOnline.Foundation.dll" \
  -r:"$FFO_COMPILE_DIR/FakeFantasyOnline.Application.dll" \
  -r:"$FFO_COMPILE_DIR/FakeFantasyOnline.PluginApi.dll" \
  -r:"$FFO_COMPILE_DIR/FakeFantasyOnline.Presentation.dll" \
  -r:"$ENGINE/UnityEngine.CoreModule.dll" \
  -out:"$OUT/GameObservationTests.exe" "$ROOT/Tests/PluginRuntime/GameObservationTests.cs"
MONO_PATH="$FFO_COMPILE_DIR:$ENGINE" "$MONO/bin/mono" "$OUT/GameObservationTests.exe"
