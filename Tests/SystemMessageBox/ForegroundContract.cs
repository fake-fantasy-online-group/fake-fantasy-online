using System; using System.Collections.Generic; using System.Reflection;
namespace UnityEngine {
public class MonoBehaviour {}
public class GameObject { public GameObject(string n){} public T AddComponent<T>() where T:new()=>new T(); }
public class Texture2D { public static Texture2D whiteTexture=new Texture2D(); }
public struct Color { public float a; public Color(float r,float g,float b,float a){this.a=a;} }
public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
public struct Rect { public float x,y,width,height; public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;} public bool Contains(Vector2 p)=>p.x>=x&&p.x<x+width&&p.y>=y&&p.y<y+height; }
public static class Screen {public static int width=1280,height=720;}
public enum EventType {Repaint,MouseDown,KeyDown,Used} public enum KeyCode {Escape}
public class Event { public static Event current=new Event(); public EventType type;public int button;public KeyCode keyCode;public Vector2 mousePosition;public void Use(){type=EventType.Used;} }
public static class Mathf { public static float Max(float a,float b)=>Math.Max(a,b); public static float Min(float a,float b)=>Math.Min(a,b); }
public static class GUI { public static int depth;public static Color color=new Color(1,1,1,1); public static List<Rect> Draws=new List<Rect>(); public static List<int> Depths=new List<int>(); public static void DrawTexture(Rect r,Texture2D t){Draws.Add(r);Depths.Add(depth);}public static void DrawTextureWithTexCoords(Rect r,Texture2D t,Rect uv){Draws.Add(r);Depths.Add(depth);} }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
using UnityEngine;
public class ResLoader { public static ResLoader I=new ResLoader(); public Texture2D LoadGsa(string p)=>new Texture2D();public Texture2D LoadGsaFrame(string p,int n)=>new Texture2D(); }
public static class UIScale {public static float Factor=1;public static float SnapX(float x)=>x;public static float SnapY(float x)=>x;public static void SnapDraw(Rect r,Texture2D t)=>GUI.DrawTexture(r,t);}
public static class TexScale {public static float W(Texture2D t)=>346;public static float H(Texture2D t)=>128;}
public static class BitmapFont {public static bool Ready=true;public static int Draws; public static List<int> Depths=new List<int>(); public static float Measure(string s,float sc)=>s.Length*8*sc;public static void DrawOutline(float x,float y,string s,Color c,float sc){Draws++;Depths.Add(GUI.depth);}}
public static class PlayerController {public static void SuppressWorldClick(){}}
public partial class SysMsgBox {public void DrawForTest(){OnGUI();}}
class Program {
static void Check(bool b,string s){if(!b)throw new Exception(s);Console.WriteLine("PASS: "+s);}
static void Reset(int depth) {
SysMsgBox.I=null;GUI.depth=depth;GUI.Draws.Clear();GUI.Depths.Clear();BitmapFont.Depths.Clear();
Event.current=new Event{type=EventType.Repaint};
}
static void Main(){int writes=0,cancels=0;
foreach(int previousDepth in new[]{0,10,100}) {
Reset(previousDepth);
SysMsgBox.Show("确定购买购物篮里的 2 件商品？\n共需 51 彩玉。",()=>writes++,()=>cancels++);
SysMsgBox.I.DrawForTest();
Check(SysMsgBox.Visible&&writes==0,"drawing never executes purchase");
Check(GUI.Draws.Count>=3&&BitmapFont.Depths.Count>0,"actual modal draws background, both buttons, and text");
Check(GUI.Depths.TrueForAll(d=>d<0)&&BitmapFont.Depths.TrueForAll(d=>d<0),"all modal visuals precede ordinary depth-zero windows");
Check(GUI.depth<0,"callback does not restore component sorting to ordinary window depth");
Event.current=new Event{type=EventType.KeyDown,keyCode=KeyCode.Escape};SysMsgBox.I.DrawForTest();
Check(!SysMsgBox.Visible&&writes==0,"Escape dismisses visible modal without buying");
}
Reset(0);SysMsgBox.Show("test",()=>writes++,()=>cancels++);SysMsgBox.I.DrawForTest();
Event.current=new Event{type=EventType.MouseDown,button=0,mousePosition=new Vector2(720,390)};SysMsgBox.I.DrawForTest();
Check(!SysMsgBox.Visible&&writes==0&&cancels==4,"cancel button preserves original callback");
Reset(0);SysMsgBox.Show("test",()=>writes++);SysMsgBox.I.DrawForTest();
Event.current=new Event{type=EventType.MouseDown,button=0,mousePosition=new Vector2(510,390)};SysMsgBox.I.DrawForTest();
Check(!SysMsgBox.Visible&&writes==1,"explicit affirmative still executes once");
SysMsgBox.I.DrawForTest();Check(writes==1,"closed modal cannot repeat confirmation");
Console.WriteLine("PASS: actual modal foreground-depth contract (API stubs, not Unity sorting/device validation)");
}
}
}
