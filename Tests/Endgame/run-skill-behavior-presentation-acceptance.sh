#!/usr/bin/env bash
# Actual production provider, resource-owner and command preflight; no Unity/Player.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?}" "${FFO_CSC:?}" "${FFO_FRAMEWORK_DIR:?}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
export MONO_PATH="$FFO_FRAMEWORK_DIR/Facades${MONO_PATH:+:$MONO_PATH}"
find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
P="$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" -nologo -langversion:9.0 -out:"$OUT/Acceptance.exe" \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Foundation/{JsonData,BoundedJson}.cs \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Application/Endgame*.cs \
 "$P/AuthoritativeProfessionSkill.cs" "$P/Win05.SkillData.cs" "$P/NativeSwordSkillTargeting.cs" "$P/NetClient.SkillCommands.cs" \
 "$ROOT/Tests/Endgame/SkillBehaviorPresentationAcceptanceStubs.cs" "$ROOT/Tests/Endgame/SkillBehaviorPresentationAcceptance.cs"
FFO_SKILL_GROWTH_FIXTURE="$ROOT/Tests/Endgame/Fixtures/skill-growth-v7.jsonl" "$FFO_MONO_ROOT/bin/mono" "$OUT/Acceptance.exe" "$@"
