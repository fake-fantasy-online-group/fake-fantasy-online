using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Web.Script.Serialization;
using FakeFantasyOnline.Presentation.ReferenceAssets;using UnityEngine;
// Metadata fixtures only; no original package bytes/art. Production dispatch,
// borrowed geometry and per-pixel partition are exercised with synthetic masks.
// Actual source-pixel/avatar evidence is a separate private offline audit.
static class BuildingSupportContract {
 sealed class Case { public int Id,Width,Height,TextureWidth,TextureHeight;public string Path; }
 static int checks; static void Check(bool b,string why){checks++;if(!b)throw new Exception(why);}
 static int Front(int column,int height,bool varied) => varied && column%2==0 ? 10 : ((height+9)/10)*10;
 static byte[] Mask(int width,int height,bool varied=false){int n=(width+9)/10;using(var m=new MemoryStream())using(var w=new BinaryWriter(m)){w.Write(-1);w.Write(0x10002);w.Write(new byte[50]);w.Write(n);for(int c=0;c<n;c++){w.Write(c*10+5);w.Write(Front(c,height,varied)-5);}for(int c=0;c<n;c++)w.Write((byte)0x81);return m.ToArray();}}
 static void One(Case c,int scale,int z,bool varied=false){
  const int sx=2000,sy=3000;var loader=new ResLoader();ResLoader.I=loader;var tex=new Texture2D(c.TextureWidth*scale,c.TextureHeight*scale);var im=new ResLoader.ObjSprite{path=c.Path,tex=tex,frames=1,hdScale=scale};loader.Images[c.Id]=im;loader.Pkg.Files[c.Path.Replace(".gso",".mask")]=Mask(c.TextureWidth,c.TextureHeight,varied);
  var map=new ReferenceGsmMap{Native=new GsmMap()};map.Native.libObjs.Add(new GsmMap.Obj{id=c.Id,sx=sx,sy=sy,xl=c.Width,yl=c.Height,py=sy+c.Height,pz=z});var owner=new GameBootstrap();var r=new ReferenceLegacyMapObjectRenderer(map,loader,new GameObject("Support").transform,objectOwner:owner);r.Build();
  Check(ReferenceWalkableDeckProfile.TryGetMaskDeck(c.Id,c.Path,c.Width,c.Height,im,out var spans),"recorded support signature admitted");
  var seen=new byte[tex.width*tex.height];var orders=new int[seen.Length];int originalFront=((c.TextureHeight+9)/10)*10;
  foreach(var sr in r.Rendered){Check(sr.sprite.texture==tex,"support borrows source texture");Check(sr.sprite.ppu==100*scale,"support preserves native PPU");Check(sr.sortingOrder<=sy+originalFront,"support never promoted over old front (including negativeZ)");var v=sr.sprite.vertices;Check(v!=null,"support dispatch reached compiled mesh owner");for(int i=0;i<v.Length;i+=4){int x0=(int)(sr.sprite.rect.x+v[i].x),x1=(int)(sr.sprite.rect.x+v[i+1].x),y0=tex.height-(int)v[i+2].y,y1=tex.height-(int)v[i].y;Check(x0>=0&&x1<=tex.width&&y0>=0&&y1<=tex.height,"geometry stays in original texture");for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++){seen[y*tex.width+x]++;orders[y*tex.width+x]=sr.sortingOrder;}}}
  Check(seen.All(v=>v==1),"support texture exactly once, no missing or duplicate source pixels");
  var expected=new int[seen.Length];for(int y=0;y<tex.height;y++)for(int x=0;x<tex.width;x++)expected[y*tex.width+x]=sy+Front(x/(10*scale),c.TextureHeight,varied);
  for(int i=0;i<spans.Length;i+=4){int step=ReferenceWalkableDeckProfile.ColumnWidth(c.Id),x0=spans[i]*step*scale,x1=Math.Min(x0+step*scale,tex.width),top=spans[i+1]*scale,bottom=spans[i+2]*scale,so=sy+Math.Min(Front(spans[i]*step/10,c.TextureHeight,varied),spans[i+3]-z);for(int y=top;y<bottom;y++)for(int x=x0;x<x1;x++)expected[y*tex.width+x]=so;}
  Check(orders.SequenceEqual(expected),"only declared support/backplane spans change; all other source pixels keep old depth");Check(owner.FadeCalls==1&&owner.FadeGroup.Count==r.Rendered.Count,"support fade registration includes every layer");
  Check(!ReferenceWalkableDeckProfile.TryGetMaskDeck(c.Id,c.Path+".changed",c.Width,c.Height,im,out _),"source path mismatch falls back");Check(!ReferenceWalkableDeckProfile.TryGetMaskDeck(c.Id,c.Path,c.Width+1,c.Height,im,out _),"logical shape mismatch falls back");im.nLeft=1;Check(!ReferenceWalkableDeckProfile.TryGetMaskDeck(c.Id,c.Path,c.Width,c.Height,im,out _),"trim mismatch falls back");im.nLeft=0;im.clipArr=new[]{1};Check(!ReferenceWalkableDeckProfile.TryGetMaskDeck(c.Id,c.Path,c.Width,c.Height,im,out _),"authored clip appearance falls back");r.Dispose();
 }
 internal static void Run(string file){var cases=new JavaScriptSerializer().Deserialize<Case[]>(File.ReadAllText(file));Check(cases.Length==87,"audited support signature count");foreach(var c in cases){One(c,1,0);One(c,1,-400);One(c,1,0,true);One(c,1,61,true);}foreach(var c in new[]{cases[0],cases[cases.Length-1]})foreach(int scale in new[]{2,4})One(c,scale,61);Console.WriteLine("PASS "+checks+" support-geometry contracts across87 recorded signatures with varying per-column ceilings (synthetic mask fixtures; not source-art visual proof)");}
}
