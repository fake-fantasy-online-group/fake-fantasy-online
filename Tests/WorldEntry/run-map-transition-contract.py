#!/usr/bin/env python3
"""Run the actual restored map iterator. --compare-baseline proves healthy frame/ready
ordering against the earliest repository source f8d8885; this is not Unity timing QA."""
from pathlib import Path
import argparse, os, shutil, subprocess, tempfile
root=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--source',type=Path)
parser.add_argument('--compare-baseline',action='store_true')
parser.add_argument('--stress',action='store_true',help='Bounded FIFO extreme schedules; exits nonzero for reproduced defects')
parser.add_argument('--stress-original',action='store_true',help='Run the same stress matrix on f8d8885')
args=parser.parse_args()
p=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
current=(args.source or p/'GameBootstrap.cs').read_text()
owner=(p/'ReferenceFirstWorldWindowOwner.cs').read_text()
map_read=owner[owner.index('            string mapPath = '):owner.index('            // BuildWorld consumes the actual returned owner')]
mono=os.environ.get('FFO_MONO') or shutil.which('mono');csc=os.environ.get('FFO_CSC') or shutil.which('csc')
if not mono or not csc:raise SystemExit('Source /workspace/shared/cloud-test-env.sh first.')
refs=['-noconfig'];framework=os.environ.get('FFO_FRAMEWORK_DIR')
if framework:refs+=['-nostdlib+']+['-r:'+str(Path(framework)/x) for x in ('mscorlib.dll','System.dll','System.Core.dll')]
def run(source,baseline):
 construction=source[source.index('    internal sealed class BuildWorldCheckpoints'):source.index('    // The real controller is available')]
 if 'internal sealed class PreparedMapSource' not in construction:
  a=current.index('    internal sealed class PreparedMapSource');b=current.index('    /// <summary>',a)
  construction+=current[a:b] # unused compatibility type for the shared test window stub
 transition=source[source.index('    public void SwitchMapAuthoritative'):source.index('    // Official 0x060003bd:')]
 if '    private void CancelMapTransitionForIndependentRuntime()' in source:
  a=source.index('    private void CancelMapTransitionForIndependentRuntime()');b=source.index('    // Host bindings support',a);transition+=source[a:b]
 a=source.index('    private void ReleaseWorld(bool preserveReceiptOverlay')
 exit_prefix=source[source.index('{',a)+1:source.index('        bool hadWorldState',a)]
 with tempfile.TemporaryDirectory(prefix='map-original-lifecycle-') as temp:
  folder=Path(temp);production=folder/'Production.cs';exe=folder/'Contract.exe'
  production.write_text('''using System; using System.Collections; using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets { public partial class GameBootstrap {
%s
%s
public void ExitForTest() { %s worldRoot=null; window=null; pcRef=null; CurMapId=0; }
}
public sealed partial class ReferenceFirstWorldWindowOwner { internal byte[] ReadConstructionMapForTest(ResLoader loader,string file) {
var resources=new TestResources{Core=loader};var mapDescriptor=new TestMapDescriptor{file=file};
%s
return mapBytes;
} } }
'''%(construction,transition,exit_prefix,map_read))
  command=([mono,csc] if csc.lower().endswith('.exe') else [csc])+refs+['-nologo','-langversion:9.0','-out:'+str(exe)]
  if baseline:command+=['-define:ORIGINAL_BASELINE']
  if args.stress or args.stress_original:
   command += ['-main:MapTransitionStressContract',str(Path(__file__).with_name('MapTransitionStressContract.cs'))]
  command += [str(production),str(p/'GameBootstrap.Sky.cs'),str(Path(__file__).with_name('MapTransitionContract.cs')),str(p/'PlayerController.Startup.cs'),str(p/'PlayerController.TransferFrame.cs')]
  subprocess.run(command,check=True)
  result=subprocess.run([mono,str(exe)],text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT);print(result.stdout,end='')
  if result.returncode:raise SystemExit(result.returncode)
  return [line for line in result.stdout.splitlines() if line.startswith('FLOW ')]
if args.stress_original:
 original=subprocess.check_output(['git','show','f8d8885:Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/GameBootstrap.cs'],cwd=root,text=True)
 run(original,True);raise SystemExit(0)
flow=run(current,False)
if args.compare_baseline:
 original=subprocess.check_output(['git','show','f8d8885:Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets/GameBootstrap.cs'],cwd=root,text=True)
 baseline=run(original,True)
 if flow!=baseline:raise SystemExit('FAILED: normal frame/build/ready/receive ordering differs from f8d8885')
 print('PASS restored normal frame/build/ready/receive ordering matches f8d8885 across '+str(len(flow))+' flows; byte-read optimization excluded from trace')
