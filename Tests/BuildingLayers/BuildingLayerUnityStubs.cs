using System;using System.Collections.Generic;using System.Text;
namespace UnityEngine {
 public class Object { public static implicit operator bool(Object o)=>o!=null;public static bool operator !(Object o)=>o==null;public static void Destroy(Object o){}public static void DestroyImmediate(Object o){} }
 public class Texture2D:Object{public int width,height;public Texture2D(int w,int h){width=w;height=h;}}
 public class Material:Object{}
 public class Component:Object{public GameObject gameObject;public Transform transform=>gameObject.transform;}
 public class Transform:Object{public GameObject gameObject;public Transform parent;public Vector3 position;public Vector3 localScale;}
 public class GameObject:Object{public string name;public Transform transform;public GameObject(string n=""){name=n;transform=new Transform{gameObject=this};}public T AddComponent<T>()where T:Component,new(){return new T{gameObject=this};}}
 public class SpriteRenderer:Component {public Sprite sprite;public Material sharedMaterial;public int sortingOrder;}
 public struct Vector2{public float x,y;public Vector2(float a,float b){x=a;y=b;}}
 public struct Vector3{public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}}
 public struct Rect{public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}}
 public class Sprite:Object{public Texture2D texture;public Rect rect;public Vector2 pivot;public float ppu;public Vector2[] vertices;public ushort[] triangles;public static Sprite Create(Texture2D t,Rect r,Vector2 p,float u)=>new Sprite{texture=t,rect=r,pivot=p,ppu=u};public void OverrideGeometry(Vector2[]v,ushort[]t){vertices=v;triangles=t;}}
 public static class Mathf{public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static int Abs(int a)=>Math.Abs(a);public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v));public static int RoundToInt(float v)=>(int)Math.Round(v);}
 public static class Application{public static bool isPlaying=false;}
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets{
 using UnityEngine;
 public class GsmMap{public struct Obj{public int id,sx,sy,xl,yl,py,pz;} public bool objsOk=true;public List<Obj>tileObjs=new List<Obj>(),libObjs=new List<Obj>();}
 public class ReferenceGsmMap{public GsmMap Native;public bool Ok=true,HasCover;public int MapWidth=1024,MapHeight=768;}
 public struct ReferenceGsmObject{public int Id,SourceX,SourceY,Width,Height,PivotY,PivotZ;public ReferenceGsmObject(int id,int sx,int sy,int w,int h,int py,int pz){Id=id;SourceX=sx;SourceY=sy;Width=w;Height=h;PivotY=py;PivotZ=pz;}}
 public class FakePkg{public Dictionary<string,byte[]>Files=new Dictionary<string,byte[]>(StringComparer.OrdinalIgnoreCase);public byte[] Read(string p)=>Files.TryGetValue(p,out var b)?b:null;}
 public class ResLoader{public static ResLoader I;public FakePkg Pkg=new FakePkg();public Dictionary<int,ObjSprite>Images=new Dictionary<int,ObjSprite>();public struct ObjSprite{public Texture2D tex;public string path;public int frames,clipWidth,clipBy,nLeft,nTop,hdScale;public int[]clipArr;}
 public class ObjAnim{public Sprite[]frames;public int[]nLeft,nTop;}
 public ObjSprite LoadObj(int id,int w=0,int h=0)=>Images[id];public ObjAnim LoadObjAnim(int id,float ppu)=>null;public void BuildCityTable(List<int>ids){} }
 public class FrameAnimator:Component{public float frameMs;public bool loop;public Func<(Sprite[],Vector3[])>loader;}
 public static class LoadTrace{public static bool On=false;public static long Now=>0;public static float Ms(long s)=>0;}
 public partial class GameBootstrap{
  public string mapName="synthetic-test-map";public static bool UseIsoY=false;public Transform WorldRootForMapConstructionForIndependentRuntime;
  public static int ObjN,ObjSliceN,ObjAnimN,ObjPlainN;public static float ObjLoadMs,ObjSliceMs,ObjSpawnMs,ObjFadeMs;
  private Dictionary<int,Sprite>objCache=new Dictionary<int,Sprite>();private Dictionary<int,List<(Sprite,int,int)>>sliceCache=new Dictionary<int,List<(Sprite,int,int)>>();
  public static Material IsoYObjMat()=>null;public static void ResetMapObjectPlacementMetricsForIndependentRuntime(){}public void RegisterMapObjectOccludersForIndependentRuntime(Transform p){}public void TouchMapObjectForIndependentRuntime(int id){}public void NoteMapObjectForIndependentRuntime(int id){}
  public List<SpriteRenderer> FadeGroup; public int FadeOrder,FadeCalls;
  public void RegisterMapObjectFadeForIndependentRuntime(List<SpriteRenderer>g,int x,float y,ResLoader.ObjSprite im,int id,float ww,float ppu,int order){FadeGroup=g;FadeOrder=order;FadeCalls++;}
  public static StringBuilder BeginMapObjectSeamForIndependentRuntime(int id,int x,int y,int draw,float ww,float ppu,ResLoader.ObjSprite im)=>null;
  public static void AppendMapObjectSeamForIndependentRuntime(StringBuilder s,Sprite sp,SpriteRenderer r,int x,int g,float wx,float ppu,int scale){}
  public static void EndMapObjectSeamForIndependentRuntime(StringBuilder s){}
 }
 public sealed partial class ReferenceLegacyMapObjectRenderer{
  private List<Sprite>embeddedSprites=new List<Sprite>();private List<Texture2D>embeddedTextures=new List<Texture2D>();
  private void PlaceEmbeddedObjects(GsmMap m,Transform p,float ppu,float w,float h){}
  public List<SpriteRenderer> Rendered=>renderers;
 }
}
