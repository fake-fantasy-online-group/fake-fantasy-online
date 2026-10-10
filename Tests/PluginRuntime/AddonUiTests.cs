// Deterministic UI state/lifetime checks with Unity/artwork stubs. Not a visual or PlayMode test.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using FakeFantasyOnline.PluginApi;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace UnityEngine
{
 public class Object { }
 public class MonoBehaviour : Object { public bool isActiveAndEnabled = true; }
 public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} public static Vector2 operator -(Vector2 a,Vector2 b){return new Vector2(a.x-b.x,a.y-b.y);} }
 public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} }
 public struct Rect { public float x,y,width,height; public Rect(float x,float y,float width,float height){this.x=x;this.y=y;this.width=width;this.height=height;} public float xMax=>x+width; public float yMax=>y+height; public Vector2 position=>new Vector2(x,y); public bool Contains(Vector2 p)=>p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax; }
 public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;} public static Color white=new Color(1,1,1,1); }
 public class Texture2D { public static Texture2D whiteTexture=new Texture2D(); }
 public static class Screen { public static int width=1000,height=700; }
 public static class Input { public static Vector3 mousePosition; }
 public static class Mathf { public static float Min(float a,float b)=>Math.Min(a,b); public static float Max(float a,float b)=>Math.Max(a,b); public static float Clamp(float a,float b,float c)=>Math.Max(b,Math.Min(c,a)); public static int RoundToInt(float a)=>(int)Math.Round(a); }
 public enum TextAnchor { UpperLeft,MiddleLeft } public enum TextClipping { Clip } public enum FocusType { Passive }
 public enum EventType { Repaint,MouseDown,MouseDrag,MouseUp,Used }
 public class Event { public static Event current=new Event(); public EventType type; public int button; public Vector2 mousePosition; public void Use(){type=EventType.Used;} }
 public class Font {}
 public class GUIStyleState { public Color textColor; }
 public class GUIStyle { public GUIStyle(){} public GUIStyle(GUIStyle style){} public Font font; public bool richText,wordWrap; public int fontSize; public TextClipping clipping; public TextAnchor alignment; public GUIStyleState normal=new GUIStyleState(); }
 public class GUISkin { public GUIStyle label=new GUIStyle(); }
 public static class GUIUtility { public static int hotControl,keyboardControl; public static int GetControlID(int hint,FocusType focus,Rect rect)=>hint; }
 public static class GUI { public static int depth; public static bool enabled=true; public static Color color=Color.white; public static GUISkin skin=new GUISkin(); public static void Label(Rect r,string s,GUIStyle style){} public static void DrawTexture(Rect r,Texture2D texture){} public static void BeginGroup(Rect r){} public static void EndGroup(){} public static string TextField(Rect r,string value,int max)=>value; }
 public static class Debug { public static void LogWarning(object text){} }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
 public static class NpcChatter { public static Font SharedFont=new Font(); public static Font CnFont()=>SharedFont; }
 public static class UIScale { public static float Factor=1; }
 internal static class ArtworkSkin { public static void Panel(Rect r,float s=1){} public static void Input(Rect r,float s=1){} public static bool Button(Rect r,string text,float s=1,bool enabled=true,bool selected=false)=>false; public static bool Close(Rect r,float s=1)=>false; }
 internal static class WinZ { internal static HashSet<object> Registered=new HashSet<object>(); public static (int,bool) Frame(object owner,Rect r,int depth){Registered.Add(owner);return (depth,false);} public static void Forget(object owner){Registered.Remove(owner);} }
}
public static class AddonUiTests
{
 static int checks;
 static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;}
 static void Throws<T>(Action action,string name) where T:Exception {try{action();}catch(T){checks++;return;}throw new Exception(name);}
 static object Call(object target,string method,params object[] args){return target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);}
 static object Panel(AddonUiHost host,string id){return ((IDictionary)typeof(AddonUiHost).GetField("panels",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(host))[id];}
 static IReadOnlyDictionary<string,object> State()=>AddonUiHost.ObserveState();
 static IReadOnlyList<object> Panels()=> (IReadOnlyList<object>)State()["panels"];
 static Dictionary<string,object> Snapshot()=> (Dictionary<string,object>)Panels()[0];
 static AddonUiHost Create(){var h=new AddonUiHost();Call(h,"Awake");Call(h,"OnEnable");return h;}
 static void Emit(AddonUiHost host,string id,string name="click"){Call(host,"Emit",Panel(host,id),"button",name);}
 public static void Main()
 {
  var host=Create();
  Check((bool)State()["available"],"enabled UI registered");
  var definition=new PluginPanelDefinition("sample:p","Sample",220,180,PluginAnchor.TopRight,20,30,true,new[]{
   new PluginWidgetDefinition("button",PluginWidgetKind.Button,"Refresh",0,0,100,25),
   new PluginWidgetDefinition("bar",PluginWidgetKind.Progress,"HP",0,30,100,22,.5)});
  var handle=host.RegisterWidgetPanel(definition);
  Check(Panels().Count==1,"panel registered");
  Check((float)Snapshot()["x"]==760&& (float)Snapshot()["y"]==30,"top-right anchor");
  Input.mousePosition=new Vector3(770,650,0);Check(AddonUiHost.MouseOver(),"plugin blocks game pointer");
  host.SetPanelVisible("sample:p",false);Check(!AddonUiHost.MouseOver(),"hidden plugin does not block");host.SetPanelVisible("sample:p",true);Check(AddonUiHost.MouseOver(),"reshown plugin blocks before next OnGUI");
  for(int i=0;i<10;i++){host.SetPanelVisible("sample:p",false);host.SetPanelVisible("sample:p",true);}Check(AddonUiHost.MouseOver(),"repeated show/hide keeps pointer protection");
  host.SetWidgetProgress("sample:p","bar",5);host.SetWidgetText("sample:p","button","New text");
  var widgets=(List<object>)Snapshot()["widgets"];
  Check((double)((Dictionary<string,object>)widgets[1])["progress"]==1,"progress clamps");
  Check((string)((Dictionary<string,object>)widgets[0])["text"]=="New text","widget text updates");
  Throws<ArgumentOutOfRangeException>(()=>host.SetWidgetProgress("sample:p","bar",double.NaN),"finite progress");
  Throws<InvalidOperationException>(()=>host.RegisterWidgetPanel(definition),"duplicate panel blocked");
  Throws<KeyNotFoundException>(()=>host.SetWidgetText("sample:p","missing","x"),"unknown widget rejected");
  int callbacks=0;host.EventRaised += e=>callbacks++;
  Emit(host,"sample:p");Check(callbacks==0,"GUI events deferred");host.DrainEvents();Check(callbacks==1,"events drained");
  Emit(host,"sample:p");handle.Dispose();var replacement=host.RegisterWidgetPanel(definition);host.DrainEvents();Check(callbacks==1,"old generation event discarded on reload");
  handle.Dispose();Check(Panels().Count==1,"old handle cannot remove replacement");
  for(int i=0;i<200;i++)Emit(host,"sample:p");host.DrainEvents(1000);Check(callbacks==129,"event backlog bounded");
  host.EventRaised += e=>{throw new Exception("bad subscriber");};Emit(host,"sample:p");host.DrainEvents();Check(callbacks==130,"subscriber exception isolated");
  Exception workerFailure=null;var worker=new Thread(()=>{try{host.SetPanelText("sample:p","wrong thread");}catch(Exception e){workerFailure=e;}});worker.Start();worker.Join();Check(workerFailure is InvalidOperationException,"UI rejects background access");
  Event.current=new Event{type=EventType.MouseDown,button=0,mousePosition=new Vector2(765,40)};Call(host,"OnGUI");Check(GUIUtility.hotControl!=0,"title drag captures");
  Event.current=new Event{type=EventType.MouseDrag,button=0,mousePosition=new Vector2(-500,-500)};Call(host,"OnGUI");
  Event.current=new Event{type=EventType.MouseUp,button=0,mousePosition=new Vector2(-500,-500)};Call(host,"OnGUI");
  Check(GUIUtility.hotControl==0,"drag releases");Check((float)Snapshot()["x"]==0&&(float)Snapshot()["y"]==0,"drag stays on screen");
  Check(WinZ.Registered.Count==1,"window z registered");
  var owned=Panel(host,"sample:p");owned.GetType().GetField("PointerCapture",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owned,421);owned.GetType().GetField("KeyboardFocus",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owned,422);GUIUtility.hotControl=421;GUIUtility.keyboardControl=422;
  replacement.Dispose();Check(WinZ.Registered.Count==0,"window z released immediately");Check(GUIUtility.hotControl==0&&GUIUtility.keyboardControl==0,"removed panel releases captured pointer and focus");
  var plain=host.RegisterPanel(new PanelDefinition("text","Old API","Hello"));host.SetPanelText("text",new string('x',10000));Check(((string)Snapshot()["text"]).Length==1024,"observation text bound");host.SetPanelText("text",new string('x',1023)+"😀"+"tail");new GameSnapshot("ui",1,1,State());checks++;plain.Dispose();
  var manyWidgets=new List<PluginWidgetDefinition>();for(int i=0;i<64;i++)manyWidgets.Add(new PluginWidgetDefinition("widget"+i,PluginWidgetKind.Label,new string('x',10000),0,0,1,1));
  for(int i=0;i<64;i++)host.RegisterWidgetPanel(new PluginPanelDefinition("p"+i,"Panel",120,70,widgets:manyWidgets));
  Throws<InvalidOperationException>(()=>host.RegisterPanel(new PanelDefinition("overflow","x","x")),"panel limit");
  Check(Panels().Count==32&&(bool)State()["truncated"],"snapshot panel bound");
  Check((bool)Snapshot()["widgetsTruncated"],"snapshot widget bound");
  new GameSnapshot("ui",1,1,AddonUiHost.ObserveState(64));checks++;
  int commands=0;host.ConfigureManager(()=>Array.Empty<AddonManagerEntry>(),(id,op)=>commands++,()=>commands++,()=>new AddonBridgeStatus(false,17845,0,"Stopped"),(enabled,port)=>commands++);
  host.ToggleManager();Check((bool)State()["managerVisible"],"manager toggles open");
  Event.current=new Event{type=EventType.Repaint};GUI.depth=731;GUI.color=new Color(.1f,.2f,.3f,.4f);Call(host,"OnGUI");Check(GUI.depth==731&&GUI.color.a==.4f,"GUI state restored");Check(commands==0,"render does not trigger commands");
  var style=(GUIStyle)typeof(AddonUiHost).GetField("labelStyle",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(host);Check(ReferenceEquals(style.font,NpcChatter.SharedFont),"Chinese UI uses client font");
  Check((string)typeof(AddonUiHost).GetMethod("DisplayAddonState",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"Enabled"})=="已启用","manager state localized without changing enum");
  host.Dispose();host.Dispose();Check(!(bool)State()["available"]&&Panels().Count==0,"dispose releases all panels and host");Check(WinZ.Registered.Count==0,"dispose releases all hit zones");
  Throws<ObjectDisposedException>(()=>host.RegisterPanel(new PanelDefinition("late","x","x")),"disposed host rejects mutation");
  Console.WriteLine("PASS "+checks+" addon UI state/lifetime assertions (Unity/artwork stubs; not visual/PlayMode)");
 }
}
