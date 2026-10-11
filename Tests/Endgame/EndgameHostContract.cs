using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Bootstrap;
using FakeFantasyOnline.Presentation;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using FakeFantasyOnline.Protocol;
class EndgameHostContract
{
 static int assertions;
 static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Check(bool value,string name) { if(!value) throw new Exception(name); assertions++; }
 static object Get(object obj,string name) => obj.GetType().GetField(name,Private).GetValue(obj);
 static void Set(object obj,string name,object value) => obj.GetType().GetField(name,Private).SetValue(obj,value);
 static void Call(object obj,string name,params object[] args) => obj.GetType().GetMethod(name,Private).Invoke(obj,args);
 static byte[] Envelope(string json) { var b=Encoding.UTF8.GetBytes(json); var p=new byte[b.Length+4];p[0]=1;p[2]=(byte)b.Length;p[3]=(byte)(b.Length>>8);Buffer.BlockCopy(b,0,p,4,b.Length);return p; }
 static void Receipt(PrivateServerGateway gateway,string json) => Call(gateway,"DispatchReceivedPacket",null,(ushort)0x80d0,Envelope(json));
 static byte[] Take(BlockingCollection<byte[]> queue,byte[] key)
 {
  byte[] encrypted;Check(queue.TryTake(out encrypted,1000),"actual native tx queue populated");var body=new byte[encrypted.Length-4];Buffer.BlockCopy(encrypted,4,body,0,body.Length);return NetCrypto.DecryptBody(body,key,key);
 }
 static string Json(byte[] frame)
 {
  Check(frame[8]==0xd0 && frame[9]==0x10 && frame[10]==1 && frame[11]==0,"actual encrypted opcode/schema");
  int length=frame[12]|frame[13]<<8; Check(length==frame.Length-14,"actual envelope length");return Encoding.UTF8.GetString(frame,14,length);
 }
 static void Drain(BlockingCollection<byte[]> queue) { byte[] data;while(queue.TryTake(out data)){} }
 static void Main()
 {
  var options=new PrivateServerOptions(new Uri("http://127.0.0.1:1/dispatch.json"),"synthetic-host");
  var host=new NativeNetClientHost(options);var view=new ClientRootView();
  host.BindOwner(new FakeFantasyOnline.Presentation.ReferenceAssets.NetClient(),view);
  var first=view.GetComponent<EndgamePanel>();Check(first!=null && first.Bound!=null,"actual host attaches and binds panel");
  host.AttachView(view);Check(first.Binds==1,"same view does not rebind");
  var next=new ClientRootView();host.AttachView(next);Check(first.Bound==null && first.Unbinds==1 && next.GetComponent<EndgamePanel>().Bound!=null,"replacement detaches old panel");
  host.DetachView(view);Check(next.GetComponent<EndgamePanel>().Bound!=null,"old detach cannot unbind new view");
  host.DetachView(next);Check(next.GetComponent<EndgamePanel>().Bound==null,"current detach unbinds panel");
  host.AttachView(next);
  var gateway=(PrivateServerGateway)Get(host,"_gateway");var net=Get(gateway,"_netClient");
  Check(!gateway.SendEndgame(new EndgameRequest("hello")),"actual gateway rejects title/disconnected command");
  var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();var tcp=new TcpClient();tcp.Connect((IPEndPoint)listener.LocalEndpoint);var peer=listener.AcceptTcpClient();listener.Stop();
  var key=new byte[32];var queue=new BlockingCollection<byte[]>(64);
  try
  {
   Set(net,"_tcp",tcp);Set(net,"_aesKey",key);Set(net,"_macKey",key);Set(net,"_txQ",queue);
   var stage=Get(gateway,"_stage");Set(gateway,"_stage",Enum.Parse(stage.GetType(),"InWorld"));Set(gateway,"endgameWorldEntered",true);
   Set(gateway,"endgameAccount","synthetic-account");Set(gateway,"endgameCharacter","9007199254740993");
   gateway.TickEndgameForIndependentRuntime();Check(Json(Take(queue,key)).Contains("\"op\":\"hello\""),"host tick sends real hello");
   Receipt(gateway,"{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");
   Check(gateway.Endgame.Negotiated,"real main dispatch negotiates hello");
   Check(Json(Take(queue,key)).Contains("\"op\":\"snapshot\""),"hello requests snapshot");Check(Json(Take(queue,key)).Contains("\"op\":\"rift.state\""),"hello requests rift");
   var request=new EndgameRequest("socket.insert","0","9007199254740993","9223372036854775806",2);
   Check(gateway.SendEndgame(request),"real gateway accepts mutation");string original=Json(Take(queue,key));Check(original==request.Json,"real native wire preserves exact command");
   Check(!gateway.SendEndgame(new EndgameRequest("rift.start","0",tier:1)),"real gateway blocks second mutation");
   bool background=true;var thread=new Thread(()=>background=gateway.RetryEndgame());thread.Start();thread.Join();Check(!background,"nonhost retry blocked");
   Receipt(gateway,"{\"kind\":\"socket.insert\",\"requestId\":\""+request.RequestID+"\",\"ok\":false,\"error\":\"pending\"}");
   Check(gateway.Endgame.Pending && gateway.RetryEndgame(),"pending receipt permits exact retry");Check(Json(Take(queue,key))==original,"retry wire JSON identical");
   Call(gateway,"DispatchReceivedPacket",null,(ushort)0x80d0,new byte[]{1,0,2,0,0xff,0xff});Check(gateway.Endgame.Error=="invalid_response" && gateway.Endgame.Pending,"malformed sidecar isolated without dropping pending");
   Call(gateway,"ResetEndgameConnection");Check(!gateway.Endgame.Negotiated && !gateway.EndgameInWorld,"actual reset clears cache and entry fence");
   Set(gateway,"endgameCharacter","other-character");Set(gateway,"endgameWorldEntered",true);gateway.TickEndgameForIndependentRuntime();Drain(queue);
   Receipt(gateway,"{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");Check(!gateway.Endgame.Pending,"other character never receives uncertain request");Drain(queue);
   Call(gateway,"ResetEndgameConnection");Set(gateway,"endgameCharacter","9007199254740993");Set(gateway,"endgameWorldEntered",true);gateway.TickEndgameForIndependentRuntime();Drain(queue);
   Receipt(gateway,"{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");Check(ReferenceEquals(gateway.Endgame.PendingRequest,request),"same character restores original nonce");
   Check(queue.Count==2,"reconnect queries only; mutation never autoexecutes");Drain(queue);
   Receipt(gateway,"{\"kind\":\"socket.insert\",\"requestId\":\""+request.RequestID+"\",\"ok\":true,\"revision\":\"1\"}");Check(!gateway.Endgame.Pending,"real terminal receipt resolves pending");
   Set(gateway,"endgameWorldEntered",false);gateway.Endgame.Reset();gateway.TickEndgameForIndependentRuntime();Check(!gateway.Endgame.HelloSent && queue.Count==0,"title fence blocks hello even with stale native stage");
  }
  finally { Call(host,"CloseAndRelease");peer.Close();tcp.Close();queue.Dispose(); }
  Console.WriteLine("PASS "+assertions+" actual host/gateway/crypto integration checks (Unity presentation substitutes; local loopback only).");
 }
}
