#!/usr/bin/env bash
# Exercises actual vendored MoonSharp + addon host against fake game and UI; never launches Unity.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
if [[ -n "${FFO_MONO_ROOT:-}" ]]; then MONO="$FFO_MONO_ROOT"; else : "${UNITY_EDITOR_DIR:?Set FFO_MONO_ROOT or UNITY_EDITOR_DIR}"; MONO="$UNITY_EDITOR_DIR/Data/MonoBleedingEdge"; fi
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
find "$ROOT/Assets/ThirdParty/MoonSharp/Interpreter" -name '*.cs' -print > "$OUT/sources.rsp"
printf '%s\n' "$ROOT"/Assets/FakeFantasyOnline/Runtime/PluginApi/*.cs \
  "$ROOT"/Assets/FakeFantasyOnline/Runtime/PluginHost/Addon*.cs \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/JsonData.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/BoundedJson.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/PluginHost/GameJson.cs" \
  "$ROOT/Tests/PluginRuntime/AddonRuntimeTests.cs" >> "$OUT/sources.rsp"
"$MONO/bin/mono" "$MONO/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/AddonRuntimeTests.exe" @"$OUT/sources.rsp"
"$MONO/bin/mono" "$OUT/AddonRuntimeTests.exe" "$ROOT/Assets/StreamingAssets/AddOns"
