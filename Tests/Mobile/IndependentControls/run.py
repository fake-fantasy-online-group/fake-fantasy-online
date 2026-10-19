#!/usr/bin/env python3
"""Run deterministic independent contracts against current production sources.

Source Documentation/tools/activate-dotnet-test-toolchain.sh first.
No Unity process, device, live server, potion use, or riding is involved.
"""
from pathlib import Path
import os
import subprocess
import tempfile

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
SOURCE = ROOT / "Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"
SUITES = {
    "LayoutSafety": ["MobileControlLayout.cs", "MobileControlLayoutStore.cs"],
    "LayoutSession": ["MobileClientPresentation.cs", "MobileClientPresentation.Layout.cs",
                      "MobileClientPresentation.LayoutGate.cs",
                      "MobileControlLayout.cs", "MobileControlLayoutStore.cs",
                      "MobileGroupWheel.cs", "MobileFixedAction.cs"],
    "PointerSafety": ["GameHud.MobileInput.cs", "GameHud.MobileFixedControls.cs",
                      "GameHud.MobileLayoutEditor.cs", "ClientInput.cs", "TouchPointerRouter.cs",
                      "MobileSkillGesture.cs", "MobileGroupWheel.cs", "MobileShortcutPointerOwnership.cs",
                      "SkillTargeting.Mobile.cs", "CombatWorld.MobileCasting.cs",
                      "MobileLayoutUnlockGesture.cs", "MobileControlLayout.cs", "MobileFixedAction.cs"],
    "FrameGate": ["PlayerController.Frame.cs"],
}
STUBS = {"LayoutSafety": "GeometryStubs.cs", "LayoutSession": "SessionStubs.cs",
         "PointerSafety": "PointerStubs.cs"}


def main():
    required = ("FFO_MONO_ROOT", "FFO_CSC", "FFO_FRAMEWORK_DIR")
    missing = [name for name in required if not os.environ.get(name)]
    if missing:
        raise SystemExit("Activate the managed test toolchain first; missing " + ", ".join(missing))
    runtime = str(Path(os.environ["FFO_MONO_ROOT"]) / "bin/mono")
    framework = Path(os.environ["FFO_FRAMEWORK_DIR"])
    refs = ["-r:" + str(framework / name) for name in ("mscorlib.dll", "System.dll", "System.Core.dll")]
    with tempfile.TemporaryDirectory(prefix="mobile-independent-contracts-") as directory:
        for name, production in SUITES.items():
            sources = [SOURCE / path for path in production]
            if name in STUBS:
                sources.append(HERE / STUBS[name])
            sources.append(HERE / (name + "Contract.cs"))
            executable = Path(directory) / (name + "Contract.exe")
            subprocess.run([runtime, os.environ["FFO_CSC"], "-nologo", "-noconfig", "-nostdlib+",
                            "-langversion:9.0", "-out:" + str(executable)] + refs +
                           [str(path) for path in sources], check=True)
            subprocess.run([runtime, str(executable)], check=True)


if __name__ == "__main__":
    main()
