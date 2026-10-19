#!/usr/bin/env bash
# Local packaging only; requires an already installed, legitimately licensed Unity Editor.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
exec python3 "$ROOT/scripts/ci/build-resource-free.py" "$@"
