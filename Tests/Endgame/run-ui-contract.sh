#!/usr/bin/env bash
# Compiles actual endgame panel partials against deterministic Unity/artwork substitutes.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
if [[ -n "${FFO_MONO_ROOT:-}" ]]; then MONO="$FFO_MONO_ROOT"; else : "${UNITY_EDITOR_DIR:?Set FFO_MONO_ROOT or UNITY_EDITOR_DIR}"; MONO="$UNITY_EDITOR_DIR/Data/MonoBleedingEdge"; fi
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
"$MONO/bin/mono" "$MONO/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/EndgameUiContract.exe" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/JsonData.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/BoundedJson.cs" \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Application/Endgame*.cs \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/EndgamePanel*.cs \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/RiftElite*.cs \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/TooltipContent.Get.cs" \
 "$ROOT/Tests/Endgame/EndgameUiStubs.cs" "$ROOT/Tests/Endgame/EndgameUiContract.cs" "$ROOT/Tests/Endgame/RiftHudProgressContract.cs" "$ROOT/Tests/Endgame/RiftQuietUiContract.cs" "$ROOT/Tests/Endgame/RiftWorldVisualContract.cs" "$ROOT/Tests/Endgame/RiftEliteVisualContract.cs"
FFO_RIFT_WIRE_FIXTURES="${FFO_RIFT_WIRE_FIXTURES:-$ROOT/Tests/Endgame/Fixtures/rift79-quiet-wire.json}" FFO_ENHANCED_TOOLTIP_FIXTURES="${FFO_ENHANCED_TOOLTIP_FIXTURES:-$ROOT/Tests/Endgame/Fixtures/equipment-local-base-colors.json}" "$MONO/bin/mono" "$OUT/EndgameUiContract.exe"
python3 "$ROOT/Tests/Endgame/source_contract.py"
python3 "$ROOT/Tests/Endgame/elite_visual_source_contract.py"
