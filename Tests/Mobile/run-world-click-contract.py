#!/usr/bin/env python3
"""Compile actual collider callbacks; extract only RemotePlayer callback to exclude unrelated UI/party properties."""
from pathlib import Path
import os, subprocess, tempfile
root=Path(__file__).resolve().parents[2]
p=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
s=(p/'RemotePlayer.Selection.cs').read_text()
a=s.index('    internal void OnMouseDown()'); start=s.index('{',a); depth=1;b=start+1
while depth:
    depth+=(s[b]=='{')-(s[b]=='}');b+=1
with tempfile.TemporaryDirectory() as folder:
    folder=Path(folder); method=folder/'RemoteClick.cs'; exe=folder/'Contract.exe'
    method.write_text('namespace FakeFantasyOnline.Presentation.ReferenceAssets { public partial class RemotePlayer {\n'+s[a:b]+'\n}}')
    mono=os.environ['FFO_MONO'];csc=os.environ['FFO_CSC']
    refs=['-r:'+str(x) for x in sorted(Path(os.environ['FFO_FRAMEWORK_DIR']).glob('*.dll'))]
    subprocess.run([mono,csc,'-noconfig','-nostdlib+','-nologo','-langversion:9.0','-out:'+str(exe)]+refs+[str(p/'MonsterEntity.Click.cs'),str(p/'GroundItemEntity.Click.cs'),str(method),str(root/'Tests/Mobile/WorldClickContract.cs')],check=True)
    subprocess.run([mono,str(exe)],check=True)
