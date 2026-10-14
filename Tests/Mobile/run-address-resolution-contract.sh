#!/usr/bin/env bash
# Actual complete Protocol assembly; only request, parse, wait and trace capabilities are substituted.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Source Documentation/tools/activate-dotnet-test-toolchain.sh}"
: "${FFO_COMPILE_DIR:?Run Tests/Endgame/run-managed-compile.sh first}"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
"$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest \
  -r:"$FFO_COMPILE_DIR/FakeFantasyOnline.Foundation.dll" \
  -r:"$FFO_COMPILE_DIR/FakeFantasyOnline.Application.dll" \
  -r:"$FFO_COMPILE_DIR/FakeFantasyOnline.Protocol.dll" \
  -out:"$OUT/AddressResolutionContract.exe" "$ROOT/Tests/Mobile/AddressResolutionContract.cs"
MONO_PATH="$FFO_COMPILE_DIR${MONO_PATH:+:$MONO_PATH}" "$FFO_MONO_ROOT/bin/mono" "$OUT/AddressResolutionContract.exe"
