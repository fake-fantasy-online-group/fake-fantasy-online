#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?}" "${FFO_CSC:?}" "${FFO_FRAMEWORK_DIR:?}"
OUT="$(mktemp -d)";trap 'rm -rf "$OUT"' EXIT
find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
compile(){ "$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" -nologo -langversion:9.0 -out:"$OUT/Test.exe" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ReferencePortalApertureProfile.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ReferenceWalkableDeckProfile.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ReferenceWalkableDeckProfile.MaskCorridors.cs" "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ReferenceWalkableDeckProfile.StaticSurfaces.cs" "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ReferenceWalkableDeckProfile.AuthoredSurfaces.cs" \
 "$1" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ReferenceLegacyMapObjectRenderer.Footprint.cs" \
 "$ROOT/Tests/Endgame/PortalOcclusionStubs.cs" "$ROOT/Tests/Endgame/PortalOcclusionContract.cs"; }
PORTAL="$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ReferenceLegacyMapObjectRenderer.PortalAperture.cs"
compile "$PORTAL"
"$FFO_MONO_ROOT/bin/mono" "$OUT/Test.exe"
python3 "$ROOT/Tests/Endgame/portal_occlusion_source_contract.py"

for mode in actor jamb staticactor staticcache; do
 python3 - "$PORTAL" "$OUT/PortalNegative.cs" "$mode" <<'PYNEG'
import sys
s=open(sys.argv[1]).read()
replacements={
 'actor': ('bool actorDeck = ReferenceWalkableDeckProfile.TryGetMaskDeck','bool actorDeck = false && ReferenceWalkableDeckProfile.TryGetMaskDeck'),
 'jamb': ('int[] jamb = actorDeck ? ReferenceWalkableDeckProfile.PortalJamb(value.Id) : null;', 'int[] jamb = null;'),
 'staticactor': ('bool staticActorDeck = !hasNativeFootprint', 'bool staticActorDeck = false && !hasNativeFootprint'),
 'staticcache': ('var apertureKey = (image.tex, actorDeck);', 'var apertureKey = (image.tex, true);'),
}
old,new=replacements[sys.argv[3]]
assert old in s
open(sys.argv[2],'w').write(s.replace(old,new))
PYNEG
 compile "$OUT/PortalNegative.cs"
 if "$FFO_MONO_ROOT/bin/mono" "$OUT/Test.exe" >"$OUT/negative-$mode.log" 2>&1; then
  echo "FAIL: portal $mode overlay negative was not caught"; exit 1
 fi
 grep -Eq 'exact original/portal/actor per-pixel min oracle|matched/nonmatched static signature cache' "$OUT/negative-$mode.log"
 echo "PASS negative control: disabling portal $mode overlay is caught by pixel-depth oracle"
done
