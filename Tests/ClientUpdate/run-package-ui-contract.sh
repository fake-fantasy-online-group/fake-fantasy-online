#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Source Documentation/tools/activate-dotnet-test-toolchain.sh}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
for platform in desktop android windows macos; do
 define=()
 case "$platform" in android) define=(-define:UNITY_ANDROID);; windows) define=(-define:UNITY_STANDALONE_WIN);; macos) define=(-define:UNITY_STANDALONE_OSX);; esac
 "$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest "${define[@]}" -out:"$OUT/$platform.exe" \
  "$ROOT"/Assets/FakeFantasyOnline/Runtime/Foundation/*.cs \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Bootstrap/MobileResourceInstaller.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Bootstrap/MobileResourceInstaller.ClientPackages.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Bootstrap/ClientUpdateIcons.cs" \
  "$ROOT/Tests/ClientUpdate/UpdateGateUnityStubs.cs" "$ROOT/Tests/ClientUpdate/ClientPackageUiContract.cs"
 "$FFO_MONO_ROOT/bin/mono" "$OUT/$platform.exe"
done
