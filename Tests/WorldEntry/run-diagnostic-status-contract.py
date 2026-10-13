#!/usr/bin/env python3
"""Execute production status routing with title-lifetime substitutes; no Unity launch."""
from pathlib import Path
import os
import shutil
import subprocess
import tempfile

root = Path(__file__).resolve().parents[2]
source = (root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ClientRootView.cs').read_text()
status = source[source.index('    private string _worldStatus'):source.index('    // Existing host admission')]
world = source[source.index('    private bool HasWorldForIndependentRuntime'):source.index('    private void AdoptNativeStartupWorldForIndependentRuntime')]
entry = (root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ClientRootView.EnterWorldReceipt.cs').read_text()
assert '_status = WorldFailureStatus(failure);' in entry
assert entry.count('Debug.LogError(_status);') == 2
assert 'ReferenceFirstWorldWindowFailure failure =\n                boot.LastBuildFailure;' in entry
harness = '''
using System;
enum ClientSessionState { Title, InWorld }
class Session { public ClientSessionState State; }
class Title { public string netMsg = "login"; }
class Boot { public bool HasNativeStartupWorldForIndependentRuntime; }
class Contract {
    Session _session = new Session();
    Title _titleLogin = new Title();
    object _world;
    Boot _gameBootstrap;
%s
%s
    static void Require(bool value, string name) { if (!value) throw new Exception(name); }
    static void Main() {
        var c = new Contract();
        c._status = "title error";
        Require(c._titleLogin.netMsg == "title error" && c._worldStatus == "", "title stays native");
        c._world = new object(); c._session.State = ClientSessionState.InWorld;
        c._status = "world.reference.fail_closed.MissingSourceAsset";
        Require(c._titleLogin.netMsg == "title error", "pending title destruction cannot own world failure");
        Require(c._status == c._worldStatus && c._status.Contains("MissingSourceAsset"), "world failure visible immediately");
        c._titleLogin = null;
        Require(c._status.Contains("MissingSourceAsset"), "world failure survives title destruction");
        c._status = "";
        Require(c._status == "", "successful entry clears prior world failure");
        c._session.State = ClientSessionState.Title; c._titleLogin = new Title();
        c._status = "retry login";
        Require(c._titleLogin.netMsg == "retry login", "return to title routes natively");
        c._gameBootstrap = new Boot { HasNativeStartupWorldForIndependentRuntime = true };
        c._status = "native world failure";
        Require(c._worldStatus == "native world failure", "native startup world uses persistent status");
        Console.WriteLine("PASS 7 world diagnostic lifetime contracts; rejection logging guards intact");
    }
}
''' % (status, world)
mono = os.environ.get('FFO_MONO') or shutil.which('mono')
csc = os.environ.get('FFO_CSC') or shutil.which('csc')
if not mono or not csc:
    raise SystemExit('Set FFO_CSC and put Mono on PATH.')
refs = ['-noconfig']
framework = os.environ.get('FFO_FRAMEWORK_DIR')
if framework:
    refs += ['-nostdlib+'] + ['-r:' + str(Path(framework) / name) for name in ('mscorlib.dll', 'System.dll', 'System.Core.dll')]
with tempfile.TemporaryDirectory(prefix='world-diagnostic-contract-') as folder:
    path = Path(folder)
    (path / 'Contract.cs').write_text(harness)
    compiler = [mono, csc] if csc.lower().endswith('.exe') else [csc]
    subprocess.run(compiler + refs + ['-nologo', '-target:exe', '-out:' + str(path / 'Contract.exe'), str(path / 'Contract.cs')], check=True)
    subprocess.run([mono, str(path / 'Contract.exe')], check=True)
