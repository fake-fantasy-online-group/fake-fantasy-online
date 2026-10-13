using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Protocol;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace UnityEngine {
 public class Object { public bool destroyed; public static void Destroy(Object o){if(o!=null)o.destroyed=true;} public static implicit operator bool(Object o)=>o!=null&&!o.destroyed; }
 public class Transform:Object {public Vector3 position; public Transform parent; public Vector3 localScale; public void SetParent(Transform p,bool keep){parent=p;}}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 zero=>new Vector3();}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public struct Color {public static Color white=>new Color();}
 public class Texture2D:Object {}
 public class GameObject:Object {public bool active=true; public Transform transform=new Transform(); public void SetActive(bool v){active=v;}}
 public class MonoBehaviour:Object {
  public GameObject gameObject=new GameObject();public Transform transform=>gameObject.transform;public bool isActiveAndEnabled=>gameObject.active&&!destroyed;
  public T GetComponent<T>() where T:new()=>new T();
  public void StartCoroutine(IEnumerator routine){SoundManager.Pending.Add(routine);routine.MoveNext();}
 }
 public class Sprite:Object {}
 public class SpriteRenderer:Object {public int sortingOrder;public Transform transform=new Transform();}
 public class AudioClip:Object {}
 public enum AudioType {WAV}
 public static class Time {public static float realtimeSinceStartup,deltaTime;}
}
namespace UnityEngine.Networking {
 public class UnityWebRequest:IDisposable {public enum Result{Success,Failure} public Result result=Result.Success;public object SendWebRequest()=>null;public void Dispose(){} }
 public static class UnityWebRequestMultimedia {public static UnityWebRequest GetAudioClip(string path,AudioType type)=>new UnityWebRequest();}
 public static class DownloadHandlerAudioClip {public static AudioClip GetContent(UnityWebRequest r)=>new AudioClip();}
}
namespace FakeFantasyOnline.Protocol {
 public sealed partial class PrivateServerGateway {
  public WorldGroundItemState Last; public int Published;
  void PublishGroundItemState(WorldGroundItemState s,System.Threading.CancellationToken ct,bool replaceExisting=false){Last=s;Published++;}
  public void Receive(byte[] bytes,IGroundItemReceiveContext c)=>ReceiveGroundSpawn(bytes,c);
 }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 public class ResLoader {public static ResLoader I=new ResLoader();}
 public static class CombatNumber {public static float Ppu=100;}
 public static class ArtDetail {public static event Action OnBudgetScaleChanged;public static void Change()=>OnBudgetScaleChanged?.Invoke();public static int Subscribers=>OnBudgetScaleChanged?.GetInvocationList().Length??0;}
 public partial class EffectPlayer:MonoBehaviour {
  struct KF {public float t;public Vector3 pos;public Vector2 scale;public Color rgb;}
  class Layer {public string sprite;public float speed,cField;public int blendMode;public List<KF> kfs=new List<KF>();}
  class Template {public List<Layer> layers=new List<Layer>();public float lifeMs;}
  static Dictionary<string,Template> tplCache=new Dictionary<string,Template>();static Dictionary<string,Sprite[]> spriteCache=new Dictionary<string,Sprite[]>();
  public static float NextScale=1f;
  public static bool Ready=true;public static int Creates; public static EffectPlayer Last;
  public SpriteRenderer attachSort;public bool attachUnder;SpriteRenderer[] rends=new[]{new SpriteRenderer()};
  public int GroundOrder=>rends[0].sortingOrder;public float GroundScale=>rends[0].transform.localScale.x;
  static byte[] ReadRes(string path)=>Ready?new byte[]{1}:null;
  static Sprite MakeSprite(Texture2D tex,int cx,int cy,int left,int top,int scale)=>new Sprite();
  static void NoteFx(string key,Sprite[] sprites){}
  public static EffectPlayer PlaySustained(Vector3 pos,string e,float ppu,Transform owner,float duration){Creates++;Last=new EffectPlayer();return Last;}
  public static void ClearTestCache(){tplCache.Clear();spriteCache.Clear();}
 }
 public static class DimgDecoder {
  public struct FrameTex {public Texture2D tex;public int nLeft,nTop;}
  public struct Anim {public bool ok;public FrameTex[] f;public int boundCx,boundCy,scale;}
  public static Anim DecodeAnim(byte[] bytes)=>new Anim{ok=true,boundCx=54,boundCy=179,scale=1,f=new[]{new FrameTex{tex=new Texture2D(),nLeft=2,nTop=10},new FrameTex{tex=new Texture2D(),nLeft=3,nTop=11}}};
 }

 internal partial class GroundItemEntity:MonoBehaviour {public void Tick()=>UpdateUniqueDrop(); public void Disable(){gameObject.active=false;ReleaseUniqueDrop();}public void Enable(){gameObject.active=true;OnEnable();}public void Kill(){OnDestroy();destroyed=true;}}
 public partial class SoundManager:MonoBehaviour {
  static SoundManager I; string sndDir="normal",snd24Dir="high"; readonly Dictionary<int,AudioClip> cache=new Dictionary<int,AudioClip>(),cache24=new Dictionary<int,AudioClip>();
  public static readonly List<IEnumerator> Pending=new List<IEnumerator>();public static int Emits;
  static void Ensure(){if(I==null)I=new SoundManager();}
  static string PickPath(string dir,int id)=>dir+"/"+id+".wav";
  void Emit(AudioClip clip){Emits++;}
  public static void Finish(){var p=Pending.ToArray();Pending.Clear();foreach(var x in p)while(x.MoveNext()){};}
  public static void Clear(){I=null;Pending.Clear();Emits=0;}
 }
}
class UniqueDropPresentationContract {
 static int checks;
 static void Require(bool b,string label){checks++;if(!b)throw new Exception(label);}
 static byte[] Packet(byte[] tail){var w=new WireWriter();w.WriteInt32(77);w.WriteInt32(1601);w.WriteInt32(1);w.WriteInt32(100);w.WriteInt32(200);w.WriteString("unique");w.WriteByte(1);w.WriteInt32(30000);w.WriteByte(0);w.WriteBytes(tail);return w.ToArray();}
 static byte[] Tail(byte quality=4,byte version=1,byte flags=1,int identity=1601,long token=9007199254740993){var w=new WireWriter();w.WriteByte(quality);w.WriteByte(version);w.WriteByte(flags);w.WriteInt32(identity);w.WriteInt64(token);return w.ToArray();}
 class Receive:IGroundItemReceiveContext {public WorldGroundItemState State;public bool SpawnGroundItem(WorldGroundItemState s){State=s;return true;}public void RemoveGroundItem(int i){}}
 static void Wire(){
  foreach(var tail in new[]{new byte[0],new byte[]{4},Tail()}) {Require(PrivateServerGroundItemCodec.TryDecodeSpawn(Packet(tail),out var s),"legacy and v1 accepted");var c=new Receive();var g=new PrivateServerGateway();g.Receive(Packet(tail),c);Require(c.State!=null&&g.Published==1,"native receive path");Require(s.IsUnique==(tail.Length==15),"yellow is not unique");if(tail.Length==15)Require(s.DropToken==9007199254740993&&s.FreshDrop,"exact int64 token");}
  var valid=Tail();for(int n=2;n<15;n++){var t=new byte[n];Array.Copy(valid,t,n);Require(!PrivateServerGroundItemCodec.TryDecodeSpawn(Packet(t),out _),"truncated tail "+n);}
  var missingQuality=new byte[14];Array.Copy(valid,1,missingQuality,0,14);Require(!PrivateServerGroundItemCodec.TryDecodeSpawn(Packet(missingQuality),out _),"missing quality rejected");
  foreach(var tail in new[]{Tail(version:2),Tail(flags:2),Tail(identity:-1),Tail(token:-1),Tail(token:0)}) {Require(!PrivateServerGroundItemCodec.TryDecodeSpawn(Packet(tail),out _),"malformed v1 rejected");var c=new Receive();new PrivateServerGateway().Receive(Packet(tail),c);Require(c.State==null,"native fail closed");}
  var longer=new byte[16];Array.Copy(valid,longer,15);Require(!PrivateServerGroundItemCodec.TryDecodeSpawn(Packet(longer),out _),"extra byte rejected");
  Require(PrivateServerGroundItemCodec.TryDecodeSpawn(Packet(Tail(flags:0)),out var snapshot)&&!snapshot.FreshDrop&&snapshot.IsUnique,"snapshot carries beam identity only");
 }
 static void Journal(){var j=new WorldUniqueDropPresentation();Require(!j.TryConsume(0,true,1),"ordinary and formation ignored");Require(!j.TryConsume(1601,true,0),"missing token ignored");Require(j.TryConsume(1601,true,100),"first edge");Require(!j.TryConsume(1601,true,100),"duplicate edge");Require(j.TryConsume(1601,true,99),"out of order edge preserved");Require(!j.TryConsume(1601,false,101),"snapshot silent");Require(!j.TryConsume(1601,true,101),"stale edge after snapshot silent");Require(j.TryConsume(1601,true,102),"same item new lifetime");}
 static void Lifecycle(){
  SoundManager.Clear();Time.realtimeSinceStartup=0;EffectPlayer.NextScale=2f;int created=EffectPlayer.Creates;var g=new GroundItemEntity();g.ConfigureUniqueDrop(1601,true,10001);Require(EffectPlayer.Creates==created+1,"fresh beam");Require(EffectPlayer.Last.transform.parent==g.transform,"beam parent lifetime");Require(EffectPlayer.Last.GroundOrder==-10&&EffectPlayer.Last.GroundScale==0.65f,"first frame item-relative order and source scale");Require(EffectPlayer.NextScale==2f,"ground beam cannot consume another effect scale");Require(SoundManager.Pending.Count==1,"one async audio request");g.ConfigureUniqueDrop(1601,true,10001);Require(SoundManager.Pending.Count==1,"duplicate configure no audio");SoundManager.Finish();Require(SoundManager.Emits==1,"async audio emitted once");
  var first=EffectPlayer.Last;ArtDetail.Change();Require(first.gameObject.destroyed&&!first.gameObject.active,"quality switch releases old beam immediately");g.Tick();Require(EffectPlayer.Creates==created+2&&SoundManager.Emits==1,"quality switch rebuilds without audio");g.Disable();Require(ArtDetail.Subscribers==0&&EffectPlayer.Last.gameObject.destroyed,"pickup and disable release subscription and beam");g.Enable();g.Tick();Require(EffectPlayer.Creates==created+3&&SoundManager.Emits==1,"reenable projection no audio");g.Kill();Require(ArtDetail.Subscribers==0,"destroy unsubscribed");
  var replay=new GroundItemEntity();replay.ConfigureUniqueDrop(1601,false,10001);Require(EffectPlayer.Creates==created+4&&SoundManager.Emits==1,"reconnect snapshot beam no audio");replay.ConfigureUniqueDrop(0,false,10001);Require(EffectPlayer.Last.gameObject.destroyed,"identity changed to ordinary releases beam");replay.Kill();
  SoundManager.Clear();var cancelled=new GroundItemEntity();cancelled.ConfigureUniqueDrop(1601,true,10002);cancelled.CancelUniqueDrop();SoundManager.Finish();Require(SoundManager.Emits==0,"pickup cancels before deferred Unity destruction");cancelled.Kill();
  SoundManager.Clear();var old=new GroundItemEntity();old.ConfigureUniqueDrop(1601,true,10003);Time.realtimeSinceStartup=3;SoundManager.Finish();Require(SoundManager.Emits==0,"stale load expires");old.Kill();
  SoundManager.Clear();EffectPlayer.ClearTestCache();EffectPlayer.Ready=false;created=EffectPlayer.Creates;var delayed=new GroundItemEntity();delayed.ConfigureUniqueDrop(1601,true,10004);Require(EffectPlayer.Creates==created,"missing asset no blank beam");SoundManager.Finish();Require(SoundManager.Emits==1,"audio independent of icon and effect availability");EffectPlayer.Ready=true;Time.realtimeSinceStartup=4;delayed.Tick();Require(EffectPlayer.Creates==created+1,"negative cache is retryable");delayed.Tick();Require(EffectPlayer.Creates==created+1&&SoundManager.Emits==1,"asset completion no duplicate effects");delayed.Kill();
  created=EffectPlayer.Creates;var yellow=new GroundItemEntity();yellow.ConfigureUniqueDrop(0,true,10005);yellow.Tick();Require(EffectPlayer.Creates==created,"yellow ordinary no beam");yellow.Kill();Require(ArtDetail.Subscribers==0,"no dangling subscriptions");
 }
 static void Main(){Wire();Journal();Lifecycle();Console.WriteLine("PASS "+checks+" unique drop protocol, receipt and production lifecycle checks");}
}
