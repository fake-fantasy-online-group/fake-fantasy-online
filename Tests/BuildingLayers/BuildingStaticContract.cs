using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Web.Script.Serialization;
using FakeFantasyOnline.Presentation.ReferenceAssets;using UnityEngine;
// Only metadata fixtures, synthetic unsupported masks, and the actual Build
// dispatch. Source-image/actor visual acceptance is a separate offline audit.
static class BuildingStaticContract {
 sealed class Case {public int Id,Width,Height,TextureWidth,TextureHeight,ClipBy,ClipWidth,StaticDepth;public string Path;}
 static int checks;static void Check(bool b,string why){checks++;if(!b)throw new Exception(why);}
 static void One(Case c,int scale){
  var l=new ResLoader();ResLoader.I=l;var t=new Texture2D(c.TextureWidth*scale,c.TextureHeight*scale);var im=new ResLoader.ObjSprite{path=c.Path,tex=t,frames=1,hdScale=scale,clipWidth=c.ClipWidth,clipBy=c.ClipBy};l.Images[c.Id]=im;l.Pkg.Files[c.Path.Replace(".gso",".mask")]=new byte[]{0};
  var map=new ReferenceGsmMap{Native=new GsmMap()};var owner=new GameBootstrap();var r=new ReferenceLegacyMapObjectRenderer(map,l,new GameObject("StaticSupport").transform,objectOwner:owner);
  Check(ReferenceWalkableDeckProfile.TryGetStaticDeck(c.Id,c.Path,c.Width,c.Height,im,c.StaticDepth,out var spans),"static signature admitted");
  var zs=new[]{0,61,-400,0,144,-148};
  for(int k=0;k<zs.Length;k++){int sy=3000+k*11;map.Native.libObjs.Add(new GsmMap.Obj{id=c.Id,sx=2000+k*1200,sy=sy,xl=c.Width,yl=c.Height,py=sy+c.StaticDepth,pz=zs[k]});}r.Build();
  for(int k=0;k<zs.Length;k++){int z=zs[k],sy=3000+k*11;var seen=new byte[t.width*t.height];var order=new int[seen.Length];
   int count=0;for(int j=0;j<r.Rendered.Count;j++){if(r.Placements[j].ObjectIndex!=k)continue;count++;var sr=r.Rendered[j];var s=sr.sprite;Check(s.texture==t&&s.ppu==100*scale,"static source texture and native PPU preserved");Check(s.vertices!=null,"static live repair dispatch reached");for(int i=0;i<s.vertices.Length;i+=4){int x0=(int)s.vertices[i].x,x1=(int)s.vertices[i+1].x,y0=t.height-(int)s.vertices[i+2].y,y1=t.height-(int)s.vertices[i].y;for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++){seen[y*t.width+x]++;order[y*t.width+x]=sr.sortingOrder;}}}
   Check(seen.All(x=>x==1),"static texture partition exactly once");var expected=Enumerable.Repeat(sy+c.StaticDepth,seen.Length).ToArray();for(int i=0;i<spans.Length;i+=4){int step=ReferenceWalkableDeckProfile.ColumnWidth(c.Id),a=spans[i]*step*scale,b=Math.Min(t.width,a+step*scale),so=sy+Math.Min(c.StaticDepth,spans[i+3]-z);for(int y=spans[i+1]*scale;y<spans[i+2]*scale;y++)for(int x=a;x<b;x++)expected[y*t.width+x]=so;}
   Check(order.SequenceEqual(expected),"static mixed-PZ exact source-column min oracle "+c.Path+" scale="+scale+" z="+z+" sy="+sy);Check(count>0&&owner.FadeCalls==zs.Length,"static fade keeps all per-instance groups");
  }
  Check(!ReferenceWalkableDeckProfile.TryGetStaticDeck(c.Id,c.Path,c.Width,c.Height,im,c.StaticDepth+1,out _),"changed GSM pivot offset rejected");Check(!ReferenceWalkableDeckProfile.TryGetStaticDeck(c.Id,c.Path+"x",c.Width,c.Height,im,c.StaticDepth,out _),"changed static path rejected");im.clipBy++;Check(!ReferenceWalkableDeckProfile.TryGetStaticDeck(c.Id,c.Path,c.Width,c.Height,im,c.StaticDepth,out _),"changed source anchor rejected");im.clipBy--;im.clipArr=new[]{1};Check(!ReferenceWalkableDeckProfile.TryGetStaticDeck(c.Id,c.Path,c.Width,c.Height,im,c.StaticDepth,out _),"new authored clip rejects old static repair");r.Dispose();
  // A newly supported mask changes the baseline premise. The static profile
  // must yield to native footprint handling instead of reusing stale geometry.
  using(var ms=new MemoryStream())using(var bw=new BinaryWriter(ms)){
   bw.Write(-1);bw.Write(0x10002);bw.Write(new byte[50]);bw.Write(1);bw.Write(5);bw.Write(5);bw.Write((byte)0x81);l.Pkg.Files[c.Path.Replace(".gso",".mask")]=ms.ToArray();
  }
  im.clipArr=null;l.Images[c.Id]=im;var native=new ReferenceLegacyMapObjectRenderer(map,l,new GameObject("ChangedMask").transform,objectOwner:new GameBootstrap());native.Build();Check(native.Rendered.All(x=>x.sprite.vertices==null),"new compatible footprint bypasses stale static profile");native.Dispose();
 }
 internal static void Run(string file){var cs=new JavaScriptSerializer().Deserialize<Case[]>(File.ReadAllText(file));Check(cs.Length>0,"static fixture nonempty");foreach(var c in cs)foreach(int scale in new[]{1,2,4})One(c,scale);Console.WriteLine("PASS "+checks+" static support contracts across "+cs.Length+" profiles, HD1/2/4, six mixed-PZ uses, synthetic unsupported masks only");}
}
