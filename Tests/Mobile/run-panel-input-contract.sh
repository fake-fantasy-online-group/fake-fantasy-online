#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
BASE="${FFO_TEST_BASE:-$ROOT}"
INPUT="${FFO_TEST_INPUT:-$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets}"
P="$BASE/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
python3 - "$BASE" "$OUT/Stubs.cs" <<'PY'
import sys,re
from pathlib import Path
base=Path(sys.argv[1]);s=(base/'Tests/Mobile/MobileMoveCastStubs.cs').read_text()
a=s.index(' public static class MobileClientPresentation {');b=s.index(' public partial class GameHud {',a);s=s[:a]+s[b:]
s=s.replace('public static class Win05 {','public class Win05 {public static Win05 I; public bool IsOpen;public bool MobileOverlayContains(Vector2 p)=>false;')
s=s.replace('public static class WorldMapDlg {','public static class WorldMapDlg {public static bool Visible;')
s=s.replace('public static int width=1000,height=1000;','public static int width=960,height=540;public static Rect safeArea=new Rect(0,0,960,540);')
s=s.replace('public static float Max(float a,float b)', 'public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)')
s=s.replace('public struct Rect {','public struct Rect {public float xMax=>x+width;public float yMax=>y+height;public bool Overlaps(Rect r)=>x<r.xMax&&xMax>r.x&&y<r.yMax&&yMax>r.y;')
s=s.replace('public static GameHud I;', 'internal static int ControlButtonCountForIndependentRuntime=>9;internal static Rect MobileDiagramRect=new Rect(300,100,300,200);public bool showAvatar=true,showChat=true;public static GameHud I;')
s=s.replace('public static void SetTarget(MonsterEntity m){}','public static int Attacks;public static void SetTarget(MonsterEntity m){Attacks++;}')
layout=(base/'Tests/Mobile/MobileLayoutStubs.cs').read_text();extra=layout[layout.index('public static class AccountSecDlg'):].split('\nnamespace ')[0]
extra=extra.replace('public static class InventoryDlg {public static bool SplitOpen;public static bool IsOpen;}','public class InventoryDlg {public static InventoryDlg I;public static bool SplitOpen;public static bool IsOpen;public bool MobileOverlayContains(UnityEngine.Vector2 p)=>false;}')
extra=extra.replace('public static class WorldMapDlg {public static bool Visible;}','')
s+='\nnamespace UnityEngine {public static class TouchScreenKeyboard {public static bool visible;public static Rect area;}}\nnamespace FakeFantasyOnline.Presentation.ReferenceAssets {\n'+extra
s+='''\nnamespace FakeFantasyOnline.Presentation.ReferenceAssets {
public class EquipDlg {public static EquipDlg I;public bool IsOpen;public bool MobileOverlayContains(UnityEngine.Vector2 p)=>false;}
public class UIManager {public static UIManager I;public bool MobileWindowContains(UnityEngine.Vector2 p)=>false;}
public static class UIScale {public static float User=1f;}
public static class AddonUiHost {public static bool PointerOver(UnityEngine.Vector2 p)=>false;}
public static class WinZ {public static bool AnyWindowAt(UnityEngine.Vector2 p)=>false;}
public static class UITip {public static bool MobileContainsPointer(UnityEngine.Vector2 p)=>false;}
}'''
Path(sys.argv[2]).write_text(s)
PY
find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" -nologo -langversion:9.0 -out:"$OUT/Contract.exe" \
 "$INPUT/GameHud.MobileInput.cs" "$INPUT/ClientInput.cs" "$INPUT/MobileClientPresentation.cs" "$INPUT/TouchPointerRouter.cs" "$P/MobileSkillGesture.cs" "$P/MobileGroupWheel.cs" "$P/MobileShortcutPointerOwnership.cs" "$P/SkillTargeting.Mobile.cs" "$P/CombatWorld.MobileCasting.cs" \
 "$OUT/Stubs.cs" "$ROOT/Tests/Mobile/MobilePanelInputContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/Contract.exe"
