#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
"$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/Contract.exe" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Application/IWorldBuffReceiveOwner.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ReferenceWorldReceiveOwner.BuffState.cs" \
 "$ROOT/Tests/Endgame/LegendaryBuffSnapshotContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/Contract.exe"
