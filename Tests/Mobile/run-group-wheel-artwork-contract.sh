#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
P="$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"
find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" -nologo -langversion:9.0 -out:"$OUT/Contract.exe" "$P/MobileGroupWheel.cs" "$P/MobileGroupWheelArtwork.cs" "$ROOT/Tests/Mobile/MobileGroupWheelArtworkContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/Contract.exe" "$@"
