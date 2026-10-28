using System;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace UnityEngine {
 public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} public static Vector2 operator -(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y); }
 public struct Rect { public float x,y,width,height; public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;} public bool Contains(Vector2 p)=>p.x>=x&&p.x<x+width&&p.y>=y&&p.y<y+height; }
 public static class Screen { public static int width=1280,height=900; }
 public enum EventType { MouseDown,MouseDrag,MouseUp,Used }
 public class Event { public static Event current; public EventType type; public int button; public Vector2 mousePosition; public void Use(){type=EventType.Used;} }
 public struct Color { }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 internal partial class InventoryDlg {
  public struct Item { public int id,bagIdx,count; public string name; public bool bound,cosmetic; }
  public int tab=2; public static int heldSlot=-1,heldWhSlot=-1; public static bool lockMod; public static int locks;
  static bool LockModHeld(Event e)=>lockMod;
  static void HandleLockForIndependentRuntime(int tab,Item item,Action<string,Color> action){locks++;}
  static void WardrobeStoreConfirmed(byte tab,int slot){}
  public bool Right(Event e,Item item,bool pet,out bool consumed)=>SlotRightButton(e,item,pet,out consumed);
  public static Action<int,int> OnUseItem;
  static void EquipHeld(Item item){throw new Exception("permanent cube must not equip");}
  public void Activate(Item item)=>ActivateOrdinaryItem(ref tab,item,item.bagIdx);
  /* ACTUAL_NATIVE_HANDLER */
  /* ACTUAL_NATIVE_ACTIVATION */
 }
 internal static class EndgamePanel {
  public static bool SpiritWorkbenchOpen,spiritAccept=true; public static int spiritCalls,spiritEntryCalls; public const int spiritID=24000;
  public static bool HandlePowerItemForIndependentRuntime(int id,int tab,int slot)=>false;
  public static bool HandleSpiritItemForIndependentRuntime(int id,int tab,int slot){if(id!=spiritID)return false;spiritEntryCalls++;lastTab=tab;lastSlot=slot;return true;}
  public static bool TryPutSpiritBagItem(int tab,int slot){spiritCalls++;lastTab=tab;lastSlot=slot;return spiritAccept;}
  public static bool CubeContainerOpen,accept=true,openAccept=true; public static int calls,lastTab,lastSlot,entryCalls,opened,configuredID=1900028301;
  public static bool TryPutCubeBagItem(int tab,int slot){calls++;lastTab=tab;lastSlot=slot;return accept;}
  public static bool HandleCubeItemForIndependentRuntime(int id,int tab,int slot){if(id!=configuredID)return false;entryCalls++;lastTab=tab;lastSlot=slot;if(openAccept)opened++;return true;}
 }
 internal static class ChatUI { public static void Add(string name,string text,Color color){} }
 internal static class StoreDlg { public static bool Visible; public static byte CurTab; public static void OpenNow(){Visible=true;} }
 internal class NetClient { public static NetClient I=new NetClient(); public int moves,uses,wardrobes; public static object LoadItemIcon(int id)=>null; public static bool IsPetBindItem(int id)=>false; public void UseItem(int id,byte tab,int slot){uses++;} public void WardrobeStoreForIndependentRuntime(byte tab,int slot){wardrobes++;} public void WarehouseMoveForIndependentRuntime(int a,byte tab,int slot,byte to,int count,int target){moves++;} }
 internal static class Win05 { public static bool IsShowing(string name)=>false; }
 internal static class SysMsgBox { public static void Show(string text,Action yes,Action no){} }
 internal static class SuitFxDB { public static bool IsAvatar(int id)=>false; public static bool IsDragon(int id)=>false; public static bool IsSoul(int id)=>false; }
 internal static class AvatarFuse { public static int calls; public static void Begin(int tab,int slot,int mode){calls++;} }
 internal static class ToolTarget { public static void Begin(int id){} }
 internal static class EquipFuseDlg { public static void Open(){} }
 internal static class AttrWashDlg { public static void Open(){} }
 internal static class PetForgeIds { public static bool IsTalisman(int id)=>false; public static bool IsBook(int id)=>false; public static bool IsPot(int id)=>false; }
 internal static class PetForgeDlg { public static void Open(int kind,byte tab,int slot,int id,string name,object icon){} }
 internal static class StallConfigDlg { public static void Open(int kind){} }
 internal sealed class ShopDlg { public static bool Visible; public static ShopDlg I=new ShopDlg(); public static void OpenSellOnly(){} public void BindRepairSelectionForIndependentRuntime(Action<byte> action){} }
 internal static class RepairDlg { public static void BeginPick(byte tab){} }
 internal static class GameHud { public static int cooldowns; public static float CdFrac(int id)=>0; public static void StartItemCd(int id){cooldowns++;} }
 internal static class CharData { public static string petName,petPrename; public static int petLevel; public static bool petBound; }
}
class NativeCubeInventoryContract {
 static int count;static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
 static Event Right()=>new Event{type=EventType.MouseDown,button=1};
 static void Main(){
 // Both production activation owners must intercept the permanent item before any
 // consumable, cosmetic, equipment, portable-shop, or cooldown branch can run.
 int equips=0;
 Check(ItemActions.Handle(EndgamePanel.spiritID,2,5,true,true,"灵",()=>equips++),"native spirit interception precedes cosmetic/equipment activation");
 Check(ItemActions.HandleWindowPrefixForIndependentRuntime(EndgamePanel.spiritID,2,5,"灵",null),"hosted native prefix intercepts spirit before consumable dispatch");
 Check(EndgamePanel.spiritEntryCalls==2&&NetClient.I.uses==0&&equips==0,"spirit item remains unconsumed until authoritative target operation");

 foreach(bool canOpen in new[]{true,false}) {
  EndgamePanel.openAccept=canOpen;int opened=EndgamePanel.opened;
  Check(ItemActions.Handle(EndgamePanel.configuredID,2,7,true,true,"周天鼎",()=>equips++),"full native item activation is handled even when opening is blocked");
  Check(EndgamePanel.opened==opened+(canOpen?1:0)&&EndgamePanel.lastTab==2&&EndgamePanel.lastSlot==7,"full native activation preserves exact bag identity");
  opened=EndgamePanel.opened;
  Check(ItemActions.HandleWindowPrefixForIndependentRuntime(EndgamePanel.configuredID,1,3,"周天鼎",null),"hosted native prefix intercepts permanent cube");
  Check(EndgamePanel.opened==opened+(canOpen?1:0)&&EndgamePanel.lastTab==1&&EndgamePanel.lastSlot==3,"hosted activation preserves exact bag identity");
 }
 Check(NetClient.I.uses==0&&GameHud.cooldowns==0&&AvatarFuse.calls==0&&equips==0,"portable cube neither consumes item nor starts cooldown or falls through to equip/cosmetic");
 Check(!ItemActions.Handle(1900028201,1,0,false,false,"材料")&&!ItemActions.HandleWindowPrefixForIndependentRuntime(1900028201,1,0,"材料",null),"ordinary materials retain normal native activation");
 var bag=new InventoryDlg();var item=new InventoryDlg.Item{id=1900028201,bagIdx=7,count=8,name="材料"};bool consumed;
 EndgamePanel.SpiritWorkbenchOpen=EndgamePanel.CubeContainerOpen=true;var se=Right();int priorCube=EndgamePanel.calls;
 Check(bag.Right(se,item,false,out consumed)&&consumed&&EndgamePanel.spiritCalls==1&&EndgamePanel.calls==priorCube,"spirit workbench wins before cube deposit and consumes right click");
 EndgamePanel.spiritAccept=false;se=Right();Check(bag.Right(se,item,false,out consumed)&&consumed&&se.type==EventType.Used,"rejected spirit input cannot fall through to item use");
 InventoryDlg.lockMod=true;int priorSpirit=EndgamePanel.spiritCalls;se=Right();bag.Right(se,item,false,out consumed);Check(EndgamePanel.spiritCalls==priorSpirit,"lock modifier precedes spirit reservation");InventoryDlg.lockMod=false;InventoryDlg.locks=0;
 EndgamePanel.SpiritWorkbenchOpen=false;
 EndgamePanel.CubeContainerOpen=true;var e=Right();Check(bag.Right(e,item,false,out consumed)&&consumed&&e.type==EventType.Used&&EndgamePanel.lastTab==2&&EndgamePanel.lastSlot==7,"native right click routes exact bag location and consumes activation");
 EndgamePanel.accept=false;e=Right();Check(bag.Right(e,item,false,out consumed)&&consumed&&e.type==EventType.Used,"rejected cube selection cannot fall through to equip/use");EndgamePanel.accept=true;
 int calls=EndgamePanel.calls;InventoryDlg.lockMod=true;e=Right();Check(bag.Right(e,item,false,out consumed)&&consumed&&InventoryDlg.locks==1&&EndgamePanel.calls==calls,"native lock modifier retains priority");InventoryDlg.lockMod=false;
 StoreDlg.Visible=true;e=Right();Check(bag.Right(e,item,false,out consumed)&&consumed&&NetClient.I.moves==0,"open cube reservation does not issue warehouse move");StoreDlg.Visible=false;
 EndgamePanel.CubeContainerOpen=false;e=Right();Check(bag.Right(e,item,false,out consumed)&&!consumed,"closed cube leaves original activation path intact");
 var cubeItem=new InventoryDlg.Item{id=EndgamePanel.configuredID,bagIdx=4,count=1,name="周天鼎"};EndgamePanel.openAccept=true;
 e=Right();int entries=EndgamePanel.entryCalls;Check(bag.Right(e,cubeItem,false,out consumed)&&!consumed,"permanent item right click reaches original item-activation suffix");bag.Activate(cubeItem);
 Check(EndgamePanel.entryCalls==entries+1&&EndgamePanel.lastTab==2&&EndgamePanel.lastSlot==4&&NetClient.I.uses==0&&GameHud.cooldowns==0&&cubeItem.count==1,"real native activation opens portable cube without consumption or cooldown");
 EndgamePanel.openAccept=false;bag.Activate(cubeItem);Check(NetClient.I.uses==0&&GameHud.cooldowns==0,"blocked portable entry cannot fall through from native activation");
 bag.tab=1;bag.Activate(item);Check(NetClient.I.uses==1&&GameHud.cooldowns==1,"ordinary item still uses native consumable and cooldown route");EndgamePanel.CubeContainerOpen=true;
 e=Right();Check(bag.Right(e,item,true,out consumed)&&!consumed,"pets do not enter cube");InventoryDlg.heldSlot=1;e=Right();Check(!bag.Right(e,item,false,out consumed)&&!consumed,"held cursor retains original cancellation behavior");InventoryDlg.heldSlot=-1;
 e=new Event{type=EventType.MouseDown,button=0};Check(!bag.Right(e,item,false,out consumed)&&!consumed,"native left click pickup untouched");
 object owner=new object(),other=new object();float x=100,y=200;Rect hit=new Rect(100,200,100,30);
 Event.current=new Event{type=EventType.MouseDown,button=0,mousePosition=new Vector2(120,210)};WinDrag.Handle(owner,hit,ref x,ref y);Check(Event.current.type==EventType.Used,"native title acquires drag");
 Event.current=new Event{type=EventType.MouseDrag,mousePosition=new Vector2(140,240)};WinDrag.Handle(owner,hit,ref x,ref y);Check(x==120&&y==230,"native drag retains pointer offset");
 WinDrag.Forget(other);Event.current=new Event{type=EventType.MouseDrag,mousePosition=new Vector2(150,250)};WinDrag.Handle(owner,hit,ref x,ref y);Check(x==130&&y==240,"unrelated window close cannot release this drag");
 WinDrag.Forget(owner);Event.current=new Event{type=EventType.MouseDrag,mousePosition=new Vector2(300,300)};WinDrag.Handle(owner,hit,ref x,ref y);Check(x==130&&y==240&&Event.current.type==EventType.MouseDrag,"close/reset releases captured drag");
 int generation=-1;Check(WinDrag.Stale(x,ref generation)&&!WinDrag.Stale(x,ref generation),"native placement generation remains stable");Screen.width=800;Check(WinDrag.Stale(x,ref generation),"viewport resize invalidates native placement");
 Console.WriteLine("PASS "+count+" native cube activation/inventory/drag contracts (production owners and WinDrag; no engine).");
 }
}
