#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
BASE="${FFO_TEST_BASE:-$ROOT}"
INPUT="${FFO_TEST_INPUT:-$BASE/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
P="$BASE/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"
python3 - "$BASE/Tests/Mobile/MobileMoveCastStubs.cs" "$OUT/Stubs.cs" <<'PY'
import sys
s=open(sys.argv[1]).read()
s=s.replace('private Map guiMinimap;', 'private Map guiMinimap=new Map(); public static int ZoomInCount,ZoomOutCount;')
s=s.replace('public void ToggleDiagram(){}public void ZoomMinimap(bool b){}public void CloseDiagram(){}', 'public void ToggleDiagram(){I.diagramOpen=!I.diagramOpen;MobileClientPresentation.DiagramOpen=I.diagramOpen;}public void ZoomMinimap(bool b){if(b)ZoomInCount++;else ZoomOutCount++;}public void CloseDiagram(){I.diagramOpen=false;MobileClientPresentation.DiagramOpen=false;}')
s=s.replace('public static class WorldMapDlg {public static void Toggle(){}}', 'public static class WorldMapDlg {public static int Opens;public static void Toggle(){Opens++;}}')
open(sys.argv[2],'w').write(s)
PY
find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" -nologo -langversion:9.0 -out:"$OUT/Contract.exe" \
 "$INPUT/GameHud.MobileInput.cs" "$INPUT/ClientInput.cs" "$INPUT/TouchPointerRouter.cs" "$P/MobileSkillGesture.cs" "$P/MobileGroupWheel.cs" "$P/MobileShortcutPointerOwnership.cs" "$P/SkillTargeting.Mobile.cs" "$P/CombatWorld.MobileCasting.cs" \
 "$OUT/Stubs.cs" "$ROOT/Tests/Mobile/MobileMinimapCloseContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/Contract.exe"
