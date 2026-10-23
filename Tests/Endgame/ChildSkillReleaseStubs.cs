// Unity/asset IO surrogates only. Production codec, queue, visual cursor, native phase
// runner, native skill/sound table parsers and child presentation dispatcher are compiled.
using System;
using System.Collections;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Protocol;
namespace UnityEngine {
 public class Object { public bool dead; public static implicit operator bool(Object v)=>v!=null&&!v.dead; public static void Destroy(Object o){o.dead=true;} }
 public class Transform:Object { public Vector3 position; public Dictionary<Type,object> components=new Dictionary<Type,object>(); public T GetComponent<T>() where T:class {object o;return components.TryGetValue(typeof(T),out o)?o as T:null;} }
 public class GameObject:Object {public string name;public GameObject(string n){name=n;} public T AddComponent<T>() where T:MonoBehaviour,new(){var x=new T();x.gameObject=this;MonoBehaviour.Created.Add(x);return x;} }
 public class MonoBehaviour:Object { public static List<MonoBehaviour> Created=new List<MonoBehaviour>(); public GameObject gameObject;public static List<object> Yields=new List<object>(); public void StartCoroutine(IEnumerator e){while(e.MoveNext())Yields.Add(e.Current);} }
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} }
 public class WaitForSeconds {public float seconds;public WaitForSeconds(float n){seconds=n;} }
 public static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);}
 public static class Debug {public static void LogWarning(string s){} }
 public static class JsonUtility {
  public static T FromJson<T>(string s)=>(T)Read(typeof(T),FakeFantasyOnline.Foundation.BoundedJson.Parse(s));
  private static object Read(Type t,object v){if(v==null)return null;if(t==typeof(int))return Convert.ToInt32(v);if(t.IsArray){var l=(IReadOnlyList<object>)v;var a=Array.CreateInstance(t.GetElementType(),l.Count);for(int i=0;i<l.Count;i++)a.SetValue(Read(t.GetElementType(),l[i]),i);return a;}var o=Activator.CreateInstance(t,true);var d=(IReadOnlyDictionary<string,object>)v;foreach(var f in t.GetFields())if(d.TryGetValue(f.Name,out var x))f.SetValue(o,Read(f.FieldType,x));return o;}
 }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 using UnityEngine;
 public static class NetClient {public static int SelfEntityId=7;}
 public static class CombatNumber {public static Transform PlayerTransform=new Transform();}
 public class RemotePlayer {public Transform transform=new Transform();}
 public class PlayerController:Object {public int CurDir=2;public bool Flip=true;}
 public class MonsterEntity:Object {public int CurDir=3;public bool Flip=false;}
 public class ActorEffects:Object {public void PlayCast(string n,int a,int d,bool f){EffectPlayer.PlayNamed(new Vector3(),n,100,1,d,f,false);} }
 public partial class CombatWorld {
  public float ppu=100,worldW=1000,worldH=800;
  public static Dictionary<int,RemotePlayer> Players=new Dictionary<int,RemotePlayer>();
  public static Dictionary<int,Transform> Monsters=new Dictionary<int,Transform>();
  public static RemotePlayer FindPlayer(int id){RemotePlayer p;Players.TryGetValue(id,out p);return p;}
  public static Transform MonsterTransform(int id){Transform t;Monsters.TryGetValue(id,out t);return t;}
  public void ActivateForIndependentRuntime(){}
  public static int Hits;
  public static void PlayEntityEffect(int id,string n){Hits++;}
  public void PlayGroundEffect(WorldSkillHitEffect e){throw new Exception("not ground");}
 }
 public class Frame {public CombatWorld CombatOwnerForIndependentRuntime=new CombatWorld();}
 public class Character {public int MapId=2;}
 public partial class ReferenceFirstWorldWindowOwner {
  public PrivateServerGateway session;public Frame frame=new Frame();public Character character=new Character();
  private long skillHitEffectsVersion=-1,skillHitEffectCursor;
  public bool Sync(bool force=false)=>SyncSkillHitEffects(force);
 }
 public class EffectPlayer:Object {
  public static List<EffectPlayer> Created=new List<EffectPlayer>();public string Name;public Vector3 Position;public Vector3? Destination;public Transform Target;public float MoveDuration;public int Direction;public bool Flip;
  public static EffectPlayer PlayNamed(Vector3 p,string n,float s,int f,int d,bool flip,bool follow){var x=new EffectPlayer{Position=p,Name=n,Direction=d,Flip=flip};Created.Add(x);return x;}
  public static EffectPlayer Play(Vector3 p,string n,float s,int f,int d,bool flip,bool follow)=>PlayNamed(p,n,s,f,d,flip,follow);
  public static EffectPlayer PlayById(Vector3 p,int id,float s)=>PlayNamed(p,"script:"+id,s,1,-1,false,false);
  public void MoveToPos(Vector3 p,float ms){Destination=p;MoveDuration=ms;}
  public void MoveTo(Transform t,float ms){Target=t;MoveDuration=ms;}
 }
 public static class SoundManager {public static List<int> Played=new List<int>();public static void Play(int id,int delay=0){Played.Add(id);} }
 public class ReferenceWorldCombatSound{}
 public static class GameTables {
  public static string LoadText(string n)=>n.StartsWith("skillshow")?"{\"shows\":[{\"id\":123,\"phy\":1,\"ph\":[{\"s\":10,\"e\":42,\"d\":0},{\"s\":20,\"e\":43,\"d\":120},{\"s\":30,\"e\":44,\"d\":0},{\"s\":50,\"e\":45,\"d\":0}]},{\"id\":124,\"ph\":[{\"s\":10,\"e\":46,\"d\":0}]}]}":"{\"sounds\":[{\"id\":123,\"cast\":100,\"fire\":[101],\"fd\":[0]},{\"id\":124,\"fire\":[102]}]}";
 }
 public class ResLoader:Object {public static ResLoader I=new ResLoader();public Pkg Pkg=new Pkg();}
 public class Pkg {public byte[] Read(string path)=>System.Text.Encoding.UTF8.GetBytes("function Skill123()\nPlayEffect(999, attackee)\nend");}
 public static class Gbk {public static string GetString(byte[] b)=>System.Text.Encoding.UTF8.GetString(b);}
}
