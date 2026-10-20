#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
if [[ -n "${FFO_MONO_ROOT:-}" ]]; then
  compile=("$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe")
  runtime=("$FFO_MONO_ROOT/bin/mono")
else
  compile=(mcs)
  runtime=(mono)
fi
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT
"${compile[@]}" -langversion:latest -out:"$OUT/TarResourceContract.exe" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/MobileResourceTarReader.cs" \
  "$ROOT/Tests/Mobile/TarResourceContract.cs"
if [[ "$#" -eq 0 ]]; then
  # Match the real archive producer (Python tarfile's default PAX format), including
  # fractional mtime, Unicode fallback names, long paths, and unrelated AppIcon files.
  python3 - "$OUT/python-resource.tar" <<'PYTHON'
import io
import sys
import tarfile
with tarfile.open(sys.argv[1], "w") as archive:
    for name, data in [
        ("Assets/StreamingAssets/data/中文.txt", b"Hello"),
        ("AppIcon/icon.png", b"PNG"),
        ("Assets/StreamingAssets/" + "path/" * 25 + "long.txt", b"path"),
    ]:
        entry = tarfile.TarInfo(name)
        entry.size = len(data)
        entry.mtime = 1750000000.123456
        archive.addfile(entry, io.BytesIO(data))
PYTHON
  set -- "$OUT/python-resource.tar"
fi
"${runtime[@]}" "$OUT/TarResourceContract.exe" "$@"
