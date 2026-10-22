#!/usr/bin/env bash
# Production portal and position-reporting ordering; deterministic Unity/transport substitutes.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
: "${FFO_DOTNET:?Set FFO_DOTNET to a .NET 8 SDK executable}"
SDK_ROOT="$(dirname "$FFO_DOTNET")"
CSC="$(find "$SDK_ROOT/sdk" -path '*/Roslyn/bincore/csc.dll' | sort -V | tail -1)"
REF="$(find "$SDK_ROOT/packs/Microsoft.NETCore.App.Ref" -type d -path '*/ref/net8.0' | sort -V | tail -1)"
[[ -n "$CSC" && -n "$REF" ]] || { echo 'FFO_DOTNET requires the .NET 8 SDK' >&2; exit 1; }
find "$REF" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
R="$ROOT/Assets/FakeFantasyOnline/Runtime"
"$FFO_DOTNET" "$CSC" -nologo -noconfig -nostdlib+ -langversion:9.0 @"$OUT/framework.rsp" -out:"$OUT/Contract.dll" \
 "$R"/Foundation/{JsonData,BoundedJson,Result}.cs "$R"/Application/Endgame*.cs \
 "$R"/Presentation/ReferenceAssets/{PlayerController.Startup,PlayerController.TransferFrame,PlayerController.MovementClock,EndgamePanel.RiftPortal,GameBootstrap.PositionReporting}.cs \
 "$ROOT/Tests/Endgame/RiftPortalPositionContract.cs"
printf '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"}}}\n' > "$OUT/Contract.runtimeconfig.json"
"$FFO_DOTNET" "$OUT/Contract.dll"
