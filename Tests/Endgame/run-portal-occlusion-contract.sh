#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?}" "${FFO_CSC:?}" "${FFO_FRAMEWORK_DIR:?}"
OUT="$(mktemp -d)";trap 'rm -rf "$OUT"' EXIT
find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" -nologo -langversion:9.0 -out:"$OUT/Test.exe" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ReferencePortalApertureProfile.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ReferenceLegacyMapObjectRenderer.PortalAperture.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ReferenceLegacyMapObjectRenderer.Footprint.cs" \
 "$ROOT/Tests/Endgame/PortalOcclusionStubs.cs" "$ROOT/Tests/Endgame/PortalOcclusionContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/Test.exe"
python3 "$ROOT/Tests/Endgame/portal_occlusion_source_contract.py"
