#!/usr/bin/env python3
"""Compile actual engine-free core plus ground presenter using explicit Unity substitutes."""
import os, pathlib, subprocess, tempfile, json
root=pathlib.Path(__file__).resolve().parents[2]
dotnet=pathlib.Path(os.environ.get('FFO_DOTNET_ROOT','/tmp/ffo-dotnet'))
sdk=sorted((dotnet/'sdk').glob('*/Roslyn/bincore/csc.dll'))[-1]
ref=sorted((dotnet/'packs/Microsoft.NETCore.App.Ref').glob('*/ref/net8.0'))[-1]
version=sorted((dotnet/'shared/Microsoft.NETCore.App').iterdir())[-1].name
with tempfile.TemporaryDirectory(prefix='ground-fx-',dir=os.environ.get('TMPDIR','/workspace/shared/.build-tmp')) as tmp:
 out=pathlib.Path(tmp); args=['-nostdlib+','-target:exe','-out:'+str(out/'Ground.dll')]+['-r:'+str(p) for p in ref.glob('*.dll')]
 runtime=root/'Assets/FakeFantasyOnline/Runtime'
 for group in ['Foundation','Domain','Application','Protocol']:args += [str(p) for p in (runtime/group).rglob('*.cs')]
 args += [str(root/'Tests/Endgame/GroundEffectContract.cs'),str(root/'Tests/Endgame/GroundEffectPresentationStubs.cs'),str(runtime/'Presentation/ReferenceAssets/CombatWorld.GroundEffects.cs')]
 (out/'compile.rsp').write_text('\n'.join(args))
 (out/'Ground.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':version}}}))
 subprocess.run([str(dotnet/'dotnet'),'exec',str(sdk),'@'+str(out/'compile.rsp')],check=True)
 subprocess.run([str(dotnet/'dotnet'),'exec',str(out/'Ground.dll')],check=True)
 clear=(runtime/'Presentation/ReferenceAssets/CombatWorld.ClearAll.cs').read_text()
 assert 'I.ClearGroundEffects();' in clear and 'ClearGroundEffects();\n        if (I == this)' in clear
 owner=(runtime/'Presentation/ReferenceAssets/ReferenceFirstWorldWindowOwner.cs').read_text()
 assert 'edge.MapId == character.MapId' in owner and 'owner.PlayGroundEffect(edge)' in owner
 print('PASS teardown and map ownership integration source contract; Unity rendering not tested')
