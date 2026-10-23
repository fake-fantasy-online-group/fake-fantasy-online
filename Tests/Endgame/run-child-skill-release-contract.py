#!/usr/bin/env python3
"""Compile real managed pipeline plus actual native art/sound owners against Unity IO surrogates."""
import os,pathlib,subprocess,tempfile,json,uuid
root=pathlib.Path(__file__).resolve().parents[2]
dotnet=pathlib.Path(os.environ.get('FFO_DOTNET_ROOT','/tmp/ffo-dotnet'))
sdk=sorted((dotnet/'sdk').glob('*/Roslyn/bincore/csc.dll'))[-1]
ref=sorted((dotnet/'packs/Microsoft.NETCore.App.Ref').glob('*/ref/net8.0'))[-1]
version=sorted((dotnet/'shared/Microsoft.NETCore.App').iterdir())[-1].name
runtime=root/'Assets/FakeFantasyOnline/Runtime';p=runtime/'Presentation/ReferenceAssets'
with tempfile.TemporaryDirectory(prefix='child-release-',dir=os.environ.get('TMPDIR','/workspace/shared/.build-tmp')) as tmp:
 out=pathlib.Path(tmp);args=['-nostdlib+','-target:exe','-out:'+str(out/'Child.dll')]+['-r:'+str(x) for x in ref.glob('*.dll')]
 for group in ['Foundation','Domain','Application','Protocol']:args += [str(x) for x in (runtime/group).rglob('*.cs')]
 args += [str(root/'Tests/Endgame'/x) for x in ['ChildSkillReleaseContract.cs','ChildSkillReleaseStubs.cs']]
 args += [str(p/x) for x in ['CombatWorld.ChildRelease.cs','SkillCaster.cs','SkillCaster.ChildRelease.cs','SkillEffectDB.cs','SkillSoundDB.cs','SkillData.Nested.cs']]
 # Compile the exact production sync method, not a hand-maintained reimplementation.
 source=(p/'ReferenceFirstWorldWindowOwner.cs').read_text();start=source.index('    private bool SyncSkillHitEffects(bool force)');end=source.index('    private bool SyncLocalStatusEffects',start)
 (out/'Sync.cs').write_text('using System; using FakeFantasyOnline.Application; namespace FakeFantasyOnline.Presentation.ReferenceAssets { public partial class ReferenceFirstWorldWindowOwner { '+source[start:end]+' } }')
 args.append(str(out/'Sync.cs'))
 (out/'compile.rsp').write_text('\n'.join(args));(out/'Child.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':version}}}))
 subprocess.run([str(dotnet/'dotnet'),'exec',str(sdk),'@'+str(out/'compile.rsp')],check=True)
 subprocess.run([str(dotnet/'dotnet'),'exec',str(out/'Child.dll')],check=True)
 for f in ['CombatWorld.ChildRelease.cs','SkillCaster.ChildRelease.cs']:
  text=(p/f).read_text()
  for forbidden in ['.PlayCast(','.PlayAttack(','.AimFace(','.StartSkillCooldown(','.PlayChant(','.CastKey(']:assert forbidden not in text,(f,forbidden)
 assert '31 | 32 | 64 | 128' in (runtime/'Protocol/NetClient.cs').read_text()
 entry=(runtime/'Protocol/NetClient.EnterGame.cs').read_text();assert entry.index('SendClientCaps();')<entry.index('Send(ProtocolOpcodes.EnterWorldRequest')
 print('PASS child-only isolation and pre-entry capability128 integration guards; server old-cap suppression is covered by server contracts')
