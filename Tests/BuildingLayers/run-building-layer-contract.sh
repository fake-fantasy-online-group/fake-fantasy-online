#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?}" "${FFO_CSC:?}" "${FFO_FRAMEWORK_DIR:?}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
S="$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"
compile(){ "$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" -nologo -langversion:9.0 -out:"$OUT/Test.exe" \
 "$1" "$S/ReferenceLegacyMapObjectRenderer.Footprint.cs" "$S/ReferenceLegacyMapObjectRenderer.PortalAperture.cs" "$S/ReferencePortalApertureProfile.cs" \
 "${2:-$S/ReferenceLegacyMapObjectRenderer.WalkableDeck.cs}" "$S/ReferenceWalkableDeckProfile.cs" "$S/ReferenceWalkableDeckProfile.MaskCorridors.cs" "$S/ReferenceWalkableDeckProfile.StaticSurfaces.cs" "$S/ReferenceWalkableDeckProfile.AuthoredSurfaces.cs" \
 "$S/GameBootstrap.ObjectAdmission.cs" "$S/GameBootstrap.ObjectSlices.cs" "$S/GameBootstrap.GroundBandOrder.cs" \
 "$ROOT/Tests/BuildingLayers/BuildingLayerUnityStubs.cs" "$ROOT/Tests/BuildingLayers/BuildingLayerContract.cs" "$ROOT/Tests/BuildingLayers/BuildingSupportContract.cs" "$ROOT/Tests/BuildingLayers/BuildingMixedPzContract.cs" "$ROOT/Tests/BuildingLayers/BuildingStaticContract.cs" "$ROOT/Tests/BuildingLayers/BuildingAuthoredContract.cs"; }
compile "$S/ReferenceLegacyMapObjectRenderer.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/Test.exe" "$ROOT/Tests/BuildingLayers/BuildingSupportCases.json"
# Negative control: bypass only the live deck dispatch, without changing tests or geometry.
python3 - "$S/ReferenceLegacyMapObjectRenderer.cs" "$OUT/Renderer.cs" <<'PY'
import sys
s=open(sys.argv[1]).read();old='TryPlaceWalkableDeck(receiver, value, parent, pixelsPerUnit, worldWidth,'
assert s.count(old)==2
s=s.replace(old,'false && TryPlaceWalkableDeck(receiver, value, parent, pixelsPerUnit, worldWidth,')
open(sys.argv[2],'w').write(s)
PY
compile "$OUT/Renderer.cs"
if "$FFO_MONO_ROOT/bin/mono" "$OUT/Test.exe" "$ROOT/Tests/BuildingLayers/BuildingSupportCases.json" >"$OUT/negative.log" 2>&1; then
 echo 'FAIL: bypassing live entry point was not caught'; exit 1
fi
grep -q 'reachable board is behind the actor' "$OUT/negative.log"
echo 'PASS negative control: bypassed live dispatch fails reachable-board assertion'

# The ordinary-support hook must be independently necessary; CL's authored
# hook still runs, so a CL-only negative cannot hide missing generic dispatch.
python3 - "$S/ReferenceLegacyMapObjectRenderer.cs" "$OUT/RendererGeneric.cs" <<'PYGEN'
import sys
s=open(sys.argv[1]).read();old='TryPlaceWalkableDeck(receiver, value, parent, pixelsPerUnit, worldWidth,'
i=s.rfind(old);assert i>=0
s=s[:i]+'false && '+s[i:]
open(sys.argv[2],'w').write(s)
PYGEN
compile "$OUT/RendererGeneric.cs"
if "$FFO_MONO_ROOT/bin/mono" "$OUT/Test.exe" "$ROOT/Tests/BuildingLayers/BuildingSupportCases.json" >"$OUT/generic-negative.log" 2>&1; then
 echo 'FAIL: bypassing generic support dispatch was not caught'; exit 1
fi
grep -q 'support dispatch reached compiled mesh owner' "$OUT/generic-negative.log"
echo 'PASS negative control: CL passes but bypassed generic entry fails support mesh assertion'

python3 - "$S/ReferenceLegacyMapObjectRenderer.WalkableDeck.cs" "$OUT/TextureOnlyCache.cs" <<'PYCACHE'
import sys
s=open(sys.argv[1]).read();old='var cacheKey = (image.tex, value.PivotZ);';assert old in s
open(sys.argv[2],'w').write(s.replace(old,'var cacheKey = (image.tex, 0);'))
PYCACHE
compile "$S/ReferenceLegacyMapObjectRenderer.cs" "$OUT/TextureOnlyCache.cs"
if "$FFO_MONO_ROOT/bin/mono" "$OUT/Test.exe" "$ROOT/Tests/BuildingLayers/BuildingSupportCases.json" >"$OUT/cache-negative.log" 2>&1; then
 echo 'FAIL: texture-only cache negative was not caught'; exit 1
fi
grep -q 'mixed-PZ source-column order mismatch' "$OUT/cache-negative.log"
echo 'PASS negative control: shared texture with different PZ rejects texture-only cache'

# Static fallback candidates have their own admission path; generic/CL passing
# must not hide a missing static-support hook.
python3 - "$S/ReferenceLegacyMapObjectRenderer.WalkableDeck.cs" "$OUT/StaticDisabled.cs" <<'PYSTATIC'
import sys
s=open(sys.argv[1]).read();old='staticDeck = ReferenceWalkableDeckProfile.TryGetStaticDeck';assert old in s
open(sys.argv[2],'w').write(s.replace(old,'staticDeck = false && ReferenceWalkableDeckProfile.TryGetStaticDeck'))
PYSTATIC
compile "$S/ReferenceLegacyMapObjectRenderer.cs" "$OUT/StaticDisabled.cs"
if "$FFO_MONO_ROOT/bin/mono" "$OUT/Test.exe" "$ROOT/Tests/BuildingLayers/BuildingSupportCases.json" >"$OUT/static-negative.log" 2>&1; then
 echo 'FAIL: static support dispatch negative was not caught'; exit 1
fi
grep -q 'static live repair dispatch reached' "$OUT/static-negative.log"
echo 'PASS negative control: disabling only static support fails its live dispatch contract'

python3 - "$S/ReferenceLegacyMapObjectRenderer.WalkableDeck.cs" "$OUT/AuthoredDisabled.cs" <<'PYAUTH'
import sys
s=open(sys.argv[1]).read();old='stage || ReferenceWalkableDeckProfile.TryGetAuthoredDeck';assert old in s
open(sys.argv[2],'w').write(s.replace(old,'stage || false && ReferenceWalkableDeckProfile.TryGetAuthoredDeck'))
PYAUTH
compile "$S/ReferenceLegacyMapObjectRenderer.cs" "$OUT/AuthoredDisabled.cs"
if "$FFO_MONO_ROOT/bin/mono" "$OUT/Test.exe" "$ROOT/Tests/BuildingLayers/BuildingSupportCases.json" >"$OUT/authored-negative.log" 2>&1; then
 echo 'FAIL: additional authored-support dispatch negative was not caught'; exit 1
fi
grep -q 'authored support live dispatch reached' "$OUT/authored-negative.log"
echo 'PASS negative control: disabling only additional authored support fails its live dispatch contract'
