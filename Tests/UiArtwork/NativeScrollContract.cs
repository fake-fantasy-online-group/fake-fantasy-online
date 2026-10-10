// Executes production NativeScroll.cs against deterministic Unity API stubs.
// This is not a Unity, IMGUI, PlayMode, visual, input-system or network test.
using System;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace UnityEngine {
 public struct Vector2 { public float x,y; public Vector2(float a,float b){x=a;y=b;} }
 public struct Rect { public float x,y,width,height; public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;} public float xMax=>x+width; public float yMax=>y+height; public bool Contains(Vector2 p)=>p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax; }
 public struct Color { public float r,g,b,a; public Color(float w,float x,float y,float z){r=w;g=x;b=y;a=z;} public static Color white=new Color(1,1,1,1); }
 public class Texture2D { public static Texture2D whiteTexture=new Texture2D(); }
 public static class Mathf { public static float Abs(float n)=>Math.Abs(n); public static float Clamp01(float n)=>Clamp(n,0,1); public static float Clamp(float n,float lo,float hi)=>Math.Min(hi,Math.Max(lo,n)); public static int RoundToInt(float n)=>(int)Math.Round(n); }
 public enum EventType { Repaint,MouseDown,MouseUp,MouseDrag,ScrollWheel,Used }
 public class Event { public static Event current; public EventType type; public Vector2 mousePosition,delta; public int button; public void Use(){type=EventType.Used;} }
 public static class GUI { public static Color color=Color.white; public static int draws; public static Rect last; public static void DrawTexture(Rect r,Texture2D t){draws++;last=r;} }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 public static class UIScale { public static float Factor=1; }
 public class ResLoader { public static ResLoader I=new ResLoader(); public int loads; public UnityEngine.Texture2D LoadGsa(string path){loads++;return new UnityEngine.Texture2D();} }
}
class NativeScrollContract {
 static int assertions;
 static void Check(bool condition,string label){if(!condition)throw new Exception(label);assertions++;}
 static bool Near(float a,float b)=>Math.Abs(a-b)<.002f;
 static void E(UnityEngine.EventType type,float x,float y,float dy=0){UnityEngine.Event.current=new UnityEngine.Event{type=type,mousePosition=new UnityEngine.Vector2(x,y),delta=new UnityEngine.Vector2(0,dy)};}
 static void Main(){
  var owner=new object();
  var left=new UnityEngine.Rect(13,51,208,266);var right=new UnityEngine.Rect(237,51,220,266);
  var l=new UnityEngine.Vector2(0,0);var r=new UnityEngine.Vector2(0,10);
  E(UnityEngine.EventType.Repaint,0,0);NativeScroll.Bar(owner,left,30,ref l);Check(UnityEngine.GUI.draws==0,"fitting list has no scrollbar");
  E(UnityEngine.EventType.Repaint,0,0);NativeScroll.Bar(owner,left,600,ref l);Check(Near(UnityEngine.GUI.last.x,205)&&Near(UnityEngine.GUI.last.y,52)&&Near(UnityEngine.GUI.last.width,15)&&Near(UnityEngine.GUI.last.height,20),"left thumb natural geometry");
  int loads=ResLoader.I.loads;NativeScroll.Bar(owner,right,600,ref r);Check(ResLoader.I.loads==loads,"thumb loaded only once");
  E(UnityEngine.EventType.MouseDown,210,57);NativeScroll.Bar(owner,left,600,ref l);NativeScroll.Bar(owner,right,600,ref r);Check(UnityEngine.Event.current.type==UnityEngine.EventType.Used&&Near(r.y,10),"left thumb captures without touching right column");
  E(UnityEngine.EventType.MouseDrag,210,250);NativeScroll.Bar(owner,left,600,ref l);NativeScroll.Bar(owner,right,600,ref r);Check(Near(l.y,(250-5-52)/244f*334)&&Near(r.y,10),"left drag isolated by track even for same owner");
  E(UnityEngine.EventType.MouseDrag,210,9999);NativeScroll.Bar(owner,left,600,ref l);Check(Near(l.y,334),"drag bottom clamp");
  E(UnityEngine.EventType.MouseDrag,210,-100);NativeScroll.Bar(owner,left,600,ref l);Check(Near(l.y,0),"drag top clamp");
  E(UnityEngine.EventType.MouseUp,210,80);NativeScroll.Bar(owner,left,600,ref l);float released=l.y;
  E(UnityEngine.EventType.MouseDrag,210,180);NativeScroll.Bar(owner,left,600,ref l);Check(Near(l.y,released),"mouse up releases drag");
  r.y=0;E(UnityEngine.EventType.MouseDown,446,57);NativeScroll.Bar(owner,left,600,ref l);NativeScroll.Bar(owner,right,600,ref r);
  E(UnityEngine.EventType.MouseDrag,210,180);NativeScroll.Bar(owner,left,600,ref l);NativeScroll.Bar(owner,right,600,ref r);Check(Near(l.y,released)&&r.y>0,"right drag remains right-owned even with mouse over left");
  float rightBefore=r.y;E(UnityEngine.EventType.MouseDrag,446,250);NativeScroll.Bar(new object(),right,600,ref r);Check(Near(r.y,rightBefore),"another window cannot borrow active drag");
  E(UnityEngine.EventType.MouseUp,0,0);NativeScroll.Bar(owner,right,600,ref r);
  E(UnityEngine.EventType.MouseDown,210,290);NativeScroll.Bar(owner,left,600,ref l);Check(l.y>250&&UnityEngine.Event.current.type==UnityEngine.EventType.Used,"track click jumps and consumes");
  l.y=0;r.y=0;E(UnityEngine.EventType.ScrollWheel,50,80,1);NativeScroll.Wheel(left,left.height,600,ref l,30);NativeScroll.Wheel(right,right.height,600,ref r,30);Check(Near(l.y,30)&&Near(r.y,0)&&UnityEngine.Event.current.type==UnityEngine.EventType.Used,"left wheel leaves right unchanged");
  E(UnityEngine.EventType.ScrollWheel,50,80,-1);NativeScroll.Wheel(left,left.height,600,ref l,30);Check(Near(l.y,0),"wheel direction preserved");
  E(UnityEngine.EventType.ScrollWheel,50,80,1);NativeScroll.Wheel(left,left.height,30,ref l,30);Check(Near(l.y,0)&&UnityEngine.Event.current.type==UnityEngine.EventType.ScrollWheel,"fitting content does not consume wheel");
  foreach(float s in new[]{.5f,1f,1.25f,1.5f,2f,3f}){
   UIScale.Factor=s;var v=new UnityEngine.Rect(13*s,51*s,208*s,266*s);var t=NativeScroll.TrackRect(v,s);
   Check(Near(t.x,205*s)&&Near(t.y,52*s)&&Near(t.width,15*s)&&Near(t.height,264*s),"scaled scrollbar gutter geometry");
   var bottom=NativeScroll.ThumbRect(t,334*s,334*s,20*s);Check(Near(bottom.yMax,t.yMax),"scaled bottom thumb containment");
  }
  Console.WriteLine("PASS "+assertions+" NativeScroll production-source assertions using Unity API stubs (not Unity runtime)");
 }
}
