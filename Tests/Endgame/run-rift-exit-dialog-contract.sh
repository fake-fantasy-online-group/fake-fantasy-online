#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
: "${FFO_MONO_ROOT:?Set FFO_MONO_ROOT to the supported Mono test runtime}"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT
"$FFO_MONO_ROOT/bin/mono" "$FFO_MONO_ROOT/lib/mono/4.5/mcs.exe" -langversion:latest -out:"$OUT/RiftExitDialogContract.exe" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/JsonData.cs" \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Foundation/BoundedJson.cs" \
 "$ROOT"/Assets/FakeFantasyOnline/Runtime/Application/Endgame*.cs \
 "$ROOT/Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/DialogUI.RiftExit.cs" \
 "$ROOT/Tests/Endgame/RiftExitDialogContract.cs"
"$FFO_MONO_ROOT/bin/mono" "$OUT/RiftExitDialogContract.exe"
python3 - "$ROOT" <<'PY'
from pathlib import Path
import sys
p=Path(sys.argv[1])/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/DialogUI.cs'
s=p.read_text()
assert s.count('AddRiftExitOptions();')==1
assert s.index('AddRiftExitOptions();')<s.index('AddDungeonExitAndHardOptions();')
assert s.count('CaptureRiftExit(npc);')==1
exit_source=(p.parent/'DialogUI.RiftExit.cs').read_text()
assert 'SysMsgBox' not in exit_source
print('PASS native dialogue composition hook and no extra confirmation panel')
PY
