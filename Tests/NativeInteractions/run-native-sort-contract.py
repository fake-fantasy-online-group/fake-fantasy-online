#!/usr/bin/env python3
"""Production sort draw helper + real shared cache with deterministic drawing substitutes."""
from pathlib import Path
import os,subprocess,tempfile
root=Path(__file__).resolve().parents[2];p=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
assert 'new Rect(X + s * 365f, Y + s * 40f,\n            s * NativeSortButtonArtwork.Width, s * NativeSortButtonArtwork.Height)' in (p/'InventoryDlg.OnGUI.cs').read_text()
assert 'L(363, 38, NativeSortButtonArtwork.Width, NativeSortButtonArtwork.Height)' in (p/'StoreDlg.Drawing.cs').read_text()
for file,action in [('InventoryDlg.OnGUI.cs','GUI.Button(sort,'),('StoreDlg.Drawing.cs','WhUI.Click(sort, mouse)')]:
 s=(p/file).read_text();assert 'NativeSortButtonArtwork.Draw(sort,' in s and action in s
stubs=(root/'Tests/UiArtwork/ArtworkSkinContract.cs').read_text().split('class Test {')[0]
stubs=stubs.replace('new UnityEngine.Texture2D{name=p+":"+i}', 'new UnityEngine.Texture2D{name=p+":"+i,width=p==NativeSortButtonArtwork.BackgroundPath?86:79,height=p==NativeSortButtonArtwork.BackgroundPath?24:39}')
with tempfile.TemporaryDirectory(prefix='native-sort-contract-') as tmp:
 tmp=Path(tmp);(tmp/'Stubs.cs').write_text(stubs);exe=tmp/'Contract.exe'
 refs=['-r:'+str(Path(os.environ['FFO_FRAMEWORK_DIR'])/n) for n in ('mscorlib.dll','System.dll','System.Core.dll')]
 mono=str(Path(os.environ['FFO_MONO_ROOT'])/'bin/mono')
 subprocess.run([mono,os.environ['FFO_CSC'],'-nologo','-noconfig','-nostdlib+','-langversion:9.0','-out:'+str(exe)]+refs+[str(tmp/'Stubs.cs'),str(p/'ArtworkSkin.cs'),str(p/'NativeSortButtonArtwork.cs'),str(root/'Tests/NativeInteractions/NativeSortButtonContract.cs')],check=True)
 subprocess.run([mono,str(exe)],check=True)
