#!/usr/bin/env python3
"""Execute actual login presentation code against deterministic Unity substitutes.

Optional raw RGBA fixtures stay outside the repository. No Unity/player/network is run.
"""
from pathlib import Path
import os
import re
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"


def method(source, signature):
    start = source.index(signature)
    opening = source.index("{", start)
    depth, end = 1, opening + 1
    while depth:
        depth += (source[end] == "{") - (source[end] == "}")
        end += 1
    return source[start:end]


gui = (BASE / "TitleFlow.LoginGui.cs").read_text()
login = method(gui, "private void DrawLogin()")
assert login.count("NativeLoginButton(") == 3
assert login.count("NativeLoginCheck(") == 2
assert login.count("SaveLoginPrefs();") == 1
password_match = re.search(r"Rect passwordField = new Rect\((\d+)f, (\d+)f, (\d+)f, (\d+)f\)", login)
assert password_match
_, password_y, _, password_height = map(int, password_match.groups())
check_rects = re.findall(r"NativeLoginCheck\(new Rect\((\d+)f, (\d+)f, (\d+)f, (\d+)f\)", login)
assert len(check_rects) == 2
for check in check_rects:
    # MobileTitleTextInput expands the field by 4px on each edge; neither checkbox
    # may share an input target with it, even when the baked panel wells are taller.
    assert password_y + password_height + 4 <= int(check[1])
for forbidden in ("DrawResSelector", "DrawFpsSelector", "DrawVersionBadge", "OpenRecoverForm", "OpenAppealForm", "GUI.Label"):
    assert forbidden not in login, forbidden
for retained in ("MobileField(\"login-account\"", "DrawMaskedField(passwordField", "BeginLogin(submittedAccount)", "OpenRegisterForm()", "GameBootstrap.QuitGame()", "DrawUpdateGate()", "_rememberAccount = true;"):
    assert retained in login, retained
native = (BASE / "TitleFlow.NativeGui.cs").read_text()
assert "DrawNoticeButton();" not in native and "DrawUpdateNotice();" not in native

# The new UI never changes persistence semantics or credential handling.
old = subprocess.check_output(["git", "show", "99c0e5ce88dbc1ffb01c5586b7d6ff43eb58c9aa:" + str((BASE / "TitleFlow.LoginGui.cs").relative_to(ROOT))], cwd=ROOT, text=True)
for signature in ("private void LoadLoginPrefs()", "private void SaveLoginPrefs()"):
    assert method(gui, signature) == method(old, signature), signature

with tempfile.TemporaryDirectory(prefix="login-presentation-contract-") as temporary:
    exe = Path(temporary) / "contract.exe"
    framework = Path(os.environ["FFO_FRAMEWORK_DIR"])
    mono = Path(os.environ["FFO_MONO_ROOT"]) / "bin/mono"
    subprocess.run([str(mono), os.environ["FFO_CSC"], "-nologo", "-noconfig", "-nostdlib+",
                    "-langversion:9.0", "-out:" + str(exe)]
                   + ["-r:" + str(framework / name) for name in ("mscorlib.dll", "System.dll", "System.Core.dll")]
                   + [str(BASE / "TitleFlow.LoginPresentation.cs"), str(ROOT / "Tests/UiArtwork/LoginPresentationContract.cs")], check=True)
    subprocess.run([str(mono), str(exe)] + sys.argv[1:], check=True)
print("PASS login source contracts: three native buttons, two memory controls, unchanged persistence, no removed UI entry points")
