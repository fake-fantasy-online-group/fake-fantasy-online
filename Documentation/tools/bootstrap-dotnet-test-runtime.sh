#!/usr/bin/env bash
# Optional official Microsoft runtime for the explicit dotnet-exec test profile.
# This is separate from the Debian Mono bootstrap; no source mirrors or fallback.
set -euo pipefail
ROOT="${1:-${FFO_TOOLCHAINS_ROOT:-/workspace/shared/toolchains}/dotnet-runtime}"
VERSION=6.0.36
URL="https://builds.dotnet.microsoft.com/dotnet/Runtime/$VERSION/dotnet-runtime-$VERSION-linux-x64.tar.gz"
SHA512=afb6018fcabec468ccd7ae2f1131d8c9de7f4de7645b8f0c223efbbdbfdc515fb0642a399ebfe372c02044416c4cae463c9c802cd156b9da4181efff0e33ee94
# Hash verified against Microsoft's 6.0 releases.json and official runtime page.
[[ "$(uname -s)/$(uname -m)" == Linux/x86_64 ]] || { echo 'Requires Linux x86_64.' >&2; exit 1; }
mkdir -p "$ROOT/downloads"; ROOT="$(cd "$ROOT" && pwd)"
ARCHIVE="$ROOT/downloads/dotnet-runtime-$VERSION-linux-x64.tar.gz"
if [[ ! -f "$ARCHIVE" ]]; then
  curl --fail --location --silent --show-error --max-time 180 "$URL" -o "$ARCHIVE.partial"
  printf '%s  %s\n' "$SHA512" "$ARCHIVE.partial" | sha512sum -c -
  mv "$ARCHIVE.partial" "$ARCHIVE"
fi
printf '%s  %s\n' "$SHA512" "$ARCHIVE" | sha512sum -c -
if [[ ! -x "$ROOT/$VERSION/dotnet" ]]; then
  python3 - "$ARCHIVE" "$ROOT/$VERSION" <<'PY'
from pathlib import Path, PurePosixPath
import sys
import tarfile
archive, destination = Path(sys.argv[1]), Path(sys.argv[2])
if destination.exists():
    raise SystemExit("Refusing to overwrite an incomplete/unknown runtime directory")
with tarfile.open(archive) as source:
    for entry in source.getmembers():
        path = PurePosixPath(entry.name)
        if path.is_absolute() or '..' in path.parts or not (entry.isfile() or entry.isdir()):
            raise SystemExit(f"Unexpected archive member: {entry.name}")
    destination.mkdir()
    source.extractall(destination, filter='data')
PY
fi
"$ROOT/$VERSION/dotnet" --list-runtimes
echo 'Runtime ready. Activation also requires the separately verified Roslyn package and recovered vendor reference archive.'
