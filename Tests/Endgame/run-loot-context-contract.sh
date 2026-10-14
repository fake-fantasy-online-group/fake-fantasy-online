#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
if [[ -n "${FFO_MONO_ROOT:-}" ]]; then MONO="$FFO_MONO_ROOT"; else : "${UNITY_EDITOR_DIR:?Set FFO_MONO_ROOT or UNITY_EDITOR_DIR}"; MONO="$UNITY_EDITOR_DIR/Data/MonoBleedingEdge"; fi
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
"$MONO/bin/mono" "$MONO/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/EndgameLootContextContract.exe" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/JsonData.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/BoundedJson.cs" \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Application/Endgame*.cs \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/EndgamePanel*.cs \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/RiftElite*.cs \
 "$ROOT/Tests/Endgame/EndgameUiStubs.cs" "$ROOT/Tests/Endgame/EndgameLootContextContract.cs"
"$MONO/bin/mono" "$OUT/EndgameLootContextContract.exe"
