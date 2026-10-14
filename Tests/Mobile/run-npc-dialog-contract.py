#!/usr/bin/env python3
"""Run production layout, pagination, gesture routing and original ActivateOption with managed stubs."""
from pathlib import Path
import os, subprocess, tempfile
root=Path(__file__).resolve().parents[2]; p=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
s=(p/'DialogUI.Mobile.cs').read_text()
def method(source,signature):
 a=source.index(signature); b=source.index('{',a)+1; depth=1
 while depth:
  depth+=(source[b]=='{')-(source[b]=='}'); b+=1
 return source[a:b]
# Drop only rendering; keep the production fields, synchronization, hit tests,
# event ownership, page/close routing and actual original action invocation.
s=s.replace(method(s,'    private void DrawMobileDialog()'),'').replace(method(s,'    private void DrawMobileControl('),'')
orig=(p/'DialogUI.cs').read_text()
s=s.replace('public sealed partial class DialogUI\n{','public sealed partial class DialogUI\n{\n'+method(orig,'    private bool ActivateOption(int index)'))
assert 'if (MobileClientPresentation.Enabled) { DrawMobileDialog(); return; }' in orig
assert 'if (MobileClientPresentation.Enabled) return true;' in orig
assert 'DialogUI.VisibleS ||' in (p/'MobileClientPresentation.cs').read_text()
with tempfile.TemporaryDirectory() as tmp:
 tmp=Path(tmp); source=tmp/'Mobile.cs'; source.write_text(s); exe=tmp/'Contract.exe'
 refs=['-r:'+str(x) for x in sorted(Path(os.environ['FFO_FRAMEWORK_DIR']).glob('*.dll'))]
 subprocess.run([os.environ['FFO_MONO'],os.environ['FFO_CSC'],'-noconfig','-nostdlib+','-nologo','-langversion:9.0','-out:'+str(exe)]+refs+[str(source),str(p/'MobileNpcDialogLayout.cs'),str(root/'Tests/Mobile/NpcDialogContract.cs')],check=True)
 subprocess.run([os.environ['FFO_MONO'],str(exe)],check=True)
