#!/usr/bin/env bash
# Local-only helper compilation. Does not install SDKs, sign, upload or publish a release.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
[[ $# == 2 ]] || { echo 'Usage: build-desktop-updater.sh windows|macos OUTPUT_DIRECTORY' >&2; exit 2; }
platform="$1"; output="$2"
[[ "$platform" == windows || "$platform" == macos ]] || { echo 'Unsupported platform' >&2; exit 2; }
command -v dotnet >/dev/null || { echo 'Install an official stable .NET SDK 8 or newer locally before building the packaged helper.' >&2; exit 2; }
sdk="$(dotnet --list-sdks | awk '/^[0-9]+\.[0-9]+\.[0-9]+ / {split($1,a,"."); if(a[1]>=8) v=$1} END{print v}')"
[[ -n "$sdk" ]] || { echo 'An official stable .NET SDK 8 or newer is required.' >&2; exit 2; }
[[ ! -e "$output" ]] || { echo 'Output directory must not already exist.' >&2; exit 2; }
mkdir -p "$output"; output="$(cd "$output" && pwd)"
work="$(mktemp -d)"; trap 'rm -rf "$work"' EXIT
printf '{"sdk":{"version":"%s","rollForward":"latestPatch","allowPrerelease":false}}\n' "$sdk" > "$work/global.json"
cd "$work"
rids=(win-x64); [[ "$platform" == windows ]] || rids=(osx-x64 osx-arm64)
for rid in "${rids[@]}"; do
 dotnet publish "$ROOT/Tools/DesktopUpdater/DesktopUpdater.csproj" -c Release -r "$rid" --self-contained true -o "$work/$rid" \
  -p:BaseIntermediateOutputPath="$work/obj/$rid/" -p:BaseOutputPath="$work/bin/$rid/"
 if [[ "$rid" == win-x64 ]]; then cp "$work/$rid/DesktopUpdater.exe" "$output/DesktopUpdater.exe";
 else cp "$work/$rid/DesktopUpdater" "$output/DesktopUpdater-$rid"; chmod 755 "$output/DesktopUpdater-$rid"; fi
done
printf 'Built unsigned helper candidates in %s. Apply approved platform signing before formal packaging.\n' "$output"
