#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Source Documentation/tools/activate-dotnet-test-toolchain.sh or set FFO_MONO_ROOT}"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
"$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/RemoteResourceContract.exe" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceManifest.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceVerification.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceBootstrapConfig.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceHttpTransport.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileRemoteResourceInstall.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceArchive.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceTarReader.cs" \
  "$ROOT/Tests/Mobile/RemoteResourceContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/RemoteResourceContract.exe"
