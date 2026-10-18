#!/usr/bin/env bash
# Requires the core assemblies from Tests/PluginRuntime/run-managed-compile.sh.
# Uses explicit Unity/font doubles; does not validate real Unity rendering.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Set FFO_MONO_ROOT to managed host root}"
: "${FFO_CSC:?Set FFO_CSC to Roslyn csc.exe}"
: "${FFO_COMPILE_DIR:?Set FFO_COMPILE_DIR to compiled core output}"
OUT="$FFO_COMPILE_DIR"
export MONO_PATH="$OUT${MONO_PATH:+:$MONO_PATH}"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" -nologo -langversion:9.0 -out:"$OUT/TaskTrackRenderContract.exe" -r:"$OUT/FakeFantasyOnline.Application.dll" \
 "$ROOT/Tests/QuestFeedback/TaskTrackRenderContract.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/PlayerTaskTrack.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ReferenceWorldPlayerTaskTrack.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/TaskPromptLayout.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/TaskTrackRenderContract.exe"
