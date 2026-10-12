#!/usr/bin/env bash
# Runs production avatar selection/build/pose/depth/decoder/cache code with headless Unity textures.
# Optional: FFO_AVATAR_EVIDENCE_DIR contains weapon_dual.json and resources/avatar from authorized assets.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?}" "${FFO_CSC:?}" "${FFO_FRAMEWORK_DIR:?}" "${FFO_COMPILE_DIR:?}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
export MONO_PATH="$FFO_COMPILE_DIR:$FFO_FRAMEWORK_DIR/Facades${MONO_PATH:+:$MONO_PATH}"
find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
P="$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" -nologo -langversion:9.0 -out:"$OUT/Contract.exe" \
 -r:"$FFO_COMPILE_DIR/FakeFantasyOnline.Application.dll" -r:"$FFO_COMPILE_DIR/FakeFantasyOnline.Foundation.dll" -r:"$FFO_COMPILE_DIR/FakeFantasyOnline.Domain.dll" \
 "$P"/PlayerAvatar.*.cs "$P"/DimgDecoder*.cs "$P/HdMagic.cs" "$P/ArtBudget.cs" "$P/AvatarDepth.cs" \
 "$P/SuitFxDB.Tables.cs" "$P/SuitFxDB.NestedData.cs" "$P/SuitFxDB.StandAnimation.cs" \
 "$P/ReferenceAvatarWeaponPresentation.cs" "$P/ReferenceAvatarAssetPlan.cs" \
 "$ROOT/Tests/WorldEntry/DualWeaponPresentationStubs.cs" "$ROOT/Tests/WorldEntry/DualWeaponPresentationContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/Contract.exe" "$@"
