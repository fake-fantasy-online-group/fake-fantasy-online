using System;
using System.Collections.Generic;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace UnityEngine {
 public struct Vector2 { public float x,y; public Vector2(float a,float b){x=a;y=b;} }
 public struct Rect { public float x,y,width,height; public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;} public float xMax{get{return x+width;}} public float yMax{get{return y+height;}} public bool Contains(Vector2 p){return p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax;} }
 public struct Color { public float r,g,b,a; public Color(float w,float x,float y,float z){r=w;g=x;b=y;a=z;} public static Color white=new Color(1,1,1,1),gray=new Color(.5f,.5f,.5f,1); public static Color operator *(Color x,Color y){return new Color(x.r*y.r,x.g*y.g,x.b*y.b,x.a*y.a);} }
 public class Texture2D { public int width=79,height=39; public string name; public static Texture2D whiteTexture=new Texture2D(); }
 public static class Mathf { public static int RoundToInt(float n){return (int)Math.Round(n);} public static int FloorToInt(float n){return (int)Math.Floor(n);} public static float Min(float a,float b){return Math.Min(a,b);} public static float Max(float a,float b){return Math.Max(a,b);} public static int Max(int a,int b){return Math.Max(a,b);} public static int Clamp(int n,int lo,int hi){return Math.Min(hi,Math.Max(lo,n));} }
 public enum ScaleMode { StretchToFill }
 public enum TextAnchor { MiddleCenter } public enum TextClipping { Clip }
 public class RectOffset { public RectOffset(int a,int b,int c,int d){} }
 public class GUIContent {public string text;}
 public class Font {}
 public class GUIStyleState { public Color textColor; }
 public class GUIStyle {public GUIStyle(){} public GUIStyle(GUIStyle old){} public Font font; public TextAnchor alignment; public TextClipping clipping; public bool richText,wordWrap; public RectOffset padding; public int fontSize; public GUIStyleState normal=new GUIStyleState(); public Vector2 CalcSize(GUIContent c){return new Vector2(c.text.Length*fontSize,fontSize);} }
 public class GUISkin {public GUIStyle label=new GUIStyle();}
 public enum EventType { Repaint,MouseDown,MouseUp,KeyDown,Used } public enum KeyCode { Space,Return,KeypadEnter,Escape } public enum FocusType { Keyboard }
 public class Event { public static Event current=new Event(); public EventType type; public int button; public Vector2 mousePosition; public KeyCode keyCode; public EventType GetTypeForControl(int id){return type;} public void Use(){type=EventType.Used;} }
 public static class GUIUtility { public static int hotControl,keyboardControl; public static int GetControlID(int hint,FocusType focus,Rect r){return 100+(int)r.x;} }
 public static class GUI {public static GUISkin skin=new GUISkin(); public static string lastLabel; public static void Label(Rect r,GUIContent c,GUIStyle st){lastLabel=c.text;} public static Color color=Color.white; public static bool enabled=true; public static List<Rect> cells=new List<Rect>(); public static Texture2D last; public static Color lastColor; public static void DrawTexture(Rect r,Texture2D t){cells.Add(r);last=t;lastColor=color;} public static void DrawTexture(Rect r,Texture2D t,ScaleMode mode,bool alphaBlend){DrawTexture(r,t);} public static void DrawTextureWithTexCoords(Rect r,Texture2D t,Rect uv){if(uv.x < -0.0001||uv.y < -0.0001||uv.xMax>1.0001||uv.yMax>1.0001)throw new Exception("UV outside texture");DrawTexture(r,t);} }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 public static class NpcChatter { public static UnityEngine.Font CnFont(){return null;} }
 public class ResLoader { public static ResLoader I; public object Pkg=new object(); public int calls; public UnityEngine.Texture2D LoadGsa(string p){calls++; return new UnityEngine.Texture2D{name=p};} public UnityEngine.Texture2D LoadGsaFrameA(string p,int i){calls++;return new UnityEngine.Texture2D{name=p+":"+i};} }
 public static class TexScale { public static float W(UnityEngine.Texture2D t){return t.width;} public static float H(UnityEngine.Texture2D t){return t.height;} }
 public static class BitmapFont { public static bool Ready=true; public static float lastScale; public static float Measure(string s,float z){return s.Length*12*z;} public static void CenterOutline(UnityEngine.Rect r,string s,UnityEngine.Color c,float z){lastScale=z;} }
}
class Test {
 static int count;
 static void Assert(bool condition,string name){if(!condition)throw new Exception(name);count++;}
 static void E(UnityEngine.EventType t,float x=5,float y=5,int b=0,UnityEngine.KeyCode k=UnityEngine.KeyCode.Space){UnityEngine.Event.current=new UnityEngine.Event{type=t,mousePosition=new UnityEngine.Vector2(x,y),button=b,keyCode=k};}
 static void Main(){
  var r=new UnityEngine.Rect(0,0,70,22); ResLoader.I=new ResLoader();
  E(UnityEngine.EventType.Repaint);ArtworkSkin.ButtonFace(r,"写信"); int loads=ResLoader.I.calls; ArtworkSkin.ButtonFace(r,"写信");Assert(loads==ResLoader.I.calls,"per-frame resource reload");
  int packageCalls=ResLoader.I.calls;ResLoader.I.Pkg=new object();ArtworkSkin.Panel(r);Assert(ResLoader.I.calls>packageCalls,"same loader package replacement");loads=ResLoader.I.calls; var old=ResLoader.I;ResLoader.I=new ResLoader();ArtworkSkin.Panel(r);Assert(ResLoader.I.calls>0&&old.calls==loads,"owner replacement cache");
  UnityEngine.GUI.color=new UnityEngine.Color(.2f,.3f,.4f,.5f);ArtworkSkin.Panel(r,1,true,.7f);Assert(UnityEngine.GUI.color.r==.2f&&UnityEngine.GUI.color.a==.5f,"panel state restoration");ArtworkSkin.Input(r,1,true);Assert(UnityEngine.GUI.color.r==.2f,"input state restoration");
  ArtworkSkin.ButtonFace(r,"写信",enabled:false);Assert(UnityEngine.GUI.last.name.EndsWith(":3"),"disabled frame");ArtworkSkin.ButtonFace(r,"写信",pressed:true);Assert(UnityEngine.GUI.last.name.EndsWith(":2"),"pressed frame");ArtworkSkin.ButtonFace(r,"写信",selected:true);Assert(UnityEngine.GUI.last.name.EndsWith(":1"),"hover frame");
  foreach(float scale in new[]{.5f,1f,2f}){UnityEngine.GUI.cells.Clear();var tiny=new UnityEngine.Rect(0,0,4*scale,3*scale);ArtworkSkin.ButtonFace(tiny,"长文本",scale); foreach(var c in UnityEngine.GUI.cells)Assert(c.width>=0&&c.height>=0&&c.x>=0&&c.y>=0&&c.xMax<=tiny.width+.001&&c.yMax<=tiny.height+.001,"slice bounds");}
  ArtworkSkin.ButtonFace(r,"这是一段很长的按钮文字");Assert(BitmapFont.lastScale*12*"这是一段很长的按钮文字".Length <= r.width,"long text fit");
  E(UnityEngine.EventType.MouseDown);Assert(!ArtworkSkin.Button(r,"a"),"activation on down");Assert(UnityEngine.GUIUtility.hotControl==100,"mouse capture");E(UnityEngine.EventType.MouseUp);Assert(ArtworkSkin.Button(r,"a"),"activation on release");Assert(UnityEngine.GUIUtility.hotControl==0,"capture release");
  E(UnityEngine.EventType.MouseDown);ArtworkSkin.Button(r,"a");E(UnityEngine.EventType.MouseUp,200,200);Assert(!ArtworkSkin.Button(r,"a")&&UnityEngine.GUIUtility.hotControl==0,"drag outside cancel");
  E(UnityEngine.EventType.MouseDown);ArtworkSkin.Button(r,"a");E(UnityEngine.EventType.MouseUp);Assert(!ArtworkSkin.Button(r,"a",enabled:false)&&UnityEngine.GUIUtility.hotControl==0,"disable mid-click");
  E(UnityEngine.EventType.MouseDown,b:1);Assert(!ArtworkSkin.Button(r,"a")&&UnityEngine.GUIUtility.hotControl==0,"right click ignored");
  UnityEngine.GUIUtility.keyboardControl=100;E(UnityEngine.EventType.KeyDown,k:UnityEngine.KeyCode.Return);Assert(ArtworkSkin.Button(r,"a")&&UnityEngine.Event.current.type==UnityEngine.EventType.Used,"focused keyboard activation");
  UnityEngine.GUI.enabled=false;E(UnityEngine.EventType.KeyDown);Assert(!ArtworkSkin.Button(r,"a"),"global disabled");UnityEngine.GUI.enabled=true;
  BitmapFont.Ready=false;ArtworkSkin.ButtonFace(r,"fallback label");Assert(UnityEngine.GUI.lastLabel=="fallback label","missing bitmap label fallback");BitmapFont.Ready=true; ResLoader.I=null;E(UnityEngine.EventType.Repaint);ArtworkSkin.Panel(r);Assert(true,"missing loader fallback");
  var image=new UnityEngine.Texture2D{name="title artwork"};
  UnityEngine.GUI.color=new UnityEngine.Color(.4f,.5f,.6f,.7f);
  ArtworkSkin.SingleImageFace(r,image);
  Assert(UnityEngine.GUI.last==image&&UnityEngine.GUI.lastColor.r==.4f,"single artwork identity and idle tint");
  ArtworkSkin.SingleImageFace(r,image,hover:true);
  Assert(UnityEngine.GUI.lastColor.r>.4f,"single artwork hover brightens");
  Assert(UnityEngine.GUI.color.r==.4f&&UnityEngine.GUI.color.a==.7f,"single artwork restores GUI tint");
  ArtworkSkin.SingleImageFace(r,image,hover:true,pressed:true);
  Assert(UnityEngine.GUI.lastColor.r<.4f&&UnityEngine.GUI.lastColor.r>.3f,"single artwork pressed overrides hover");
  ArtworkSkin.SingleImageFace(r,image,hover:true,pressed:true,enabled:false);
  Assert(UnityEngine.GUI.lastColor.r<.3f,"single artwork disabled overrides pressed and hover");
  UnityEngine.GUI.enabled=false;ArtworkSkin.SingleImageFace(r,image,hover:true);
  Assert(UnityEngine.GUI.lastColor.r<.3f,"single artwork global disabled overrides hover");
  UnityEngine.GUI.enabled=true;UnityEngine.GUI.cells.Clear();ArtworkSkin.SingleImageFace(r,null,hover:true);
  Assert(UnityEngine.GUI.cells.Count==0,"missing single artwork draws nothing");
  ArtworkSkin.SingleImageFace(r,image,hover:true);
  var imageRect=UnityEngine.GUI.cells[0];
  Assert(imageRect.x==r.x&&imageRect.y==r.y&&imageRect.width==r.width&&imageRect.height==r.height,"single artwork hover preserves geometry");
  ArtworkSkin.ButtonFace(new UnityEngine.Rect(0,0,152,33),"申请账号",fontScale:1f);
  Assert(BitmapFont.lastScale==1f,"native title label scale remains unscaled");
  Console.WriteLine("PASS "+count+" deterministic artwork/input contract assertions (Unity API stubs; not PlayMode)");
 }
}
