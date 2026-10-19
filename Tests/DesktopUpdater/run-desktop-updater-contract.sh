#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Source Documentation/tools/activate-dotnet-test-toolchain.sh}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
F="$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation"
"$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest \
 -main:DesktopUpdaterContract -out:"$OUT/DesktopUpdaterContract.exe" \
 "$F/BoundedJson.cs" "$F/JsonData.cs" "$F/MobileResourceManifest.cs" "$F/MobileResourceBootstrapConfig.cs" \
 "$F/ClientUpdatePlan.cs" "$F/DesktopUpdateContract.cs" "$ROOT"/Tools/DesktopUpdater/*.cs "$ROOT/Tests/DesktopUpdater/DesktopUpdaterContract.cs"
python3 "$ROOT/Tests/DesktopUpdater/make-package-fixtures.py" "$OUT/packaged"
"$FFO_MONO_ROOT/bin/mono" "$OUT/DesktopUpdaterContract.exe" "$OUT/packaged/windows.zip" "$OUT/packaged/macos.zip"
