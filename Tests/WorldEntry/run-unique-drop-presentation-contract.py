#!/usr/bin/env python3
"""Run production codec, native receive, unique receipt, attached beam and guarded audio code."""
from pathlib import Path
import os,shutil,subprocess,tempfile
root=Path(__file__).resolve().parents[2];base=root/'Assets/FakeFantasyOnline/Runtime'
def member(source,signature):
 a=source.index(signature);i=source.index('{',a);depth=1;b=i+1
 while depth:
  if source[b]=='{':depth+=1
  if source[b]=='}':depth-=1
  b+=1
 return source[a:b]
state=member((base/'Application/GameSessionContracts.cs').read_text(),'public sealed class WorldGroundItemState')
receive=member((base/'Protocol/PrivateServerGateway.GroundReceive.cs').read_text(),'    private void ReceiveGroundSpawn(')
audio=member((base/'Presentation/ReferenceAssets/SoundManager.cs').read_text(),'    private IEnumerator LoadAndPlay(')
mono=os.environ.get('FFO_MONO')or shutil.which('mono');csc=os.environ.get('FFO_CSC')or shutil.which('csc')
if not mono or not csc:raise SystemExit('Source the managed toolchain env.sh first.')
refs=['-noconfig'];framework=os.environ.get('FFO_FRAMEWORK_DIR')
if framework:refs+=['-nostdlib+']+['-r:'+str(Path(framework)/n) for n in ['mscorlib.dll','System.dll','System.Core.dll']]
with tempfile.TemporaryDirectory(prefix='unique-drop-contract-')as d:
 out=Path(d);prod=out/'Extracted.cs'
 prod.write_text('using System;using System.Collections;using System.Collections.Generic;using System.Threading;using FakeFantasyOnline.Application;using UnityEngine;using UnityEngine.Networking;\nnamespace FakeFantasyOnline.Application{'+state+'}\nnamespace FakeFantasyOnline.Protocol{public sealed partial class PrivateServerGateway{'+receive+'}}\nnamespace FakeFantasyOnline.Presentation.ReferenceAssets{public partial class SoundManager{'+audio+'}}')
 files=[prod,Path(__file__).with_name('UniqueDropPresentationContract.cs')]+[base/p for p in ['Application/IGroundItemReceiveContext.cs','Application/WorldUniqueDropPresentation.cs','Protocol/WireBuffer.cs','Protocol/PrivateServerGroundItemCodec.cs','Presentation/ReferenceAssets/GroundItemEntity.UniqueDrop.cs','Presentation/ReferenceAssets/EffectPlayer.GroundAttachment.cs','Presentation/ReferenceAssets/SoundManager.Guarded.cs']]
 exe=out/'Contract.exe';compiler=[mono,csc]if csc.endswith('.exe')else[csc]
 subprocess.run(compiler+refs+['-nologo','-langversion:9.0','-target:exe','-out:'+str(exe)]+list(map(str,files)),check=True)
 subprocess.run([mono,str(exe)],check=True)
# Production integration points, independent of the deterministic Unity boundary above.
spawn=(base/'Presentation/ReferenceAssets/GroundItems.Spawn.cs').read_text();assert 'entity.ConfigureUniqueDrop(uniqueId, freshDrop, dropToken)'in spawn
receipt=(base/'Presentation/ClientRootView.GroundReceive.cs').read_text();assert 'state.UniqueId, state.FreshDrop, state.DropToken' in receipt
actor=(base/'Presentation/ReferenceAssets/ReferenceWorldGroundItem.cs').read_text();assert 'ConfigureUniqueDrop(state.UniqueId, false, state.DropToken)'in actor
visual=(base/'Presentation/ReferenceAssets/GroundItemEntity.Visuals.cs').read_text();assert 'ReleaseUniqueDrop();' in member(visual,'    private void OnDisable()');assert 'UpdateUniqueDrop();'in visual
entry=(base/'Protocol/NetClient.EnterGame.cs').read_text();assert entry.index('SendClientCaps();')<entry.index('Send(ProtocolOpcodes.EnterWorldRequest')
items=(base/'Presentation/ReferenceAssets/GroundItems.Item.cs').read_text();assert 'CancelUniqueDrop();'in items
print('PASS native spawn, projection, disable, and pre-entry capability wiring')
