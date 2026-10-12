#!/usr/bin/env python3
"""Real compiled Protocol gateway/0x8003 receipt and 0x800a decoder, no sockets or Unity.
Uses production world appearance gate with deterministic presentation substitutes.
First run Tests/Endgame/run-managed-compile.sh with the same FFO_COMPILE_DIR.
"""
from pathlib import Path
import os, re, shutil, subprocess, tempfile, sys
root = Path(__file__).resolve().parents[2]
compiled = Path(os.environ['FFO_COMPILE_DIR'])
source = (root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/ReferenceFirstWorldWindowOwner.cs').read_text()
start = source.index('    private bool SyncPlayerAppearance(')
end = source.index('    // Official CombatWorld.NearbyPlayers', start)
method = source[start:end]
interface = (root / 'Assets/FakeFantasyOnline/Runtime/Application/IWorldReceiveOwner.cs').read_text()
interface = re.sub(r'///[^\n]*', '', interface)
methods = re.findall(r'\b(void|WorldEntryRetainedScalars)\s+(\w+)\s*\(([^;]*?)\)\s*;', interface, re.S)
stubs = []
for result, name, args in methods:
    body = '' if result == 'void' else 'return new WorldEntryRetainedScalars(60, 0, 100, 100, 100, 50, 50);'
    stubs.append('public %s %s(%s) { %s }' % (result, name, args, body))
mono = os.environ.get('FFO_MONO') or shutil.which('mono')
csc = os.environ['FFO_CSC']
framework = Path(os.environ['FFO_FRAMEWORK_DIR'])
refs = ['-noconfig', '-nostdlib+'] + ['-r:' + str(p) for p in framework.glob('*.dll')]
refs += ['-r:' + str(compiled / ('FakeFantasyOnline.' + name + '.dll')) for name in ('Foundation', 'Domain', 'Application', 'Protocol')]
with tempfile.TemporaryDirectory(prefix='appearance-handoff-') as folder:
    path = Path(folder)
    (path / 'ProductionGate.cs').write_text('using System; using FakeFantasyOnline.Application; partial class WorldOwner {\n' + method + '\n}')
    (path / 'ReceiveOwner.cs').write_text('using System.Collections.Generic; using FakeFantasyOnline.Application; class ReceiveOwner : IWorldReceiveOwner {\n' + '\n'.join(stubs) + '\n}')
    exe = path / 'Contract.exe'
    subprocess.run([mono, csc] + refs + ['-nologo', '-target:exe', '-out:' + str(exe), str(path / 'ProductionGate.cs'), str(path / 'ReceiveOwner.cs'), str(Path(__file__).with_name('AppearanceHandoffContract.cs'))], check=True)
    env = dict(os.environ, MONO_PATH=str(compiled) + ':' + str(framework / 'Facades'))
    subprocess.run([mono, str(exe)] + sys.argv[1:], env=env, check=True)
