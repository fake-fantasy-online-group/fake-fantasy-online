#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -nologo -noconfig -nostdlib+ -langversion:9.0 \
 -r:"$FFO_FRAMEWORK_DIR/mscorlib.dll" -r:"$FFO_FRAMEWORK_DIR/System.dll" \
 -out:"$OUT/Contract.exe" "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/MobilePageSwipe.cs" "$ROOT/Tests/Mobile/MobilePageSwipeContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/Contract.exe"
