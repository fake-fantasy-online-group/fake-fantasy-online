#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
# Offline activation of the already verified toolchain. No package downloads.
source "$ROOT/Documentation/tools/activate-dotnet-test-toolchain.sh"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
"$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/ParallelResourceDownloadContract.exe" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceManifest.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceVerification.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceBootstrapConfig.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceHttpTransport.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileRemoteResourceInstall.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceArchive.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceTarReader.cs" \
  "$ROOT/Tests/Mobile/ParallelResourceDownloadContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/ParallelResourceDownloadContract.exe"
