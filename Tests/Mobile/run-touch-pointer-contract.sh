#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?}" "${FFO_CSC:?}" "${FFO_FRAMEWORK_DIR:?}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" -nologo -langversion:9.0 -out:"$OUT/Contract.exe" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/TouchPointerRouter.cs" \
 "$ROOT/Tests/Mobile/TouchPointerStubs.cs" "$ROOT/Tests/Mobile/TouchPointerContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/Contract.exe"
