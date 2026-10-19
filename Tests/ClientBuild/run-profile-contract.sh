#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
SOURCES=("$ROOT/Assets/FakeFantasyOnline/Editor/ResourceFreeBuildProfile.cs" "$ROOT/Assets/FakeFantasyOnline/Editor/DesktopBuildPayload.cs" "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceBootstrapConfig.cs" "$ROOT/Tests/ClientBuild/ResourceFreeBuildProfileContract.cs")
if [[ -n "${FFO_DOTNET_CSC:-}" ]]; then
    : "${FFO_DOTNET:?Set FFO_DOTNET}"
    : "${FFO_DOTNET_FRAMEWORK_DIR:?Set FFO_DOTNET_FRAMEWORK_DIR to installed runtime assemblies}"
    find "$FFO_DOTNET_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
    "$FFO_DOTNET" exec "$FFO_DOTNET_CSC" -nologo -noconfig -nostdlib+ @"$OUT/framework.rsp" -target:exe -out:"$OUT/contract.dll" "${SOURCES[@]}"
    printf '{"runtimeOptions":{"tfm":"net6.0","framework":{"name":"Microsoft.NETCore.App","version":"6.0.0"},"rollForward":"LatestMajor"}}\n' > "$OUT/contract.runtimeconfig.json"
    "$FFO_DOTNET" "$OUT/contract.dll"
else
    mcs -out:"$OUT/contract.exe" "${SOURCES[@]}"
    mono "$OUT/contract.exe"
fi
