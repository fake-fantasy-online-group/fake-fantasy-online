#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
"$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/Contract.exe" \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Foundation/{JsonData,BoundedJson}.cs \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Application/Endgame*.cs \
 "$ROOT/Tests/Endgame/LegendaryItemContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/Contract.exe"
