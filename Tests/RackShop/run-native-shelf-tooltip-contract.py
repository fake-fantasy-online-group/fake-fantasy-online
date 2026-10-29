#!/usr/bin/env python3
"""Compile production native tooltip rendering and assert the removed shelf navigation.
Requires Mono + Roslyn; deterministic stubs are not actual Unity UI verification.
"""
from pathlib import Path
import argparse,os,subprocess,tempfile
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--mono',default=os.environ.get('FFO_MONO_ROOT','')+'/bin/mono')
p.add_argument('--csc',default=os.environ.get('FFO_CSC'))
a=p.parse_args();root=Path(__file__).resolve().parents[2];src=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
rack=(src/'RackShopDlg.cs').read_text();body=rack[rack.index('private void OnGUI()'):]
assert 'TopTab(' not in body and 'DrawRecharge(' not in body and 'DrawRefund(' not in body and 'DrawFlagGrid(' not in body and 'DrawFittingRoom(' not in body
assert 'topTab = 0;' in body and 'DrawShop(mouse);' in body
assert 'covered = covered || SysMsgBox.Visible;' in body
assert body.index('new Vector2(-9999f, -9999f)') < body.index('DrawShop(mouse);')
assert 'Event.current.mousePosition = savedMouse;' in body
assert 'WinDrag.Handle(' in body and 'Click(close, mouse)' in body
print('PASS shelf has no top navigation, ignores modal-covered input, retains native shop/close/drag')
with tempfile.TemporaryDirectory(prefix='native-shelf-tooltip-') as td:
 out=Path(td)/'Contract.exe'
 subprocess.run([a.mono,a.csc,'-nologo','-target:exe','-out:'+str(out),str(src/'UITip.cs'),str(src/'PetInfoTooltip.cs'),str(src/'SysMsgBox.cs'),str(src/'SysMsgBox.Drawing.cs'),str(src/'EquipmentTooltipColors.cs'),str(Path(__file__).with_name('NativeShelfTooltipContract.cs'))],check=True)
 subprocess.run([a.mono,str(out)],check=True)
