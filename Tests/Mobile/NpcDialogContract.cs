using System;
using System.Collections.Generic;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace UnityEngine
{
 public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} public static Vector2 operator -(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y);public float sqrMagnitude=>x*x+y*y; }
 public struct Rect { public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;} public float xMax=>x+width;public float yMax=>y+height; public bool Contains(Vector2 p)=>p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax;public Vector2 center=>new Vector2(x+width/2,y+height/2); }
 public static class Mathf { public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(v,b)); }
 public static class Time { public static int frameCount;public static float unscaledTime; }
 public enum TouchPhase { Canceled }
 public struct Touch { public TouchPhase phase; }
 public static class Input { public static int touchCount;public static Touch GetTouch(int i)=>new Touch(); }
 public enum EventType { MouseDown,MouseDrag,MouseUp,ScrollWheel,Used }
 public class Event { public EventType type;public int button;public Vector2 mousePosition;public void Use(){type=EventType.Used;} }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
 public static class MobileClientPresentation { public static bool Enabled=true;public static Rect SafeAreaGui; public static float Scale; }
 public sealed partial class DialogUI
 {
  private string NpcName="NPC",Greeting="Hello";private List<string> optLabels=new List<string>();private List<Action> optActs=new List<Action>();private bool closed; private bool Visible=>!closed;
  private void Close(){closed=true;}
  static int checks; static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
  static bool Inside(Rect a,Rect b)=>a.x>=b.x-.01&&a.y>=b.y-.01&&a.xMax<=b.xMax+.01&&a.yMax<=b.yMax+.01;
  static void Main()
  {
   foreach(Rect safe in new[]{new Rect(24,0,764,360),new Rect(0,20,390,824),new Rect(60,30,1800,1020),new Rect(0,0,1280,720)})
   foreach(int count in new[]{0,1,10,100})
   {
    float scale=Math.Min(safe.width/960f,safe.height/540f);var l=new MobileNpcDialogLayout(safe,scale,count);
    foreach(Rect r in new[]{l.Panel,l.Close,l.Body,l.TextPrevious,l.TextNext,l.Previous,l.Next}) Check(Inside(r,safe),"safe area bounds");
    Check(l.RowHeight==44*scale,"44 logical points"); Check(l.RowsPerPage*l.PageCount>=count,"all actions reachable");
    for(int i=0;i<l.RowsPerPage;i++) Check(l.Row(i).yMax<=l.Previous.y+.01,"rows don't overlap pager");
   }
   var pages=MobileNpcDialogLayout.TextPages("/C01"+new string('任',501)+"\n奖励",40,3,c=>10);
   Check(pages.Count>40,"long text paginated");Check(string.Join("",pages).Replace("/C01","").Replace("\n","")==new string('任',501)+"奖励","all text preserved");
   foreach(string page in pages) Check(page.StartsWith("/C01"),"color carried across page");
   MobileClientPresentation.SafeAreaGui=new Rect(24,0,764,360);MobileClientPresentation.Scale=.65f;
   var d=new DialogUI();int fired=-1,calls=0;
   for(int i=0;i<50;i++){int original=i;d.optLabels.Add("option"+i);d.optActs.Add(()=>{fired=original;calls++;});}
   var layout=d.SyncMobileDialog();
   for(int page=0;page<layout.PageCount;page++)
   {
    d.mobileOptionPage=page;
    for(int slot=0;slot<layout.RowsPerPage&&page*layout.RowsPerPage+slot<50;slot++)
    {
     Time.frameCount++;int before=calls;Vector2 point=layout.Row(slot).center;
     d.HandleMobilePrimaryDown(point);Check(fired==page*layout.RowsPerPage+slot&&calls==before+1,"routes original index exactly once");
     d.HandleMobilePrimaryDown(point);Check(calls==before+1,"same frame duplicate suppressed");
    }
   }
   Time.frameCount++;d.mobileOptionPage=0;
   var down=new Event{type=EventType.MouseDown,mousePosition=layout.Row(0).center};d.HandleMobileEvent(layout,down);Check(down.type==EventType.Used,"modal consumes down");
   int beforeRefresh=calls;d.optActs[0]=()=>calls++;layout=d.SyncMobileDialog();
   var up=new Event{type=EventType.MouseUp,mousePosition=layout.Row(0).center};d.HandleMobileEvent(layout,up);Check(calls==beforeRefresh,"refreshed options cancel stale finger");Check(up.type==EventType.Used,"modal consumes release");
   d.mobileOptionPage=3;d.mobileTextPage=2;d.NpcName="Next NPC";d.SyncMobileDialog();Check(d.mobileOptionPage==0&&d.mobileTextPage==0,"new NPC resets pages");
   d.HandleMobileEvent(layout,new Event{type=EventType.MouseDown,mousePosition=layout.Row(0).center});
   d.HandleMobileEvent(layout,new Event{type=EventType.MouseDrag,mousePosition=new Vector2(-100,-100)});
   d.HandleMobileEvent(layout,new Event{type=EventType.MouseUp,mousePosition=layout.Row(0).center});Check(calls==beforeRefresh,"drag never clicks row");
   // Complete-like callbacks leave the same view open: suppress a second tap
   // across frames, then permit an intentional later retry.
   Time.frameCount++;Time.unscaledTime=10;int beforeDouble=calls;
   d.HandleMobilePrimaryDown(layout.Row(0).center);
   Time.frameCount++;Time.unscaledTime=10.1f;d.HandleMobilePrimaryDown(layout.Row(0).center);
   Check(calls==beforeDouble+1,"cross-frame double tap emits one original action");
   Time.frameCount++;Time.unscaledTime=10.5f;d.HandleMobilePrimaryDown(layout.Row(0).center);
   Check(calls==beforeDouble+2,"later retry preserves original semantics");
   foreach(int cancelKind in new[]{0,1,2,3})
   {
    Time.frameCount++;Time.unscaledTime+=1;int beforeCancel=calls;
    d.HandleMobileEvent(layout,new Event{type=EventType.MouseDown,mousePosition=layout.Row(0).center});
    if(cancelKind==0) d.ObserveMobileTouches(1,true);
    if(cancelKind==1) d.ObserveMobileTouches(2,false);
    if(cancelKind==2) d.OnApplicationFocus(false);
    if(cancelKind==3) d.OnApplicationPause(true);
    d.HandleMobileEvent(layout,new Event{type=EventType.MouseUp,mousePosition=layout.Row(0).center});
    Check(calls==beforeCancel,"cancel/multitouch/focus/pause blocks release "+cancelKind);
    d.OnApplicationFocus(true);d.OnApplicationPause(false);d.ObserveMobileTouches(0,false);
    d.HandleMobileEvent(layout,new Event{type=EventType.MouseUp,mousePosition=layout.Row(0).center});
    Check(calls==beforeCancel,"stale release after resume cannot activate "+cancelKind);
   }
   Time.frameCount++;d.HandleMobilePrimaryDown(layout.Close.center);Check(d.closed,"close invokes existing Close");
   Console.WriteLine("PASS NPC mobile layout, text and production action routing: "+checks+" checks; managed stubs, not device rendering.");
  }
 }
}
