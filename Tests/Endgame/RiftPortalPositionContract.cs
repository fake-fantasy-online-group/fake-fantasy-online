using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;
namespace UnityEngine {
 public struct Vector2 { public float x,y; public Vector2(float a,float b){x=a;y=b;} }
 public struct Color { public Color(float a,float b,float c,float d){} }
 public static class Time { public static float deltaTime; }
 public static class Mathf {public static float Clamp(float x,float lo,float hi)=>Math.Max(lo,Math.Min(hi,x));}
 public static class Debug {public static void LogException(Exception e){throw e;}}
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 internal static class CharData {internal static int[] attrs=new int[10];}
 public partial class PlayerController {public float worldX,worldY,followLostT;public int followId; public bool followLastOk;private void LoadMask(){} }
 public static class MapLink {public class MapProc {public Vector2[] poly;public string cmd;public int mapTo;public float toX,toY;} public static bool PointInPoly(Vector2 p,Vector2[] poly)=>p.x>=240&&p.x<=250&&p.y>=290&&p.y<=310;}
 public static class SocialClient {public static List<(int,string,int,int,int,int,int,int,bool)> Party=new List<(int,string,int,int,int,int,int,int,bool)>();}
 public class NetClient {public static NetClient I=new NetClient();public void GmCmdForIndependentRuntime(string x){} }
 public static class ChatUI {public static void Add(string a,string b,Color c,string d){} }
 public static class DifficultyBox {public static void Show(string a,string b,string c,Action d,Action e){} }
 public sealed partial class EndgamePanel {
  internal static EndgamePanel Current;private IEndgameClient client; private EndgameClientState State=>client?.Endgame;
  internal static bool WorldEntryAvailable=>Current?.client!=null&&Current.client.EndgameInWorld&&Current.State.Negotiated;
  private bool Send(EndgameRequest req)=>client.SendEndgame(req); public EndgamePanel(IEndgameClient c){client=c;Current=this;}
 }
 public static class LoadTrace {public static void Tick(){}}
 public static class ArtUploadClock {public static void BeginFrame(){}}
 public class FakeWindow {public void TrackPositionReportForIndependentRuntime(Task<Result> x){}}
 public class FakePc {public object Root=new object();public PlayerController Player;public float WorldX=>Player.worldX;public float WorldY=>Player.worldY;}
 public class FakeSession {public bool Connected=>true;public Task<Result> ReportPlayerMoveAsync(int x,int y,CancellationToken token){WireLog.Move(x,y);return Task.FromResult(Result.Success());}}
 public static class WireLog {public static int ServerX,ServerY;public static List<string> Packets=new List<string>();public static void Move(int x,int y){ServerX=x;ServerY=y;Packets.Add("move("+x+","+y+")");}}
 public partial class GameBootstrap {
  public static GameBootstrap I; private FakePc pcRef; private FakeSession session=new FakeSession(); private FakeWindow window=new FakeWindow();
  private CancellationTokenSource positionReportLifetime=new CancellationTokenSource(); private int _lastReportX,_lastReportY;
  private void UpdateTraceDiagnosticsForIndependentRuntime(){} private void UpdateFade(){} private void ReportPos(bool enter){}
  public GameBootstrap(PlayerController p,int x,int y){I=this;pcRef=new FakePc{Player=p};_lastReportX=x;_lastReportY=y;WireLog.ServerX=x;WireLog.ServerY=y;WireLog.Packets.Clear();}
  public void Tick(float dt){Time.deltaTime=dt;Update();}
 }
}
class RiftPortalPositionContract {
 static int checks;static void Check(bool x,string m){if(!x)throw new Exception(m);checks++;Console.WriteLine("PASS "+m);}
 sealed class Client:IEndgameClient {
  public EndgameClientState Endgame{get;}=new EndgameClientState();public bool EndgameInWorld=>true;public int Attempts,Accepts;public bool Reject;
  public bool SendEndgame(EndgameRequest req){if(!Endgame.Begin(req))return false;Attempts++;WireLog.Packets.Add(req.Op+"@server("+WireLog.ServerX+","+WireLog.ServerY+")");
   long dx=WireLog.ServerX-200,dy=WireLog.ServerY-300;bool pass=!Reject&&dx*dx+dy*dy<=160*160;if(pass)Accepts++;
   // A successful send remains pending until the separate scene/receipt path completes.
   if(!pass)Endgame.Apply(EndgameResponse.Parse("{\"kind\":\"rift.advance\",\"requestId\":\""+req.RequestID+"\",\"ok\":"+(pass?"true":"false")+",\"error\":\""+(pass?"":"exit")+"\"}"));return true;}
  public bool RetryEndgame()=>false;public bool EquipEndgameItem(EndgameItemView x)=>false;public bool UnequipEndgameItem(EndgameItemView x)=>false;
 }
 static void Apply(Client c,string j)=>c.Endgame.Apply(EndgameResponse.Parse(j));
 static (Client,PlayerController,GameBootstrap) Setup(){var c=new Client();new EndgamePanel(c);c.Endgame.Begin(new EndgameRequest("hello"));
  Apply(c,"{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.rift-portals.v1\"]}");
  Apply(c,"{\"kind\":\"rift.context\",\"ok\":true,\"result\":{\"inInstance\":\"true\"}}");
  Apply(c,"{\"kind\":\"snapshot\",\"ok\":true,\"revision\":\"2\",\"snapshotId\":\"ready\",\"chunk\":0,\"chunks\":1,\"items\":[]}");
  Apply(c,"{\"kind\":\"rift.state\",\"ok\":true,\"rift\":{\"runId\":\"race\",\"revision\":\"4\",\"phase\":\"active\",\"floorIndex\":1,\"exitX\":200,\"exitY\":300}}");
  var p=new PlayerController{worldX=395,worldY=300};p.StartForIndependentRuntime();p.AdvanceTransferForIndependentRuntime();var g=new GameBootstrap(p,395,300);return(c,p,g);
 }
 static void Main(){
  foreach(bool native in new[]{false,true}){
   var(c,p,g)=Setup();
   if(native){p.transferProcs=new List<MapLink.MapProc>{new MapLink.MapProc{poly=new Vector2[3]}};p.onTransfer=(map,x,y)=>throw new Exception("private gate escaped into public callback");}
   p.worldX=245;g.Tick(.25f);p.AdvanceTransferForIndependentRuntime();
   Check(c.Attempts==1&&c.Accepts==1,"fresh physical "+(native?"native":"seeded")+" gate reports position before advance");
   Check(WireLog.Packets.Count==2&&WireLog.Packets[0]=="move(245,300)"&&WireLog.Packets[1]=="rift.advance@server(245,300)","production ordered packet sequence is move then advance");
   for(int i=0;i<100;i++)p.AdvanceTransferForIndependentRuntime();
   Check(c.Attempts==1&&WireLog.Packets.Count==2,"standing inside does not duplicate successful mutation or forced movement");
  }
  {
   var(c,p,g)=Setup();c.Reject=true;p.worldX=245;g.Tick(.25f);p.AdvanceTransferForIndependentRuntime();
   Check(c.Attempts==1&&c.Accepts==0&&c.Endgame.Error=="exit","real authoritative rejection is still honored");
   c.Reject=false;for(int i=0;i<100;i++)p.AdvanceTransferForIndependentRuntime();
   Check(c.Attempts==1,"authoritative rejection does not blindly resubmit while standing inside");
   p.worldX=246;p.AdvanceTransferForIndependentRuntime();p.worldX=245;p.AdvanceTransferForIndependentRuntime();
   Check(c.Attempts==2&&c.Accepts==1,"fresh physical edge re-arms authoritative rejection");
  }
  {
   var(c,p,g)=Setup();p.worldX=394;g.Tick(.06f);g.Tick(.25f);
   Check(WireLog.ServerX==394,"periodic Update reports previous player position and resets timer before player Update");
   CharData.attrs[9]=300;
   float step=p.MovementStepForIndependentRuntime();
   Check(step==150,"actual movement clock permits 150px in a 250ms frame at 600px per second");
   p.worldX-=step;
   GameBootstrap.FlushMoveReport();
   Check(WireLog.ServerX==394,"unchanged ordinary flush would skip same-frame movement because periodic Update reset timer");
   p.AdvanceTransferForIndependentRuntime();
   Check(c.Attempts==1&&c.Accepts==1&&WireLog.ServerX==244,"portal force publishes the legal same-frame movement before advance");
   CharData.attrs[9]=0;
  }
  {
   var(c,p,g)=Setup();p.worldX=394;g.Tick(.01f);GameBootstrap.FlushMoveReport();
   Check(WireLog.Packets.Count==0,"ordinary skill flush keeps original 60ms throttle");
   g.Tick(.06f);GameBootstrap.FlushMoveReport();
   Check(WireLog.Packets.Count==0,"ordinary skill flush keeps original 24px distance threshold");
   p.worldX=370;GameBootstrap.FlushMoveReport();
   Check(WireLog.Packets.Count==1&&WireLog.ServerX==370,"ordinary skill flush still sends after both original thresholds pass");
   p.worldX=369;GameBootstrap.FlushMoveReport(true);
   Check(WireLog.Packets.Count==2&&WireLog.ServerX==369,"portal force bypasses only the local distance and time throttles");
  }
  Console.WriteLine("PASS "+checks+" portal position ordering contracts; production trigger/reporting/state, deterministic transport and server 160px geometry, no Unity Player.");
 }
}
