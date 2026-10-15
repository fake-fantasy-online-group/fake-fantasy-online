#!/usr/bin/env bash
# Shared core/optional-framework managed compilation entry point.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
exec bash "$ROOT/Tests/PluginRuntime/run-managed-compile.sh" "$@"
