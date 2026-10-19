#!/usr/bin/env bash
# Source Documentation/tools/activate-dotnet-test-toolchain.sh first. No Unity process/build.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Activate managed toolchain}"
: "${FFO_CSC:?Activate managed toolchain}"
: "${FFO_FRAMEWORK_DIR:?Activate managed toolchain}"
P="$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" -nologo -langversion:9.0 -out:"$OUT/Contract.exe" \
 "$P/GameSettings.Persistence.cs" "$P/GameSettings.LocalDisplayConfig.cs" "$P/GameSettings.FrameRate.cs" \
 "$P/GameSettings.Resolution.cs" "$P/GameSettings.HeadHpBar.cs" "$ROOT/Tests/DisplaySettings/LocalDisplayConfigContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/Contract.exe"
