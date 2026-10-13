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
 sealed class HostContext : SynchronizationContext
 {
  readonly ConcurrentQueue<Action> callbacks=new ConcurrentQueue<Action>();
  public override void Post(SendOrPostCallback callback,object state) => callbacks.Enqueue(()=>callback(state));
  public void Drain() { Action callback;while(callbacks.TryDequeue(out callback))callback(); }
 }
 static void Main()
 {
  var previousContext=SynchronizationContext.Current;var hostContext=new HostContext();SynchronizationContext.SetSynchronizationContext(hostContext);
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
   Receipt(gateway,"{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"native-pair-old\",\"revision\":\"0\",\"chunks\":1,\"items\":[{\"uid\":\"81\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0}]}");
   Check(gateway.Endgame.InventorySnapshotCurrent,"actual gateway publishes a complete native/sidecar pair");
   long inventoryVersion = gateway.InventoryStateVersion;
   Call(gateway,"PublishInventoryState",WorldInventoryState.Empty,CancellationToken.None);
   Check(gateway.InventoryStateVersion > inventoryVersion && !gateway.Endgame.InventorySnapshotCurrent && gateway.Endgame.Find("bag",2,0,1001)==null,
    "actual native inventory publisher invalidates previous sidecar UID before the next packet");
   Receipt(gateway,"{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"native-pair-next\",\"revision\":\"0\",\"chunks\":2,\"items\":[{\"uid\":\"82\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0}]}");
   Check(gateway.Endgame.SnapshotLoading && !gateway.Endgame.InventorySnapshotCurrent,"actual dispatched partial sidecar cannot restore stale item authority");
   gateway.TickEndgameForIndependentRuntime();Check(queue.Count==0,"assembling push does not issue a duplicate snapshot read");
   Receipt(gateway,"{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"native-pair-next\",\"revision\":\"0\",\"chunk\":1,\"chunks\":2,\"items\":[]}");
   Check(gateway.Endgame.InventorySnapshotCurrent && gateway.Endgame.Find("bag",2,0,1001).UID=="82","actual ordered dispatch restores only the newly published same-template UID");
   gateway.TickEndgameForIndependentRuntime();Check(queue.Count==0,"complete same-frame native/sidecar push needs no extra read");
   foreach(string nativeOnly in new[]{"warehouse","item-lock","hair","wardrobe"})
   {
    // These server paths emit native inventory directly, including quantity,
    // money, and metadata-only changes, without pushInventory's sidecar.
    Call(gateway,"PublishInventoryState",WorldInventoryState.Empty,CancellationToken.None);
    Call(gateway,"PublishInventoryState",WorldInventoryState.Empty,CancellationToken.None);
    Check(!gateway.Endgame.InventorySnapshotCurrent && queue.Count==0,nativeOnly+" native batch fences immediately without per-packet sends");
    gateway.TickEndgameForIndependentRuntime();
    Check(Json(Take(queue,key)).Contains("\"op\":\"snapshot\"") && queue.Count==0,nativeOnly+" native-only batch requests one coalesced read");
    gateway.TickEndgameForIndependentRuntime();gateway.TickEndgameForIndependentRuntime();
    Check(queue.Count==0,nativeOnly+" waiting read never retries each frame");
    Receipt(gateway,"{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"native-only-"+nativeOnly+"\",\"revision\":\"0\",\"chunks\":1,\"items\":[{\"uid\":\"82\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0}]}");
    Check(gateway.Endgame.InventorySnapshotCurrent,nativeOnly+" read restores same-revision native authority");
   }
   Call(gateway,"PublishInventoryState",WorldInventoryState.Empty,CancellationToken.None);
   gateway.TickEndgameForIndependentRuntime();Drain(queue);
   gateway.Endgame.Tick(DateTime.UtcNow.AddSeconds(16));
   gateway.TickEndgameForIndependentRuntime();gateway.TickEndgameForIndependentRuntime();
   Check(gateway.Endgame.Error=="timeout" && !gateway.Endgame.InventorySnapshotCurrent && queue.Count==0,"lost recovery read times out without an automatic retry loop");
   Receipt(gateway,"{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"after-timeout\",\"revision\":\"0\",\"chunks\":1,\"items\":[]}");
   System.Threading.Tasks.Task entryPublish=null;
   var entryThread=new Thread(()=>entryPublish=(System.Threading.Tasks.Task)gateway.GetType().GetMethod("PublishInventoryStateOnHostAsync",Private).Invoke(gateway,new object[]{WorldInventoryState.Empty,CancellationToken.None}));
   entryThread.Start();entryThread.Join();
   Check(entryPublish!=null && !entryPublish.IsCompleted && gateway.Endgame.InventorySnapshotCurrent,"retained entry fallback never mutates sidecar collections on its continuation thread");
   hostContext.Drain();entryPublish.GetAwaiter().GetResult();
   Check(!gateway.Endgame.InventorySnapshotCurrent,"retained entry publishes its native identity fence only after host dispatch");
   Receipt(gateway,"{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"after-entry-fallback\",\"revision\":\"0\",\"chunks\":1,\"items\":[]}");
   using(var cancelledEntry=new CancellationTokenSource())
   {
    entryThread=new Thread(()=>entryPublish=(System.Threading.Tasks.Task)gateway.GetType().GetMethod("PublishInventoryStateOnHostAsync",Private).Invoke(gateway,new object[]{WorldInventoryState.Empty,cancelledEntry.Token}));
    entryThread.Start();entryThread.Join();cancelledEntry.Cancel();hostContext.Drain();
    Check(entryPublish.IsCanceled && gateway.Endgame.InventorySnapshotCurrent,"cancelled retained entry cannot invalidate the replacement connection's sidecar state");
   }
   var request=new EndgameRequest("socket.insert","0","9007199254740993","9223372036854775806",2);
   Check(gateway.SendEndgame(request),"real gateway accepts mutation");string original=Json(Take(queue,key));Check(original==request.Json,"real native wire preserves exact command");
   Check(!gateway.SendEndgame(new EndgameRequest("rift.start","0",tier:1)),"real gateway blocks second mutation");
   bool background=true;var thread=new Thread(()=>background=gateway.RetryEndgame());thread.Start();thread.Join();Check(!background,"nonhost retry blocked");
   Receipt(gateway,"{\"kind\":\"socket.insert\",\"requestId\":\""+request.RequestID+"\",\"ok\":false,\"error\":\"pending\"}");
   Check(gateway.Endgame.Pending && gateway.RetryEndgame(),"pending receipt permits exact retry");Check(Json(Take(queue,key))==original,"retry wire JSON identical");
   Call(gateway,"PublishInventoryState",WorldInventoryState.Empty,CancellationToken.None);
   Receipt(gateway,"{\"kind\":\"snapshot\",\"ok\":false,\"error\":\"snapshot_size\",\"message\":\"器物资料超出传输限制，请检查配置\"}");
   gateway.Endgame.Tick(DateTime.UtcNow.AddSeconds(16));gateway.TickEndgameForIndependentRuntime();
   Check(gateway.Endgame.Error=="snapshot_size" && !gateway.Endgame.InventorySnapshotCurrent && gateway.Endgame.Pending && queue.Count==0,"failed snapshot preserves precise error and pending mutation without timeout or auto-read");
   Check(gateway.SendEndgame(new EndgameRequest("snapshot")),"explicit refresh remains sendable after snapshot failure");
   Check(Json(Take(queue,key)).Contains("\"op\":\"snapshot\""),"manual recovery sends only the read request");
   Call(gateway,"DispatchReceivedPacket",null,(ushort)0x80d0,new byte[]{1,0,2,0,0xff,0xff});Check(gateway.Endgame.Error=="invalid_response" && gateway.Endgame.Pending,"malformed sidecar isolated without dropping pending");
   Call(gateway,"PublishInventoryState",WorldInventoryState.Empty,CancellationToken.None);
   Set(gateway,"endgameWorldEntered",false);gateway.TickEndgameForIndependentRuntime();
   Check(queue.Count==0,"departure prevents a pending native-only refresh from sending");
   Call(gateway,"ResetEndgameConnection");Check(!gateway.Endgame.Negotiated && !gateway.EndgameInWorld && !gateway.Endgame.NeedsInventorySnapshotRefresh,"actual reset clears cache, entry fence, and old native-only refresh");
   Set(gateway,"endgameCharacter","other-character");Set(gateway,"endgameWorldEntered",true);gateway.TickEndgameForIndependentRuntime();Drain(queue);
   Receipt(gateway,"{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");Check(!gateway.Endgame.Pending,"other character never receives uncertain request");Drain(queue);
   Call(gateway,"ResetEndgameConnection");Set(gateway,"endgameCharacter","9007199254740993");Set(gateway,"endgameWorldEntered",true);gateway.TickEndgameForIndependentRuntime();Drain(queue);
   Receipt(gateway,"{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");Check(ReferenceEquals(gateway.Endgame.PendingRequest,request),"same character restores original nonce");
   Check(queue.Count==2,"reconnect queries only; mutation never autoexecutes");Drain(queue);
   Receipt(gateway,"{\"kind\":\"socket.insert\",\"requestId\":\""+request.RequestID+"\",\"ok\":true,\"revision\":\"1\"}");Check(!gateway.Endgame.Pending,"real terminal receipt resolves pending");
   Check(gateway.SendEndgame(new EndgameRequest("hello")),"V5 refresh hello sends");Drain(queue);
   Receipt(gateway,"{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.growth.v5\"]}");
   Check(gateway.Endgame.GrowthV5,"actual dispatch negotiates growth V5");
   Receipt(gateway,"{\"kind\":\"skill.ranks\",\"ok\":true,\"revision\":\"1\",\"skillRanks\":[{\"id\":10401,\"hardRank\":19,\"grantRank\":\"0\",\"softRank\":\"7\",\"effectiveRank\":\"26\",\"canCast\":true,\"canInvest\":true}],\"result\":{\"hardCap\":\"20\",\"playerCap\":\"100\",\"petCap\":\"80\",\"unspentPoints\":\"2\",\"formulaVersion\":\"5\"}}");
   Check(gateway.UpgradeSkillAsync(10401,CancellationToken.None).GetAwaiter().GetResult().IsSuccess,"actual upgrade routes through V5 sidecar");
   var skillRequest=gateway.Endgame.PendingRequest;string skillWire=Json(Take(queue,key));
   Check(skillRequest!=null && skillWire.Contains("\"op\":\"skill.upgrade\"") && skillWire.Contains("\"skillId\":10401") && skillWire.Contains("\"expectedRevision\":\"1\""),"actual skill wire contains guarded immutable command");
   Check(!gateway.UpgradeSkillAsync(10401,CancellationToken.None).GetAwaiter().GetResult().IsSuccess && queue.Count==0,"actual duplicate upgrade never sends");
   Check(gateway.RetryEndgame() && Json(Take(queue,key))==skillWire,"actual skill retry keeps same request bytes");
   Receipt(gateway,"{\"kind\":\"skill.upgrade\",\"requestId\":\""+skillRequest.RequestID+"\",\"ok\":true,\"revision\":\"2\"}");
   Check(!gateway.Endgame.Pending && !gateway.Endgame.CanInvestSkill(10401),"receipt waits for new rank revision");
   long combatVersion=gateway.Endgame.CombatStatsVersion;
   Receipt(gateway,"{\"kind\":\"combat.stats\",\"ok\":true,\"revision\":\"2\",\"combatStats\":{\"magicFind\":2147483648,\"physicalMitigationBP\":600,\"elements\":[{\"element\":\"fire\",\"resistBP\":15000}]}}");
   Check(gateway.Endgame.CombatStats.MagicFind==2147483648L && gateway.Endgame.CombatStatsVersion>combatVersion,"actual dispatch accepts independent Int64 combat observation");
   Check(gateway.Endgame.CombatStats.Elements[0].ResistBP==15000 && queue.Count==0,"combat push preserves overcap values without sending mutations");
   foreach(int starter in new[]{13601,13602,11005})
   {
    Check(gateway.UpgradeSkillAsync(starter,CancellationToken.None).GetAwaiter().GetResult().IsSuccess,"starter or life route remains available "+starter);
    byte[] native=Take(queue,key);Check(native[8]==0x12 && native[9]==0x10,"starter or life keeps original native opcode "+starter);
   }
   Set(gateway,"endgameWorldEntered",false);gateway.Endgame.Reset();gateway.TickEndgameForIndependentRuntime();Check(!gateway.Endgame.HelloSent && queue.Count==0,"title fence blocks hello even with stale native stage");
  }
  finally { Call(host,"CloseAndRelease");peer.Close();tcp.Close();queue.Dispose();SynchronizationContext.SetSynchronizationContext(previousContext); }
  Console.WriteLine("PASS "+assertions+" actual host/gateway/crypto integration checks (Unity presentation substitutes; local loopback only).");
 }
}
