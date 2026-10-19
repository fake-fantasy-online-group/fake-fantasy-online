#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Source Documentation/tools/activate-dotnet-test-toolchain.sh}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
F="$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation"
"$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/ClientUpdatePlanContract.exe" \
 "$F/BoundedJson.cs" "$F/JsonData.cs" "$F/MobileResourceManifest.cs" "$F/MobileResourceBootstrapConfig.cs" \
 "$F/ClientDownloadRate.cs" "$F/ClientUpdateAddressStore.cs" "$F/ClientResourcePaths.cs" "$F/ClientUpdatePlan.cs" "$ROOT/Tests/Mobile/ClientUpdatePlanContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/ClientUpdatePlanContract.exe"
