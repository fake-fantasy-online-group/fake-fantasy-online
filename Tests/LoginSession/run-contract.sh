#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Set FFO_MONO_ROOT}"; : "${FFO_CSC:?Set FFO_CSC}"; : "${FFO_FRAMEWORK_DIR:?Set FFO_FRAMEWORK_DIR}"
export FFO_COMPILE_DIR="${FFO_COMPILE_DIR:-$ROOT/Tests/LoginSession/out}"
bash "$ROOT/Tests/PluginRuntime/run-managed-compile.sh"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$FFO_COMPILE_DIR/framework.rsp" -nologo -langversion:9 -out:"$FFO_COMPILE_DIR/LoginSocketContract.exe" \
 -r:"$FFO_COMPILE_DIR/FakeFantasyOnline.Foundation.dll" -r:"$FFO_COMPILE_DIR/FakeFantasyOnline.Application.dll" -r:"$FFO_COMPILE_DIR/FakeFantasyOnline.Protocol.dll" \
 "$ROOT/Tests/LoginSession/LoginSocketContract.cs"
MONO_PATH="$FFO_COMPILE_DIR" "$FFO_MONO_ROOT/bin/mono" "$FFO_COMPILE_DIR/LoginSocketContract.exe"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$FFO_COMPILE_DIR/framework.rsp" -nologo -langversion:9 -out:"$FFO_COMPILE_DIR/TitleFlowContract.exe" \
 -r:"$FFO_COMPILE_DIR/FakeFantasyOnline.Foundation.dll" -r:"$FFO_COMPILE_DIR/FakeFantasyOnline.Application.dll" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/TitleFlow.AccountConnections.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/TitleFlow.NativeGui.cs" "$ROOT/Tests/LoginSession/TitleFlowContract.cs"
MONO_PATH="$FFO_COMPILE_DIR" "$FFO_MONO_ROOT/bin/mono" "$FFO_COMPILE_DIR/TitleFlowContract.exe"
