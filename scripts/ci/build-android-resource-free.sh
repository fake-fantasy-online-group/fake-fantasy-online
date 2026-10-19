#!/usr/bin/env bash
# Run on an already installed and legitimately licensed Unity 6000.4.9f1 Editor.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"
UNITY_EDITOR_BIN="${UNITY_EDITOR_BIN:-Unity}"
command -v "$UNITY_EDITOR_BIN" >/dev/null 2>&1 || { echo "Unity Editor not found: $UNITY_EDITOR_BIN" >&2; exit 1; }
export FAKE_FANTASY_ONLINE_ANDROID_BUILD_PATH="${FAKE_FANTASY_ONLINE_ANDROID_BUILD_PATH:-$ROOT/Build/Android/resource-free-candidate.apk}"
mkdir -p "$(dirname "$FAKE_FANTASY_ONLINE_ANDROID_BUILD_PATH")"
if ! "$UNITY_EDITOR_BIN" -batchmode -quit -buildTarget Android -projectPath "$ROOT" \
    -executeMethod FakeFantasyOnline.Editor.CiClientBuild.BuildAndroidResourceFree \
    -logFile "${FFO_UNITY_BUILD_LOG:-$ROOT/Build/Android/unity-build.log}"; then
    echo 'Unity candidate build failed; no successful artifact is claimed.' >&2
    # Expose compiler diagnostics only; never publish complete logs that might include licensing data.
    grep -E 'error CS[0-9]+:' "${FFO_UNITY_BUILD_LOG:-$ROOT/Build/Android/unity-build.log}" >&2 || true
    exit 1
fi
python3 "$ROOT/scripts/ci/verify-resource-free-apk.py" "$FAKE_FANTASY_ONLINE_ANDROID_BUILD_PATH"
