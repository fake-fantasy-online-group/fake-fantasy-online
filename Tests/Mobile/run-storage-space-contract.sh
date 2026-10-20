#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
SOURCES=("$ROOT/Assets/FakeFantasyOnline/Runtime/Bootstrap/ClientStorageSpace.cs"
    "$ROOT/Tests/Mobile/StorageSpaceUnityStubs.cs" "$ROOT/Tests/Mobile/ClientStorageSpaceContract.cs")
for platform in android desktop android-editor; do
    defines=()
    case "$platform" in android) defines=(-define:UNITY_ANDROID);; android-editor) defines=(-define:UNITY_ANDROID,UNITY_EDITOR);; esac
    if [[ -n "${FFO_DOTNET_CSC:-}" ]]; then
        : "${FFO_DOTNET:?Set FFO_DOTNET}" "${FFO_DOTNET_FRAMEWORK_DIR:?Set FFO_DOTNET_FRAMEWORK_DIR}"
        find "$FFO_DOTNET_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
        "$FFO_DOTNET" exec "$FFO_DOTNET_CSC" -nologo -noconfig -nostdlib+ @"$OUT/framework.rsp" "${defines[@]}" -target:exe -out:"$OUT/$platform.dll" "${SOURCES[@]}"
        printf '{"runtimeOptions":{"tfm":"net6.0","framework":{"name":"Microsoft.NETCore.App","version":"6.0.0"},"rollForward":"LatestMajor"}}\n' > "$OUT/$platform.runtimeconfig.json"
        "$FFO_DOTNET" "$OUT/$platform.dll"
    else
        mcs "${defines[@]}" -out:"$OUT/$platform.exe" "${SOURCES[@]}"
        mono "$OUT/$platform.exe"
    fi
done
