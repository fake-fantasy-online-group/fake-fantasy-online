#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Set FFO_MONO_ROOT to supported Mono runtime}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
"$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/RiftScenePortalContract.exe" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/JsonData.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/BoundedJson.cs" \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Application/Endgame*.cs \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/EndgamePanel*.cs \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/RiftElite*.cs \
 "$ROOT/Tests/Endgame/EndgameUiStubs.cs" "$ROOT/Tests/Endgame/RiftScenePortalContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/RiftScenePortalContract.exe"
