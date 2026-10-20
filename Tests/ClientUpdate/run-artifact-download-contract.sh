#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Source Documentation/tools/activate-dotnet-test-toolchain.sh}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
F="$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation"
"$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/ClientArtifactDownloadContract.exe" \
 "$F/BoundedJson.cs" "$F/JsonData.cs" "$F/MobileResourceManifest.cs" "$F/MobileResourceBootstrapConfig.cs" \
 "$F/MobileResourceArchive.cs" "$F/ClientUpdatePlan.cs" "$F/MobileResourceHttpTransport.cs" "$F/ClientArtifactDownload.cs" \
 "$ROOT/Tests/ClientUpdate/ClientArtifactDownloadContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/ClientArtifactDownloadContract.exe"
