#!/usr/bin/env bash
# Exercises actual vendored MoonSharp + addon host against fake game and UI; never launches Unity.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${UNITY_EDITOR_DIR:?Set UNITY_EDITOR_DIR to the Unity Editor directory containing Data/MonoBleedingEdge}"
MONO="$UNITY_EDITOR_DIR/Data/MonoBleedingEdge"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
find "$ROOT/Assets/ThirdParty/MoonSharp/Interpreter" -name '*.cs' -print > "$OUT/sources.rsp"
printf '%s\n' "$ROOT"/Assets/FakeFantasyOnline/Runtime/PluginApi/*.cs \
  "$ROOT"/Assets/FakeFantasyOnline/Runtime/PluginHost/Addon*.cs \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/PluginHost/GameJson.cs" \
  "$ROOT/Tests/PluginRuntime/AddonRuntimeTests.cs" >> "$OUT/sources.rsp"
"$MONO/bin/mono" "$MONO/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/AddonRuntimeTests.exe" @"$OUT/sources.rsp"
"$MONO/bin/mono" "$OUT/AddonRuntimeTests.exe" "$ROOT/Assets/StreamingAssets/AddOns"
