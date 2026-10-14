#!/usr/bin/env bash
# Actual world-entry partial against existing deterministic UI substitutes, no engine launch.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
if [[ -n "${FFO_MONO_ROOT:-}" ]]; then MONO="$FFO_MONO_ROOT"; else : "${UNITY_EDITOR_DIR:?Set FFO_MONO_ROOT or UNITY_EDITOR_DIR}"; MONO="$UNITY_EDITOR_DIR/Data/MonoBleedingEdge"; fi
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
"$MONO/bin/mono" "$MONO/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/EndgameWorldEntryContract.exe" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/JsonData.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/BoundedJson.cs" \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Application/Endgame*.cs \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/EndgamePanel*.cs \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/RiftElite*.cs \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/DialogUI.RiftEntrance.cs" \
 "$ROOT/Tests/Endgame/RiftEntranceDialogStubs.cs" \
 "$ROOT/Tests/Endgame/EndgameUiStubs.cs" "$ROOT/Tests/Endgame/EndgameWorldEntryContract.cs"
"$MONO/bin/mono" "$OUT/EndgameWorldEntryContract.exe"
python3 "$ROOT/Tests/Endgame/world_entry_source_contract.py"
