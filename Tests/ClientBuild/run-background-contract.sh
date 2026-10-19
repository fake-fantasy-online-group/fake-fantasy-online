#!/usr/bin/env bash
# Source contract only: no Unity Player, graphics driver or package download is involved.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
SOURCES=("$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/EmbeddedClientBackground.cs" "$ROOT/Tests/ClientBuild/EmbeddedClientBackgroundContract.cs")
if [[ -n "${FFO_CSC:-}" ]]; then
    : "${FFO_MONO_ROOT:?Set FFO_MONO_ROOT when using FFO_CSC}"
    : "${FFO_FRAMEWORK_DIR:?Set FFO_FRAMEWORK_DIR when using FFO_CSC}"
    find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
    "$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -nologo -noconfig -nostdlib+ @"$OUT/framework.rsp" -target:exe -out:"$OUT/contract.exe" "${SOURCES[@]}"
    "$FFO_MONO_ROOT/bin/mono" "$OUT/contract.exe"
else
    mcs -out:"$OUT/contract.exe" "${SOURCES[@]}"
    mono "$OUT/contract.exe"
fi
