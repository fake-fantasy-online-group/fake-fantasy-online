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
F="$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation"
"${compile[@]}" -langversion:latest -out:"$OUT/ArchiveResourceInstallContract.exe" \
  "$F/BoundedJson.cs" "$F/JsonData.cs" "$F/ClientUpdatePlan.cs" \
  "$F/MobileResourceManifest.cs" "$F/MobileResourceVerification.cs" \
  "$F/MobileResourceBootstrapConfig.cs" "$F/MobileResourceHttpTransport.cs" \
  "$F/MobileResourceArchive.cs" "$F/MobileResourceTarReader.cs" \
  "$F/MobileRemoteResourceInstall.cs" "$ROOT/Tests/Mobile/ArchiveResourceInstallContract.cs"
"${runtime[@]}" "$OUT/ArchiveResourceInstallContract.exe"
