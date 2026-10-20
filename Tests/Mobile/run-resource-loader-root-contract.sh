#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Source Documentation/tools/activate-dotnet-test-toolchain.sh}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
# Compile the actual loader selection methods, not a reimplementation of their behavior.
python3 - "$ROOT" "$OUT" <<'PY'
import sys
from pathlib import Path
root, out = map(Path, sys.argv[1:])
text = (root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ResLoader.cs').read_text()
methods = text[text.index('    private static bool UseInstalledResourceRoot()'):text.index('    private void Awake()')]
(out / 'LoaderMethods.cs').write_text('using System; using System.IO; using System.Collections.Generic; namespace FakeFantasyOnline.Presentation.ReferenceAssets { public partial class ResLoader {\n' + methods + '\n}}')
PY
for platform in windows android macos windows-editor; do
 defines=(); case "$platform" in windows) defines=(-define:UNITY_STANDALONE_WIN);; android) defines=(-define:UNITY_ANDROID);; macos) defines=(-define:UNITY_STANDALONE_OSX);; windows-editor) defines=(-define:UNITY_STANDALONE_WIN,UNITY_EDITOR);; esac
 "$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest "${defines[@]}" -out:"$OUT/$platform.exe" \
  "$OUT/LoaderMethods.cs" "$ROOT/Tests/Mobile/ResourceLoaderRootContract.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ResourceFileRoot.cs" \
  "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/ClientResourcePaths.cs"
 "$FFO_MONO_ROOT/bin/mono" "$OUT/$platform.exe"
done
