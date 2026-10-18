using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;
using UnityEngine.Networking;
namespace UnityEngine {
 public class Object { public static implicit operator bool(Object o)=>o!=null; public static void Destroy(Object o){} public static void DestroyImmediate(Object o){} }
 public class GameObject:Object { public T AddComponent<T>() where T:new()=>new T(); }
 public class Coroutine { public IEnumerator Iterator; }
 public class MonoBehaviour:Object { public GameObject gameObject=new GameObject(); public Coroutine Last; public Coroutine StartCoroutine(IEnumerator i){ Last=new Coroutine{Iterator=i};i.MoveNext();return Last; } public void StopCoroutine(Coroutine c){} public static void DontDestroyOnLoad(Object o){} }
 public class AudioClip:Object { public string name; }
 public class AudioSource:Object { public bool loop,playOnAwake;public float volume; public AudioClip clip; public int Starts,Stops;public void Play(){Starts++;} public void Stop(){Stops++;} }
 public enum AudioType { OGGVORBIS }
 public static class Application { public static bool isPlaying=false,isMobilePlatform=false;public static string dataPath="/tmp"; }
 public static class PlayerPrefs { public static float GetFloat(string k,float d)=>d;public static void SetFloat(string k,float v){} }
 public static class Debug { public static int Warnings; public static void LogWarning(object m){Warnings++;} }
 public static class JsonUtility { public static T FromJson<T>(string s)=>default(T); }
}
namespace UnityEngine.Networking {
 public class DownloadHandlerAudioClip { public bool streamAudio;public static AudioClip GetContent(UnityWebRequest r){if(r.Disposed)throw new Exception("Disposed request read");return r.Clip;} }
 public class UnityWebRequest:IDisposable { public object downloadHandler=new DownloadHandlerAudioClip();public string error;public bool isDone,Disposed;public AudioClip Clip=new AudioClip();public object SendWebRequest()=>this;public void Dispose(){Disposed=true;} }
 public static class UnityWebRequestMultimedia { public static UnityWebRequest Last;public static UnityWebRequest GetAudioClip(string s,AudioType t)=>Last=new UnityWebRequest(); }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 public static class ResourceFileRoot { public static string Path="/tmp"; }
 public static class GameTables { public static string LoadText(string n)=>null; }
 public class PkgArchive { public static byte[] DefBytes; public byte[] Read(string p)=>DefBytes; }
 public class ReferenceResourceLoader { public PkgArchive PrimaryPackageForIndependentRuntime; }
 public class ResLoader { public static ResLoader I;public PkgArchive Pkg=new PkgArchive();public static byte[] Table; public static byte[] Read24Res(string p)=>Table; }
 public static class Gbk { public static string GetString(byte[] b)=>System.Text.Encoding.ASCII.GetString(b); }
}
class MapMusicContract {
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
 static object Field(MusicManager m,string n)=>typeof(MusicManager).GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(m);
 static void Set(MusicManager m,string n,object v)=>typeof(MusicManager).GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(m,v);
 static void Enter(MusicManager m,int id)=>m.PlayMap(GameBootstrap.MapSourceName(id));
 static void Complete(MusicManager m){UnityWebRequestMultimedia.Last.isDone=true;m.Last.Iterator.MoveNext();}
 static byte[] Table(int id,string name){using(var stream=new MemoryStream()){using(var w=new BinaryWriter(stream)){w.Write(0x5566);w.Write(1);w.Write(20);w.Write(1);w.Write(id);var n=new byte[16];System.Text.Encoding.ASCII.GetBytes(name).CopyTo(n,0);w.Write(n);return stream.ToArray();}}}
 static int Main(){string dir=Path.Combine(Path.GetTempPath(),"music-contract-"+Guid.NewGuid());Directory.CreateDirectory(dir);try{
 foreach(string n in new[]{"M002","M003","M034"})File.WriteAllText(Path.Combine(dir,n+".ogg"),"test boundary bytes");
 var m=new MusicManager();Set(m,"src",new AudioSource());Set(m,"musicDir",dir);MusicManager.I=m;
 var maps=(Dictionary<string,string>)Field(m,"mapMus");maps["qw0007"]="M002";maps["qw0037"]="M003";maps["qw0038"]="M034";maps["qw0039"]="M034";maps["qw0040"]="Missing";
 Enter(m,7);Complete(m);Check(m.CurrentClipName=="M002","public map starts configured track");
 Enter(m,999);Check(m.Source.clip==null&&m.CurrentTrack=="","missing map mapping must not retain outgoing music");Enter(m,7);Complete(m);
 Enter(m,90037001);Check(m.Source.clip==null,"enter dungeon retires old clip before load");Complete(m);Check(m.CurrentClipName=="M003","custom instance resolves source map music");
 int count=m.Source.Starts;Enter(m,90037002);Check(m.Source.Starts==count&&m.CurrentClipName=="M003","same source different instance does not restart shared track");
 Enter(m,38001); // Ordinary low IDs are not encoded instances; verify unknown clears.
 Check(m.Source.clip==null&&m.CurrentTrack=="","unmapped destination silences old music");
 Enter(m,90038001);Complete(m);Enter(m,90039002);Check(m.CurrentClipName=="M034","different floor sharing track retains playback");
 Enter(m,7);Complete(m);Check(m.CurrentClipName=="M002","leaving dungeon restores public map track");
 Enter(m,40);Check(m.CurrentTrack==""&&m.Source.clip==null,"missing OGG cannot retain outgoing clip or poison selection");
 File.WriteAllText(Path.Combine(dir,"Missing.ogg"),"test");Enter(m,40);Complete(m);Check(m.CurrentClipName=="Missing","missing asset can be retried when available");
 Enter(m,37);var stale=m.Last;var old=UnityWebRequestMultimedia.Last;Enter(m,38);Enter(m,37);var newest=UnityWebRequestMultimedia.Last;
 stale.Iterator.MoveNext();Check(old.Disposed&&m.HasInflightRequest&&!newest.Disposed&&m.Source.clip==null,"A B A obsolete completion cannot overwrite or clear new request");Complete(m);Check(m.CurrentClipName=="M003","latest A request wins");
 Enter(m,38);UnityWebRequestMultimedia.Last.error="decode failed";Complete(m);Check(m.CurrentTrack==""&&m.Source.clip==null&&!m.HasInflightRequest,"decode failure clears pending selection");Enter(m,38);Complete(m);Check(m.CurrentClipName=="M034","failed same track can retry");
 Enter(m,37);stale=m.Last;Check(!m.TryPlayMap("unknown")&&m.Source.clip==null,"TryPlayMap unknown also clears stale music");stale.Iterator.MoveNext();Check(m.CurrentTrack==""&&!m.HasInflightRequest,"obsolete completion after unmapped map is ignored");
 m.PlayMap(null);Check(m.CurrentTrack=="","null map is safe");
#if ORIGINAL_TABLE
 string original=Environment.GetEnvironmentVariable("FFO_ORIGINAL_MUSIC_TABLE");
 string defs=Environment.GetEnvironmentVariable("FFO_ORIGINAL_MAP_DEF");
 PkgArchive.DefBytes=defs!=null?File.ReadAllBytes(defs):System.Text.Encoding.ASCII.GetBytes("[MapDef]\nMapId=2\nFile=qw0002\n[MapDef]\nMapId=7\nFile=qw0007\n[MapDef]\nMapId=37\nFile=qw0037\n[MapDef]\nMapId=38\nFile=qw0038\n");
 ResLoader.I=new ResLoader();MapDef.EnsureLoaded(ResLoader.I.Pkg);
 Check(m.AddOriginalMapMusic(Table(2,"M003"))&&m.TryGetMapTrack("qw0002",out var added)&&added=="M003","original table joins authored map file");
 Check(m.AddOriginalMapMusic(Table(7,"M034"))&&m.TryGetMapTrack("qw0007",out added)&&added=="M002","explicit JSON track wins over original table");
 maps["qw0002"]="";Check(m.AddOriginalMapMusic(Table(2,"M003"))&&maps["qw0002"]=="","explicit blank mapping is preserved");
 int size=m.MapCount;Check(m.AddOriginalMapMusic(Table(999999,"M003"))&&m.MapCount==size,"unknown original map ID is not guessed");
 foreach(int offset in new[]{0,4,8,12}){var invalid=Table(2,"M003");invalid[offset]=255;Check(!m.AddOriginalMapMusic(invalid)&&m.MapCount==size,"invalid original table header rejected at "+offset);}
 Check(!m.AddOriginalMapMusic(new byte[12])&&!m.AddOriginalMapMusic(Table(2,"../evil")),"truncated table and path-like track rejected");
 var fresh=new MusicManager();Set(fresh,"src",new AudioSource());Set(fresh,"musicDir",dir);ResLoader.Table=Table(2,"M003");fresh.PlayMap("qw0002");Complete(fresh);Check(fresh.CurrentClipName=="M003","lazy production fallback restores missing JSON mapping");
 var broken=new MusicManager();Set(broken,"src",new AudioSource());Set(broken,"musicDir",dir);ResLoader.Table=new byte[1];int warnings=Debug.Warnings;broken.PlayMap("unknown");broken.PlayMap("unknown");Check(Debug.Warnings==warnings+1,"bad fallback table is logged once");
 if(original!=null){var actual=new MusicManager();Check(actual.AddOriginalMapMusic(File.ReadAllBytes(original)),"actual original PKG table parses and joins real map.def");Check(actual.MapCount==230,"actual table has 230 distinct authored map-file joins");foreach(var pair in new[]{"qw0007:M002","qw0037:M015a","qw0038:M016","qw0088:M034","qw0090:M033","qw0127:M046"}){var kv=pair.Split(':');Check(actual.TryGetMapTrack(kv[0],out added)&&added==kv[1],"actual original assignment "+pair);}}
#endif
 return 0;
 }finally{Directory.Delete(dir,true);}}
}
