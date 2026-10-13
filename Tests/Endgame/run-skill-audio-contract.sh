#!/usr/bin/env bash
# Production combat/cast/attack/audio owners; deterministic Unity/audio backend.
# Does not launch Unity, load external audio assets, or establish audible playback.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?}" "${FFO_CSC:?}" "${FFO_FRAMEWORK_DIR:?}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
export MONO_PATH="$FFO_FRAMEWORK_DIR/Facades${MONO_PATH:+:$MONO_PATH}"
find "$FFO_FRAMEWORK_DIR" -maxdepth 1 -name '*.dll' | sort | sed 's/^/-r:/' > "$OUT/framework.rsp"
P="$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"
"$FFO_MONO_ROOT/bin/mono" "$FFO_CSC" -noconfig -nostdlib+ @"$OUT/framework.rsp" -nologo -langversion:9.0 -out:"$OUT/SkillAudio.exe" \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Foundation/{JsonData,BoundedJson}.cs \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Application/WorldSkillCastReceipt.cs" \
 "$P/CombatWorld.Combat.cs" "$P/CombatWorld.Cast.cs" "$P/CombatWorld.Projectile.cs" \
 "$P/CombatWorld.ProjectileSelection.cs" "$P/CombatWorld.HitSound.cs" "$P/CombatWorld.SwingSnd.cs" "$P/CombatWorld.BasicAttackPose.cs" \
 "$P/PlayerController.PlayAttack.cs" "$P/PlayerController.ActionEntry.cs" "$P/PlayerController.Actions.cs" \
 "$P/SkillSoundDB.cs" "$P/SkillData.Nested.cs" \
 "$P/ReferenceWorldReceiveOwner.SkillCastReceipt.cs" "$P/ReferenceWorldReceiveOwner.RemotePlayerTails.cs" \
 "$ROOT/Tests/Endgame/SkillAudioStubs.cs" "$ROOT/Tests/Endgame/SkillAudioContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/SkillAudio.exe"
