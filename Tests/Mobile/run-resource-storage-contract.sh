#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Source Documentation/tools/activate-dotnet-test-toolchain.sh}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
"$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/ResourceStorageContract.exe" \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Foundation/*.cs "$ROOT/Tests/Mobile/ResourceStorageContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/ResourceStorageContract.exe"
