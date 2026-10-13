#!/usr/bin/env bash
# Execute the production diagnostic tooltip with deterministic substitutes, not Unity.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
if [[ -n "${FFO_MONO_ROOT:-}" ]]; then MONO="$FFO_MONO_ROOT"; else : "${UNITY_EDITOR_DIR:?Set FFO_MONO_ROOT or UNITY_EDITOR_DIR}"; MONO="$UNITY_EDITOR_DIR/Data/MonoBleedingEdge"; fi
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
"$MONO/bin/mono" "$MONO/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/ReferenceTooltipContract.exe" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ReferenceFirstWorldUiTip.cs" \
 "$ROOT/Tests/Endgame/ReferenceTooltipStubs.cs" "$ROOT/Tests/Endgame/ReferenceTooltipContract.cs"
"$MONO/bin/mono" "$OUT/ReferenceTooltipContract.exe"
python3 "$ROOT/Tests/Endgame/tooltip_source_contract.py"
