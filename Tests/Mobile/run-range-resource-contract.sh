#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Source Documentation/tools/activate-dotnet-test-toolchain.sh or set FFO_MONO_ROOT}"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
"$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/RangeResourceContract.exe" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceHttpTransport.cs" \
  "$ROOT/Tests/Mobile/RangeResourceContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/RangeResourceContract.exe"
