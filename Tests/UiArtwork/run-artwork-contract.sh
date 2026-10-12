#!/usr/bin/env bash
# Standalone source contracts using Unity API stubs. This is not a Unity/PlayMode test.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
if [[ -n "${FFO_MONO_ROOT:-}" ]]; then MONO="$FFO_MONO_ROOT"; else : "${UNITY_EDITOR_DIR:?Set FFO_MONO_ROOT or UNITY_EDITOR_DIR}"; MONO="$UNITY_EDITOR_DIR/Data/MonoBleedingEdge"; fi
OUT="$(mktemp -d)"
trap 'rm -f "$OUT/ArtworkSkinContract.exe"; rmdir "$OUT"' EXIT
"$MONO/bin/mono" "$MONO/lib/mono/4.5/mcs.exe" \
  -out:"$OUT/ArtworkSkinContract.exe" \
  "$ROOT/Tests/UiArtwork/ArtworkSkinContract.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ArtworkSkin.cs"
"$MONO/bin/mono" "$OUT/ArtworkSkinContract.exe"
