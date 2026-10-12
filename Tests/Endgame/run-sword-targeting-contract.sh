#!/usr/bin/env bash
# Actual native loaders + target classification + cast cache, with test-only Unity IO.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Set FFO_MONO_ROOT}"
: "${FFO_CSC:?Set FFO_CSC}"
: "${FFO_FRAMEWORK_DIR:?Set FFO_FRAMEWORK_DIR}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
export MONO_PATH="$FFO_FRAMEWORK_DIR/Facades${MONO_PATH:+:$MONO_PATH}"
find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" -nologo -langversion:9.0 -out:"$OUT/SwordTargeting.exe" \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Foundation/{JsonData,BoundedJson}.cs \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Application/Endgame*.cs \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/AuthoritativeProfessionSkill.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/NativeSwordSkillTargeting.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/Win05.SkillData.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/SkillTargeting.Classification.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ReferenceWorldLocalSkillCast.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/CombatWorld.SkillRequests.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/CombatWorld.Approach.cs" \
 "$ROOT/Tests/Endgame/NativeSwordTargetingStubs.cs" \
 "$ROOT/Tests/Endgame/NativeSwordTargetingContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/SwordTargeting.exe"
