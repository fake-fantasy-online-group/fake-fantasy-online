#!/usr/bin/env bash
# Actual host source + actual full managed assemblies; documented Unity/presentation substitutes.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Set FFO_MONO_ROOT}"
: "${FFO_COMPILE_DIR:?Run run-managed-compile.sh and set FFO_COMPILE_DIR}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
"$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest \
 -r:"$FFO_COMPILE_DIR/FakeFantasyOnline.Foundation.dll" -r:"$FFO_COMPILE_DIR/FakeFantasyOnline.Application.dll" -r:"$FFO_COMPILE_DIR/FakeFantasyOnline.Protocol.dll" \
 -out:"$OUT/EndgameHostContract.exe" "$ROOT/Assets/FakeFantasyOnline/Runtime/Bootstrap/NativeNetClientHost.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/INetClientInstanceHost.cs" \
 "$ROOT/Tests/Endgame/EndgameHostStubs.cs" "$ROOT/Tests/Endgame/EndgameHostContract.cs"
MONO_PATH="$FFO_COMPILE_DIR${MONO_PATH:+:$MONO_PATH}" "$FFO_MONO_ROOT/bin/mono" "$OUT/EndgameHostContract.exe"
