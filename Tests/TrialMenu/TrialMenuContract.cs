using System;
using System.Collections.Generic;
public struct Color { public Color(float r,float g,float b,float a){} }
public static class GameBootstrap { public static int CurMapId; }
public static class TaskClient {
 public class Offer { public int id,state; public string title,desc,award; }
 public static string OfferNpcName;
 public static List<Offer> Offers=new List<Offer>();
}
public class WorldNpcTaskOfferState { public string Title; public WorldNpcTaskOfferState(int id,int state,string title,string desc,string award){Title=title;} }
public enum ReferenceNpcTaskMutationKind { None }
public class ReferenceNpcDialogOption {
 public string Label; public Action Action;
 public ReferenceNpcDialogOption(string label,Action action,bool leave=false){Label=label;Action=action;}
 public ReferenceNpcDialogOption(string label,WorldNpcTaskOfferState row,bool a,ReferenceNpcTaskMutationKind b,Action action){Label=label;Action=action;}
}
public class NetClient { public static NetClient I=new NetClient(); public List<string> Commands=new List<string>(); public void GmCmdForIndependentRuntime(string command){Commands.Add(command);} public void BuyWarehouseForIndependentRuntime(){} }
public static class ShopDlg { public static void Open(int id,bool sell){} }
public static class SocialClient { public static string FamilyName; }
public static class ChatUI { public static void Add(string a,string b,Color c){} }
public static class StoreDlg { public static void Toggle(){} }
public static class SysMsgBox { public static void Show(string text,Action action){} }
public static class Win05 { public static void OpenLifeLearn(){} }
public static class FamilyStashDlg { public static void Open(){} }
public static class PetArousalDlg { public static void Open(){} }
public static class AccountSecDlg { public static void Open(int x){} }
public static class AccountSec { public static bool HasSecCode; }
public static class EndgamePanel { public static bool WorldEntryAvailable=true; }
public class WorldNpcState {}
public partial class DialogUI {
 private WorldNpcState spiritNpc = new WorldNpcState();
 private bool SpiritNpcCurrent(WorldNpcState source) { return GameBootstrap.CurMapId == 7 && IsSpiritMerchant(NpcName) && NpcName != "戒灵仙子"; }
 private void AddSpiritOption(WorldNpcState source, string label, string mode) { Add(new ReferenceNpcDialogOption(label, () => {})); }
 public string NpcName; public bool Visible,InSubView; public int CurrentSellList; private int curTrans;
 private const string LeaveLabel="离开";
 private List<string> optLabels=new List<string>(); private List<Action> optActs=new List<Action>();
 private void Add(ReferenceNpcDialogOption row){optLabels.Add(row.Label);optActs.Add(row.Action);}
 private bool BuildTravelMenu(){return false;}
 private void ShowAscendMenu(){} private void ShowTransferMenu(){} private void AddDungeonEntryOptions(){}
 private void AddHairSalonOptions(){} private void AddActivityOptions(){} private void AddRiftEntranceOption(){}
 private void AddCubeWorkbenchOption(){} private void AddRiftExitOptions(){} private void AddDungeonExitAndHardOptions(){} private void AddTianshenOptions(){}
 private void ShowSay(string s){InSubView=true;} private void Close(){Visible=false;}
 private string PrefixFor(WorldNpcTaskOfferState r){return "任务：";} private void ShowOffer(TaskClient.Offer r){InSubView=true;}
 public void Open(string name){NpcName=name;Visible=true;BuildMenu();}
 public void Refresh(string npc){OnTaskOffers(npc);}
 public int Count(string label){return optLabels.FindAll(s=>s==label).Count;}
 public void Click(string label){optActs[optLabels.IndexOf(label)]();}
 public string Menu(){return string.Join("|",optLabels);}
}
public static class TrialMenuContract {
 static int checks; const string Travel="传送到妖魔结界守卫(5银)";
 static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 public static void Main(){
  foreach(int map in new[]{37,38,61,62}) foreach(int state in new[]{-1,0,1,2,3}) {
   GameBootstrap.CurMapId=map;TaskClient.OfferNpcName="试炼小仙";TaskClient.Offers.Clear();
   if(state>=0)TaskClient.Offers.Add(new TaskClient.Offer{id=1900030001,state=state,title="问鼎周天"});
   var d=new DialogUI();d.Open("试炼小仙");
   Check(d.Count(Travel)==1,"missing or duplicate travel on village "+map+" quest state "+state);
   Check(d.Count("领取试炼任务")==1&&d.Count("交任务·领取试炼奖励")==1,"native task rows changed");
   Check(d.Count("任务：问鼎周天")== (state<0?0:1),"quest row lost");
   d.Refresh("试炼小仙");d.Refresh("试炼小仙");Check(d.Count(Travel)==1,"offer rebuild duplicated or lost travel");
   string menu=d.Menu();d.Refresh("巧匠左慈");Check(menu==d.Menu(),"unrelated reply changed menu");
   if(state>=0){d.Click("任务：问鼎周天");d.Refresh("试炼小仙");Check(d.InSubView,"offer refresh escaped detail");d.Open("试炼小仙");Check(d.Count(Travel)==1,"reopen lost travel");}
   int n=NetClient.I.Commands.Count;d.Click(Travel);Check(!d.Visible&&NetClient.I.Commands.Count==n+1&&NetClient.I.Commands[n]=="trialgo","wrong travel wire intent");
  }
  foreach(int map in new[]{0,7,19,40,41,191}) {GameBootstrap.CurMapId=map;var d=new DialogUI();d.Open("试炼小仙");Check(d.Count(Travel)==0,"unsupported map granted travel "+map);}
  GameBootstrap.CurMapId=37;var other=new DialogUI();other.Open("普通村民");Check(other.Count(Travel)==0,"unrelated NPC gained travel");
  GameBootstrap.CurMapId=7;other.Open("试炼大仙");Check(other.Count(Travel)==0&&other.Count("交任务·领取试炼奖励")==1,"city service changed");
  TaskClient.OfferNpcName="羽灵仙子";TaskClient.Offers.Clear();TaskClient.Offers.Add(new TaskClient.Offer{id=42,state=0,title="羽灵日常"});
  var wing = new DialogUI { CurrentSellList=500 };wing.Open("羽灵仙子");
  Check(wing.Count("出售物品")==0 && wing.Count("购买物品")==1,"wing NPC retains purchase but no sell menu");
  Check(wing.Count("解封")==1 && wing.Count("加工 / 回退")==1 && wing.Count("归元")==1,"wing keeps all three real spirit workbench menu entries");
  Check(wing.Count("任务：羽灵日常")==1,"wing keeps daily task offers");
  wing.Refresh("羽灵仙子");Check(wing.Count("出售物品")==0 && wing.Count("购买物品")==1 && wing.Count("解封")==1,"wing offer refresh keeps services without reintroducing sale");
  foreach(string name in new[]{"鞋灵仙子","拳灵仙子","羽灵仙子","衣灵仙子","项灵仙子","面灵仙子","帽灵仙子","法宝灵仙子","武灵仙子","戒灵仙子"}) {
   TaskClient.OfferNpcName=name;TaskClient.Offers.Clear();TaskClient.Offers.Add(new TaskClient.Offer{id=43,state=0,title="灵器日常"});
   var merchant = new DialogUI { CurrentSellList=500 };merchant.Open(name);
   Check(merchant.Count("出售物品")==0 && merchant.Count("购买物品")==1,"all ten spirit NPCs omit sell but retain purchase: "+name);
   Check(merchant.Count("任务：灵器日常")==1,"all spirit NPC daily offers retained: "+name);
   int expected=name=="戒灵仙子"?0:1;
   Check(merchant.Count("解封")==expected && merchant.Count("加工 / 回退")==expected && merchant.Count("归元")==expected,"nine processing routes preserved, ring not opted in: "+name);
   merchant.Refresh(name);Check(merchant.Count("出售物品")==0,"offer refresh never reintroduces spirit sale: "+name);
  }
  var ordinary = new DialogUI { CurrentSellList=500 };ordinary.Open("普通商人");Check(ordinary.Count("出售物品")==1 && ordinary.Count("购买物品")==1,"ordinary merchant retains buy and sell");
  var ring = new DialogUI { CurrentSellList=0 };ring.Open("戒灵仙子");Check(ring.Count("出售物品")==0 && ring.Count("购买物品")==0 && ring.Count("解封")==0,"actual ring with no shop keeps original behavior");
  Console.WriteLine("PASS "+checks+" production trial menu assertions; UI/network owners are doubles, not Unity runtime validation.");
 }
}
