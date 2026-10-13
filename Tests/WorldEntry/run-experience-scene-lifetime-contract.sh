#!/usr/bin/env bash
# Full production popup pool, animation and spawn owners; no Unity Player or network.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Source the existing managed toolchain env.sh}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
P="$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"
"$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/Lifetime.exe" \
 "$P/CombatNumber.Pool.cs" "$P/CombatNumber.Animation.cs" "$P/CombatNumber.SceneLifetime.cs" \
 "$P/CombatNumber.Spawn.cs" "$P/CombatNumber.SpawnOn.cs" "$P/CombatNumber.PlayerTransform.cs" \
 "$ROOT/Tests/WorldEntry/ExperienceSceneLifetimeContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/Lifetime.exe"
