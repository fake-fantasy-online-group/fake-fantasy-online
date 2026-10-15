#!/usr/bin/env python3
"""Exercise production mobile inventory dispatch and original action chain with managed stubs.

This is a source-level contract, not a Unity rendering or device input test. Only the
small UI-independent methods are extracted; dispatch/admission logic is never copied
into the fixture. Run after sourcing the repository's managed toolchain environment.
"""
from pathlib import Path
import os
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"


def method(filename, signature):
    source = (SOURCE / filename).read_text()
    start = source.index(signature)
    end = source.index("{", start) + 1
    depth = 1
    while depth:
        depth += (source[end] == "{") - (source[end] == "}")
        end += 1
    return source[start:end]


inventory_methods = [
    ("InventoryDlg.Mobile.cs", "    private void ExecuteMobileAction("),
    ("InventoryDlg.Mobile.cs", "    private string MobilePrimaryLabel("),
    ("InventoryDlg.OnGUI.cs", "    private bool SlotPickers("),
    ("InventoryDlg.OnGUI.cs", "    private bool SlotRightButton("),
    ("InventoryDlg.OnGUI.cs", "    private void SlotLeftButton("),
    ("InventoryDlg.OnGUI.cs", "    private void SlotActivate("),
    ("InventoryDlg.OnGUI.cs", "    private void ActivateOrdinaryItem(ref int bagTab, Item it, int gridIndex)"),
    ("InventoryDlg.OnGUICallbacks.cs", "    private static void WardrobeStoreConfirmed("),
    ("InventoryDlg.Split.cs", "    private void OpenSplit("),
    ("InventoryDlg.Split.cs", "    private static void CommitSplit("),
]
source = "using System; using UnityEngine; namespace FakeFantasyOnline.Presentation.ReferenceAssets {\n"
source += "internal partial class InventoryDlg {\n"
source += "\n".join(method(*entry) for entry in inventory_methods)
source += "\n}\ninternal sealed partial class DealDlg {\n"
source += method("DealDlg.Window.cs", "    public static bool IsOffered(")
source += "\n}\n}"

with tempfile.TemporaryDirectory(prefix="native-inventory-action-") as temporary:
    directory = Path(temporary)
    generated = directory / "ProductionActionMethods.cs"
    generated.write_text(source)
    executable = directory / "Contract.exe"
    references = ["-r:" + str(path) for path in sorted(Path(os.environ["FFO_FRAMEWORK_DIR"]).glob("*.dll"))]
    production_files = ["InventoryDlg.ItemAt.cs", "InventoryDlg.SetItems.cs", "InventoryDlg.PatchSlot.cs", "InventoryDlg.LockInput.cs",
                        "InventoryDlg.Share.cs", "DealDlg.BagActions.cs", "FusedGuard.cs"]
    subprocess.run([os.environ["FFO_MONO"], os.environ["FFO_CSC"], "-nologo", "-noconfig", "-nostdlib+",
                    "-langversion:9.0", "-out:" + str(executable)] + references +
                   [str(generated), str(ROOT / "Tests/Mobile/native-inventory-action-contract.cs")] +
                   [str(SOURCE / filename) for filename in production_files], check=True)
    subprocess.run([os.environ["FFO_MONO"], str(executable)], check=True)
