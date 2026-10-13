#!/usr/bin/env python3
"""Replay real Session.OnPacket busy bytes through real codec/state/native gate owners."""
from pathlib import Path
import argparse,os,subprocess,tempfile
root=Path(__file__).resolve().parents[2];r=root/'Assets/FakeFantasyOnline/Runtime';p=r/'Presentation/ReferenceAssets'
args=argparse.ArgumentParser();args.add_argument('--fixture',type=Path,required=True);args.add_argument('--client-state-source',type=Path);args.add_argument('--expect-vulnerable',action='store_true');args.add_argument('--admission-fixtures',type=Path);a=args.parse_args()
assert a.fixture.is_file()
assert not (a.expect_vulnerable and a.admission_fixtures), 'before-fix reproduction exits before admission assertions'
if a.admission_fixtures: assert a.admission_fixtures.is_dir()
codec=(r/'Protocol/PrivateServerEndgameCodec.cs').read_text();codec=codec[:codec.index('public sealed partial class NetClient')]+'}\n'
sources=[r/'Foundation/JsonData.cs',r/'Foundation/BoundedJson.cs',*sorted((r/'Application').glob('Endgame*.cs')),p/'EndgamePanel.RiftPortal.cs',p/'PlayerController.Startup.cs',p/'PlayerController.TransferFrame.cs',root/'Tests/Endgame/EndgameRiftPortalContract.cs',root/'Tests/Endgame/RiftReconnectBusyContract.cs']
if a.client_state_source:sources=[a.client_state_source if x.name=='EndgameClientState.cs' else x for x in sources]
mono=Path(os.environ['FFO_MONO_ROOT'])/'bin/mono';framework=Path(os.environ['FFO_FRAMEWORK_DIR'])
refs=['-noconfig','-nostdlib+']+['-r:'+str(framework/n) for n in ('mscorlib.dll','System.dll','System.Core.dll')]
with tempfile.TemporaryDirectory(prefix='rift-reconnect-busy-') as d:
 t=Path(d);(t/'ActualCodec.cs').write_text(codec);exe=t/'Contract.exe'
 subprocess.run([str(mono),os.environ['FFO_CSC'],*refs,'-nologo','-langversion:9.0','-main:RiftReconnectBusyContract','-out:'+str(exe),str(t/'ActualCodec.cs'),*map(str,sources)],check=True)
 subprocess.run([str(mono),str(exe),str(a.fixture)]+(['--expect-vulnerable'] if a.expect_vulnerable else [str(a.admission_fixtures)] if a.admission_fixtures else []),check=True)
