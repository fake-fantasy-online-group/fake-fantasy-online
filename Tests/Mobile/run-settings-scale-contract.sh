#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
BASE="${FFO_TEST_BASE:-$ROOT}"
P="${FFO_TEST_INPUT:-$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets}"
B="$BASE/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" -nologo -langversion:9.0 -out:"$OUT/Contract.exe" \
 "$P/SysConfigDlg.cs" "$P/SysConfigDlg.PickLayout.cs" "$P/SysConfigDlg.Controls.cs" "$P/SysConfigDlg.OnGUI.cs" \
 "$B/SysConfigDlg.Apply.cs" "$B/SysConfigDlg.Preferences.cs" "$B/SysConfigDlg.Lists.cs" "$B/SysConfigDlg.Background.cs" "$B/UIScale.cs" "$B/WinDrag.cs" \
 "$ROOT/Tests/Mobile/MobileSettingsScaleStubs.cs" "$ROOT/Tests/Mobile/MobileSettingsScaleContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/Contract.exe"
