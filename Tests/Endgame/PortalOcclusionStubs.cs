using System;
using System.Collections.Generic;
namespace UnityEngine {
 public class Texture2D { public int width,height; public Texture2D(int w,int h){width=w;height=h;} }
 public class Transform { }
 public struct Vector2 { public float x,y; public Vector2(float a,float b){x=a;y=b;} }
 public struct Rect { public float x,y,width,height; public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;} }
 public class Sprite {
  public Texture2D texture; public Rect rect; public Vector2 pivot; public float ppu; public Vector2[] vertices; public ushort[] triangles; public bool destroyed;
  public static Sprite Create(Texture2D t,Rect r,Vector2 p,float u){return new Sprite{texture=t,rect=r,pivot=p,ppu=u};}
  public void OverrideGeometry(Vector2[] v,ushort[] t){foreach(var p in v)if(p.x<0||p.y<0||p.x>rect.width||p.y>rect.height)throw new Exception("OverrideGeometry must use rect-local pixels");foreach(var i in t)if(i>=v.Length)throw new Exception("index");vertices=v;triangles=t;}
 }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 using UnityEngine;
 public class GameBootstrap { public static bool UseIsoY; }
 public enum ReferenceMapObjectKind { Tile,Library }
 public struct ReferenceGsmObject {
  public int Id,SourceX,SourceY,Width,Height,PivotY,PivotZ;
 }
 public class GsmMap { public struct Obj {public int id,sx,sy,xl,yl,py,pz;} public List<Obj> libObjs=new List<Obj>(); }
 public class FakePkg {public Dictionary<string,byte[]> Files=new Dictionary<string,byte[]>();public byte[] Read(string p){return Files.TryGetValue(p,out var b)?b:null;} }
 public class ResLoader {
  public static ResLoader I;public FakePkg Pkg=new FakePkg();public ObjSprite Portal;
  public struct ObjSprite {public Texture2D tex;public string path;public int frames,clipWidth,clipBy,nLeft,nTop,hdScale;public int[] clipArr;}
  public ObjSprite LoadObj(int id,int w,int h){return Portal;}
 }
 public sealed partial class ReferenceLegacyMapObjectRenderer {
  private ResLoader resources;
  public class Placed {public Sprite Sprite;public int Order;}
  public List<Placed> Results=new List<Placed>();
  public ReferenceLegacyMapObjectRenderer(ResLoader r){resources=r;}
  private static void DestroyOwned(Sprite s){if(s!=null)s.destroyed=true;}
  private bool PlaceSpriteCore(GameBootstrap receiver,ReferenceGsmObject value,Transform parent,float ppu,float ww,float wh,ReferenceMapObjectKind kind,int idx,string path,ResLoader.ObjSprite image,int left,int width,int order,bool animate,Sprite sprite,int? logicalOffset=null){Results.Add(new Placed{Sprite=sprite,Order=order});return true;}
  public static Sprite Mesh(ResLoader.ObjSprite image,int left,int width,float ppu,int[] poly,bool rear){return CreateApertureSprite(image,left,width,ppu,poly,rear);}
  public void Prepare(GsmMap map){PreparePortalAnchors(map);}
  public bool Place(ReferenceGsmObject v,ResLoader.ObjSprite image,ReferenceMapObjectKind kind=ReferenceMapObjectKind.Library){Results.Clear();return TryPlacePortalAperture(null,v,null,100,14464,7232,kind,0,image);}
  public void Clean(){DisposePortalApertures();DisposeFootprintStrips();}
 }
}
