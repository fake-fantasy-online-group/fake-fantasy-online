using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Collections.Concurrent;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Protocol;

// Real compiled production gateway, session coordinator, NetClient, crypto and TCP threads.
// Reflection observes transport identity/keys and injects only synthetic error-queue metadata
// for deterministic late-worker scheduling. The production public RSA key has no test private
// counterpart. This is not a production-server authentication test.
static class LoginSocketContract {
 static int checks;
 sealed class Trace:IWorldReceiveTracePort {public bool On=>false;public long Now=>0;public float Ms(long start)=>0;public void Backlog(int count){}public void Net(int opcode,int bytes,float ms){} }
 sealed class PumpContext:SynchronizationContext {readonly ConcurrentQueue<Action> queue=new ConcurrentQueue<Action>();public override void Post(SendOrPostCallback cb,object state){queue.Enqueue(()=>cb(state));}internal void Drain(){while(queue.TryDequeue(out var cb))cb();}}

 static object Get(object o,string n)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
 // Deterministic scheduling seam for retired Rx/Tx errors only: enqueue metadata exactly
 // as a delayed worker would, rather than relying on an unrepeatable OS thread interleaving.
 static void EnqueueError(object net,string message,int? generation,bool addressRetry=false){
  Type type=net.GetType().GetNestedType("QueuedNetworkError",BindingFlags.NonPublic);
  object error=Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{message,addressRetry,generation},null);
  object queue=Get(net,"_errs");queue.GetType().GetMethod("Enqueue").Invoke(queue,new[]{error});
 }
 static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;}
 static void Until(Func<bool> done,Action pump,string label){var end=DateTime.UtcNow.AddSeconds(6);while(!done()){if(DateTime.UtcNow>end)throw new Exception("timeout: "+label);pump();Thread.Sleep(2);} }
 sealed class Fixture:IDisposable {
  internal readonly TcpListener Listener=new TcpListener(IPAddress.Loopback,0);
  internal readonly PrivateServerGateway Gateway;
  internal readonly ClientSessionCoordinator Session;
  internal readonly object Net; internal readonly PumpContext Context=new PumpContext(); internal void Pump(){Context.Drain();Gateway.Pump();}
  internal int Receipts; internal int Code; internal string Message;
  internal Fixture(){
   Listener.Start();var srv=new ClientConfig.Srv{host="127.0.0.1",port=((IPEndPoint)Listener.LocalEndpoint).Port};
   var host=new SynchronizationContextHostExecutor(Context,Thread.CurrentThread.ManagedThreadId);
   Gateway=new PrivateServerGateway(new PrivateServerOptions(null,"socket-regression",localServer:srv),host,()=>0f,receiveTrace:new Trace(),dispatchMillisecondsProvider:()=>100f);
   Gateway.BindAddressResolutionForIndependentRuntime(null,null,null,()=>new[]{srv},s=>{});
   Gateway.ConfigureLocalServerForIndependentRuntime(srv);
   Gateway.ResolveServerAddressForIndependentRuntime().MoveNext();
   Net=Get(Gateway,"_netClient");
   Session=new ClientSessionCoordinator(Gateway.CreateAddressDiscoveryForIndependentRuntime(),Gateway,new InProcessEventBus());
   Session.BindTitleLoginResultForIndependentRuntime((code,msg)=>{Receipts++;Code=code;Message=msg;});
  }
  internal Peer Login(string account){bool done=false;Gateway.ConnectTitleLoginForIndependentRuntime(ok=>{Check(ok,"real connect succeeds");Gateway.SendTitleLoginForIndependentRuntime(account,"test-password");done=true;});
   Until(()=>Listener.Pending(),Pump,"accept");var peer=new Peer(Listener.AcceptTcpClient());
   Check(peer.ReadFrame().Length==256,"RSA-2048 handshake emitted on real TCP");
   Until(()=>done,Pump,"connect callback pumped");peer.Aes=(byte[])Get(Net,"_aesKey");peer.Mac=(byte[])Get(Net,"_macKey");
   byte[] plain=NetCrypto.DecryptBody(peer.ReadFrame(),peer.Aes,peer.Mac);
   Check(BitConverter.ToUInt16(plain,8)==0x1002,"actual Login opcode emitted");
   Check(System.Text.Encoding.UTF8.GetString(plain,12,BitConverter.ToUInt16(plain,10))==account,"actual account serialized");return peer;
  }
  internal void Reply(Peer peer,int code,string text){peer.Reply(code,text);int expected=Receipts+1;Until(()=>Receipts==expected,Pump,"encrypted login receipt");Check(Code==code&&Message==text,"reply decoded through production Rx/Update/Dispatch");}
  public void Dispose(){Gateway.Dispose();Gateway.CompleteHostShutdownForIndependentRuntime();Listener.Stop();}
 }
 sealed class Peer:IDisposable {
  readonly TcpClient client;readonly NetworkStream stream; internal byte[] Aes,Mac;
  internal Peer(TcpClient c){client=c;stream=c.GetStream();stream.ReadTimeout=4000;stream.WriteTimeout=4000;}
  byte[] Read(int count){var b=new byte[count];for(int n=0;n<count;){int r=stream.Read(b,n,count-n);if(r==0)throw new EndOfStreamException();n+=r;}return b;}
  internal byte[] ReadFrame(){int length=BitConverter.ToInt32(Read(4),0);Check(length>0&&length<65536,"bounded frame");return Read(length);}
  internal void Reply(int code,string text){byte[] bytes=System.Text.Encoding.UTF8.GetBytes(text);byte[] plain=new byte[16+bytes.Length];Buffer.BlockCopy(BitConverter.GetBytes((ushort)0x8002),0,plain,8,2);Buffer.BlockCopy(BitConverter.GetBytes(code),0,plain,10,4);Buffer.BlockCopy(BitConverter.GetBytes((ushort)bytes.Length),0,plain,14,2);Buffer.BlockCopy(bytes,0,plain,16,bytes.Length);byte[] frame=NetCrypto.EncryptFrame(plain,Aes,Mac);stream.Write(frame,0,frame.Length);}
  public void Dispose(){client.Close();}
 }
 static void Main(){
  using(var f=new Fixture())using(var a=f.Login("accountA")){
   f.Reply(a,0,"A OK");Check(f.Session.State==ClientSessionState.ServerSelection,"A success drives real coordinator");
   object tcp=Get(f.Net,"_tcp"),queue=Get(f.Net,"_txQ"),aes=Get(f.Net,"_aesKey"),mac=Get(f.Net,"_macKey"),life=Get(f.Gateway,"_connectionLifetime");
   f.Session.ReturnToLoginFromServerPageForIndependentRuntime();Check(!f.Gateway.Connected&&f.Session.State==ClientSessionState.Offline,"GUI return closes authentication session");
   Check(((System.Threading.CancellationTokenSource)life).IsCancellationRequested,"old lifetime cancelled");
   using(var b=f.Login("accountB")){Check(!ReferenceEquals(tcp,Get(f.Net,"_tcp")),"new TCP owner");Check(!ReferenceEquals(queue,Get(f.Net,"_txQ")),"new TX queue");Check(!ReferenceEquals(aes,Get(f.Net,"_aesKey"))&&!ReferenceEquals(mac,Get(f.Net,"_macKey")),"fresh symmetric keys");Check(!ReferenceEquals(life,Get(f.Gateway,"_connectionLifetime")),"new lifetime");f.Reply(b,0,"B OK");
    object live=Get(f.Net,"_tcp");f.Session.ApplyServerSelectionForIndependentRuntime(0,"127.0.0.1",1,false);Check(f.Session.State==ClientSessionState.CharacterSelection&&ReferenceEquals(live,Get(f.Net,"_tcp")),"same-server character selection retains socket");f.Gateway.SendTitleWorldReturnForIndependentRuntime();Check(ReferenceEquals(live,Get(f.Net,"_tcp"))&&f.Gateway.Connected,"ordinary world-return writer retains socket");
   }
  }
  using(var f=new Fixture())using(var first=f.Login("same")){
   f.Reply(first,7,"rejected");object tcp=Get(f.Net,"_tcp");using(var second=f.Login("same")){Check(!ReferenceEquals(tcp,Get(f.Net,"_tcp")),"same-account rejection retry replaces TCP");f.Reply(second,0,"retry OK");}
  }
  using(var f=new Fixture())using(var first=f.Login("slow")){
   int duplicate=0;f.Gateway.ConnectTitleLoginForIndependentRuntime(ok=>duplicate++);f.Gateway.SendTitleLoginForIndependentRuntime("duplicate","pw");Check(duplicate==0&&!f.Listener.Pending(),"duplicate click does not connect or callback");
   first.Reply(0,"stale queued success");Until(()=>((System.Collections.ICollection)Get(f.Net,"_inbox")).Count>0,()=>{},"old packet queued before timeout");
   f.Gateway.CancelTitleLoginForIndependentRuntime();using(var retry=f.Login("retry")){Check(f.Receipts==0,"timeout clears old queued reply");f.Reply(retry,0,"fresh success");Check(f.Receipts==1,"only retry publishes success");}
  }
  using(var f=new Fixture()){
   var endpoint=new ServerEndpoint("127.0.0.1",((IPEndPoint)f.Listener.LocalEndpoint).Port);
   var connect=f.Gateway.ConnectAsync(endpoint,CancellationToken.None);
   Until(()=>f.Listener.Pending(),f.Pump,"task TCP accept");using(var peer=new Peer(f.Listener.AcceptTcpClient())){
    Check(peer.ReadFrame().Length==256,"task path RSA handshake");Until(()=>connect.IsCompleted,f.Pump,"task connect");Check(connect.Result.IsSuccess,"task connect success");
    object tcp=Get(f.Net,"_tcp");int gui=0;bool guiOk=true;f.Gateway.ConnectTitleLoginForIndependentRuntime(ok=>{gui++;guiOk=ok;});Check(gui==1&&!guiOk&&ReferenceEquals(tcp,Get(f.Net,"_tcp")),"Task Connect-before-Authenticate excludes GUI admission");
    peer.Aes=(byte[])Get(f.Net,"_aesKey");peer.Mac=(byte[])Get(f.Net,"_macKey");var auth=f.Gateway.AuthenticateAsync(new LoginCredentials("task","pw"),CancellationToken.None);
    var login=NetCrypto.DecryptBody(peer.ReadFrame(),peer.Aes,peer.Mac);Check(BitConverter.ToUInt16(login,8)==0x1002,"task Authenticate sends real login");
    var duplicate=f.Gateway.ConnectAsync(endpoint,CancellationToken.None);Until(()=>duplicate.IsCompleted,f.Pump,"duplicate task rejection");Check(!duplicate.Result.IsSuccess&&ReferenceEquals(tcp,Get(f.Net,"_tcp")),"second task cannot retire active authentication");
    peer.Reply(0,"task OK");Until(()=>auth.IsCompleted,f.Pump,"task authentication completion");Check(auth.Result.IsSuccess,"ordinary task authentication succeeds");Check(Get(f.Gateway,"_stage").ToString()=="Authenticated","task success commits authenticated stage");
   }
  }
  using(var f=new Fixture())using(var peer=f.Login("gui-owned")){
   object tcp=Get(f.Net,"_tcp");var busy=f.Gateway.ConnectAsync(new ServerEndpoint("127.0.0.1",1),CancellationToken.None);Until(()=>busy.IsCompleted,f.Pump,"GUI excludes task");Check(!busy.Result.IsSuccess&&ReferenceEquals(tcp,Get(f.Net,"_tcp")),"GUI active login rejects Task without closing its TCP");f.Reply(peer,0,"GUI OK");
  }
  using(var f=new Fixture())using(var cancelled=new CancellationTokenSource()){
   var endpoint=new ServerEndpoint("127.0.0.1",((IPEndPoint)f.Listener.LocalEndpoint).Port);var connect=f.Gateway.ConnectAsync(endpoint,cancelled.Token);
   Until(()=>f.Listener.Pending(),f.Pump,"cancel-window accept");using(var old=new Peer(f.Listener.AcceptTcpClient())){
    old.ReadFrame();Until(()=>connect.IsCompleted,f.Pump,"cancel-window connect");Check(connect.Result.IsSuccess,"connect completed before cancellation");object tcp=Get(f.Net,"_tcp");cancelled.Cancel();
    using(var retry=f.Login("after-cancel")){Check(!ReferenceEquals(tcp,Get(f.Net,"_tcp")),"cancelled Task connect owner retires before GUI retry");f.Reply(retry,0,"cancel retry OK");}
   }
  }
  using(var f=new Fixture()){
   int oldCallback=0;f.Gateway.ConnectTitleLoginForIndependentRuntime(ok=>oldCallback++);
   Until(()=>f.Listener.Pending(),()=>{},"un-pumped native accept");using(var abandoned=new Peer(f.Listener.AcceptTcpClient())){
    abandoned.ReadFrame();Until(()=>((System.Collections.ICollection)Get(f.Net,"_connectDone")).Count>0,()=>{},"native worker publishes before callback");f.Gateway.CancelTitleLoginForIndependentRuntime();
    f.Pump();Check(oldCallback==0&&!f.Gateway.Connected,"retired native worker completion cannot publish/call old owner");
    using(var retry=f.Login("after-late-connect")){f.Reply(retry,0,"late connect retry OK");}
   }
  }
  using(var f=new Fixture())using(var first=f.Login("same-success")){
   f.Reply(first,0,"first OK");object tcp=Get(f.Net,"_tcp");using(var second=f.Login("same-success")){Check(!ReferenceEquals(tcp,Get(f.Net,"_tcp")),"already-authenticated same-account retry starts fresh TCP");f.Reply(second,0,"second OK");}
  }
  using(var f=new Fixture())using(var old=f.Login("old-generation")){
   int retired=(int)Get(f.Net,"_gen");int errors=0;f.Gateway.BindNativeNetworkErrorForIndependentRuntime(message=>{errors++;f.Gateway.CancelTitleLoginForIndependentRuntime();});
   EnqueueError(f.Net,"old error already queued",retired);f.Gateway.CancelTitleLoginForIndependentRuntime();
   using(var current=f.Login("new-generation")){
    Check(errors==0,"retirement prevents already-queued old error from reaching title");object tcp=Get(f.Net,"_tcp");
    EnqueueError(f.Net,"late old Rx error",retired);EnqueueError(f.Net,"late old Tx error",retired);f.Pump();
    Check(errors==0&&f.Gateway.Connected&&ReferenceEquals(tcp,Get(f.Net,"_tcp")),"delayed retired-generation errors cannot cancel new login");
    f.Reply(current,0,"new generation OK");Check(f.Receipts==1,"new login succeeds despite retired errors");
    EnqueueError(f.Net,"current generation error",(int)Get(f.Net,"_gen"));f.Pump();Check(errors==1,"current-generation network error still reaches consumer");
    EnqueueError(f.Net,"ordinary local notice",null);f.Pump();Check(errors==2,"ungenerated ordinary error unchanged");
    EnqueueError(f.Net,"resolved address retry",null,true);f.Pump();Check(errors==2,"resolved address retry suppression unchanged");
   }
  }
  Console.WriteLine("PASS LoginSocketContract: "+checks+" assertions (real managed production transport and coordinator)");
 }
}
