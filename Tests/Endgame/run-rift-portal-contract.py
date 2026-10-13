#!/usr/bin/env python3
"""Compile actual rift context, portal interception and native transfer flow without Unity."""
from pathlib import Path
import os, shutil, subprocess, tempfile
root = Path(__file__).resolve().parents[2]
mono = os.environ.get('FFO_MONO') or shutil.which('mono')
csc = os.environ.get('FFO_CSC') or shutil.which('csc')
if not mono or not csc:
    raise SystemExit('Source Documentation/tools toolchain env before running this contract.')
framework = Path(os.environ['FFO_FRAMEWORK_DIR'])
refs = ['-noconfig', '-nostdlib+'] + ['-r:' + str(framework / n) for n in ('mscorlib.dll', 'System.dll', 'System.Core.dll')]
a = root / 'Assets/FakeFantasyOnline/Runtime/Application'
p = root / 'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
sources = [root/'Assets/FakeFantasyOnline/Runtime/Foundation/JsonData.cs',root/'Assets/FakeFantasyOnline/Runtime/Foundation/BoundedJson.cs',*sorted(a.glob('Endgame*.cs')),p/'EndgamePanel.RiftPortal.cs',p/'PlayerController.Startup.cs',p/'PlayerController.TransferFrame.cs',Path(__file__).with_name('EndgameRiftPortalContract.cs')]
with tempfile.TemporaryDirectory(prefix='rift-portal-contract-') as folder:
    exe = Path(folder)/'Contract.exe'
    subprocess.run(([mono,csc] if csc.endswith('.exe') else [csc])+refs+['-nologo','-langversion:9.0','-target:exe','-out:'+str(exe)]+list(map(str,sources)),check=True)
    subprocess.run([mono,str(exe)],check=True)
# The capability gate must protect the actual gateway dispatch too.
gateway=(root/'Assets/FakeFantasyOnline/Runtime/Protocol/PrivateServerGateway.Endgame.cs').read_text()
assert gateway.index('if (response.Kind == "rift.context")') < gateway.index('if (!EndgameInWorld || !Endgame.HelloSent)')
print('PASS context delivery before map-ready/hello presentation guard')
