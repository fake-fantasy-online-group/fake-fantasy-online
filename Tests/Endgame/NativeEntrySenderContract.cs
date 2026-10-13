using System;
using System.Collections.Generic;
using System.Text;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using Wire = FakeFantasyOnline.Protocol.NetClient;
namespace UnityEngine {
 public struct Color { public Color(float r,float g,float b,float a) {} }
 public static class Random { public static int Next; public static int Range(int min,int max)=>Next; }
}
namespace FakeFantasyOnline.Protocol {
 public sealed partial class NetClient {
  public ushort Opcode; public byte[] Payload; public int Writes;
  private void Send(ushort id,Action<Buf> body) { var b=new Buf();body(b);Opcode=id;Payload=b.ToArray();Writes++; }
  internal partial class Buf { private byte[] _b; private int _pos; }
 }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 internal partial class NetClient { public static NetClient I; private void Capture(int target,int item) { throw new Exception("unexpected capture"); } }
 public static class CombatWorld { public static int CaptureTargetId; }
 public static class GameBootstrap { public static Action<int,float,float> DoTeleport; }
 public static class SocialClient { public static List<int> Party=new List<int>(); }
 public static class ChatUI { public static int Notices; public static void Add(string ch,string msg,UnityEngine.Color c) { Notices++; } }
 public static class NpcChatter { public static string GetLine(object role)=>""; public static object RoleOf(object npc)=>null; }
 public static class SysMsgBox { public static Action Confirm; public static void Show(string text,Action ok,object cancel) { Confirm=ok; } }
 public class ReferenceNpcDialogOption { public string Label;public Action Action;public ReferenceNpcDialogOption(string l,Action a) { Label=l;Action=a; } }
 public static class DungeonEntrances {
  public class Dungeon { public string name;public int map;public float x,y; }
  public static List<Dungeon> Data=new List<Dungeon>();public static IEnumerable<Dungeon> For(string name)=>Data;
 }
 public sealed partial class DialogUI {
  public string NpcName,Greeting;public bool Visible=true,InSubView;private bool show=true;private int curTrans;
  private List<string> optLabels=new List<string>();private List<Action> optActs=new List<Action>();
  private static UnityEngine.Color[] BuildRich()=>new UnityEngine.Color[0];
  private void Add(ReferenceNpcDialogOption o) { optLabels.Add(o.Label);optActs.Add(o.Action); }
  private void BuildMenu() { optLabels.Clear();optActs.Clear();BuildTravelMenu();AddDungeonEntryOptions();AddDungeonExitAndHardOptions(); }
  public void Open(string name) { NpcName=name;Visible=true;BuildMenu(); }
  public void Click(int index)=>optActs[index]();
  public void Trial(string command) { optActs.Clear();AddTrialOption("native",command);Click(0); }
  public void OpenList(int list) {curTrans=list;ShowTransferMenu();}
 }
 public partial class TransportUI {
  private int sel,transList;private bool show=true;private List<int> dests=new List<int>();
  public void Select(int list,int index,int count) {transList=list;sel=index;dests.Clear();for(int i=0;i<count;i++)dests.Add(i);DoTeleport();}
 }
 public partial class InventoryDlg {
  public class Item {public int id,bagIdx;public bool bound,cosmetic;public string name;}
  public static Action<int,int> OnUseItem;private void EquipHeld(Item item) {}
  public void Activate(int tab,Item item)=>ActivateOrdinaryItem(ref tab,item,item.bagIdx);
 }
 public static class ItemActions { public static bool Handle(int id,byte tab,int slot,bool bound,bool cosmetic,string name,Action equip)=>false; }
 public static class GameHud { public static float CdFrac(int id)=>0;public static void StartItemCd(int id) {} }
}
internal static class NativeEntrySenderContract {
 static int checks,loads,map;static float x,y;
 static void Check(bool b,string s) {if(!b)throw new Exception(s);checks++;}
 static string Command(Wire w)=>Encoding.UTF8.GetString(w.Payload,2,w.Payload.Length-2);
 static int I32(byte[] b,int o)=>BitConverter.ToInt32(b,o);
 public static int Main() {
  var wire=new Wire();var client=new NetClient();NetClient.I=client;
  client.BindChatWriterForIndependentRuntime(r=> {Check(r.Kind==WorldChatSendKind.GmCommand,"menu commands use exact native chat kind");wire.GmCmd(r.Text);});
  client.BindOpenTeleportWriterForIndependentRuntime(wire.OpenTeleport);client.BindTeleportWriterForIndependentRuntime(wire.Teleport);
  client.BindReviveWriterForIndependentRuntime(wire.Revive);
  client.BindItemUseWritersForIndependentRuntime(wire.SendOrdinaryItemRequestForIndependentRuntime,(a,b)=>{throw new Exception("targeted");},(s,c)=>{});
  GameBootstrap.DoTeleport=(m,a,b)=>{loads++;map=m;x=a;y=b;};
  var dialog=new DialogUI();int[] maps={61,37,62,38};float[] xs={6923,5978,4863,4965},ys={1763,2432,3529,3681};
  for(int i=0;i<4;i++) { int before=loads;dialog.Open("传送仙子");dialog.Click(i<2?1:2);dialog.Click(i%2);Check(loads==before+1&&map==maps[i]&&x==xs[i]&&y==ys[i]&&!dialog.Visible&&wire.Writes==0,"actual village callback prebuilds exact configured destination "+i); }
  for(int i=0;i<4;i++) {UnityEngine.Random.Next=i;dialog.Open("传送仙子");dialog.Click(0);Check(map==maps[i]&&x==xs[i]&&y==ys[i]&&wire.Writes==0,"random village callback uses same four native rows");}
  DungeonEntrances.Data.Add(new DungeonEntrances.Dungeon{name="table dungeon",map=20801,x=527,y=621});
  int n=loads;dialog.Open("native table entrance");dialog.Click(0);Check(loads==n&&ChatUI.Notices==1,"table-backed normal dungeon preserves native party requirement");
  SocialClient.Party.Add(1);dialog.Open("native table entrance");dialog.Click(0);Check(loads==n+1&&map==20801&&wire.Writes==0,"normal dungeon row calls map owner directly, not transport list");DungeonEntrances.Data.Clear();
  foreach(string command in new[]{"trial","trialgo","trialleave"}) {n=loads;dialog.Trial(command);Check(wire.Opcode==0x1006&&Command(wire)==command&&loads==n,"trial sender waits for server: "+command);}
  n=wire.Writes;dialog.Open("结界出口");dialog.Click(0);Check(wire.Writes==n&&SysMsgBox.Confirm!=null,"trial exit waits for confirmation");SysMsgBox.Confirm();Check(wire.Opcode==0x1006&&Command(wire)=="trialleave","confirmed native exit emits original trialleave command");
  string[] hardNpcs={"斯芬尼克","纯阳塔神卫","墨玉","二郎神杨戬","寂仙子","病叟"};int[] hardMaps={20901,20600,20701,20801,20300,20100};
  for(int i=0;i<hardNpcs.Length;i++) {n=wire.Writes;int oldLoads=loads;dialog.Open(hardNpcs[i]);dialog.Click(0);Check(wire.Writes==n,"hard menu does not send before confirmation");SysMsgBox.Confirm();Check(wire.Opcode==0x1006&&Command(wire)=="dungeonhard "+hardMaps[i]&&loads==oldLoads,"hard native command is preserved; this does not claim server implementation");}
  n=loads;dialog.OpenList(109);Check(wire.Opcode==0x1071&&wire.Payload.Length==4&&I32(wire.Payload,0)==109&&loads==n,"NPC list opener sends exact list ID and does not prebuild");
  var transport=new TransportUI();transport.Select(109,0,1);Check(wire.Opcode==0x1072&&wire.Payload.Length==8&&I32(wire.Payload,0)==109&&I32(wire.Payload,4)==0&&loads==n,"stone floor2-to3 selection emits list,index only");
  transport.Select(133,0,1);Check(I32(wire.Payload,0)==133&&loads==n,"floor3 exit list remains server-first");int writes=wire.Writes;transport.Select(109,1,1);Check(wire.Writes==writes,"out-of-range transport row cannot send");
  // Actual return scroll/book IDs also exercised by server NativeReturnItem tests.
  foreach(int item in new[]{3381,10025}) { new InventoryDlg().Activate(1,new InventoryDlg.Item{id=item,bagIdx=7});Check(wire.Opcode==0x1015&&wire.Payload.Length==9&&I32(wire.Payload,0)==item&&wire.Payload[4]==1&&I32(wire.Payload,5)==7&&loads==n,"return item inventory path preserves exact item/tab/slot without a local return map"); }
  client.ReviveForIndependentRuntime(0);Check(wire.Opcode==0x1018&&wire.Payload.Length==1&&wire.Payload[0]==0&&loads==n,"death-return sender leaves landing to server");
  client.GmCmdForIndependentRuntime("goto 7 100 200");Check(wire.Opcode==0x1006&&Command(wire)=="goto 7 100 200"&&loads==n,"GM sender preserves command and awaits authority");
  Console.WriteLine("PASS "+checks+" production native menu/facade/opcode/payload sender assertions; transport and Unity substituted.");return 0;
 }
}
