#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
P="$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -nologo -noconfig -nostdlib+ -langversion:9.0 \
 -r:"$FFO_FRAMEWORK_DIR/mscorlib.dll" -r:"$FFO_FRAMEWORK_DIR/System.dll" \
 -out:"$OUT/Contract.exe" "$P/MobilePanelInput.cs" "$P/MobileItemGesture.cs" "$P/MobileSkillSelection.cs" "$P/Win05.MobileSkills.cs" "$P/Win05.MobilePetSkills.cs" "$P/MobileShortcutEligibility.cs" "$P/InventoryDlg.Mobile.cs" "$P/GameHud.MobileSkillBinding.cs" "$P/GameHud.HotIcon.cs" "$P/GameHud.HotbarSync.cs" \
 "$ROOT/Tests/Mobile/MobileSkillBindingStubs.cs" "$ROOT/Tests/Mobile/MobileSkillBindingContract.cs" "$ROOT/Tests/Mobile/OriginalPanelContract.cs" "$ROOT/Tests/Mobile/BagBindingContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/Contract.exe"
