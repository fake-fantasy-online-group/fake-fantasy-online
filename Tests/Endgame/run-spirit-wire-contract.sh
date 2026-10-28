#!/usr/bin/env bash
# Pure managed DTO/command/state tests. Does not launch Unity, a game or a server.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
export FFO_SPIRIT_WIRE_FIXTURES="${FFO_SPIRIT_WIRE_FIXTURES:-$ROOT/Tests/Endgame/Fixtures/spirit-wire}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
SOURCES=("$ROOT"/Assets/FakeFantasyOnline/Runtime/Foundation/{JsonData,BoundedJson}.cs "$ROOT"/Assets/FakeFantasyOnline/Runtime/Application/Endgame*.cs "$ROOT/Tests/Endgame/SpiritWireContracts.cs")
if [[ -n "${FFO_MONO_ROOT:-}" ]]; then
  "$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/SpiritWire.exe" "${SOURCES[@]}"
  "$FFO_MONO_ROOT/bin/mono" "$OUT/SpiritWire.exe"
else
  : "${FFO_DOTNET_ROOT:?Set FFO_MONO_ROOT or FFO_DOTNET_ROOT}"
  : "${FFO_ROSLYN_CSC:?Set FFO_ROSLYN_CSC to the installed Roslyn csc.dll}"
  RUNTIME="$(find "$FFO_DOTNET_ROOT/shared/Microsoft.NETCore.App" -mindepth 1 -maxdepth 1 -type d | sort -V | tail -1)"
  REFS=(); for dll in "$RUNTIME"/*.dll; do REFS+=("-r:$dll"); done
  "$FFO_DOTNET_ROOT/dotnet" "$FFO_ROSLYN_CSC" -nologo -langversion:latest -target:exe -out:"$OUT/SpiritWire.dll" "${REFS[@]}" "${SOURCES[@]}"
  VERSION="$(basename "$RUNTIME")"
  printf '{"runtimeOptions":{"tfm":"net6.0","framework":{"name":"Microsoft.NETCore.App","version":"%s"}}}' "$VERSION" > "$OUT/SpiritWire.runtimeconfig.json"
  "$FFO_DOTNET_ROOT/dotnet" "$OUT/SpiritWire.dll"
fi
