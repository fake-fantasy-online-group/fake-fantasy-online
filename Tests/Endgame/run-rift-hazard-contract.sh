#!/usr/bin/env bash
# Actual DTO/clock and native movement sources, with deterministic Unity substitutes.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Set FFO_MONO_ROOT to the official Mono test runtime}"
: "${FFO_CSC:?Set FFO_CSC to official Roslyn csc.exe}"
: "${FFO_FRAMEWORK_DIR:?Set FFO_FRAMEWORK_DIR to Mono 4.7.2-api}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
P="$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ -nologo -langversion:9.0 \
 -r:"$FFO_FRAMEWORK_DIR/mscorlib.dll" -r:"$FFO_FRAMEWORK_DIR/System.dll" -r:"$FFO_FRAMEWORK_DIR/System.Core.dll" \
 -target:exe -out:"$OUT/RiftHazards.exe" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/JsonData.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/BoundedJson.cs" \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Application/Endgame*.cs \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Protocol/PrivateServerEndgameCodec.cs" \
 "$P/PlayerController.Navigation.cs" "$P/PlayerController.Displacement.cs" "$P/PlayerController.Mask.cs" "$P/PlayerController.RiftCollision.cs" \
 "$ROOT/Tests/Endgame/RiftHazardStubs.cs" "$ROOT/Tests/Endgame/RiftHazardContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/RiftHazards.exe" "$ROOT/Tests/Endgame/Fixtures"
