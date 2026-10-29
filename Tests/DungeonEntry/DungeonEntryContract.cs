using System;
using System.Collections.Generic;
using System.Reflection;
using FakeFantasyOnline.Application;
namespace UnityEngine {
 public struct Color { public Color(float r,float g,float b,float a){} }
 public static class JsonUtility { public static T FromJson<T>(string text) {return (T)Next;} public static object Next; }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 public static class GameTables {public static string Text="fixture";public static string LoadText(string name)=>Text;}
 public static class SocialClient {public static List<int> Party=new List<int>();}
 public static class GameBootstrap {public static int CurMapId=7;public static Action<int,float,float> DoTeleport;}
 public static class ChatUI {public static List<string> Messages=new List<string>();public static void Add(string c,string text,UnityEngine.Color color){Messages.Add(text);}}
 public class NetClient {public static NetClient I=new NetClient();public List<int> Opens=new List<int>();public List<(int list,int index)> Choices=new List<(int,int)>();public void OpenTeleportForIndependentRuntime(int list)=>Opens.Add(list);public void TeleportForIndependentRuntime(int list,int index)=>Choices.Add((list,index));}
 public sealed class ReferenceNpcDialogOption {public string Label;public Action Action;public ReferenceNpcDialogOption(string label,Action action){Label=label;Action=action;}}
 public sealed partial class DialogUI {
  public static DialogUI I;
  internal void ObserveTaskCompletionReceipt() {}
  string NpcName;int curTrans;bool Visible=true;bool InSubView;List<ReferenceNpcDialogOption> options=new List<ReferenceNpcDialogOption>();int transportCalls,builds;
  void Add(ReferenceNpcDialogOption o)=>options.Add(o);
  void ShowTransferMenu(){Visible=false;transportCalls++;}
  void BuildMenu(){builds++;options.Clear();BuildTravelRows();}
  static int checks;static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
  static void SetTable(params DungeonEntrances.Entry[] entries){
   var root=Activator.CreateInstance(typeof(DungeonEntrances).GetNestedType("Root",BindingFlags.NonPublic));
   root.GetType().GetField("entries").SetValue(root,entries);UnityEngine.JsonUtility.Next=root;GameTables.Text="fixture";
   typeof(DungeonEntrances).GetField("byNpc",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,null);
  }
  static DialogUI Open(string npc,int list,DialogUI existing=null){var d=existing??new DialogUI();I=d;d.NpcName=npc;d.curTrans=list;d.InSubView=false;d.ResetDungeonTransport();d.BuildMenu();d.Visible=true;d.BeginDungeonTransport();return d;}
  static WorldTransportState Reply(int list,params WorldTransportDestinationState[] rows)=>new WorldTransportState(list,rows);
  static WorldTransportDestinationState Row(string name,int map,int price=0)=>new WorldTransportDestinationState(name,map,100,200,price);
  static void Main(){
   TaskClient.SetState(new List<TaskClient.ActiveTask>{new TaskClient.ActiveTask{id=355},new TaskClient.ActiveTask{id=356},new TaskClient.ActiveTask{id=359},new TaskClient.ActiveTask{id=361}},new List<int>());
   SetTable(new DungeonEntrances.Entry{npc="无服务表入口",dungeons=new[]{new DungeonEntrances.Dungeon{name="甲副本",map=20100,x=10,y=20},new DungeonEntrances.Dungeon{name="非任务副本",map=29201,x=30,y=40}}});
   { const string state="authoritative server rows";
    var d=Open("仙人元藏几",110);Check(d.options.Count==1&&d.options[0].Label=="读取副本入口",state+": no invented optimistic rows or generic transport");
    Check(ConsumeDungeonTransport(Reply(110,Row("九天蛰眠之龙（普通）",24400),Row("试炼神将（普通）",26800),Row("九天蛰眠之龙（困难）",24400),Row("试炼神将（困难）",26800))),"native server reply consumed");
    Check(d.options.Count==4&&d.options[3].Label=="进入试炼神将（困难）",state+": complete server order/difficulty retained");
    var choice=d.options[3];int before=NetClient.I.Choices.Count;choice.Action();choice.Action();Check(NetClient.I.Choices.Count==before+1&&NetClient.I.Choices[before]==(110,3)&&!d.Visible,"one native list/index choice, no double click");
   }
   var normal=Open("飞空艇仙子",17);Check(normal.options.Count==1&&normal.options[0].Label=="传送","ordinary transport retained");Check(!ConsumeDungeonTransport(Reply(17,Row("龙城",7))),"ordinary reply stays TransportUI");normal.options[0].Action();Check(normal.transportCalls==1,"ordinary callback retained");
   var excluded=Open("天神考验使者",114);Check(excluded.options[0].Label=="传送"&&!ConsumeDungeonTransport(Reply(114,Row("天魔战场",34200))),"conflicting explicitly-excluded map is not silently enabled");
   var d2=Open("旧NPC",107);Open("新NPC",108,d2);ConsumeDungeonTransport(Reply(107,Row("旧副本",20801)));Check(d2.options.Count==1&&d2.options[0].Label=="读取副本入口","late old NPC reply discarded");
   ConsumeDungeonTransport(Reply(108,Row("失落神殿",20901)));Check(d2.options[0].Label=="进入失落神殿","new NPC reply accepted");
   var stale=d2.options[0];int sends=NetClient.I.Choices.Count;Open("同NPC重开",108,d2);stale.Action();Check(NetClient.I.Choices.Count==sends,"old delegate generation rejected after re-open");
   d2.Visible=false;int builds=d2.builds;ConsumeDungeonTransport(Reply(108,Row("迟到",20901)));Check(!d2.Visible&&d2.builds==builds,"close does not resurrect generic or native dialog");
   Open("新NPC",108,d2);d2.InSubView=true;builds=d2.builds;ConsumeDungeonTransport(Reply(108,Row("失落神殿",20901)));Check(d2.InSubView&&d2.builds==builds,"task subview not interrupted");d2.InSubView=false;d2.BuildMenu();Check(d2.options[0].Label=="进入失落神殿","return uses received rows");
   GameBootstrap.CurMapId=9;sends=NetClient.I.Choices.Count;d2.options[0].Action();ConsumeDungeonTransport(Reply(108,Row("跨图旧回包",20901)));Check(NetClient.I.Choices.Count==sends&&d2.options[0].Label=="进入失落神殿","map transition invalidates click and reply");GameBootstrap.CurMapId=7;
   var exit=Open("古代石碑",133);ConsumeDungeonTransport(Reply(133,Row("离开失落神殿",81)));Check(exit.options[0].Label=="离开失落神殿","exit remains exit, not enter");
   var priced=Open("入口",116);ConsumeDungeonTransport(Reply(116,Row("费用",36700,100)));Check(priced.options[0].Label=="进入费用（100铜）","price displayed unchanged, no local affordability rule");
   int teleports=0;GameBootstrap.DoTeleport=(m,x,y)=>teleports++;SocialClient.Party.Clear();var solo=Open("仙人刘丹",111);ConsumeDungeonTransport(Reply(111,Row("月牙塔",29900)));sends=NetClient.I.Choices.Count;solo.options[0].Action();Check(NetClient.I.Choices.Count==sends+1&&teleports==0,"server-owned solo route, no new client party gate or local map switch");
   ChatUI.Messages.Clear();var unbound=Open("无服务表入口",0);Check(unbound.options.Count==2,"original table-only entries retained");unbound.options[1].Action();Check(teleports==0&&ChatUI.Messages.Count==1,"unchanged table-only party gate");SocialClient.Party.Add(1);Open("无服务表入口",0,unbound);unbound.options[1].Action();Check(teleports==1,"unchanged table-only callback");
   I=null;Check(ConsumeDungeonTransport(Reply(108,Row("失落神殿",20901))),"closed owner still swallows late dungeon response");
   for(int list=101;list<=133;list++)Check(IsNativeDungeonTransportList(list)==(list!=114),"bounded audited SQL list set");Check(!IsNativeDungeonTransportList(100)&&!IsNativeDungeonTransportList(134),"no inferred future list classification");
   foreach(var pair in new[]{(map:29200,quest:355),(map:29400,quest:356),(map:29900,quest:359),(map:30000,quest:361)}) {
    SetTable(new DungeonEntrances.Entry{npc="纯任务入口",dungeons=new[]{new DungeonEntrances.Dungeon{name="任务副本",map=pair.map,x=30,y=40}}});
    foreach(int state in new[]{-1,0,1,2}) {
     I=null;TaskClient.SetState(state==0||state==1?new List<TaskClient.ActiveTask>{new TaskClient.ActiveTask{id=pair.quest,ready=(byte)state}}:new List<TaskClient.ActiveTask>(),state==2?new List<int>{pair.quest}:new List<int>());
     bool permitted=state==0||state==1;
     Check(DungeonQuestAccess.CanEnter(pair.map)==permitted,"exact solo task state policy");
     var native=Open("纯任务入口",0);Check(native.options.Count==(permitted?1:0),"table entry hidden unless unfinished");
     if(permitted) {SocialClient.Party.Clear();int count=teleports;native.options[0].Action();Check(teleports==count+1,"solo task native entry does not demand a party");}
     var mixed=Open("mixed authority",110);var reply=Reply(110,Row("任务",pair.map),Row("普通",24400),Row("任务2",pair.map),Row("普通（困难）",24400));ConsumeDungeonTransport(reply);
     Check(mixed.options.Count==(permitted?4:2),"only task-only rows filtered");
     int last=mixed.options.Count-1,countSent=NetClient.I.Choices.Count;mixed.options[last].Action();Check(NetClient.I.Choices.Count==countSent+1&&NetClient.I.Choices[countSent]==(110,3),"hidden rows never renumber hard wire index");
    }
    I=null;TaskClient.SetState(new List<TaskClient.ActiveTask>{new TaskClient.ActiveTask{id=pair.quest}},new List<int>());
    var current=Open("task dialogue",111);ConsumeDungeonTransport(Reply(111,Row("任务",pair.map)));var old=current.options[0];
    TaskClient.SetState(new List<TaskClient.ActiveTask>(),new List<int>{pair.quest});int writes=NetClient.I.Choices.Count;old.Action();Check(current.options.Count==0&&NetClient.I.Choices.Count==writes,"completion receipt hides and invalidates captured task choice");
    TaskClient.SetState(new List<TaskClient.ActiveTask>{new TaskClient.ActiveTask{id=pair.quest,ready=1}},new List<int>());Check(current.options.Count==1,"legitimate unfinished reaccept receipt restores option");
    old=current.options[0];TaskClient.SetState(new List<TaskClient.ActiveTask>(),new List<int>());old.Action();Check(current.options.Count==0&&NetClient.I.Choices.Count==writes,"abandon hides and refuses stale click");
    TaskClient.SetState(new List<TaskClient.ActiveTask>{new TaskClient.ActiveTask{id=pair.quest}},new List<int>());current.InSubView=true;builds=current.builds;
    TaskClient.SetState(new List<TaskClient.ActiveTask>(),new List<int>{pair.quest});Check(current.InSubView&&current.builds==builds,"task receipt does not interrupt a task detail subview");current.InSubView=false;current.BuildMenu();Check(current.options.Count==0,"back to menu reflects completed history");
   }
   I=null;TaskClient.SetState(new List<TaskClient.ActiveTask>{new TaskClient.ActiveTask{id=355},new TaskClient.ActiveTask{id=356},new TaskClient.ActiveTask{id=359},new TaskClient.ActiveTask{id=361}},new List<int>());
   Check(DungeonQuestAccess.RequiredQuest(29200)==355&&DungeonQuestAccess.CanEnter(29200),"retained unfinished 355 record admits without restoring disabled definition");
   int catalogRows=0, hardRows=0;
   foreach(var reply in AuthoritativeRoutes()) {
    Check(reply.TransportList>=101&&reply.TransportList<=133,"fixture excludes ordinary transport lists");
    catalogRows+=reply.Destinations.Count;
    for(int i=0;i<reply.Destinations.Count;i++) {
     if(reply.Destinations[i].Name.EndsWith("（困难）",StringComparison.Ordinal)) hardRows++;
     var menu=Open("audited SQL owner",reply.TransportList);
     if(reply.TransportList==114) {Check(!ConsumeDungeonTransport(reply)&&menu.options[0].Label=="传送","114 remains the explicit review exception");continue;}
     Check(ConsumeDungeonTransport(reply)&&menu.options.Count==reply.Destinations.Count,"complete authoritative list, without dropping difficulty rows");
     Check(menu.options[i].Label==(reply.TransportList==133?"":"进入")+reply.Destinations[i].Name,"all authoritative names and order preserved");
     int count=NetClient.I.Choices.Count;menu.options[i].Action();
     Check(NetClient.I.Choices.Count==count+1&&NetClient.I.Choices[count]==(reply.TransportList,i),"every audited row sends exact original index");
    }
   }
   Check(catalogRows==71&&hardRows==17,"54 SQL destinations plus17 hard variants, not ordinary list1");
   Console.WriteLine("PASS dungeon entry presentation: "+checks+" checks; actual owner/lookup/state classes, managed fixtures, not Unity rendering or live-server validation.");
  }
 }
}
