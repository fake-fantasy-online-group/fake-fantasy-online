#!/usr/bin/env bash
# Pure managed protocol/state contracts; no Unity assets, Player or server required.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
if [[ -n "${FFO_DOTNET:-}" ]]; then
  # Invoke the SDK compiler/runtime directly, avoiding global CLI first-run state.
  SDK_ROOT="$(dirname "$FFO_DOTNET")"
  CSC="$(find "$SDK_ROOT/sdk" -path '*/Roslyn/bincore/csc.dll' | sort -V | tail -1)"
  REF="$(find "$SDK_ROOT/packs/Microsoft.NETCore.App.Ref" -type d -path '*/ref/net8.0' | sort -V | tail -1)"
  [[ -n "$CSC" && -n "$REF" ]] || { echo "FFO_DOTNET requires the .NET 8 SDK" >&2; exit 1; }
  find "$REF" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
  "$FFO_DOTNET" "$CSC" -nologo -noconfig -nostdlib+ -langversion:9.0 @"$OUT/framework.rsp" -out:"$OUT/RiftPages.dll" \
    "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/JsonData.cs" \
    "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/BoundedJson.cs" \
    "$ROOT"/Assets/FakeFantasyOnline/Runtime/Application/Endgame*.cs \
    "$ROOT/Assets/FakeFantasyOnline/Runtime/Protocol/PrivateServerEndgameCodec.cs" \
    "$ROOT/Tests/Endgame/RiftPageContract.cs"
  printf '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"}}}\n' > "$OUT/RiftPages.runtimeconfig.json"
  "$FFO_DOTNET" "$OUT/RiftPages.dll" "$@"
else
  : "${FFO_MONO_ROOT:?Set FFO_MONO_ROOT or FFO_DOTNET}"
  "$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/RiftPages.exe" \
    "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/JsonData.cs" \
    "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/BoundedJson.cs" \
    "$ROOT"/Assets/FakeFantasyOnline/Runtime/Application/Endgame*.cs \
    "$ROOT/Assets/FakeFantasyOnline/Runtime/Protocol/PrivateServerEndgameCodec.cs" \
    "$ROOT/Tests/Endgame/RiftPageContract.cs"
  "$FFO_MONO_ROOT/bin/mono" "$OUT/RiftPages.exe" "$@"
fi
