#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
if [[ -n "${FFO_MONO_ROOT:-}" ]]; then
  compile=("$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe")
  runtime=("$FFO_MONO_ROOT/bin/mono")
else
  compile=(mcs)
  runtime=(mono)
fi
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
"${compile[@]}" -langversion:latest -out:"$OUT/ResourceVerificationPerformanceContract.exe" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceManifest.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceVerification.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceBootstrapConfig.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceHttpTransport.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileRemoteResourceInstall.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/ClientUpdateAddressStore.cs" \
  "$ROOT/Tests/Mobile/ResourceVerificationPerformanceContract.cs"
"${runtime[@]}" "$OUT/ResourceVerificationPerformanceContract.exe"

"${compile[@]}" -langversion:latest -out:"$OUT/StreamingRangeResourceContract.exe" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceManifest.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceVerification.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceBootstrapConfig.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceHttpTransport.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileRemoteResourceInstall.cs" \
  "$ROOT/Tests/Mobile/StreamingRangeResourceContract.cs"
"${runtime[@]}" "$OUT/StreamingRangeResourceContract.exe"
