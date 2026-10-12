using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace UnityEngine
{
    public struct Vector2 { public float x, y; public Vector2(float a, float b) { x = a; y = b; } }
    public struct Color { public Color(float r, float g, float b, float a) { } }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public partial class PlayerController
    {
        public float worldX, worldY, followLostT;
        public int followId; public bool followLastOk;
        private void LoadMask() { }
    }
    public static class MapLink
    {
        public class MapProc { public Vector2[] poly; public string cmd; public int mapTo; public float toX, toY; }
        public static bool PointInPoly(Vector2 p, Vector2[] poly) => p.x >= 10 && p.x <= 20 && p.y >= 10 && p.y <= 20;
    }
    public static class SocialClient { public static List<(int,string,int,int,int,int,int,int,bool)> Party = new List<(int,string,int,int,int,int,int,int,bool)> { default }; }
    public class NetClient { public static NetClient I = new NetClient(); public int Commands; public void GmCmdForIndependentRuntime(string cmd) { Commands++; } }
    public static class ChatUI { public static void Add(string channel,string text,Color color,string sender) { } }
    public static class DifficultyBox { public static int Shown; public static void Show(string t,string n,string h,Action a,Action b) { Shown++; } }
    public sealed partial class EndgamePanel
    {
        internal static EndgamePanel Current;
        private IEndgameClient client;
        private EndgameClientState State => client?.Endgame;
        internal static bool WorldEntryAvailable => Current?.client != null && Current.client.EndgameInWorld && Current.State.Negotiated;
        private bool Send(EndgameRequest request) => client.SendEndgame(request);
        public EndgamePanel(IEndgameClient c) { client = c; Current = this; }
    }
}
class EndgameRiftPortalContract
{
    static int checks;
    static void Check(bool value,string name) { if (!value) throw new Exception(name); checks++; }
    sealed class Client : IEndgameClient
    {
        public EndgameClientState Endgame { get; } = new EndgameClientState();
        public bool EndgameInWorld { get; set; } = true;
        public List<EndgameRequest> Sent = new List<EndgameRequest>();
        public bool SendEndgame(EndgameRequest req) { if (!Endgame.Begin(req)) return false; Sent.Add(req); return true; }
        public bool RetryEndgame() => Endgame.Pending;
        public bool EquipEndgameItem(EndgameItemView item) => false;
        public bool UnequipEndgameItem(EndgameItemView item) => false;
    }
    static void Apply(Client c,string json) => c.Endgame.Apply(EndgameResponse.Parse(json));
    static void Context(Client c,bool inside) => Apply(c,"{\"kind\":\"rift.context\",\"ok\":true,\"result\":{\"inInstance\":\""+(inside?"true":"false")+"\"}}");
    static void Hello(Client c,bool supported=true) { c.Endgame.Begin(new EndgameRequest("hello")); Apply(c,"{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\""+(supported?",\"endgame.rift-portals.v1\"":"")+"]}"); }
    static void Rift(Client c,string phase="active",int floor=3,bool transition=false) => Apply(c,"{\"kind\":\"rift.state\",\"ok\":true,\"rift\":{\"runId\":\"portal-run\",\"revision\":\"4\",\"phase\":\""+phase+"\",\"tier\":7,\"floorIndex\":"+floor+",\"transitioning\":"+(transition?"true":"false")+"}}");
    static Client New() { var c=new Client(); new EndgamePanel(c); return c; }
    static PlayerController Player(Action loaded,string cmd=null) { var p=new PlayerController { transferProcs=new List<MapLink.MapProc>{new MapLink.MapProc{poly=new Vector2[3],mapTo=999,cmd=cmd}},onTransfer=(m,x,y)=>loaded() };p.StartForIndependentRuntime();return p; }
    static bool Enter(PlayerController p) {p.worldX=p.worldY=15;return p.AdvanceTransferForIndependentRuntime();}
    static void Leave(PlayerController p) {p.worldX=p.worldY=0;p.AdvanceTransferForIndependentRuntime();}
    static void Main()
    {
        var c=New();int loads=0;var p=Player(()=>loads++);
        Check(Enter(p)&&loads==0&&c.Sent.Count==0,"unknown reconnect context never speculates a public load");
        Context(c,true);Check(!c.Endgame.RiftInInstance,"unnegotiated context cannot grant ownership");
        Hello(c);Check(c.Endgame.RiftPortalsV1&&!c.Endgame.RiftContextKnown,"new capability waits for context");
        Context(c,true);Check(c.Endgame.RiftInInstance,"context can arrive before run projection");
        Leave(p);Enter(p);Check(loads==0&&c.Sent.Count==0,"missing run state holds portal closed");
        Apply(c,"{\"kind\":\"snapshot\",\"ok\":true,\"revision\":\"2\",\"snapshotId\":\"s\",\"chunk\":0,\"chunks\":1,\"items\":[]}");Rift(c);
        Leave(p);Enter(p);Check(loads==0&&c.Sent.Count==1&&c.Sent[0].Op=="rift.advance","native portal directly requests next floor");
        var req=c.Sent[0];Check(req.Json.Contains("\"runId\":\"portal-run\"")&&req.Json.Contains("\"floorIndex\":3")&&req.Json.Contains("\"expectedRevision\":\"2\"")&&!req.Json.Contains("tier"),"identity and inventory revision are frozen without changing difficulty");
        for(int i=0;i<20;i++) Enter(p);
        Leave(p);Enter(p);Check(c.Sent.Count==1&&!p.TransferredForIndependentRuntime,"inside and repeated edges never duplicate pending transition");
        Apply(c,"{\"kind\":\"rift.advance\",\"ok\":false,\"requestId\":\""+req.RequestID+"\",\"error\":\"exit\"}");
        Leave(p);Enter(p);Check(c.Sent.Count==2&&loads==0,"authoritative rejection re-arms after leaving gate");
        req=c.Sent[1];Apply(c,"{\"kind\":\"rift.advance\",\"ok\":true,\"requestId\":\""+req.RequestID+"\"}");
        Rift(c,"active",4,true);Leave(p);Enter(p);Check(c.Sent.Count==2&&loads==0,"transition state never runs ordinary callback");
        foreach(string phase in new[]{"boss","completed","failed"}){Rift(c,phase,4);Leave(p);Enter(p);Check(c.Sent.Count==2&&loads==0,"terminal/guardian context requires explicit exit: "+phase);}
        Rift(c,"active",4);p=Player(()=>loads++,"taoyu");Enter(p);Check(c.Sent.Count==3&&DifficultyBox.Shown==0&&NetClient.I.Commands==0,"native script gate also maps to next floor");
        Context(c,false);Rift(c,"active",3);Check(!c.Endgame.RiftInInstance,"late run view cannot restore departed scene context");
        p=Player(()=>loads++);Enter(p);Check(loads==1,"same scenery in public context uses ordinary map callback");
        p=Player(()=>loads++,"old-script");Enter(p);Check(NetClient.I.Commands==1,"ordinary script gate is unchanged");
        Context(c,true);c.Endgame.Begin(new EndgameRequest("hello"));Apply(c,"{\"kind\":\"hello\",\"ok\":false,\"error\":\"storage\"}");Check(c.Endgame.RiftInInstance&&c.Endgame.RiftPortalsV1,"failed capability refresh cannot clear known private ownership");
        c.Endgame.Reset();Check(!c.Endgame.RiftInInstance&&!c.Endgame.RiftContextKnown&&!c.Endgame.RiftPortalsV1,"disconnect resets context and capability");
        c=New();Hello(c,false);Check(c.Endgame.RiftContextKnown&&!c.Endgame.RiftInInstance,"old sidecar server remains compatible");
        p=Player(()=>loads++);Enter(p);Check(loads==2,"old sidecar server ordinary gate works");
        c=New();c.Endgame.Begin(new EndgameRequest("hello"));Apply(c,"{\"kind\":\"hello\",\"ok\":false,\"error\":\"unavailable\"}");Check(c.Endgame.RiftContextKnown,"explicit unsupported server releases ordinary gates");
        c=New();DateTime now=DateTime.UtcNow;
        for(int i=0;i<3;i++){c.Endgame.Begin(new EndgameRequest("hello"));c.Endgame.Tick(now.AddMinutes(i+1));if(i<2){Check(!c.Endgame.RiftContextKnown,"transient hello timeout stays bounded and closed");c.Endgame.Tick(now.AddMinutes(i+1).AddSeconds(3));}}
        Check(c.Endgame.RiftContextKnown&&!c.Endgame.RiftInInstance,"no-sidecar server final timeout releases only unknown ownership");
        p=Player(()=>loads++);Enter(p);Check(loads==3,"no-sidecar server is not permanently locked out of public gates");
        c=New();Hello(c);Context(c,true);c.Endgame.Begin(new EndgameRequest("hello"));Apply(c,"{\"kind\":\"hello\",\"ok\":false,\"error\":\"storage\"}");
        Check(c.Endgame.RiftInInstance&&EndgamePanel.TryTransferRiftPortalFromWorld(),"transient refresh failure never fails open a confirmed rift");
        Console.WriteLine("PASS "+checks+" production rift portal/context/transfer contracts; deterministic Unity substitutes, no Player run.");
    }
}
