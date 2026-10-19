#!/usr/bin/env bash
# This checks configuration only. Unity is the authority on whether a license is valid.
set -euo pipefail
if [[ "${UNITY_CI_LICENSE_MODE:-}" != "pro-ulf" || -z "${UNITY_LICENSE:-}" ]]; then
    message='Unity build blocked: Personal authentication/session setup is pending. This workflow does not yet implement a verified Personal CI licensing flow. Do not copy a local license or substitute a fabricated ULF. A separately approved Pro ULF configuration is an optional alternative only for an eligible Pro license holder.'
    echo "::error::$message" >&2
    if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then printf '%s\n' "$message" >> "$GITHUB_STEP_SUMMARY"; fi
    exit 1
fi
# Never log the license, copy a developer-machine license, or generate a fake license.
echo 'License configuration is present; Unity activation must still succeed.'
