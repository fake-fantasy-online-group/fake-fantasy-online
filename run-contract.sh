#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")" && pwd)"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
"${CSC:-csc}" -nologo -langversion:9.0 -out:"$OUT/Contract.exe" "$ROOT/DepthPartition.cs" "$ROOT/DepthPartitionContract.cs"
"${RUNTIME:-mono}" "$OUT/Contract.exe"
