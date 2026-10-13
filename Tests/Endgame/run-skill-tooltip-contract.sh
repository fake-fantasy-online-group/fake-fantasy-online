#!/usr/bin/env bash
# Compile and execute the actual native skill composition, UITip layout and render
# source with deterministic Unity/art/font substitutes. This is not a Unity/Player build.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Set FFO_MONO_ROOT to a MonoBleedingEdge-compatible runtime}"
: "${FFO_CSC:?Set FFO_CSC to official Roslyn csc.exe (C# 9+)}"
: "${FFO_FRAMEWORK_DIR:?Set FFO_FRAMEWORK_DIR to .NET Framework 4.7.2 reference directory}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
export MONO_PATH="$FFO_FRAMEWORK_DIR/Facades${MONO_PATH:+:$MONO_PATH}"
find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" \
 -nologo -langversion:9.0 -out:"$OUT/NativeSkillTooltipContract.exe" \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Foundation/{JsonData,BoundedJson}.cs \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Application/Endgame*.cs \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/Win05.GrowthSkills.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/Win05.SkillTipText.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/UITip.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/UITip.SkillScroll.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/UIScale.cs" \
 "$ROOT/Tests/Endgame/NativeSkillTooltipStubs.cs" \
 "$ROOT/Tests/Endgame/NativeSkillTooltipContract.cs"
FIXTURES=("$@")
if [[ ${#FIXTURES[@]} -eq 0 ]]; then
 FIXTURES=(
  "$ROOT/Tests/Endgame/Fixtures/skill-tooltip-all149-v5.json"
  "$ROOT/Tests/Endgame/Fixtures/skill-tooltip-high-equipped-v5.json"
 )
fi
for fixture in "${FIXTURES[@]}"; do
 [[ -f "$fixture" ]] || { echo "Missing required skill tooltip fixture: $fixture" >&2; exit 1; }
done
FFO_SKILL_MANA_FIXTURE="$ROOT/Tests/Endgame/Fixtures/skill-mana-cost-v7.json" FFO_SKILL_GROWTH_FIXTURE="$ROOT/Tests/Endgame/Fixtures/skill-growth-v7.jsonl" FFO_SKILL_SYNERGY_FIXTURE="$ROOT/Tests/Endgame/Fixtures/skill-synergy-equipped-v11.json" FFO_VARIANT_FIXTURE_DIR="$ROOT/Tests/Endgame/Fixtures/skill-variants-v1" "$FFO_MONO_ROOT/bin/mono" "$OUT/NativeSkillTooltipContract.exe" "${FIXTURES[@]}"
python3 "$ROOT/Tests/Endgame/skill_tooltip_source_contract.py"
