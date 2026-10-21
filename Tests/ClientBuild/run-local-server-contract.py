#!/usr/bin/env python3
"""Production local config -> bootstrap -> native resolver, with real loopback socket."""
from pathlib import Path
import os,subprocess,tempfile
root=Path(__file__).resolve().parents[2];r=root/'Assets/FakeFantasyOnline/Runtime'
s=(r/'Bootstrap/ClientBootstrap.cs').read_text();block=s[s.index('            PrivateServerOptions options ='):s.index('            InitializeClient(options, installer);')]
h=r'''
using System;using System.IO;using System.Net;using System.Net.Sockets;using System.Collections.Generic;
using FakeFantasyOnline.Foundation;using FakeFantasyOnline.Protocol;using FakeFantasyOnline.Application;
namespace UnityEngine {class Application{public static string persistentDataPath,streamingAssetsPath;public static bool isMobilePlatform=false;}}
class ClientResourcePaths{public static string InstalledRoot="unused-remote-resource-root";}
class Installer{public bool ResourceFree=true;public string DispatchUrl="https://release.example.test/dispatch.json";}
namespace FakeFantasyOnline.Protocol { public sealed partial class NetClient{public bool AddressReady;public string AddressError,host;public int port;public Action<string> OnError;Action<string> _warningSink=x=>{};class QueuedNetworkError{public QueuedNetworkError(string x,bool isAddressRetry){}}Queue<QueuedNetworkError> _errs=new Queue<QueuedNetworkError>();}}
class Request:NetClient.IAddressRequest{public int Timeout{set{}}public object Send()=>null;public int Result=>1;public string Error=>null;public string Text=>"remote";public void Dispose(){}}
class Program{
 static void Check(bool x,string s){if(!x)throw new Exception(s);}static void Reject(Action a){try{a();}catch(Exception){return;}throw new Exception("expected invalid local error");}
 static PrivateServerOptions Boot(Installer installer){
__BOOT__
 return options;}
 static NetClient Resolve(PrivateServerOptions options,out int requests){int calls=0;ClientConfig.Srv[] rows=null;var n=new NetClient();n.BindAddressResolutionForIndependentRuntime(()=>{calls++;return new Request();},s=>new ClientConfig.Dispatch{servers=new[]{new ClientConfig.Srv{host="release.example.test",port=19000}}},x=>null,()=>rows,x=>rows=x);n.ConfigureLocalServer(options.LocalServer);var e=n.ResolveServerAddress();int steps=0;while(e.MoveNext())if(++steps>3)throw new Exception("resolve retry");Check(rows!=null,"published directory");requests=calls;return n;}
 static void Main(){var dir=Path.Combine(Path.GetTempPath(),"local-server-"+Guid.NewGuid());var user=Path.Combine(dir,"user");var bundle=Path.Combine(dir,"bundle");Directory.CreateDirectory(user);Directory.CreateDirectory(bundle);UnityEngine.Application.persistentDataPath=user;UnityEngine.Application.streamingAssetsPath=bundle;var installer=new Installer();try{
 int requests;var nofile=Boot(installer);Check(nofile.LocalServer==null&&nofile.DispatchUri.AbsoluteUri==installer.DispatchUrl,"no config retains release dispatch");var remote=Resolve(nofile,out requests);Check(requests==1&&remote.host=="release.example.test","normal discovery unchanged");
 File.WriteAllText(Path.Combine(bundle,"server.json"),"{\"host\":\"legacy.example.test\",\"port\":19001}");Check(Boot(installer).LocalServer.host=="legacy.example.test","legacy streaming path");
 var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();int port=((IPEndPoint)listener.LocalEndpoint).Port;File.WriteAllText(Path.Combine(user,"server.json"),"{\"host\":\"127.0.0.1\",\"port\":"+port+"}");
 // No build configuration/debug switch appears in the production path: formal and development use the same choice.
 for(int mode=0;mode<2;mode++){var local=Boot(installer);Check(local.DispatchUri==null&&local.LocalServer.host=="127.0.0.1","user local wins over bundled and release");var net=Resolve(local,out requests);Check(requests==0&&net.port==port,"local game lookup makes zero dispatch requests");using(var client=new TcpClient()){client.Connect(net.host,net.port);using(var accept=listener.AcceptTcpClient())Check(IPAddress.IsLoopback(((IPEndPoint)accept.Client.RemoteEndPoint).Address),"real local socket");}}
 listener.Stop();
 foreach(string bad in new[]{"broken","{}","{\"host\":\"127.0.0.1\",\"port\":0}","{\"host\":\"http://example.test\",\"port\":19000}","{\"host\":\"0.0.0.0\",\"port\":19000}","{\"host\":\"127.0.0.1\",\"port\":1.5}"}){File.WriteAllText(Path.Combine(user,"server.json"),bad);Reject(()=>Boot(installer));}
 File.Delete(Path.Combine(user,"server.json"));Check(Boot(installer).LocalServer.host=="legacy.example.test","removal restores legacy local");File.Delete(Path.Combine(bundle,"server.json"));Check(Boot(installer).LocalServer==null,"removal restores dispatch");
 Console.WriteLine("PASS production Bootstrap/local config/resolver: external > legacy > dispatch; invalid config fails closed; local zero discovery requests; real TCP loopback; no development-only gate");
 }finally{Directory.Delete(dir,true);}}
}
'''.replace('__BOOT__',block)
files=['Foundation/BoundedJson.cs','Foundation/JsonData.cs','Protocol/PrivateServerOptions.cs','Protocol/NetClient.AddressResolution.cs','Application/ClientConfig.cs']
fw=Path(os.environ['FFO_FRAMEWORK_DIR']);mono=os.environ['FFO_MONO'];csc=os.environ['FFO_CSC']
with tempfile.TemporaryDirectory(prefix='local-server-contract-') as d:
 d=Path(d);(d/'Harness.cs').write_text(h);refs=['-r:'+str(fw/n) for n in ['mscorlib.dll','System.dll','System.Core.dll']]
 subprocess.run([mono,csc,'-nologo','-noconfig','-nostdlib+']+refs+['-out:'+str(d/'Test.exe'),str(d/'Harness.cs')]+[str(r/f) for f in files],check=True)
 subprocess.run([mono,str(d/'Test.exe')],check=True,timeout=20)
assert 'options.LocalServer' in (r/'Bootstrap/NativeNetClientHost.cs').read_text()
assert '_netClient.ConfigureLocalServer(server)' in (r/'Protocol/PrivateServerGateway.AddressResolution.cs').read_text()
assert 'client.Connect(host, port)' in (r/'Protocol/NetClient.Connection.cs').read_text()
assert 'if (string.IsNullOrEmpty(endpointForIndependentRuntime)) yield break;' in (r/'Presentation/ReferenceAssets/NoticeBoard.Lifecycle.cs').read_text()
print('PASS actual host/gateway/transport wiring and no inferred HTTP notice endpoint')
