#!/usr/bin/env python3
"""Exact pre-harmonization pregame source equivalence, excluding documented plugin hooks.

This checks production source, not rendered pixels or Unity event execution.
Requires git history containing the original baseline (deepen a shallow checkout if needed).
"""
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
BASE = "99c800c6f0c369a231733b292887d12ad7132803"
INITIAL = "f8d8885"
PREFIX = "Assets/FakeFantasyOnline/Runtime/Presentation/"

def original(path, commit=BASE):
    return subprocess.check_output(["git", "show", f"{commit}:{path}"], cwd=ROOT, text=True)

def once(source, before, after=""):
    assert source.count(before) == 1, f"Expected exactly one allowed hook: {before}"
    return source.replace(before, after, 1)

def method(source, signature):
    start = source.index("{", source.index(signature))
    depth = 1
    end = start + 1
    while depth:
        depth += (source[end] == "{") - (source[end] == "}")
        end += 1
    return source[start + 1:end - 1]

files = subprocess.check_output(["git", "ls-tree", "-r", "--name-only", BASE, PREFIX], cwd=ROOT, text=True).splitlines()
files = [p for p in files if p.endswith(".cs") and
         (Path(p).name.startswith("RecoveredTitle") or Path(p).name.startswith("TitleFlow."))]
# These two files contain connection/lifecycle functionality, not visual drawing.
nonvisual = {"TitleFlow.AccountConnections.cs", "TitleFlow.CharacterSelection.Runtime.cs"}
checks = 0
for path in files:
    name = Path(path).name
    baseline = original(path)
    assert baseline == original(path, INITIAL), f"Initial-release provenance changed: {name}"
    checks += 1
    if name in nonvisual:
        continue
    current = (ROOT / path).read_text()
    if name == "TitleFlow.LoginGui.cs":
        current = once(current, "        pluginTitleOwner = this;\n")
        current = once(current, '''        string editedAccount = GUI.TextField(accountField, account ?? string.Empty, 20, _fieldStyle);
        if (!string.Equals(account, editedAccount, StringComparison.Ordinal)) pluginTitleRevision++;
        account = editedAccount;
        string editedPassword = DrawMaskedField(passwordField, password ?? string.Empty);
        // Only an opaque counter leaves this component. Never retain credential values or
        // their hashes in plugin fingerprints, observations, receipts, or action journals.
        if (!string.Equals(password, editedPassword, StringComparison.Ordinal)) pluginTitleRevision++;
        password = editedPassword;
''', '''        account = GUI.TextField(accountField, account ?? string.Empty, 20, _fieldStyle);
        password = DrawMaskedField(passwordField, password ?? string.Empty);
''')
    elif name == "TitleFlow.CharacterSelection.cs":
        current = once(current, '                PublishPluginTitleResult("enter_timeout");\n')
    elif name == "TitleFlow.NativeGui.cs":
        # The saved GUI-state guard surrounds the same draw sequence. The helper is
        # only a shared native server-binding extraction for plugin and GUI callers.
        guard = method(current, "private void OnGUI()")
        assert "try { DrawTitleGui(); }" in guard and "finally" in guard
        for state in ("color", "backgroundColor", "contentColor", "enabled", "matrix"):
            assert f"GUI.{state} = " in guard
        drawing = method(current, "private void DrawTitleGui()")
        helper = method(current, "private void PrepareServerPageForPluginAndGui()")
        drawing = once(drawing, "PrepareServerPageForPluginAndGui();", helper)
        drawing = once(drawing, '            PublishPluginTitleResult("login_timeout");\n')
        assert re.sub(r"\s+", "", drawing) == re.sub(r"\s+", "", method(baseline, "private void OnGUI()")), name
        checks += 1
        continue
    assert current == baseline, f"Original pregame source mismatch: {name}"
    assert "ArtworkSkin." not in current, f"Unexpected harmonized title skin: {name}"
    checks += 1
print(f"PASS {checks} original-pregame provenance/equivalence checks across {len(files)} source files (not Unity visual validation)")
