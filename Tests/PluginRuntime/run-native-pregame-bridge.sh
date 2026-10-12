#!/usr/bin/env bash
# Production bridge/login dispatch against isolated native/Unity test doubles; no network.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
if [[ -n "${FFO_MONO_ROOT:-}" ]]; then MONO="$FFO_MONO_ROOT"; else : "${UNITY_EDITOR_DIR:?Set FFO_MONO_ROOT or UNITY_EDITOR_DIR}"; MONO="$UNITY_EDITOR_DIR/Data/MonoBleedingEdge"; fi
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
"$MONO/bin/mono" "$MONO/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/tests.exe" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/TitleFlow.PluginBridge.Contracts.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/TitleFlow.PluginBridge.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/TitleFlow.AccountConnections.cs" \
 "$ROOT/Tests/PluginRuntime/NativePregameBridgeTests.cs"
"$MONO/bin/mono" "$OUT/tests.exe"
python3 "$ROOT/Tests/PluginRuntime/check-native-pregame-bridge.py"
