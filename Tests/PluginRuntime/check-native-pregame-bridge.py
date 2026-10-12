#!/usr/bin/env python3
"""Source-level native bridge invariants. Does not launch Unity or contact a server.

These complement adapter execution tests; they do not claim native runtime coverage.
"""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[2]
base = root / "Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets"
bridge = (base / "TitleFlow.PluginBridge.cs").read_text()
contracts = (base / "TitleFlow.PluginBridge.Contracts.cs").read_text()
login = (base / "TitleFlow.AccountConnections.cs").read_text()
gui = (base / "TitleFlow.NativeGui.cs").read_text()
server = (base / "TitleFlow.Servers.cs").read_text()
character = (base / "TitleFlow.CharacterSelection.cs").read_text()
lifetime = (base / "TitleFlow.CharacterSelection.Runtime.cs").read_text()

def method(source, signature):
    start = source.index(signature)
    opening = source.index("{", start)
    depth = 1
    pos = opening + 1
    while depth:
        depth += (source[pos] == "{") - (source[pos] == "}")
        pos += 1
    return source[opening + 1:pos - 1]

snapshot = method(bridge, "public PluginTitleSnapshot ReadState()")
fingerprint = method(bridge, "private static string Fingerprint(")
submit = method(bridge, "public string SubmitLogin()")
credentials = method(bridge, "public string SubmitCredentials(")
confirm = method(bridge, "public string ConfirmSelectedServer()")
enter = method(bridge, "public string EnterWorld()")
select = method(bridge, "public string SelectCharacter(")

# Read-only projection cannot route any action or echo a native status string.
for forbidden in ("BeginLogin(", "ConfirmServer(", "EnterGame(", "RequestTitle", "SaveLoginPrefs("):
    assert forbidden not in snapshot, forbidden
assert 'SafeMessage = string.IsNullOrEmpty(netMsg) ? string.Empty : "Native title has a status message."' in snapshot
assert "account" not in fingerprint.lower() and "password" not in fingerprint.lower()
assert "Submitted" not in contracts and "Password" not in contracts
assert "serverTotal > servers.Count" in snapshot and "characterTotal > characters.Count" in snapshot
assert "4096" in snapshot

# Existing native path owns all requests and server endpoint-switch decisions.
assert "BeginLogin(account.Trim(), savePreferences: false)" in submit
assert "ConfirmServer();" in confirm
assert "EnterGame();" in enter
assert "MirrorSlotToCharData(selSlot);" in select
assert "ApplyServerSelectionForIndependentRuntime" not in bridge
assert "SendTitleLoginForIndependentRuntime" not in bridge
assert "RequestTitleEntryForIndependentRuntime" not in bridge
assert "PrepareServerPageForPluginAndGui();" in gui
assert "ServerSwitchReauthenticationCode" in server
assert 'PublishPluginTitleResult("enter_timeout")' in character

# Dedicated credential dispatch never leaves the supplied pair in remembered UI fields.
assert "submittedAccount.Length > 20" in credentials
assert "submittedPassword.Length > 40" in credentials
assert "finally" in credentials
assert "account = previousAccount;" in credentials and "password = previousPassword;" in credentials
assert "SaveLoginPrefs" not in credentials and "PlayerPrefs" not in credentials
assert "savePreferences && (_rememberAccount || _rememberPassword)" in login
assert "finally { transientPassword = null; }" in login
assert 'PublishPluginTitleResult(code == 0 ? "login_succeeded" : "login_failed", code)' in login
assert "ReferenceEquals(pluginTitleOwner, this)" in lifetime

# Narrow surface: no account lifecycle or character deletion/creation capability.
interface = method(contracts, "internal interface IPluginTitleBridge")
for forbidden in ("Create", "Delete", "Reset", "Register", "Recover"):
    assert forbidden not in interface
assert "private string GuardPluginTitle(" in bridge
assert 'return "native_modal_open"' in bridge
assert 'return "title_busy"' in bridge
print("PASS: native pregame bridge source invariants")
