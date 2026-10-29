using System;
using System.Collections.Generic;
using System.Threading;
namespace UnityEngine { public struct Color { public Color(float a,float b,float c,float d=1){} } }
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
public static class GameBootstrap {public static int CurMapId;}
public class ReferenceNpcDialogOption {public string Label;public Action Action;public ReferenceNpcDialogOption(string label,Action action){Label=label;Action=action;}}
public sealed partial class DialogUI {
 public string NpcName; public bool Closed; public readonly List<ReferenceNpcDialogOption> Options=new List<ReferenceNpcDialogOption>();
 private void Add(ReferenceNpcDialogOption option)=>Options.Add(option);private void Close()=>Closed=true;
 public void Build()=>AddFamilyCreationOption();
}
public static class InventoryDlg {
 public struct Item {public int id,count;public bool locked;}
 public static Item Paper;public static Item ItemAt(int tab,int slot)=>tab==1&&slot==3?Paper:default;
}
public class NetClient {
 public static NetClient I=new NetClient();public int Creates;public string Name,Declaration;
 public void FamilyCreateForIndependentRuntime(string name,string declaration){Creates++;Name=name;Declaration=declaration;}
}
public static class FamUI {public static bool Clicked=true;public static bool Btn(object a,object b,string c,object d,float e)=>Clicked;}
public partial class FamilyCreateDlg {
 public static bool visible;public static string nameField,declaration;static object icoAffirm,icoCancel,mouse;static float s=1;
 public static bool get_Visible()=>visible;public static void Toggle()=>visible=!visible;
 static object L(float x,float y,float w,float h)=>null;
}
public class FakeMailCommands {
 public int Sends;public string To,Title,Body;public byte Type;public long Money;
 public object SendAsync(string to,string title,string body,long money,long caiyu,byte type,List<(byte,int,int)> items,CancellationToken ct){Sends++;To=to;Title=title;Body=body;Money=money;Type=type;return new object();}
}
public partial class MailDlg {
 public static MailDlg I;public static bool Typing;
 public bool visible;public int view;public byte composeType;public string toName,title,body,gold,silver,copper,caiyuStr;
 public List<(byte,int,int)> attachments=new List<(byte,int,int)>();public List<int> attachmentItems=new List<int>();
 public FakeMailCommands commands=new FakeMailCommands(); public Action<string,UnityEngine.Color> notify;object pending;
 public static bool ComposeVisible=>I!=null&&I.visible&&I.view==2;public static void Ensure(){if(I==null)I=new MailDlg();}
 public void Send()=>DoSend();
}
internal static partial class ItemActions {public static bool Use(int id,byte tab,int slot)=>HandleMailPaper(id,tab,slot);}
public static class Contract {
 static int assertions;static void Check(bool test,string message){assertions++;if(!test)throw new Exception(message);}
 public static void Main(){
  Check(!ItemActions.Use(3002,1,3),"ordinary items must remain in original handlers");
  Check(ItemActions.Use(3309,1,3)&&MailDlg.I==null,"missing paper must not open");
  InventoryDlg.Paper=new InventoryDlg.Item{id=3309,count=2,locked=true};ItemActions.Use(3309,1,3);Check(MailDlg.I==null,"locked paper opened");
  InventoryDlg.Paper.locked=false;ItemActions.Use(3309,1,4);Check(MailDlg.I==null,"stale slot opened");
  NetClient.I=null;ItemActions.Use(3309,1,3);Check(MailDlg.I==null,"offline paper opened");NetClient.I=new NetClient();
  ItemActions.Use(3309,1,3);Check(MailDlg.ComposeVisible,"paper did not enter real composer");Check(InventoryDlg.Paper.count==2&&MailDlg.I.commands.Sends==0,"opening consumed or sent");
  var m=MailDlg.I;m.toName=" Recipient ";m.title=" Title ";m.body=" Body ";m.attachments.Add((1,9,1));ItemActions.Use(3309,1,3);
  Check(m.toName==" Recipient "&&m.attachments.Count==1,"repeat paper use discarded draft");m.attachments.Clear();
  m.view=0;Check(m.commands.Sends==0&&InventoryDlg.Paper.count==2,"cancel must not send/consume");ItemActions.Use(3309,1,3);
  Check(m.toName==""&&m.body==""&&m.attachments.Count==0,"new compose failed to reset old draft");
  m.toName=" ";m.Send();Check(m.commands.Sends==0&&MailDlg.ComposeVisible,"blank recipient sent or dismissed");
  m.toName=" Recipient ";m.title=" Title ";m.body=" Body ";m.Send();Check(m.commands.Sends==1&&m.commands.To=="Recipient"&&m.commands.Title=="Title"&&m.commands.Body=="Body"&&m.commands.Type==0,"original send method changed letter envelope");Check(m.view==0&&!MailDlg.Typing,"send must return to mailbox");
  int[] maps={7,19,32,58,86,154,178,247};string[] names={"城主成隍","执事官徐福","玉皇大帝","监城官哈亚图","无泪城主绝聆","城主六翼羽宸","上古城主海煌","猫猫城主大王"};
  for(int i=0;i<maps.Length;i++){
   GameBootstrap.CurMapId=maps[i];FamilyCreateDlg.visible=false;var d=new DialogUI{NpcName=names[i]};d.Build();Check(d.Options.Count==1&&d.Options[0].Label=="创建家族","leader missing native option");d.Options[0].Action();Check(d.Closed&&FamilyCreateDlg.visible&&NetClient.I.Creates==0,"NPC option must open, not create");d.Options[0].Action();Check(FamilyCreateDlg.visible,"repeat option toggled form closed");FamilyCreateDlg.DrawCancel();Check(!FamilyCreateDlg.visible&&NetClient.I.Creates==0,"cancel sent creation");
   Check(!DialogUI.IsFamilyCreationNpc(maps[i]+1000,names[i]),"same name in other map admitted");Check(!DialogUI.IsFamilyCreationNpc(maps[i],names[i]+"幻影"),"substring NPC admitted");
  }
  GameBootstrap.CurMapId=7;var stale=new DialogUI{NpcName=names[0]};stale.Build();GameBootstrap.CurMapId=19;stale.Options[0].Action();Check(!FamilyCreateDlg.visible,"stale NPC callback opened form after map change");
  FamilyCreateDlg.visible=true;FamilyCreateDlg.nameField=" ";FamilyCreateDlg.DrawConfirm();Check(NetClient.I.Creates==0&&FamilyCreateDlg.visible,"blank family name sent");
  FamilyCreateDlg.nameField=" 家族 ";FamilyCreateDlg.declaration="宣言";FamilyCreateDlg.DrawConfirm();Check(NetClient.I.Creates==1&&NetClient.I.Name=="家族"&&NetClient.I.Declaration=="宣言"&&!FamilyCreateDlg.visible,"existing confirm failed");
  Console.WriteLine("PASS "+assertions+" production-entry/action assertions (not Unity)");
 }
}
}
