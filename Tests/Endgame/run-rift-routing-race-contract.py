#!/usr/bin/env python3
"""Execute actual Rift state/native transfer/plugin routes; no Unity or live server."""
from pathlib import Path
import os, re, subprocess, tempfile
root=Path(__file__).resolve().parents[2]
a=root/'Assets/FakeFantasyOnline/Runtime/Application'
p=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
f=root/'Assets/FakeFantasyOnline/Runtime/Foundation'
mono=Path(os.environ['FFO_MONO_ROOT'])/'bin/mono'
csc=os.environ['FFO_CSC']; framework=Path(os.environ['FFO_FRAMEWORK_DIR'])
refs=['-noconfig','-nostdlib+']+['-r:'+str(framework/n) for n in ('mscorlib.dll','System.dll','System.Core.dll')]
portal=[f/'JsonData.cs',f/'BoundedJson.cs',*sorted(a.glob('Endgame*.cs')),p/'EndgamePanel.RiftPortal.cs',p/'PlayerController.Startup.cs',p/'PlayerController.TransferFrame.cs',root/'Tests/Endgame/EndgameRiftPortalContract.cs',root/'Tests/Endgame/RiftRoutingRaceContract.cs']
# Reuse the exact full source list from the existing production action contract.
runner=(root/'Tests/PluginRuntime/run-game-actions.sh').read_text()
patterns=re.findall(r'"\$ROOT"?/?([^\n]+?\.cs)"?\s*\\?',runner)
plugin=[]
for raw in patterns:
    raw=raw.strip().lstrip('/').replace('"','')
    plugin.extend(sorted(root.glob(raw)))
assert any(x.name=='PluginGameApi.Endgame.Actions.cs' for x in plugin)
plugin.append(root/'Tests/Endgame/RiftPluginRoutingRaceContract.cs')
with tempfile.TemporaryDirectory(prefix='rift-routing-races-') as d:
    for name,entry,sources in [('native','RiftRoutingRaceContract',portal),('plugin','RiftPluginRoutingRaceMain',plugin)]:
        exe=Path(d)/(name+'.exe')
        subprocess.run([str(mono),csc,*refs,'-nologo','-langversion:9.0','-target:exe','-main:'+entry,'-out:'+str(exe),*map(str,sources)],check=True)
        subprocess.run([str(mono),str(exe)],check=True)
