using System;
using System.Collections.Generic;
using System.Reflection;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace UnityEngine {
 public struct Color { public Color(float r,float g,float b,float a){} }
 public struct Vector3 { public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;} public static Vector3 up=>new Vector3(0,1,0); public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z); public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b); }
 public class Transform {public Vector3 position;}
 public class MonoBehaviour { public Transform transform=new Transform(); public T GetComponent<T>() where T:class { return null; } }
 public class Camera {public static Camera main=new Camera(); public Vector3 Point=new Vector3(4,190,1); public Vector3 WorldToScreenPoint(Vector3 v)=>Point;}
 public static class Time {public static float time=10;}
 public static class Screen {public static int width=120,height=200;}
 public static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);}
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 public class PlayerController {public float ppu=100;}
 public static class UIScale {public static float Factor=1;}
 public static class TaskClient {public class ActiveTask {public int id,step;public string progress;} public static List<ActiveTask> Active=new List<ActiveTask>();}
 public static class TaskDB {public class Entry {public string name="Task";} public static Entry Get(int i)=>i==1900030001||i==1900030002?null:new Entry();}
 public struct DrawCall { public float X,Y,Width;public string Text; }
 public static class BitmapFont {public static bool Ready=true;public static List<DrawCall> Calls=new List<DrawCall>();public static float Measure(string s,float scale)=>s.Length*6*scale;public static void DrawOutline(float x,float y,string s,UnityEngine.Color c,float scale){Calls.Add(new DrawCall{X=x,Y=y,Text=s,Width=Measure(s,scale)});}}
 public class ReferenceWorldBitmapFont { public bool Ready=true; public float Measure(string s,float scale)=>BitmapFont.Measure(s,scale);public void DrawOutline(float x,float y,string s,UnityEngine.Color c,float scale)=>BitmapFont.DrawOutline(x,y,s,c,scale); }
 public class ReferenceWorldNpcTaskFeedback {public string TaskName(int id)=>TaskDB.Get(id)?.name;}
 public static class ReferenceWorldLocalPlayerDoll {public const float PixelsPerUnit=100;}
}
class TaskTrackRenderContract {
 static int checks;
 static void Check(bool b,string label){if(!b)throw new Exception(label);checks++;}
 static void Call(object o,string name)=>o.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,null);
 static void Validate(string label){Check(BitmapFont.Calls.Count>1,label+" wraps");foreach(var c in BitmapFont.Calls){Check(c.X>=12,label+" left margin");Check(c.X+c.Width<=108,label+" right margin");Check(c.Y>=8,label+" top margin");}Check(string.Concat(BitmapFont.Calls.ConvertAll(c=>c.Text))=="abcdefghijklmnopqrstuvwxyz0123456789",label+" text preserved");}
 static WorldTaskLogState State(int step,string progress)=>new WorldTaskLogState(new[]{new WorldActiveTaskState(7,0,step,5,"",progress,false,"")},Array.Empty<int>());
 static void Main(){
  var live=new PlayerTaskTrack();Call(live,"OnEnable");PlayerTaskTrack.Show("abcdefghijklmnopqrstuvwxyz0123456789");Call(live,"OnGUI");Validate("live");
  BitmapFont.Calls.Clear();UnityEngine.Time.time=16;Call(live,"OnGUI");Check(BitmapFont.Calls.Count==0,"live expiration");UnityEngine.Time.time=10;
  ReferenceWorldPlayerTaskTrack track;Check(ReferenceWorldPlayerTaskTrack.TryCreate(new ReferenceWorldNpcTaskFeedback(),new ReferenceWorldBitmapFont(),new UnityEngine.Transform(),UnityEngine.Camera.main,out track),"create");
  track.Show("abcdefghijklmnopqrstuvwxyz0123456789");Check(track.Draw(10,1,120,200),"draw valid");Validate("adapter");BitmapFont.Calls.Clear();track.Draw(16,1,120,200);Check(BitmapFont.Calls.Count==0,"adapter expiration");
  Check(!track.Draw(float.NaN,1,120,200),"invalid time");Check(!track.Draw(10,0,120,200),"invalid scale");
  track.Observe(State(1,"1/5"),10);track.Observe(State(2,"2/5"),11);Check(track.Lines.Count==1&&track.Lines[0]=="2/5","authoritative change displayed");Check(track.ShowUntil==16,"five second receipt window");
  track.Observe(WorldTaskLogState.Empty,12);Check(track.PreviousSignatureCount==0,"removed signatures cleaned");
  foreach(int id in new[]{1900030001,1900030002})TaskClient.Active.Add(new TaskClient.ActiveTask{id=id,step=0,progress="旧进度"});
  Call(live,"Update");foreach(var task in TaskClient.Active){task.step=1;task.progress="新进度";}Call(live,"Update");
  Check(live.Lines.Count==2&&live.Lines[0]=="【问鼎周天】新进度"&&live.Lines[1]=="【幻翼初展】新进度","live two missing native titles remain readable");
  Func<int,WorldTaskLogState> pair=step=>new WorldTaskLogState(new[]{new WorldActiveTaskState(1900030001,0,step,5,"","新进度",false,""),new WorldActiveTaskState(1900030002,0,step,5,"","新进度",false,"")},Array.Empty<int>());
  track.Observe(pair(0),20);track.Observe(pair(1),21);
  Check(track.Lines.Count==2&&track.Lines[0]=="【问鼎周天】新进度"&&track.Lines[1]=="【幻翼初展】新进度","adapter two missing native titles remain readable");
  Check(TaskDB.Get(1900030001)==null&&TaskDB.Get(1900030002)==null,"tracker never inserts task definitions");
  Console.WriteLine("PASS actual live and adapter task-track sources: "+checks+" assertions with explicit Unity/font test doubles; no Unity rendering validation");
 }
}
