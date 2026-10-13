using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

class EndgameModuleContract
{
    static int count;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); count++; }
    sealed class Client : IEndgameClient
    {
        public EndgameClientState Endgame { get; } = new EndgameClientState();
        public bool EndgameInWorld { get; set; } = true;
        public List<EndgameRequest> Sent = new List<EndgameRequest>(); public int Retries;
        public bool SendEndgame(EndgameRequest request) { if (!Endgame.Begin(request)) return false; Sent.Add(request); return true; }
        public bool RetryEndgame() { Retries++; return Endgame.Pending; }
        public bool EquipEndgameItem(EndgameItemView item) => false;
        public bool UnequipEndgameItem(EndgameItemView item) => false;
    }
    static object Get(object panel,string key) => panel.GetType().GetField(key,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(panel);
    static void Set(object panel,string key,object value) => panel.GetType().GetField(key,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(panel,value);
    static object Call(object panel,string key,params object[] args) => panel.GetType().GetMethod(key,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(panel,args);
    static void Draw(object panel,string click=null) { Event.current = new Event { type = EventType.Repaint }; ArtworkSkin.click=click; ArtworkSkin.enableds.Clear();GUI.labels.Clear(); Call(panel,"OnGUI"); }
    static void Click(object panel,float x,float y,int button=0) { Event.current = new Event { type = EventType.MouseDown, button=button, mousePosition=new Vector2(x,y) }; Call(panel,"DrawCube"); }
    static void Apply(Client c,string json) => c.Endgame.Apply(EndgameResponse.Parse(json));
    static void Snapshot(Client c,string id) => Apply(c,"{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\""+id+"\",\"chunks\":1,\"revision\":\"1\",\"materials\":[{\"itemId\":1900028201,\"name\":\"星髓\",\"count\":100}]}");
    static IList Slots(object panel) => (IList)Get(panel,"cubeSlots");
    static int Value(object slot,string name) => (int)slot.GetType().GetField(name).GetValue(slot);
    static void Quote(Client c,EndgameRequest request,string id="q",string expiry="2099-01-01T00:00:00Z") => Apply(c,"{\"kind\":\"cube.quote\",\"ok\":true,\"requestId\":\""+request.RequestID+"\",\"quote\":{\"quoteId\":\""+id+"\",\"revision\":\"1\",\"expiresAt\":\""+expiry+"\",\"description\":\"服务器结果与概率\",\"money\":\"5000\"}}");
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
        Check(WinZ.lastFrame.width==450&&WinZ.lastFrame.height==484,"native furnace dimensions without landscape dashboard");
        Check(WinDrag.calls>0,"native title drag path retained");
        Screen.width=360;Screen.height=640;Draw(p);Check(WinZ.lastFrame.width<=340&&WinZ.lastFrame.height<=620,"portrait scale fits small viewport");Screen.width=1280;Screen.height=900;Draw(p);
        Check(InventoryDlg.IsOpen,"uses the original inventory for inputs");
        Check(!c.Sent.Exists(r=>r.Catalog=="recipes"||r.Catalog=="formations"||r.Catalog=="uniques"),"container never loads recipe or discovery lists");
        Check(!ArtworkSkin.enableds.ContainsKey("炼化方式")&&!ArtworkSkin.enableds.ContainsKey("选择成品星篆"),"no operation or output recipe selection");
        Call(p,"PutCubeItem","",1900028201,"星髓");Check(Slots(p).Count==1&&c.Endgame.Materials[0].Count==100,"put is a visual reservation only");
        Call(p,"PutCubeItem","",1900028201,"星髓");Check(Slots(p).Count==1,"duplicate input cannot be added");
        Set(p,"cubeQuantity","101");Draw(p);int sent=c.Sent.Count;Click(p,130,455);Check(!ArtworkSkin.enableds["确认数量"]&&c.Sent.Count==sent,"quantity above bag count cannot confirm or submit");
        Set(p,"cubeQuantity","30");Draw(p);Click(p,130,455);Check(c.Sent.Count==sent,"unconfirmed quantity cannot match");
        Draw(p,"确认数量");Click(p,130,455);var quote=c.Sent[c.Sent.Count-1];
        Check(quote.Json.Contains("cubeInputs")&&quote.Json.Contains("\"count\":30")&&!quote.Json.Contains("recipeId"),"only placed quantities identify matching request");
        Click(p,130,455);Check(ReferenceEquals(c.Sent[c.Sent.Count-1],quote),"repeated match disabled");
        Apply(c,"{\"kind\":\"cube.quote\",\"ok\":false,\"requestId\":\""+quote.RequestID+"\",\"error\":\"no_match\",\"message\":\"没有反应\"}");Draw(p);
        Check(Slots(p).Count==1&&c.Endgame.Materials[0].Count==100&&!SysMsgBox.Visible,"no match preserves inventory and inputs");
        Click(p,285,455);Check(Slots(p).Count==0&&c.Endgame.Materials[0].Count==100,"native cancel releases reservation without return packets");
        Check(EndgamePanel.TryOpenCubeFromNpc(506,77,"Nzhoutianding",()=>current),"reopen succeeds");Draw(p);
        InventoryDlg.rows[0]=new InventoryDlg.Item{id=1900028201,bagIdx=0,name="星髓"};InventoryDlg.HasHeld=true;InventoryDlg.HeldTab=1;InventoryDlg.HeldBagSlot=0;
        Click(p,300,355);Check(Slots(p).Count==1&&Value(Slots(p)[0],"Cell")==34&&!InventoryDlg.HasHeld,"native drag fills actual last grid cell and clears only held cursor");
        Click(p,300,355,1);Check(Slots(p).Count==0&&c.Endgame.Materials[0].Count==100,"native right click returns reservation without consuming");
        InventoryDlg.locked.Add(0);Check(!EndgamePanel.TryPutCubeBagItem(1,0),"locked native input rejected");InventoryDlg.locked.Clear();
        Check(EndgamePanel.TryPutCubeBagItem(1,0)&&Slots(p).Count==1,"native inventory right-click entry selects material");
        SysMsgBox.Visible=true;Check(!EndgamePanel.TryPutCubeBagItem(1,0),"native confirmation blocks inventory-side reservation");sent=c.Sent.Count;Click(p,130,455);Click(p,125,90,1);Click(p,285,455);
        Check(c.Sent.Count==sent&&Slots(p).Count==1&&(bool)Get(p,"open"),"native modal blocks submit remove and cancel");SysMsgBox.Visible=false;
        Click(p,130,455);quote=c.Sent[c.Sent.Count-1];Quote(c,quote);Draw(p);
        Check(SysMsgBox.Visible&&SysMsgBox.text.Contains("服务器结果与概率")&&SysMsgBox.text.Contains("5000"),"full server preview and price in native confirmation");
        SysMsgBox.Finish(false);Check(!c.Endgame.Pending&&Slots(p).Count==1,"cancel confirmation keeps reservations");
        Click(p,130,455);Check(SysMsgBox.Visible,"same fresh quote can be reviewed again");current=false;Call(p,"Update");SysMsgBox.Finish(true);
        Check(!c.Endgame.Pending&&Slots(p).Count==0,"NPC range loss rejects captured confirmation");
        current=true;EndgamePanel.TryOpenCubeFromNpc(506,77,"Nzhoutianding",()=>current);Draw(p);Call(p,"PutCubeItem","",1900028201,"星髓");Click(p,130,455);quote=c.Sent[c.Sent.Count-1];Quote(c,quote);Draw(p);
        Snapshot(c,"two");Draw(p);SysMsgBox.Finish(true);Check(!c.Endgame.Pending&&Slots(p).Count==0,"new authoritative inventory invalidates pending confirmation");
        Call(p,"PutCubeItem","",1900028201,"星髓");Click(p,130,455);quote=c.Sent[c.Sent.Count-1];Quote(c,quote,"expired","2000-01-01T00:00:00Z");Draw(p);Check(!SysMsgBox.Visible&&!c.Endgame.Pending,"expired quote cannot open commit confirmation");
        Click(p,130,455);quote=c.Sent[c.Sent.Count-1];Quote(c,quote);Draw(p);SysMsgBox.Finish(true);var execute=c.Sent[c.Sent.Count-1];
        Check(execute.Op=="cube.execute"&&execute.Json.Contains("npcEntityId")&&!execute.Json.Contains("cubeInputs"),"native confirmation commits only captured authoritative quote");
        sent=c.Sent.Count;Click(p,130,455);Click(p,300,355,1);Check(c.Sent.Count==sent&&Slots(p).Count==1,"in-flight execution blocks repeated commit and edits");
        Click(p,125,425);Check(c.Retries==1&&ReferenceEquals(c.Endgame.PendingRequest,execute),"query retries original uncertain transaction");
        Click(p,350,49);Check(Slots(p).Count==0&&ReferenceEquals(c.Endgame.PendingRequest,execute),"close preserves in-flight request identity");
        Apply(c,"{\"kind\":\"cube.execute\",\"ok\":false,\"requestId\":\""+execute.RequestID+"\",\"error\":\"requirements\",\"message\":\"材料已变化\"}");
        EndgamePanel.TryOpenCubeFromNpc(506,77,"Nzhoutianding",()=>current);Draw(p);Check(Slots(p).Count==0&&!c.Endgame.Pending,"rejected transaction safely reopens empty container");
        var mats=new List<string>();for(int i=0;i<36;i++)mats.Add("{\"itemId\":"+(1000+i)+",\"name\":\"材料\",\"count\":99}");
        Apply(c,"{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"capacity\",\"chunks\":1,\"revision\":\"1\",\"materials\":["+string.Join(",",mats)+"]}");Draw(p);
        for(int i=0;i<36;i++)Call(p,"PutCubeItem","",1000+i,"材料");Check(Slots(p).Count==35&&Value(Slots(p)[34],"Cell")==34,"all 35 displayed cells usable; 36th denied");
        var inputs=(IReadOnlyList<EndgameCubeInput>)Call(p,"CubeInputs");Check(new EndgameRequest("cube.quote","1",cubeInputs:inputs).Json.Contains("1034"),"full container serializes in one bounded request");
        var over=new List<EndgameCubeInput>(inputs);over.Add(new EndgameCubeInput("",1035,1));bool rejected=false;try{new EndgameRequest("cube.quote","1",cubeInputs:over);}catch(ArgumentException){rejected=true;}Check(rejected,"wire rejects 36 inputs");
        rejected=false;try{new EndgameRequest("cube.quote","1",cubeInputs:new[]{new EndgameCubeInput("",123,1),new EndgameCubeInput("",123,1)});}catch(ArgumentException){rejected=true;}Check(rejected,"wire rejects duplicate identities");
        Input.pressed=KeyCode.Escape;Call(p,"Update");Input.pressed=null;Check(!(bool)Get(p,"open")&&Slots(p).Count==0,"Escape releases all reservations");
        EndgamePanel.TryOpenCubeFromNpc(506,77,"Nzhoutianding",()=>current);Draw(p);Call(p,"PutCubeItem","",1000,"材料");c.Endgame.Reset();Call(p,"Update");Check(!(bool)Get(p,"open")&&Slots(p).Count==0,"reconnect clears selection without duplication");
        Console.WriteLine("PASS "+count+" native cube grid/module contracts.");
    }
}
