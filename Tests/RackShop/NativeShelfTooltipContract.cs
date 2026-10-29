// Execute the production UITip renderer/placement against deterministic Unity substitutes.
// This is not a Unity screenshot or an asset/popup integration test.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace UnityEngine {
 public class MonoBehaviour {}
 public class GameObject { public GameObject(string name){} public T AddComponent<T>() where T:class => (T)Activator.CreateInstance(typeof(T),true); }
 public struct Vector2 { public float x,y; public Vector2(float a,float b){x=a;y=b;} }
 public struct Vector3 { public float x,y,z; }
 public struct Rect { public float x,y,width,height; public bool Contains(Vector2 p)=>p.x>=x && p.x<x+width && p.y>=y && p.y<y+height; public float xMax=>x+width;public float yMax=>y+height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;} }
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new Color(1,1,1);}
 public struct Color32 {public byte r,g,b,a;public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;}public static implicit operator Color(Color32 c)=>new Color(c.r/255f,c.g/255f,c.b/255f,c.a/255f);}
 public class Texture2D {public static Texture2D whiteTexture=new Texture2D();}
 public static class GUI {public static string Owner; public static List<(string owner,int depth)> Queue=new List<(string,int)>(); public static int depth;public static Color color=Color.white;public static List<Rect> Boxes=new List<Rect>();public static List<int> Depths=new List<int>();public static void DrawTexture(Rect r,Texture2D t){Boxes.Add(r);Depths.Add(depth);Queue.Add((Owner,depth));}public static void DrawTextureWithTexCoords(Rect r,Texture2D t,Rect uv){Boxes.Add(r);Depths.Add(depth);Queue.Add((Owner,depth));}}
 public static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);}
 public static class Input {public static Vector3 mousePosition;}
 public static class Screen {public static int width=1280,height=900;}
 public enum EventType {Layout,Repaint,MouseMove,MouseDown,KeyDown,Used}
 public enum KeyCode {Escape}
 public class Event {public static Event current;public int button;public KeyCode keyCode;public void Use(){type=EventType.Used;}public Vector2 mousePosition;public EventType type;}
}
namespace FakeFantasyOnline.Application {public struct PetBrief {}}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 internal class ResLoader {public static ResLoader I=new ResLoader(); public Texture2D LoadUI(string p)=>null;public Texture2D LoadGsa(string p)=>new Texture2D(); public Texture2D LoadGsaFrame(string p,int n)=>new Texture2D();}
 internal static class BitmapFont {public static bool Ready=true;public static float Measure(string s,float scale)=>s.Length*6*scale;public static void Draw(float x,float y,string s,Color c,float scale){GUI.Depths.Add(GUI.depth);GUI.Queue.Add((GUI.Owner,GUI.depth));}public static void DrawOutline(float x,float y,string s,Color c,float scale)=>Draw(x,y,s,c,scale);}
 internal static class UIScale {public static float Factor=1;public static float SnapX(float x)=>x;public static float SnapY(float x)=>x;public static void SnapDraw(Rect r,Texture2D t)=>GUI.DrawTexture(r,t);}
 internal static class TexScale {public static float W(Texture2D t)=>346;public static float H(Texture2D t)=>128;}
 internal static class PlayerController {public static void SuppressWorldClick(){}}
 public partial class SysMsgBox {public void DrawForTest(){OnGUI();}}
 internal static class GameSettings {public static bool ColorWeak;}
 internal static class MobileClientPresentation {public static bool Enabled;}
 internal static partial class UITip {private static bool reqIsSkill;private static void ResetSkillScroll(){}private static void RenderMobileOriginal(string a,string b,float c,Vector2 d){throw new Exception("Unexpected mobile branch");}private static void RenderSkill(string a,float b,Vector2 c){throw new Exception("Unexpected skill branch");}}
 internal static partial class PetInfoTooltip {private static void Draw(FakeFantasyOnline.Application.PetBrief b,Vector2 p,float s,string h){GUI.DrawTexture(new Rect(p.x,p.y,10*s,10*s),Texture2D.whiteTexture);}}
}
class NativeShelfTooltipContract {
 static int checks; static void Check(bool v,string m){checks++;if(!v)throw new Exception(m);}
 static void Paint(Type owner){var f=owner.GetField("renderer",BindingFlags.Static|BindingFlags.NonPublic);object renderer=f.GetValue(null);renderer.GetType().GetMethod("OnGUI",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(renderer,null);}
 static void Clear(){GUI.Boxes.Clear();GUI.Depths.Clear();GUI.Queue.Clear();}
 static int Main(){
  Check(UITip.RenderDepth < -100 && UITip.RenderDepth > -1000,"tip is above native popups/addon panels, below cursor and transition overlays");
  foreach(var scale in new[]{0.75f,1f,1.5f,2f}) foreach(var size in new[]{new Vector2(800,600),new Vector2(1920,1080)}) foreach(var corner in new[]{new Vector2(4,4),new Vector2(size.x-4,4),new Vector2(4,size.y-4),new Vector2(size.x-4,size.y-4)}) {
   Screen.width=(int)size.x;Screen.height=(int)size.y;Event.current=new Event{type=EventType.Repaint,mousePosition=corner};GUI.depth=10;Clear();
   UITip.ShowCompare("商品\n属性 +10","已装备\n属性 +5",scale);Paint(typeof(UITip));
   Check(GUI.Boxes.Count==2,"main and comparison are both drawn");
   foreach(var box in GUI.Boxes) Check(box.x>=0 && box.y>=0 && box.xMax<=size.x && box.yMax<=size.y,"scaled root-GUI pointer placements stay on screen at all corners");
   foreach(int depth in GUI.Depths) Check(depth==UITip.RenderDepth,"every frame and text draw has overlay depth");
   Check(GUI.depth==UITip.RenderDepth,"depth persists for MonoBehaviour sorting");
   Check(UITip.RequestedTextForIndependentRuntime==null,"repaint consumes request");
   Clear();Paint(typeof(UITip));Check(GUI.Boxes.Count==0,"no stale tooltip after hover/close");
  }
  Event.current=new Event{type=EventType.Layout,mousePosition=new Vector2(123,234)};UITip.Show("old",1);UITip.Show("new",1.5f);Paint(typeof(UITip));
  Check(UITip.RequestedTextForIndependentRuntime=="new","latest request retained until repaint");
  Check(UITip.RequestedMouseForIndependentRuntime.x==123 && UITip.RequestedMouseForIndependentRuntime.y==234,"root GUI pointer is not rescaled twice");
  Event.current.type=EventType.Repaint;Paint(typeof(UITip));
  UITip.ShowAtForIndependentRuntime("explicit",1,new Vector2(77,88));Check(UITip.RequestedMouseForIndependentRuntime.x==77 && UITip.RequestedMouseForIndependentRuntime.y==88,"explicit host pointer preserved");Paint(typeof(UITip));
  Clear();PetInfoTooltip.Show(new FakeFantasyOnline.Application.PetBrief(),new Vector2(45,67),1);Paint(typeof(PetInfoTooltip));Check(GUI.Depths[0]==UITip.RenderDepth,"pet hover uses same foreground layer");Clear();Paint(typeof(PetInfoTooltip));Check(GUI.Boxes.Count==0,"pet request consumed after repaint");
  // Combine actual native confirmation + actual tooltip draw calls with two
  // overlapping panel primitives. Scrambled callback order must not alter depth
  // priority. This records production draw submissions, not a Unity compositor.
  int buys=0,cancels=0;
  foreach(var order in new[]{new[]{0,1,2,3},new[]{3,2,1,0},new[]{2,0,3,1},new[]{1,3,0,2}}){
   Clear();GUI.color=new Color(.2f,.3f,.4f,.5f);Event.current=new Event{type=EventType.Repaint,mousePosition=new Vector2(620,380)};
   Screen.width=1280;Screen.height=720;SysMsgBox.Show("确认购买？",()=>buys++,()=>cancels++);
   foreach(var part in order){
    GUI.Owner=part==0?"panel-back":part==1?"panel-front":part==2?"popup":"tooltip";
    if(part<2){GUI.depth=part==0?10:0;GUI.DrawTexture(new Rect(400,300,480,200),Texture2D.whiteTexture);}
    else if(part==2){SysMsgBox.I.DrawForTest();Check(GUI.depth==-100,"popup sets its own depth even after tooltip callback");}
    else{UITip.Show("弹框上的提示",1);Paint(typeof(UITip));Check(GUI.depth==-200,"tooltip keeps MonoBehaviour foreground priority");Check(GUI.color.a==.5f && GUI.color.r==.2f,"tooltip preserves caller GUI color");}
   }
   Check(GUI.Queue.Any(x=>x.owner=="popup")&&GUI.Queue.Any(x=>x.owner=="tooltip"),"real popup and tooltip both submit frame/text draws");
   Check(GUI.Queue.Where(x=>x.owner=="panel-back").All(x=>x.depth==10)&&GUI.Queue.Where(x=>x.owner=="panel-front").All(x=>x.depth==0),"later panels set independent depth rather than inheriting overlay");
   Check(GUI.Queue.Where(x=>x.owner=="popup").All(x=>x.depth==-100),"all actual popup background/button/text submissions at modal depth");
   var sorted=GUI.Queue.OrderByDescending(x=>x.depth).ToArray();
   Check(sorted.Last().owner=="tooltip" && sorted.Where(x=>x.owner=="tooltip").All(x=>x.depth==-200),"every tooltip draw sorts after overlapping panels and actual popup independent of callback order");
   Check(buys==0,"painting combined stack never purchases");
   Event.current=new Event{type=EventType.KeyDown,keyCode=KeyCode.Escape};SysMsgBox.I.DrawForTest();Check(!SysMsgBox.Visible,"Escape dismisses popup");
   Event.current=new Event{type=EventType.Repaint};Clear();Paint(typeof(UITip));Check(GUI.Queue.Count==0,"dismiss/reopen cannot resurrect consumed tooltip");
  }
  Check(cancels==4 && buys==0,"repeated confirmations cancel without purchase");
  Console.WriteLine("PASS "+checks+" production tooltip depth/coordinate/lifecycle assertions (stubs, not Unity UI)");return 0;
 }
}
