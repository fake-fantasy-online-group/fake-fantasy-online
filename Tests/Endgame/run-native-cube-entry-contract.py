#!/usr/bin/env python3
"""Compile production item activation owners, inventory right-click handler, and WinDrag. No Unity launch."""
from pathlib import Path
import os, subprocess, tempfile
root=Path(__file__).resolve().parents[2]
runtime=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
s=(runtime/'InventoryDlg.OnGUI.cs').read_text();start=s.index('    private bool SlotRightButton(');brace=s.index('{',start);depth=1;end=brace+1
while depth:
    depth += (s[end]=='{')-(s[end]=='}');end+=1
source=(Path(__file__).with_name('NativeCubeInventoryContract.cs')).read_text().replace('/* ACTUAL_NATIVE_HANDLER */',s[start:end])
start=s.index('    private void ActivateOrdinaryItem(ref int bagTab, Item it, int gridIndex)');brace=s.index('{',start);depth=1;end=brace+1
while depth:
    depth += (s[end]=='{')-(s[end]=='}');end+=1
source=source.replace('/* ACTUAL_NATIVE_ACTIVATION */',s[start:end])
mono=Path(os.environ['FFO_MONO_ROOT'])
with tempfile.TemporaryDirectory(prefix='native-cube-entry-') as tmp:
    tmp=Path(tmp);(tmp/'Contract.cs').write_text(source)
    subprocess.run([str(mono/'bin/mono'),str(mono/'lib/mono/4.5/mcs.exe'),'-langversion:latest','-out:'+str(tmp/'Contract.exe'),str(tmp/'Contract.cs'),str(runtime/'WinDrag.cs'),str(runtime/'ItemActions.Handle.cs'),str(runtime/'ItemActions.WindowPrefix.cs')],check=True)
    subprocess.run([str(mono/'bin/mono'),str(tmp/'Contract.exe')],check=True)

# Production caller routing is checked in addition to the executed owner methods.
inventory=(runtime/'InventoryDlg.OnGUI.cs').read_text()
assert 'ItemActions.Handle(it.id, (byte)bagTab, it.bagIdx' in inventory
assert 'ItemActions.Handle(itemId, item.Item2, item.Item3' in (runtime/'GameHud.HotbarUseAdmission.cs').read_text()
for name in ('ItemActions.Handle.cs','ItemActions.WindowPrefix.cs'):
    source=(runtime/name).read_text()
    assert source.index('EndgamePanel.HandleCubeItemForIndependentRuntime') < source.index('UseItem(')
print('PASS permanent cube intercept precedes native item consumption and cooldown wiring')
