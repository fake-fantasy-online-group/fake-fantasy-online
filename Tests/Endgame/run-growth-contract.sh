#!/usr/bin/env bash
# Managed production V5 DTO/state/tooltip helper only. No Unity or Player build.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Set FFO_MONO_ROOT}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
"$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/Growth.exe" \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Foundation/{JsonData,BoundedJson}.cs \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Application/Endgame*.cs \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/Win05.GrowthSkills.cs" \
 "$ROOT/Tests/Endgame/EndgameGrowthContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/Growth.exe"
