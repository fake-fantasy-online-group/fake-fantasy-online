#!/usr/bin/env bash
# Container-only entry point; credentials and activation logs never enter the checkout/artifacts.
set -euo pipefail
cd /workspace/project
./scripts/ci/license-preflight.sh
: "${UNITY_PATH:?Expected the verified GameCI editor image}"
[[ "$(cat "$UNITY_PATH/version")" == '6000.4.9f1' ]] || { echo 'Wrong Unity image version' >&2; exit 1; }
[[ -d "$UNITY_PATH/Editor/Data/PlaybackEngines/AndroidPlayer" ]] || { echo 'Android Build Support is missing' >&2; exit 1; }
credential_dir="$(mktemp -d)"
trap 'rm -rf "$credential_dir"' EXIT
umask 077
printf '%s' "$UNITY_LICENSE" > "$credential_dir/Unity.ulf"
unset UNITY_LICENSE
if ! unity-editor -quit -manualLicenseFile "$credential_dir/Unity.ulf" -logFile "$credential_dir/activation.log" >/dev/null 2>&1; then
    echo '::error::Unity activation failed. No player build was attempted. Activation logs are intentionally excluded from uploaded artifacts.' >&2
    exit 1
fi
rm -f "$credential_dir/Unity.ulf"
UNITY_EDITOR_BIN=unity-editor ./scripts/ci/build-android-resource-free.sh
