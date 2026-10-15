#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?}" "${FFO_CSC:?}" "${FFO_FRAMEWORK_DIR:?}"
RESOURCE_ROOT="${FFO_LOCAL_RESOURCES:-/workspace/shared/client-resources}"
[[ -f "$RESOURCE_ROOT/Assets/StreamingAssets/ReferenceClient/packages/res_q21_001.pkg" ]] || {
 echo 'Set FFO_LOCAL_RESOURCES to the local original-resource directory. Assets are not downloaded or committed.' >&2
 exit 2
}
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" -nologo -langversion:9.0 -out:"$OUT/Contract.exe" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/PkgArchive.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/Gbk.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/GsaDecoder.cs" \
 "$ROOT/Tests/Mobile/MobileHudResourceContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/Contract.exe" "$RESOURCE_ROOT"
