#!/usr/bin/env bash
# Standalone source contracts using Unity API stubs. This is not a Unity/PlayMode test.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${UNITY_EDITOR_DIR:?Set UNITY_EDITOR_DIR to the Unity Editor directory containing Data/MonoBleedingEdge}"
MONO="$UNITY_EDITOR_DIR/Data/MonoBleedingEdge"
OUT="$(mktemp -d)"
trap 'rm -f "$OUT/ArtworkSkinContract.exe"; rmdir "$OUT"' EXIT
"$MONO/bin/mono" "$MONO/lib/mono/4.5/mcs.exe" \
  -out:"$OUT/ArtworkSkinContract.exe" \
  "$ROOT/Tests/UiArtwork/ArtworkSkinContract.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ArtworkSkin.cs"
"$MONO/bin/mono" "$OUT/ArtworkSkinContract.exe"
