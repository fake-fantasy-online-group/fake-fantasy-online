#!/usr/bin/env python3
"""Compile production native tooltip rendering and assert the removed shelf navigation.
Requires the managed-test profile or Mono + Roslyn and framework references;
deterministic stubs are not actual Unity UI verification.
"""
from pathlib import Path
import argparse,os,subprocess,tempfile
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--mono',default=os.environ.get('FFO_MONO_ROOT','')+'/bin/mono')
p.add_argument('--csc',default=os.environ.get('FFO_CSC'))
p.add_argument('--framework-dir',default=os.environ.get('FFO_FRAMEWORK_DIR'))
a=p.parse_args()
if not a.csc or not a.framework_dir:
 p.error('source Documentation/tools/activate-dotnet-test-toolchain.sh, or supply --csc and --framework-dir')
refs=[Path(a.framework_dir)/name for name in ('mscorlib.dll','System.dll','System.Core.dll')]
for ref in refs:
 if not ref.is_file(): p.error('missing framework reference: '+str(ref))
root=Path(__file__).resolve().parents[2];src=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
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
 subprocess.run([a.mono,a.csc,'-nologo','-noconfig','-nostdlib+','-langversion:9.0','-target:exe','-out:'+str(out)]+['-r:'+str(ref) for ref in refs]+[str(src/'UITip.cs'),str(src/'PetInfoTooltip.cs'),str(src/'SysMsgBox.cs'),str(src/'SysMsgBox.Drawing.cs'),str(src/'EquipmentTooltipColors.cs'),str(Path(__file__).with_name('NativeShelfTooltipContract.cs'))],check=True)
 subprocess.run([a.mono,str(out)],check=True)
