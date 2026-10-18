#!/usr/bin/env bash
# Source Documentation/tools/activate-dotnet-test-toolchain.sh first.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Activate managed toolchain}"
: "${FFO_CSC:?Activate managed toolchain}"
: "${FFO_COMPILE_DIR:?Run managed core compile first}"
P="$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$FFO_COMPILE_DIR/framework.rsp" -nologo -langversion:9.0 -out:"$FFO_COMPILE_DIR/SettingsMenuContract.exe" \
 "$ROOT/Tests/SettingsMenu/SettingsMenuContract.cs" "$P/GameHud.ControlButtons.cs" "$P/UIManager.Settings.cs" \
 "$P/SysOptionDlg.cs" "$P/SysOptionDlg.Draw.cs" "$P/SysOptionDlg.Button.cs" "$P/SysConfigDlg.cs"
"$FFO_MONO_ROOT/bin/mono" "$FFO_COMPILE_DIR/SettingsMenuContract.exe"
