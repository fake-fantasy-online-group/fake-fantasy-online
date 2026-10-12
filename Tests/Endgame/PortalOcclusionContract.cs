using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
class PortalOcclusionContract {
 static int checks;
 static void Check(bool x,string s){checks++;if(!x)throw new Exception(s);}
 static byte[] Mask(int columns=22,int firstY=245){using(var m=new MemoryStream())using(var w=new BinaryWriter(m)){w.Write(-1);w.Write(0x10002);w.Write(new byte[50]);w.Write(columns);for(int x=0;x<columns;x++){w.Write(x*10+5);w.Write(firstY+(x/4)*10);}w.Write(new byte[columns]);var b=m.ToArray();for(int i=b.Length-columns;i<b.Length;i++)b[i]=0x81;return b;}}
 static void Coverage(int id,string path,int height,int scale,int nleft=0,int ntop=0,int sourceWidth=220){
  Check(ReferencePortalApertureProfile.TryGet(id,path,out var poly),"profile");
  var tex=new Texture2D((sourceWidth-nleft)*scale,(height-ntop)*scale);var im=new ResLoader.ObjSprite{tex=tex,hdScale=scale,nLeft=nleft,nTop=ntop};
  // Exercise original strip rects at nonzero texture x, independently of PPU.
  for(int left=0;left<tex.width;left+=10*scale){int width=Math.Min(10*scale,tex.width-left);var cover=new byte[width*tex.height];
   foreach(bool rear in new[]{false,true}){var mesh=ReferenceLegacyMapObjectRenderer.Mesh(im,left,width,123,poly,rear);if(mesh==null)continue;
    Check(mesh.texture==tex && mesh.rect.x==left && mesh.ppu==123*scale,"borrow texture/rect/PPU");
    Check(mesh.triangles.Length==mesh.vertices.Length/4*6,"two triangles per quad");
    for(int q=0;q<mesh.vertices.Length/4;q++){int b=q*4,t=q*6;int[] expected={b,b+1,b+2,b,b+2,b+3};for(int j=0;j<6;j++)Check(mesh.triangles[t+j]==expected[j],"quad triangle connectivity/winding");}
    for(int i=0;i<mesh.vertices.Length;i+=4){var v=mesh.vertices;int x=(int)v[i].x,top=(int)v[i+2].y,bottom=(int)v[i].y;Check(v[i+1].x==x+1,"texel column");for(int y=top;y<bottom;y++)cover[y*width+x]++;}
   }
   foreach(var n in cover)if(n!=1)throw new Exception("partition missing or duplicate pixel");checks++;
  }
 }
 static void ArchPair(int id,int ox,int oy,int oz,int px,int py,int pz){
  string path="object/JZ/JZ_DXWG_"+(id==0x182d01f5?"0501":"0502")+".gso";
  Check(ReferencePortalApertureProfile.TryGet(id,path,out var poly,out int width,out int height)&&width==427&&height==452,"arch identity and logical bounds");
  var r=new ResLoader();ResLoader.I=r;r.Portal=new ResLoader.ObjSprite{tex=new Texture2D(106,193),frames=20,clipBy=179};
  var renderer=new ReferenceLegacyMapObjectRenderer(r);var mask=Mask(43,395);r.Pkg.Files[path.Replace(".gso",".mask")]=mask;var originalMask=(byte[])mask.Clone();
  var value=new ReferenceGsmObject{Id=id,SourceX=ox,SourceY=oy,PivotZ=oz,Width=width,Height=height};
  var image=new ResLoader.ObjSprite{path=path,tex=new Texture2D(426,451),frames=1,hdScale=1};
  var gate=new GsmMap.Obj{id=ReferencePortalApertureProfile.PortalId,sx=px,sy=py,pz=pz,xl=109,yl=205};var map=new GsmMap();map.libObjs.Add(gate);renderer.Prepare(map);
  Check(renderer.Place(value,image),"actual arch/portal pair with original PZ admitted");
  int lowered=0;foreach(var p in renderer.Results){
   var sprite=p.Sprite;Check(sprite.texture==image.tex,"arch borrows source pixels");
   for(int q=0;q<sprite.vertices.Length;q+=4){
    float x=sprite.rect.x+sprite.vertices[q].x+.5f;int top=451-(int)sprite.vertices[q].y,bottom=451-(int)sprite.vertices[q+2].y;
    bool aperture=ReferencePortalApertureProfile.Column(poly,x,out float a,out float b);int originalOrder=oy+400+((int)x/10/4)*10;
    for(int y=top;y<bottom;y++){
     bool rear=aperture&&y>=Math.Ceiling(a)&&y<Math.Ceiling(b);
     Check(p.Order==(rear?Math.Min(originalOrder,py+178):originalOrder),"every arch pixel retains front depth or only lowers rear");
     if(p.Order<originalOrder)lowered++;
    }
   }
  }
  Check(lowered>0,"arch rear pixels actually lowered");for(int i=0;i<mask.Length;i++)Check(mask[i]==originalMask[i],"collision mask remains byte-identical");
  gate.pz+=600;map.libObjs[0]=gate;renderer.Prepare(map);Check(!renderer.Place(value,image),"vertically displaced portal cannot pair");gate.pz=pz;
  gate.sx=ox+(id==0x182d01f5?20:365);map.libObjs[0]=gate;renderer.Prepare(map);Check(!renderer.Place(value,image),"portal in front-pillar column cannot pair");gate.sx=px;map.libObjs[0]=gate;renderer.Prepare(map);
  image.path="object/JZ/JZ_DXWG_"+(id==0x182d01f5?"0502":"0501")+".gso";Check(!renderer.Place(value,image),"mirrored asset identity cannot be substituted");image.path=path;
  value.Width=221;Check(!renderer.Place(value,image),"stair dimensions cannot admit arch");value.Width=427;value.Height=451;Check(!renderer.Place(value,image),"unexpected logical height rejected");value.Height=452;
  map.libObjs.Add(gate);renderer.Prepare(map);Check(!renderer.Place(value,image),"ambiguous arch portal pair rejected");map.libObjs.Clear();renderer.Prepare(map);Check(!renderer.Place(value,image),"unpaired arch keeps old ordering");
  map.libObjs.Add(gate);renderer.Prepare(map);image.clipArr=new[]{395};Check(!renderer.Place(value,image),"authored arch depth preserved");image.clipArr=null;GameBootstrap.UseIsoY=true;Check(!renderer.Place(value,image),"alternate depth mode excluded for arches");GameBootstrap.UseIsoY=false;Check(!renderer.Place(value,image,ReferenceMapObjectKind.Tile),"arch tile cannot use library profile");renderer.Clean();
 }
 static void Main(){
  foreach(int scale in new[]{1,2,4}){Coverage(0x182d012d,"object/JZ/JZ_DXWG_0301.gso",304,scale);Coverage(0x182d0c1e,"object/JZ/JZ_DXWG_3102.gso",305,scale);Coverage(0x182d012d,"object/JZ/JZ_DXWG_0301.gso",304,scale,3,7);}
  foreach(int scale in new[]{1,2,4})foreach(int id in new[]{0x182d01f5,0x182d01f6}){string path="object/JZ/JZ_DXWG_"+(id==0x182d01f5?"0501":"0502")+".gso";Coverage(id,path,451,scale,0,0,426);Coverage(id,path,451,scale,3,7,426);}
  // Actual library-object coordinates for both directions and the raised 923 gate.
  ArchPair(0x182d01f5,1822,2848,0,1919,3005,0);
  ArchPair(0x182d01f6,12718,3078,0,12883,3214,0);
  ArchPair(0x182d01f6,12927,2697,0,13156,2850,0);
  ArchPair(0x182d01f5,1249,2827,0,1414,2946,0);
  ArchPair(0x182d01f5,1217,3253,170,1312,3350,100);
  Check(!ReferencePortalApertureProfile.TryGet(0x182d012f,"object/JZ/JZ_DXWG_0301.gso",out _),"other object excluded");
  Check(!ReferencePortalApertureProfile.TryGet(0x182d012d,"other.gso",out _),"other asset excluded");
  foreach(bool left in new[]{false,true}){
   var r=new ResLoader();ResLoader.I=r;r.Portal=new ResLoader.ObjSprite{tex=new Texture2D(106,193),frames=20,clipBy=179};var renderer=new ReferenceLegacyMapObjectRenderer(r);
   var value=new ReferenceGsmObject{Id=left?0x182d0c1e:0x182d012d,SourceX=left?4579:10123,SourceY=left?3321:4351,Width=221,Height=left?306:305};
   string path="object/JZ/JZ_DXWG_"+(left?"3102":"0301")+".gso";var image=new ResLoader.ObjSprite{path=path,tex=new Texture2D(220,left?305:304),frames=1,hdScale=1};r.Pkg.Files[path.Replace(".gso",".mask")]=Mask();
   var gate=new GsmMap.Obj{id=ReferencePortalApertureProfile.PortalId,sx=left?4652:10174,sy=left?3408:4440,xl=109,yl=205};var map=new GsmMap();map.libObjs.Add(gate);renderer.Prepare(map);
   Check(renderer.Place(value,image),"actual door pair admitted");int rear=0,front=0;var firstSprites=new List<Sprite>();var firstOrders=new List<int>();foreach(var p in renderer.Results){Check(p.Sprite.texture==image.tex,"no pixel copy");if(p.Order<=gate.sy+178)rear++;else front++;firstSprites.Add(p.Sprite);firstOrders.Add(p.Order);}Check(rear>0&&front>0,"rear lowered and front kept");
   gate.sy+=500;gate.sx+=500;map.libObjs[0]=gate;value.SourceY+=500;value.SourceX+=500;renderer.Prepare(map);Check(renderer.Place(value,image),"translated same asset");for(int i=0;i<firstSprites.Count;i++){Check(firstSprites[i]==renderer.Results[i].Sprite,"geometry reused independently of world order");Check(firstOrders[i]+500==renderer.Results[i].Order,"instance order translated exactly");}
   map.libObjs.Add(gate);renderer.Prepare(map);Check(!renderer.Place(value,image),"ambiguous pair rejected");map.libObjs.Clear();renderer.Prepare(map);Check(!renderer.Place(value,image),"no portal no change");map.libObjs.Add(gate);renderer.Prepare(map);
   image.clipArr=new[]{260};Check(!renderer.Place(value,image),"authored clip not replaced");image.clipArr=null;Check(!renderer.Place(value,image,ReferenceMapObjectKind.Tile),"tile not replaced");GameBootstrap.UseIsoY=true;Check(!renderer.Place(value,image),"nondefault depth excluded");GameBootstrap.UseIsoY=false;value.Width=300;Check(!renderer.Place(value,image),"unexpected geometry excluded");
   renderer.Clean();foreach(var s in firstSprites)Check(s.destroyed,"cached mesh disposed");
   value.Width=221;var noMask=new ResLoader{Portal=r.Portal};ResLoader.I=noMask;var fallback=new ReferenceLegacyMapObjectRenderer(noMask);fallback.Prepare(map);Check(!fallback.Place(value,image),"missing collision profile safely falls back");fallback.Clean();
  }
  Console.WriteLine("PASS "+checks+" portal aperture contracts; actual C# geometry and placement with Unity substitutes, not Player rendering");
 }
}
