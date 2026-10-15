#!/usr/bin/env bash
# Compile the client composition hook with no framework or AI package inputs.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Set FFO_MONO_ROOT or activate the documented .NET test profile}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
"$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/OptionalRuntime.exe" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Bootstrap/OptionalPluginRuntime.cs" \
 "$ROOT/Tests/PluginRuntime/OptionalPluginRuntimeTests.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/OptionalRuntime.exe"
