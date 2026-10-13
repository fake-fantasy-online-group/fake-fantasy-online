#!/usr/bin/env python3
"""Run the actual restored map iterator. --compare-baseline proves healthy frame/ready
ordering against the earliest repository source f8d8885; this is not Unity timing QA."""
from pathlib import Path
import argparse, base64, hashlib, json, os, shutil, subprocess, tempfile
root=Path(__file__).resolve().parents[2]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--source',type=Path)
parser.add_argument('--compare-baseline',action='store_true')
parser.add_argument('--stress',action='store_true',help='Bounded FIFO extreme schedules; exits nonzero for reproduced defects')
parser.add_argument('--stress-original',action='store_true',help='Run the same stress matrix on f8d8885')
parser.add_argument('--sender',action='store_true',help='Trace actual position sender methods through native wire serialization')
parser.add_argument('--server-trace',type=Path,help='Replay raw production Session/Router batches exported by TestNativePortalExportClientIteratorTrace')
parser.add_argument('--record-server',type=Path,help='Server checkout to execute for --server-trace; hashes actual Go production before and after capture')
args=parser.parse_args()
if args.record_server:
 if not args.server_trace:raise SystemExit('--record-server requires --server-trace output path')
 def server_digest():
  digest=hashlib.sha256()
  for path in sorted((args.record_server/'server').rglob('*.go')):
   if path.name.endswith('_test.go'):continue
   digest.update(str(path.relative_to(args.record_server)).encode()+b'\0'+path.read_bytes()+b'\0')
  return digest.hexdigest()
 digest=server_digest();env=os.environ.copy()
 env['FFO_MAP_TRACE_PATH']=str(args.server_trace.resolve())
 env['FFO_MAP_TRACE_SOURCE_SHA256']=digest
 env['FFO_MAP_TRACE_SOURCE_COMMIT']=subprocess.check_output(['git','rev-parse','HEAD'],cwd=args.record_server,text=True).strip()
 captured=subprocess.run(['go','test','./internal/session','-run','^TestNativePortalExportClientIteratorTrace$','-count=1','-v'],cwd=args.record_server/'server',env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True)
 args.server_trace.with_suffix('.server.log').write_text(captured.stdout)
 if captured.returncode:print(captured.stdout);raise SystemExit(captured.returncode)
 if digest!=server_digest():raise SystemExit('Server production changed during capture; rerecord once source is frozen')
 print('PASS captured actual server wire; commit='+env['FFO_MAP_TRACE_SOURCE_COMMIT']+' production_sha256='+digest)
p=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
current=(args.source or p/'GameBootstrap.cs').read_text()
owner=(p/'ReferenceFirstWorldWindowOwner.cs').read_text()
map_read=owner[owner.index('            string mapPath = '):owner.index('            // BuildWorld consumes the actual returned owner')]
mono=os.environ.get('FFO_MONO') or shutil.which('mono');csc=os.environ.get('FFO_CSC') or shutil.which('csc')
if not mono or not csc:raise SystemExit('Source /workspace/shared/cloud-test-env.sh first.')
refs=['-noconfig'];framework=os.environ.get('FFO_FRAMEWORK_DIR')
if framework:refs+=['-nostdlib+']+['-r:'+str(Path(framework)/x) for x in ('mscorlib.dll','System.dll','System.Core.dll')]
def method(source,signature):
 a=source.index(signature);start=source.index('{',a);depth=1;b=start+1
 while depth:
  depth+=(source[b]=='{')-(source[b]=='}');b+=1
 return source[a:b]
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
  stub=Path(__file__).with_name('MapTransitionContract.cs')
  environment=os.environ.copy()
  if args.server_trace:
   trace_file=json.loads(args.server_trace.read_text())
   traces=trace_file['traces'] if isinstance(trace_file,dict) else trace_file
   if not traces:raise SystemExit('Server trace is empty')
   rows=[]
   for trace in traces:
    rows.append('\t'.join(map(str,['T',trace['name'],int(trace['local_first']),trace['map_id'],trace['x'],trace['y'],trace['expected_builds'],trace['final_map_id'],len(trace['batches'])])))
    for index,batch in enumerate(trace['batches']):
     for payload in batch:
      if len(base64.b64decode(payload,validate=True))<2:raise SystemExit('Invalid raw server packet')
      rows.append('\t'.join(['P',str(index),payload]))
   path=folder/'server-wire.tsv';path.write_text('\n'.join(rows)+'\n')
   environment['FFO_SERVER_MAP_TRACE_TSV']=str(path)
   original=stub.read_text().replace('public static class GroundItems { public static void ClearAll() { } }','')
   original=original.replace('Clears++; Entities.Clear();','Clears++; Entities.Clear(); ServerMapTraceContract.ClearCombat();')
   original=original.replace('void ClearNetNpcs() { }','').replace('void PublishNetNpcSceneForIndependentRuntime(float width, float height, float ppu) { }','')
   original=original.replace('public class Object {','public class Object { public bool Destroyed; public static void Destroy(object value) { if(value is Object obj) obj.Destroyed=true; }')
   original=original.replace('public class Transform {','public class Transform { public Transform parent; public Vector3 position, localScale; public T GetComponent<T>() where T:class => null;')
   original=original.replace('public class GameObject : Object','public partial class GameObject : Object')
   original=original.replace('public struct Vector2 {','public partial struct Vector2 {')
   original=original.replace('public static float unscaledDeltaTime = .05f;','public static float unscaledDeltaTime = .05f; public static float time;')
   original=original.replace('public static float Min(float a, float b)', 'public static int Clamp(int value, int lo, int hi) => Math.Max(lo, Math.Min(hi, value)); public static float Min(float a, float b)')
   original=original.replace('public static class CombatNumber {','public static class CombatNumber { public static Transform PlayerTransform;')
   original=original.replace('Builds.Add(projected);','Builds.Add(projected); TestLocalNpc = new GameObject("map-local NPC outside the network registry");')
   stub=folder/'TraceStubs.cs';stub.write_text(original)
   protocol=root/'Assets/FakeFantasyOnline/Runtime/Protocol';application=root/'Assets/FakeFantasyOnline/Runtime/Application'
   dto=(application/'GameSessionContracts.cs').read_text()
   readers=(protocol/'NetClient.Buf.Read.cs').read_text()
   decoder=(protocol/'PrivateServerWorldStateDecoder.cs').read_text()
   native=folder/'NativeMapReceipts.cs'
   native.write_text('''using System; using System.Text; using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Application { %s %s
public interface INetworkNpcReceiveOwner { void ReceiveNetworkNpcSpawn(WorldNpcState row); void ReceiveNetworkNpcRemove(int id); }
}
namespace FakeFantasyOnline.Protocol {
public sealed partial class NetClient { internal class Buf { byte[] _b; int _pos,_len; %s } }
public static class PrivateServerWorldStateDecoder { %s }
}
'''%(method(dto,'public sealed class WorldGroundItemState'),method(dto,'public sealed class WorldNpcState'),'\n'.join(method(readers,s) for s in ('        public Buf(byte[] data)','        public int I32()','        public int U16()','        public string Str()')),'\n'.join(method(decoder,s) for s in ('    internal static void DecodeNpcState(', '    internal static void DecodeNpcRemoval('))))
   command+=['-main:ServerMapTraceContract',str(Path(__file__).with_name('ServerMapTraceContract.cs')),str(Path(__file__).with_name('NativeMapReceiptStubs.cs')),str(native),str(protocol/'WireBuffer.cs'),str(protocol/'PrivateServerGroundItemCodec.cs'),str(application/'IGroundItemReceiveContext.cs'),str(p.parent/'ClientRootView.GroundReceive.cs')]
   command += [str(p/x) for x in ('GroundItems.Registry.cs','GroundItems.Spawn.cs','GroundItems.Item.cs','GameBootstrap.NetworkNpcs.cs','NpcEntry.cs')]
  if args.sender:
   original=stub.read_text()
   original=original.replace(method(original,'        void SendPositionForIndependentRuntime('),'')
   original=original.replace('public sealed class TestSession','public sealed partial class TestSession')
   original=original.replace('public static float unscaledDeltaTime = .05f;','public static float unscaledDeltaTime = .05f; public static float deltaTime = .05f;')
   original=original.replace('public static void Log(object value)', 'public static void LogException(Exception value) { Errors.Add(value.ToString()); } public static void Log(object value)')
   original=original.replace('public static void Begin(string value)', 'public static void Tick() { } public static void Begin(string value)')
   original=original.replace('public void SetBuildGenerationForIndependentRuntime(long generation)', 'public void TrackPositionReportForIndependentRuntime(System.Threading.Tasks.Task<FakeFantasyOnline.Foundation.Result> pending) { } public void SetBuildGenerationForIndependentRuntime(long generation)')
   stub=folder/'SenderStubs.cs';stub.write_text(original)
   app=root/'Assets/FakeFantasyOnline/Runtime/Application/ClientSessionCoordinator.cs'
   protocol=root/'Assets/FakeFantasyOnline/Runtime/Protocol'
   forwarding='\n'.join(method(app.read_text(),s) for s in ('    public Task<Result> ReportPlayerMoveAsync(', '    public Task<Result> SavePlayerPositionAsync('))
   gateway='\n'.join(method((protocol/'PrivateServerGateway.cs').read_text(),s) for s in ('    public async Task<Result> ReportPlayerMoveAsync(', '    public async Task<Result> SavePlayerPositionAsync('))
   native='\n'.join(method((protocol/'NetClient.cs').read_text(),s) for s in ('    public void SavePos(', '    public void PlayerMove('))
   sender=folder/'ActualPositionSender.cs'
   sender.write_text('''using System; using UnityEngine; using System.Threading; using System.Threading.Tasks; using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
public partial class GameBootstrap { %s }
public sealed partial class TestSession { %s }
public sealed partial class TestGateway { %s }
public sealed partial class NativePositionSender { %s }
}
'''%(method(source,'    private async void SendPositionForIndependentRuntime('),forwarding,gateway,native))
   command+=['-main:MapPositionSenderContract',str(sender),str(p/'GameBootstrap.PositionReporting.cs'),str(Path(__file__).with_name('MapPositionSenderContract.cs'))]
  command += [str(production),str(p/'GameBootstrap.Sky.cs'),str(stub),str(p/'PlayerController.Startup.cs'),str(p/'PlayerController.TransferFrame.cs')]
  subprocess.run(command,check=True)
  result=subprocess.run([mono,str(exe)],text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,env=environment);print(result.stdout,end='')
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
