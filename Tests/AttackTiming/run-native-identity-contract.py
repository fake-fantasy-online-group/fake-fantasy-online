#!/usr/bin/env python3
"""Compile actual engine-free core plus native identity and actual remote action code using explicit Unity substitutes."""
import os, pathlib, subprocess, tempfile, json, re
root=pathlib.Path(__file__).resolve().parents[2]
dotnet=pathlib.Path(os.environ.get('FFO_DOTNET_ROOT','/tmp/ffo-dotnet'))
sdk=sorted((dotnet/'sdk').glob('*/Roslyn/bincore/csc.dll'))[-1]
ref=sorted((dotnet/'packs/Microsoft.NETCore.App.Ref').glob('*/ref/net8.0'))[-1]
version=sorted((dotnet/'shared/Microsoft.NETCore.App').iterdir())[-1].name
with tempfile.TemporaryDirectory(prefix='ground-fx-',dir=os.environ.get('TMPDIR','/workspace/shared/.build-tmp')) as tmp:
 out=pathlib.Path(tmp); args=['-nostdlib+','-target:exe','-out:'+str(out/'Ground.dll')]+['-r:'+str(p) for p in ref.glob('*.dll')]
 runtime=root/'Assets/FakeFantasyOnline/Runtime'
 for group in ['Foundation','Domain','Application','Protocol']:args += [str(p) for p in (runtime/group).rglob('*.cs')]
 args += [str(root/'Tests/AttackTiming/NativeAttackIdentityContract.cs'),str(root/'Tests/AttackTiming/NativeAttackIdentityStubs.cs')]
 args += [str(runtime/'Presentation/ReferenceAssets'/p) for p in ['CombatWorld.AttackInterval.cs','CombatWorld.BasicAttackPose.cs','CombatWorld.ProjectileSelection.cs','ReferenceWorldBasicAttackController.cs','RemotePlayer.Actions.cs']]
 interface=(runtime/'Application/IWorldReceiveOwner.cs').read_text()
 interface=re.sub(r'///[^\n]*','',interface)
 methods=re.findall(r'\b(void|WorldEntryRetainedScalars)\s+(\w+)\s*\(([^;]*?)\)\s*;',interface,re.S)
 stubs=['public %s %s(%s) { %s }' % (result,name,params,'' if result=='void' else 'return new WorldEntryRetainedScalars(60,0,100,100,100,50,50);') for result,name,params in methods]
 (out/'ReceiveOwner.cs').write_text('using System.Collections.Generic; using FakeFantasyOnline.Application; class ReceiveOwner : IWorldReceiveOwner { '+ '\n'.join(stubs)+' }')
 args.append(str(out/'ReceiveOwner.cs'))
 (out/'compile.rsp').write_text('\n'.join(args))
 (out/'Ground.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':version}}}))
 subprocess.run([str(dotnet/'dotnet'),'exec',str(sdk),'@'+str(out/'compile.rsp')],check=True)
 subprocess.run([str(dotnet/'dotnet'),'exec',str(out/'Ground.dll')],check=True)
 # Assert all lifecycle adapters preserve the negotiated field; these are integration
 # source guards, not a claim of running Unity's rendering/lifecycle machinery.
 p=runtime/'Presentation/ReferenceAssets'
 for file in p.glob('*.cs'):
  for line in file.read_text().splitlines():
   if '.SetWeapon(' in line and 'AttackDistance' in line:
    assert 'NativeAttackDistance' in line, (file,line)
 for filename in ['CombatWorld.AttackInterval.cs','CombatWorld.Projectile.cs','CombatWorld.PvpAttack.cs','PlayerController.PlayAttack.cs','EquipDlg.OnGUI.cs']:
  assert 'AttackIdentityDistance' in (p/filename).read_text(),filename
 assert 'CharData.atkDist > 0 ? CharData.atkDist' in (p/'CombatWorld.Approach.cs').read_text()
 assert 'CharData.nativeAtkDist = a.NativeAttackDistance;' in (p/'ReferenceWorldReceiveOwner.RemotePlayerTails.cs').read_text()
 assert 'CharData.nativeAtkDist = character.Appearance.NativeAttackDistance;' in (p/'ReferenceWorldLocalPlayerDoll.cs').read_text()
 assert '31 | 32 | 64' in (runtime/'Protocol/NetClient.cs').read_text()
 print('PASS integration source guards: local receive/entry, every remote SetWeapon adapter, targeting effective range, all cadence/pose/projectile consumers, pre-entry capability bit64')
