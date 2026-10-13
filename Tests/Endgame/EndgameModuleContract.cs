using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

class EndgameModuleContract
{
    static int count;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); count++; }
    sealed class Client : IEndgameClient
    {
        public EndgameClientState Endgame { get; } = new EndgameClientState();
        public bool EndgameInWorld { get; set; } = true;
        public List<EndgameRequest> Sent = new List<EndgameRequest>();
        public bool SendEndgame(EndgameRequest request) { if (!Endgame.Begin(request)) return false; Sent.Add(request); return true; }
        public bool RetryEndgame() => Endgame.Pending;
        public bool EquipEndgameItem(EndgameItemView item) => false;
        public bool UnequipEndgameItem(EndgameItemView item) => false;
    }
    static object Get(object panel,string key) => panel.GetType().GetField(key,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(panel);
    static void Set(object panel,string key,object value) => panel.GetType().GetField(key,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(panel,value);
    static void Call(object panel,string key,params object[] args) => panel.GetType().GetMethod(key,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(panel,args);
    static void Draw(object panel,string click=null) { ArtworkSkin.click=click; ArtworkSkin.enableds.Clear();UnityEngine.GUI.labels.Clear(); Call(panel,"OnGUI"); }
    static void Apply(Client c,string json) => c.Endgame.Apply(EndgameResponse.Parse(json));
    static void Snapshot(Client c,string id) => Apply(c,"{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\""+id+"\",\"chunks\":1,\"revision\":\"1\",\"materials\":[{\"itemId\":1900028201,\"name\":\"星髓\",\"count\":100}]}");
    static int Slots(object panel) => ((ICollection)Get(panel,"cubeSlots")).Count;
    static void Main()
    {
        var c=new Client();var p=new EndgamePanel();p.Bind(c);GameBootstrap.CurMapId=506;
        c.Endgame.Begin(new EndgameRequest("hello"));Apply(c,"{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");
        Check(!EndgamePanel.TryOpenCubeFromWorld(),"no arbitrary global cube entry");
        Check(!EndgamePanel.TryOpenCubeFromNpc(506,77,"Nzhoutianding",()=>true),"older server does not open new input protocol");
        c.Endgame.Begin(new EndgameRequest("hello"));Apply(c,"{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.cube-inputs.v1\"]}");
        Check(!EndgamePanel.TryOpenCubeFromNpc(506,77,"N611FY",()=>true),"artisan cannot impersonate dedicated cube");
        Check(!EndgamePanel.TryOpenCubeFromNpc(7,77,"Nzhoutianding",()=>true),"stale map cannot open cube");
        bool current=true;Check(EndgamePanel.TryOpenCubeFromNpc(506,77,"Nzhoutianding",()=>current),"dedicated live cube opens");Snapshot(c,"one");Draw(p);
        Check(!c.Sent.Exists(r=>r.Catalog=="recipes"||r.Catalog=="formations"||r.Catalog=="uniques"),"container never loads recipe or discovery lists");
        Check(!ArtworkSkin.enableds.ContainsKey("炼化方式")&&!ArtworkSkin.enableds.ContainsKey("选择成品星篆"),"no operation or output recipe selection");
        Call(p,"PutCubeItem","",1900028201,"星髓");Check(Slots(p)==1&&c.Endgame.Materials[0].Count==100,"put is a visual reservation only");
        Call(p,"PutCubeItem","",1900028201,"星髓");Check(Slots(p)==1,"duplicate input cannot be added");
        Set(p,"cubeQuantity","101");Draw(p);Check(!ArtworkSkin.enableds["确认数量"]&&!ArtworkSkin.enableds["问鼎"],"quantity above observed bag count cannot be confirmed or submitted");
        Set(p,"cubeQuantity","30");Draw(p);Check(!ArtworkSkin.enableds["问鼎"],"unconfirmed quantity cannot match");Draw(p,"确认数量");Draw(p,"问鼎");var quote=c.Sent[c.Sent.Count-1];
        Check(quote.Json.Contains("cubeInputs")&&quote.Json.Contains("\"count\":30")&&!quote.Json.Contains("recipeId"),"only placed quantities identify matching request");
        Draw(p,"问鼎");Check(ReferenceEquals(c.Sent[c.Sent.Count-1],quote),"repeated match disabled");
        Apply(c,"{\"kind\":\"cube.quote\",\"ok\":false,\"requestId\":\""+quote.RequestID+"\",\"error\":\"no_match\",\"message\":\"没有反应\"}");Draw(p);Check(Slots(p)==1&&c.Endgame.Materials[0].Count==100&&!ArtworkSkin.enableds["炼化"],"no match preserves inventory and inputs");
        Draw(p,"×");Check(Slots(p)==0&&c.Endgame.Materials[0].Count==100,"cancel releases reservation without return packets");
        Check(EndgamePanel.TryOpenCubeFromNpc(506,77,"Nzhoutianding",()=>current),"reopen succeeds");Draw(p);Call(p,"PutCubeItem","",1900028201,"星髓");Snapshot(c,"two");Draw(p);Check(Slots(p)==0,"new authoritative inventory invalidates input reservations");
        Call(p,"PutCubeItem","",1900028201,"星髓");current=false;Call(p,"Update");Check(!(bool)Get(p,"open")&&Slots(p)==0,"lost NPC authority closes container");
        current=true;EndgamePanel.TryOpenCubeFromNpc(506,77,"Nzhoutianding",()=>current);Draw(p);Call(p,"PutCubeItem","",1900028201,"星髓");c.Endgame.Reset();Call(p,"Update");Check(!(bool)Get(p,"open")&&Slots(p)==0,"reconnect clears selection without duplication");
        bool rejected=false;try{new EndgameRequest("cube.quote","1",cubeInputs:new[]{new EndgameCubeInput("",123,1),new EndgameCubeInput("",123,1)});}catch(ArgumentException){rejected=true;}Check(rejected,"request rejects duplicate selected material identities");
        Console.WriteLine("PASS "+count+" cube container/module contracts.");
    }
}
