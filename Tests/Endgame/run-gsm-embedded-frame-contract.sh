#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?}" "${FFO_CSC:?}" "${FFO_FRAMEWORK_DIR:?}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
SOURCE="$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"
find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" -nologo -langversion:9.0 -out:"$OUT/Test.exe" \
  "$SOURCE/GsmMap.cs" "$SOURCE/GsmMap.Sky.cs" \
  "$SOURCE/ReferenceGsmCover.cs" "$SOURCE/ReferenceGsmFieldColor.cs" \
  "$SOURCE/ReferenceGsmObjectReader.cs" "$SOURCE/ReferenceGsmEmbeddedObjects.cs" \
  "$ROOT/Tests/Endgame/GsmEmbeddedFrameContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/Test.exe" "$@"
