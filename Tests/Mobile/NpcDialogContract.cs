using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine
{
 public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
 public struct Rect { public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;} public float xMin=>x;public float yMin=>y;public float xMax=>x+width;public float yMax=>y+height;public Vector2 center=>new Vector2(x+width/2,y+height/2); }
 public static class Mathf { public static float Min(float a,float b)=>Math.Min(a,b); public static float Max(float a,float b)=>Math.Max(a,b); }
 public class Texture2D {public float width,height;}
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
 public static class MobileClientPresentation { public static bool Enabled=true;public static Rect SafeAreaGui; public static float Scale; }
 public static class TexScale {public static float W(Texture2D p)=>p.width;public static float H(Texture2D p)=>p.height;}
 public sealed partial class DialogUI
 {
  public const float WindowWidth=451,WindowHeight=160,OptionMinimumHeight=80,OptionRowHeight=16;
  private List<string> optLabels=new List<string>();private List<Action> optActs=new List<Action>();
  private Texture2D pic;private bool Visible=true;private bool Ready=true;
  private bool LastHelpIntent;private object LastOfferIntent,LastTaskMutationIntent;
  static int checks; static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
  static bool Inside(Rect a,Rect b)=>a.x>=b.x-.01&&a.y>=b.y-.01&&a.xMax<=b.xMax+.01&&a.yMax<=b.yMax+.01;
  static void Main()
  {
   foreach(Rect safe in new[]{new Rect(24,0,764,360),new Rect(0,20,390,824),new Rect(60,30,1800,1020),new Rect(0,0,1280,720)})
   foreach(int count in new[]{0,1,10,100})
   foreach(float portraitHeight in new[]{0f,180f,500f})
   {
    MobileClientPresentation.Enabled=true;MobileClientPresentation.SafeAreaGui=safe;
    MobileClientPresentation.Scale=Math.Min(safe.width/960f,safe.height/540f);
    var d=new DialogUI();if(portraitHeight>0)d.pic=new Texture2D{width=600,height=portraitHeight};
    int fired=-1,calls=0;
    for(int i=0;i<count;i++){int original=i;d.optLabels.Add("option"+i);d.optActs.Add(()=>{fired=original;calls++;});}
    var l=d.MobileLayout();float scale=l.Scale;
    Check(scale>0&&scale<=MobileClientPresentation.Scale,"uniform fit only shrinks original UI");
    Check(Inside(l.Panel,safe),"original panel inside safe area");
    Check(l.Panel.width/scale==451f||Math.Abs(l.Panel.width/scale-451f)<.001f,"original width ratio");
    if(d.pic!=null)Check(Inside(new Rect(l.Panel.x+(l.Panel.width-600*scale)/2,l.Panel.y-portraitHeight*scale,600*scale,portraitHeight*scale),safe),"portrait stays visible");
    Rect box=d.OptionRect(9999,9999,99);
    if(count>0)Check(Inside(box,safe),"complete original option frame inside safe area");
    for(int i=0;i<count;i++)
    {
     Rect row=OptionRowRect(box,i,scale);Check(Inside(row,safe),"every original row reachable");
     int before=calls;Check(d.HandlePrimaryDown(row.center,9999,9999,99),"shared hit adapter accepts original row");
     Check(fired==i&&calls==before+1,"original action invoked once with unchanged index");
    }
    d.Visible=false;Check(!d.HandlePrimaryDown(box.center,9999,9999,99),"closed dialog cannot act");
   }
   MobileClientPresentation.Enabled=false;
   var desktop=new DialogUI();desktop.optLabels.Add("leave");
   Rect desktopRect=desktop.WindowRect(1280,720,1);
   Check(desktopRect.x==414.5f&&desktopRect.y==190&&desktopRect.width==451&&desktopRect.height==160,"desktop geometry unchanged");
   Console.WriteLine("PASS original NPC mobile fit/action geometry: "+checks+" checks; managed stubs, not device rendering.");
  }
 }
}
