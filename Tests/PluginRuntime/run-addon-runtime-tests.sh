#!/usr/bin/env bash
# Exercises actual vendored MoonSharp + addon host against fake game and UI; never launches Unity.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
if [[ -n "${FFO_MONO_ROOT:-}" ]]; then MONO="$FFO_MONO_ROOT"; else : "${UNITY_EDITOR_DIR:?Set FFO_MONO_ROOT or UNITY_EDITOR_DIR}"; MONO="$UNITY_EDITOR_DIR/Data/MonoBleedingEdge"; fi
: "${FFO_PLUGIN_FRAMEWORK_PACKAGE:?Set FFO_PLUGIN_FRAMEWORK_PACKAGE to the framework UPM checkout}"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
find "$FFO_PLUGIN_FRAMEWORK_PACKAGE/Runtime/ThirdParty/MoonSharp/Interpreter" -name '*.cs' -print > "$OUT/sources.rsp"
printf '%s\n' "$ROOT"/Assets/FakeFantasyOnline/Runtime/PluginApi/*.cs \
  "$FFO_PLUGIN_FRAMEWORK_PACKAGE"/Runtime/Host/*.cs \
  "$ROOT"/Assets/FakeFantasyOnline/Runtime/Foundation/*.cs \
  "$ROOT/Tests/PluginRuntime/AddonRuntimeTests.cs" >> "$OUT/sources.rsp"
"$MONO/bin/mono" "$MONO/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/AddonRuntimeTests.exe" @"$OUT/sources.rsp"
"$MONO/bin/mono" "$OUT/AddonRuntimeTests.exe" "$ROOT/Assets/StreamingAssets/AddOns"
