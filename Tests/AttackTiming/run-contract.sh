#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
P="${ATTACK_SOURCE_DIR:-$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets}"
: "${FFO_MONO:?Activate managed toolchain}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
"$FFO_MONO" "$FFO_CSC" -noconfig -nostdlib+ @"$FFO_COMPILE_DIR/framework.rsp" -nologo -langversion:9.0 -out:"$OUT/Attack.exe" \
 "$ROOT/Tests/AttackTiming/AttackTimingContract.cs" "$P/CombatWorld.Update.cs" "$P/CombatWorld.Targeting.cs" "$P/CombatWorld.TryAttack.cs" "$P/CombatWorld.CastState.cs" "$P/PlayerController.Approach.cs"
"$FFO_MONO" "$OUT/Attack.exe"
