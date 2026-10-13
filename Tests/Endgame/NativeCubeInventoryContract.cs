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
  public struct Item { public int bagIdx,count; public string name; }
  public int tab=2; public static int heldSlot=-1,heldWhSlot=-1; public static bool lockMod; public static int locks;
  static bool LockModHeld(Event e)=>lockMod;
  static void HandleLockForIndependentRuntime(int tab,Item item,Action<string,Color> action){locks++;}
  static void WardrobeStoreConfirmed(byte tab,int slot){}
  public bool Right(Event e,Item item,bool pet,out bool consumed)=>SlotRightButton(e,item,pet,out consumed);
  /* ACTUAL_NATIVE_HANDLER */
 }
 internal static class EndgamePanel { public static bool CubeContainerOpen,accept=true; public static int calls,lastTab,lastSlot; public static bool TryPutCubeBagItem(int tab,int slot){calls++;lastTab=tab;lastSlot=slot;return accept;} }
 internal static class ChatUI { public static void Add(string name,string text,Color color){} }
 internal static class StoreDlg { public static bool Visible; public static byte CurTab; }
 internal class NetClient { public static NetClient I=new NetClient(); public int moves; public void WarehouseMoveForIndependentRuntime(int a,byte tab,int slot,byte to,int count,int target){moves++;} }
 internal static class Win05 { public static bool IsShowing(string name)=>false; }
 internal static class SysMsgBox { public static void Show(string text,Action yes,Action no){} }
}
class NativeCubeInventoryContract {
 static int count;static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
 static Event Right()=>new Event{type=EventType.MouseDown,button=1};
 static void Main(){
 var bag=new InventoryDlg();var item=new InventoryDlg.Item{bagIdx=7,count=8,name="材料"};bool consumed;
 EndgamePanel.CubeContainerOpen=true;var e=Right();Check(bag.Right(e,item,false,out consumed)&&consumed&&e.type==EventType.Used&&EndgamePanel.lastTab==2&&EndgamePanel.lastSlot==7,"native right click routes exact bag location and consumes activation");
 EndgamePanel.accept=false;e=Right();Check(bag.Right(e,item,false,out consumed)&&consumed&&e.type==EventType.Used,"rejected cube selection cannot fall through to equip/use");EndgamePanel.accept=true;
 int calls=EndgamePanel.calls;InventoryDlg.lockMod=true;e=Right();Check(bag.Right(e,item,false,out consumed)&&consumed&&InventoryDlg.locks==1&&EndgamePanel.calls==calls,"native lock modifier retains priority");InventoryDlg.lockMod=false;
 StoreDlg.Visible=true;e=Right();Check(bag.Right(e,item,false,out consumed)&&consumed&&NetClient.I.moves==0,"open cube reservation does not issue warehouse move");StoreDlg.Visible=false;
 EndgamePanel.CubeContainerOpen=false;e=Right();Check(bag.Right(e,item,false,out consumed)&&!consumed,"closed cube leaves original activation path intact");EndgamePanel.CubeContainerOpen=true;
 e=Right();Check(bag.Right(e,item,true,out consumed)&&!consumed,"pets do not enter cube");InventoryDlg.heldSlot=1;e=Right();Check(!bag.Right(e,item,false,out consumed)&&!consumed,"held cursor retains original cancellation behavior");InventoryDlg.heldSlot=-1;
 e=new Event{type=EventType.MouseDown,button=0};Check(!bag.Right(e,item,false,out consumed)&&!consumed,"native left click pickup untouched");
 object owner=new object(),other=new object();float x=100,y=200;Rect hit=new Rect(100,200,100,30);
 Event.current=new Event{type=EventType.MouseDown,button=0,mousePosition=new Vector2(120,210)};WinDrag.Handle(owner,hit,ref x,ref y);Check(Event.current.type==EventType.Used,"native title acquires drag");
 Event.current=new Event{type=EventType.MouseDrag,mousePosition=new Vector2(140,240)};WinDrag.Handle(owner,hit,ref x,ref y);Check(x==120&&y==230,"native drag retains pointer offset");
 WinDrag.Forget(other);Event.current=new Event{type=EventType.MouseDrag,mousePosition=new Vector2(150,250)};WinDrag.Handle(owner,hit,ref x,ref y);Check(x==130&&y==240,"unrelated window close cannot release this drag");
 WinDrag.Forget(owner);Event.current=new Event{type=EventType.MouseDrag,mousePosition=new Vector2(300,300)};WinDrag.Handle(owner,hit,ref x,ref y);Check(x==130&&y==240&&Event.current.type==EventType.MouseDrag,"close/reset releases captured drag");
 int generation=-1;Check(WinDrag.Stale(x,ref generation)&&!WinDrag.Stale(x,ref generation),"native placement generation remains stable");Screen.width=800;Check(WinDrag.Stale(x,ref generation),"viewport resize invalidates native placement");
 Console.WriteLine("PASS "+count+" native cube inventory/drag contracts (production handler and WinDrag; no engine).");
 }
}
