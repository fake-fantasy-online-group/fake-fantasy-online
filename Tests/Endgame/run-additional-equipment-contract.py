#!/usr/bin/env python3
"""Execute native equipment OnGUI with deterministic substitutes; no Unity/Player build."""
from pathlib import Path
import argparse
import os
import shutil
import subprocess
import tempfile

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--mono", default=os.environ.get("FFO_MONO"), help="Mono runtime executable")
parser.add_argument("--csc", default=os.environ.get("FFO_CSC"), help="C# 9+ compiler executable")
parser.add_argument("--framework-dir", type=Path, default=os.environ.get("FFO_FRAMEWORK_DIR"))
args = parser.parse_args()
mono = args.mono or shutil.which("mono")
csc = args.csc or shutil.which("csc")
if not mono or not csc:
    parser.error("Pass --mono and --csc (C# 9+), or set FFO_MONO and FFO_CSC")
root = Path(__file__).resolve().parents[2]
tests = Path(__file__).resolve().parent
compiler = [mono, csc] if csc.lower().endswith(".exe") else [csc]
references = ["-noconfig"]
if args.framework_dir:
    references += ["-nostdlib+"] + ["-r:" + str(args.framework_dir / name) for name in ("mscorlib.dll", "System.dll", "System.Core.dll")]
with tempfile.TemporaryDirectory(prefix="additional-equipment-contract-") as folder:
    exe = Path(folder) / "EquipmentAdditionalSlotsContract.exe"
    subprocess.run(compiler + references + ["-nologo", "-langversion:9.0", "-target:exe", "-out:" + str(exe),
        str(root / "Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/EquipDlg.OnGUI.cs"),
        str(root / "Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/EquipDlg.Mobile.cs"),
        str(root / "Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/MobilePanelInput.cs"),
        str(root / "Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/MobileItemGesture.cs"),
        str(tests / "EquipmentAdditionalSlotsStubs.cs"), str(tests / "EquipmentAdditionalSlotsContract.cs")], check=True)
    subprocess.run([mono, str(exe)], check=True)
