#!/usr/bin/env bash
# Full production monster status owner against deterministic Unity/resource substitutes.
# No network, Unity runtime, Player build, or asset mutation.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
if [[ -n "${FFO_MONO_ROOT:-}" ]]; then MONO="$FFO_MONO_ROOT"; else : "${UNITY_EDITOR_DIR:?Set FFO_MONO_ROOT or UNITY_EDITOR_DIR}"; MONO="$UNITY_EDITOR_DIR/Data/MonoBleedingEdge"; fi
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
"$MONO/bin/mono" "$MONO/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/StatusIcons.exe" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/MonsterEntity.StatusIcons.cs" \
 "$ROOT/Tests/Endgame/MonsterStatusIconReadinessContract.cs"
"$MONO/bin/mono" "$OUT/StatusIcons.exe"
