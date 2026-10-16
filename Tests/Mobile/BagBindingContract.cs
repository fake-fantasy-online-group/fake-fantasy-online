using System;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
 internal partial class InventoryDlg
 {
  internal struct Pet { public int opened; }
  internal struct Item { public int id,bagIdx,count; public Pet? pet; public bool locked; public Texture2D icon; }
  static InventoryDlg I; const int Cols=10; int tab; float s=1,winX,winY; bool splitOpen,open=true; Vector2 splitWin; static bool HasHeldAny;
  Rect SL(float x,float y,float width,float height)=>new Rect(splitWin.x+x*s,splitWin.y+y*s,width*s,height*s);
  internal static Item current=new Item{id=100,count=2};
  static Item ItemAt(int t,int i)=>i==0?current:default;
  internal static int FabaoSkillOf(int id)=>id==14000?14001:0;
  void ShowSlotTooltip(Item i,Vector2 p,bool b){}
  bool SlotPickers(Event e,Item i,bool p)=>false;
  void SlotRightButton(Event e,Item i,bool p,out bool consumed){consumed=false;}
  void SlotActivate(Event e,Item i,bool a,bool b,bool c,int d){throw new Exception("binding must not use");}
  void HandleLockForIndependentRuntime(int t,Item i,Action<string,Color> a){}
  void SlotLeftButton(Event e,Item i,bool p){}
  void TryShareBagClickForIndependentRuntime(Event e,byte t,Item i,Action a){} void ShareToChat(){}
  internal void Select(int t=0){tab=t;mobileTab=t;mobileSlot=0;mobileId=current.id;mobileActions=true;mobileActionsRect=MobilePanelLayout.Actions(new Vector2(0,0),6,out _);}
  internal void Draw()=>DrawMobileInventorySelection(Event.current);
  internal bool Binding=>mobileBinding;
 }
 static class DealDlg {public static bool Visible,CanEditBagOffer=true;public static bool IsOffered(byte t,int s)=>false;public static void ChangeBagOffer(byte t,int s){} }
 static class EquipFuseDlg {public static bool Picking;} static class ToolTarget {public static bool Active;} static class PetForgeDlg {public static bool Picking;} static class AttrWashDlg {public static bool Picking;} static class AvatarFuse {public static bool Active;} static class RepairDlg {public static bool Picking;} static class StoreDlg {public static bool Visible;} static class StallConfigDlg {public static bool Active;}
 static class ChatUI {public static void Add(string c,string m,Color co){} }
 public partial class Win05 {public static bool IsShowing(string s)=>false;}
}
static class BagBindingContract
{
 internal static void Run()
 {
  var bag=new InventoryDlg();bag.Select();int writes=NetClient.I.writes;
  GUI.click="放入快捷栏";bag.Draw();if(!bag.Binding)throw new Exception("bag chooser missing");
  GUI.click="1页1";bag.Draw();if(GameHud.hotSkill[0]!=100||GameHud.hotKind[0]!=2||NetClient.I.writes!=writes+1)throw new Exception("bag item binding");
  bag.Select();GUI.click="放入快捷栏";bag.Draw();InventoryDlg.current.id=101;GUI.click="1页2";bag.Draw();if(NetClient.I.writes!=writes+1)throw new Exception("stale bag binding");
  bag.Select();GUI.click="放入快捷栏";bag.Draw();GUI.click="取消";bag.Draw();if(bag.Binding)throw new Exception("bag cancel");
  InventoryDlg.current.id=14000;bag.Select(2);GUI.click="放入快捷栏";bag.Draw();GUI.click="1页3";bag.Draw();if(GameHud.hotSkill[2]!=14001||GameHud.hotKind[2]!=0)throw new Exception("fabao mapping");
  Console.WriteLine("PASS real bag action chooser, stale item, cancel and fabao mapping");
 }
}
