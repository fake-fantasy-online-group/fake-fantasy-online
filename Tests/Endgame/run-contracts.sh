#!/usr/bin/env bash
# Pure managed tests; never launches Unity, a Player, or a server.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
if [[ -n "${FFO_MONO_ROOT:-}" ]]; then MONO="$FFO_MONO_ROOT"; else : "${UNITY_EDITOR_DIR:?Set FFO_MONO_ROOT or UNITY_EDITOR_DIR}"; MONO="$UNITY_EDITOR_DIR/Data/MonoBleedingEdge"; fi
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
"$MONO/bin/mono" "$MONO/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/EndgameContracts.exe" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/JsonData.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/BoundedJson.cs" \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Application/Endgame*.cs \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Protocol/PrivateServerEndgameCodec.cs" \
 "$ROOT/Tests/Endgame/EndgameContracts.cs"
"$MONO/bin/mono" "$OUT/EndgameContracts.exe" "$ROOT/Tests/Endgame/Fixtures"
