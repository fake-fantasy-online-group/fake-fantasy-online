#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?}" "${FFO_CSC:?}" "${FFO_FRAMEWORK_DIR:?}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
S="$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"
find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
compile() { "$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" -nologo -langversion:9.0 -out:"$OUT/Contract.exe" \
 "$1" "$S/ReferenceLegacyMapObjectRenderer.Footprint.cs" "$S/GameBootstrap.ObjectSlices.cs" \
 "$S/GameBootstrap.ObjectAdmission.cs" "$S/GameBootstrap.GroundBandOrder.cs" \
 "$ROOT/Tests/BuildingLayers/BuildingLayerUnityStubs.cs" "$ROOT/Tests/BuildingLayers/BuildingGenericContract.cs"; }
compile "$S/ReferenceLegacyMapObjectRenderer.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/Contract.exe"
python3 - "$S/ReferenceLegacyMapObjectRenderer.cs" "$OUT/Negative.cs" <<'PY'
import sys
s=open(sys.argv[1]).read();old='value.SourceY + slice.Item3';assert old in s
open(sys.argv[2],'w').write(s.replace(old,'value.SourceY + slice.Item3 - 1'))
PY
compile "$OUT/Negative.cs"
if "$FFO_MONO_ROOT/bin/mono" "$OUT/Contract.exe" >"$OUT/negative.log" 2>&1; then
 echo 'FAIL: order mutation escaped the contract'; exit 1
fi
grep -q 'generic authored/footprint/static order' "$OUT/negative.log"
echo 'PASS negative control: authored order mutation rejected'
