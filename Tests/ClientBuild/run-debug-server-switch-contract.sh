#!/usr/bin/env bash
# Engine-free contract for the debug-only server switch store. Same toolchain options as run-profile-contract.sh.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
R="$ROOT/Assets/FakeFantasyOnline/Runtime"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
SOURCES=("$R/Foundation/BoundedJson.cs" "$R/Foundation/JsonData.cs" "$R/Application/ClientConfig.cs" "$R/Protocol/PrivateServerOptions.cs" "$R/Protocol/DebugServerSwitchStore.cs" "$ROOT/Tests/ClientBuild/DebugServerSwitchStoreContract.cs")
if [[ -n "${FFO_DOTNET_CSC:-}" ]]; then
    : "${FFO_DOTNET:?Set FFO_DOTNET}"
    : "${FFO_DOTNET_FRAMEWORK_DIR:?Set FFO_DOTNET_FRAMEWORK_DIR to installed runtime assemblies}"
    find "$FFO_DOTNET_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
    "$FFO_DOTNET" exec "$FFO_DOTNET_CSC" -nologo -noconfig -nostdlib+ -define:FFO_DEBUG_SERVER_SWITCH @"$OUT/framework.rsp" -target:exe -out:"$OUT/contract.dll" "${SOURCES[@]}"
    printf '{"runtimeOptions":{"tfm":"net6.0","framework":{"name":"Microsoft.NETCore.App","version":"6.0.0"},"rollForward":"LatestMajor"}}\n' > "$OUT/contract.runtimeconfig.json"
    "$FFO_DOTNET" "$OUT/contract.dll"
else
    mcs -define:FFO_DEBUG_SERVER_SWITCH -out:"$OUT/contract.exe" "${SOURCES[@]}"
    mono "$OUT/contract.exe"
fi
