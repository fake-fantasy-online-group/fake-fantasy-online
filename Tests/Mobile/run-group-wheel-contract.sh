#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
P="$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"
find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" -nologo -langversion:9.0 -out:"$OUT/Contract.exe" \
 "$P/GameHud.MobileInput.cs" "$P/ClientInput.cs" "$P/TouchPointerRouter.cs" "$P/MobileSkillGesture.cs" "$P/MobileGroupWheel.cs" "$P/MobileShortcutPointerOwnership.cs" "$P/SkillTargeting.Mobile.cs" "$P/CombatWorld.MobileCasting.cs" \
 "$ROOT/Tests/Mobile/MobileMoveCastStubs.cs" "$ROOT/Tests/Mobile/MobileGroupWheelContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/Contract.exe"
