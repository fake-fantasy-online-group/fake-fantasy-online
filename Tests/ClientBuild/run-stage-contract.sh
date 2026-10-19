#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
SOURCES=("$ROOT/Assets/FakeFantasyOnline/Editor/ResourceFreeBuildStage.cs" "$ROOT/Tests/ClientBuild/ResourceFreeBuildStageContract.cs" "$ROOT/Assets/FakeFantasyOnline/Editor/GbkBuildTable.cs" "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/Gbk.cs")
if [[ -n "${FFO_CSC:-}" ]]; then
    : "${FFO_MONO_ROOT:?Set FFO_MONO_ROOT when using FFO_CSC}"
    : "${FFO_FRAMEWORK_DIR:?Set FFO_FRAMEWORK_DIR when using FFO_CSC}"
    find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
    "$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -nologo -noconfig -nostdlib+ @"$OUT/framework.rsp" -target:exe -out:"$OUT/contract.exe" "${SOURCES[@]}"
    "$FFO_MONO_ROOT/bin/mono" "$OUT/contract.exe" "$ROOT/Assets"
else
    mcs -out:"$OUT/contract.exe" "${SOURCES[@]}"
    mono "$OUT/contract.exe" "$ROOT/Assets"
fi
