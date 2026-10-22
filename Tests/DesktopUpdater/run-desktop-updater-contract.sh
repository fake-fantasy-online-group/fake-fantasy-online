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
compression_ref="${FFO_FRAMEWORK_DIR:+$FFO_FRAMEWORK_DIR/}System.IO.Compression.dll"
OUT="$(mktemp -d)"; trap 'rm -rf "$OUT"' EXIT

# Use the updater project's source graph so a missing shared Compile Include
# cannot be hidden by the contract runner's own list. This is not an SDK build.
python3 - "$ROOT/Tools/DesktopUpdater/DesktopUpdater.csproj" > "$OUT/project-sources.txt" <<'PY'
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

project = Path(sys.argv[1])
tree = ET.parse(project)
sources = [
    path for path in sorted(project.parent.rglob("*.cs"))
    if not {"bin", "obj"}.intersection(path.relative_to(project.parent).parts[:-1])
]
for item in tree.iter():
    name = item.tag.rsplit("}", 1)[-1]
    if "Condition" in item.attrib and any(
        child.tag.rsplit("}", 1)[-1] == "Compile" for child in item.iter()
    ):
        raise SystemExit("DesktopUpdater source check does not support conditional Compile items.")
    if name in ("EnableDefaultItems", "EnableDefaultCompileItems"):
        raise SystemExit("DesktopUpdater source check requires the SDK's default Compile items.")
    if name == "Compile":
        include = item.attrib.get("Include", "")
        if not include or any(char in include for char in "*?;$") or any(
            attr in item.attrib for attr in ("Exclude", "Remove", "Update")
        ):
            raise SystemExit("DesktopUpdater source check requires literal Compile Include paths.")
        sources.append(project.parent / include.replace("\\", "/"))
for source in dict.fromkeys(path.resolve() for path in sources):
    if not source.is_file():
        raise SystemExit(f"DesktopUpdater project source does not exist: {source}")
    print(source)
PY
mapfile -t sources < "$OUT/project-sources.txt"

# Compile the production entry point before introducing any test-only sources.
"${compile[@]}" -langversion:latest -r:"$compression_ref" \
 -main:FakeFantasyOnline.DesktopUpdater.Program -out:"$OUT/DesktopUpdater.exe" \
 "${sources[@]}"
"${compile[@]}" -langversion:latest -r:"$compression_ref" \
 -main:DesktopUpdaterContract -out:"$OUT/DesktopUpdaterContract.exe" \
 "${sources[@]}" "$ROOT/Tests/DesktopUpdater/DesktopUpdaterContract.cs"
python3 "$ROOT/Tests/DesktopUpdater/make-package-fixtures.py" "$OUT/packaged"
"${runtime[@]}" "$OUT/DesktopUpdaterContract.exe" "$OUT/packaged/windows.zip" "$OUT/packaged/macos.zip" \
 "$OUT/packaged/windows-unsigned.zip" "$OUT/packaged/macos-unsigned.zip"
