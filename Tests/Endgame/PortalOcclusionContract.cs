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
 // Independent double-precision ray oracle; no production Column helper and
 // no min/max envelope. Half-open edge crossings preserve concave gaps.
 static bool Inside(int[] p,double x,double y){bool inside=false;for(int i=0,j=p.Length-2;i<p.Length;j=i,i+=2){double ax=p[i],bx=p[j];if(x<Math.Min(ax,bx)||x>=Math.Max(ax,bx))continue;double cy=p[i+1]+(x-ax)*(p[j+1]-p[i+1])/(bx-ax);if(y>=cy-0.00001)inside=!inside;}return inside;}
 static void GeometryOracle(int[] poly,int width,int height,int scale,int nleft=0,int ntop=0,bool oldProfile=false){
  var tex=new Texture2D((width-nleft)*scale,(height-ntop)*scale);var im=new ResLoader.ObjSprite{tex=tex,hdScale=scale,nLeft=nleft,nTop=ntop};var front=ReferencePortalApertureProfile.PreservedFront(poly);
  for(int left=0;left<tex.width;left+=10*scale){int w=Math.Min(10*scale,tex.width-left);var partition=new byte[w*tex.height];var rearMap=new bool[w*tex.height];
   foreach(bool rear in new[]{false,true}){var mesh=ReferenceLegacyMapObjectRenderer.Mesh(im,left,w,100,poly,rear);if(mesh==null)continue;
    for(int i=0;i<mesh.vertices.Length;i+=4){int x=(int)mesh.vertices[i].x,a=tex.height-(int)mesh.vertices[i].y,b=tex.height-(int)mesh.vertices[i+2].y;for(int y=a;y<b;y++){partition[y*w+x]++;if(rear)rearMap[y*w+x]=true;}}
   }
   for(int x=0;x<w;x++){double lx=nleft+(left+x+.5)/scale;var crossings=new List<double>();AddCrossings(poly);foreach(var f in front)AddCrossings(f);
    for(int y=0;y<tex.height;y++){Check(partition[y*w+x]==1,"exact complementary mesh partition");double ly=ntop+(double)y/scale;bool expected=Inside(poly,lx,ly);foreach(var f in front)if(expected&&Inside(f,lx,ly))expected=false;
     // Float production versus independent double oracle may disagree only on
     // exact arithmetic boundaries, never a full pixel or a concave interval.
     bool boundary=false;foreach(double edge in crossings)if(Math.Abs(ly-edge)<0.0001)boundary=true;
     if(!boundary)Check(rearMap[y*w+x]==expected,"mesh agrees with independent polygon and foreground oracle");
     if(oldProfile){bool was=ReferencePortalApertureProfile.Column(poly,(float)lx,out float a,out float b);bool previous=was&&y>=Math.Ceiling((a-ntop)*scale)&&y<Math.Ceiling((b-ntop)*scale);Check(rearMap[y*w+x]==previous,"old four profiles remain pixel-identical");}
    }
    void AddCrossings(int[] p){for(int i=0,j=p.Length-2;i<p.Length;j=i,i+=2){double ax=p[i],bx=p[j];if(lx<Math.Min(ax,bx)||lx>=Math.Max(ax,bx))continue;crossings.Add(p[i+1]+(lx-ax)*(p[j+1]-p[i+1])/(bx-ax));}}
   }
  }
 }
 static void NewGeometry(){
  string[] names={"SWS_4701","DFYW_1301","JXMG_0124","JXMG_0125","JXMG_0126","JXMG_0127","HJ_2401","HYYSC_2101","HYYSC_2102","HYYSC_1602"};int[] ids={0x172d125d,0x062d0515,0x1c2d007c,0x1c2d007d,0x1c2d007e,0x1c2d007f,0x0c2d0961,0x1f2d0835,0x1f2d0836,0x1f2d0642};
  for(int i=0;i<ids.Length;i++){string path="object/JZ/JZ_"+names[i]+".gso";Check(ReferencePortalApertureProfile.TryGet(ids[i],path,out var p,out int w,out int h),"audited identity resolves");Check(!ReferencePortalApertureProfile.TryGet(ids[i],"object/JZ/other.gso",out _),"identity rejects other path");
   foreach(int scale in new[]{1,2,4}){Coverage(ids[i],path,h-1,scale,0,0,w-1);GeometryOracle(p,w-1,h-1,scale,3,7);}
  }
  ReferencePortalApertureProfile.TryGet(0x0c2d0961,"object/JZ/JZ_HJ_2401.gso",out var concave);var cuts=new List<float>();ReferencePortalApertureProfile.ColumnIntersections(concave,271.5f,cuts);Check(cuts.Count==4,"HJ real notch has four crossings");Check(!Inside(concave,271.5,357),"HJ real notch stays front");
  int[] synthetic={0,0,10,0,10,10,7,10,7,3,3,3,3,10,0,10};GeometryOracle(synthetic,12,12,4);
  string[] oldNames={"0301","3102","0501","0502"};int[] oldIds={0x182d012d,0x182d0c1e,0x182d01f5,0x182d01f6};for(int i=0;i<4;i++){ReferencePortalApertureProfile.TryGet(oldIds[i],"object/JZ/JZ_DXWG_"+oldNames[i]+".gso",out var p,out int w,out int h);foreach(int scale in new[]{1,2,4})GeometryOracle(p,w-1,h-1,scale,3,7,true);}
 }
 static void NewPair(int id,string path,int ox,int oy,int order,int oz,int w,int h,int pid,string pp,int px,int py,int pz,int pw,int ph,int foot){
  var r=new ResLoader();ResLoader.I=r;r.Portal=new ResLoader.ObjSprite{path=pp,tex=new Texture2D(pw-1,ph-1),frames=15,clipBy=foot};var renderer=new ReferenceLegacyMapObjectRenderer(r);var im=new ResLoader.ObjSprite{path=path,tex=new Texture2D(w-1,h-1),frames=1,hdScale=1};var v=new ReferenceGsmObject{Id=id,SourceX=ox,SourceY=oy,PivotY=order,PivotZ=oz,Width=w,Height=h};var g=new GsmMap.Obj{id=pid,sx=px,sy=py,pz=pz,xl=pw,yl=ph};var map=new GsmMap();map.libObjs.Add(g);
  if(id!=0x172d125d)r.Pkg.Files[path.Replace(".gso",".mask")]=Mask((w+9)/10,((h-65)/10)*10+5);renderer.Prepare(map);Check(renderer.Place(v,im),"new actual world pair is admitted: "+path);int lower=0;foreach(var result in renderer.Results){Check(result.Sprite.texture==im.tex,"new geometry borrows original image");if(id==0x172d125d)Check(result.Order==order||result.Order==Math.Min(order,py+foot-1),"SWS keeps original whole-object foreground pivot depth");if(result.Order<=py+foot-1)lower++;}Check(lower>0,"new rear plane lowered");
  var pixels=new byte[(w-1)*(h-1)];ReferencePortalApertureProfile.TryGet(id,path,out var profile);var foreground=ReferencePortalApertureProfile.PreservedFront(profile);
  foreach(var result in renderer.Results){var mesh=result.Sprite;for(int q=0;q<mesh.vertices.Length;q+=4){int x=(int)mesh.rect.x+(int)mesh.vertices[q].x,a=h-1-(int)mesh.vertices[q].y,b=h-1-(int)mesh.vertices[q+2].y;int original=id==0x172d125d?order:oy+((h-65)/10)*10+10+(x/10/4)*10;for(int y=a;y<b;y++){pixels[y*(w-1)+x]++;if(result.Order>original){bool marked=false;foreach(var f in foreground)if(Inside(f,x+.5,y+.0001))marked=true;Check(marked&&ReferencePortalApertureProfile.ContinuousForeground(id),"only explicit foreground silhouette can move ahead");Check(result.Order==py+foot+1,"foreground promotion limited to paired portal plus one");}else Check(result.Order==original||result.Order==Math.Min(original,py+foot-1),"other source pixels only keep order or lower rear");}}
   // A normal actor in front of every existing foreground band remains in front;
   // a normal actor behind all bands remains behind. No actor order is rewritten.
   int frontActor=Math.Max(oy+h+500,py+foot+500),rearActor=Math.Min(oy-500,py-500);Check(frontActor>result.Order&&rearActor<result.Order,"actor front/back ordering extrema preserved");
  }
  foreach(byte n in pixels)Check(n==1,"three production meshes partition each source pixel exactly once");
  if(ReferencePortalApertureProfile.IsSupportSurface(id)){
   var originals=new List<ReferenceLegacyMapObjectRenderer.Placed>(renderer.Results);var oldV=v;var oldG=g;
   v.SourceX+=137;v.SourceY+=211;v.PivotY+=211;v.PivotZ+=61;g.sx+=137;g.sy+=211;g.pz+=61;map.libObjs[0]=g;renderer.Prepare(map);
   Check(renderer.Place(v,im),"translated support pair with matching elevation remains admitted");Check(renderer.Results.Count==originals.Count,"translated support mesh count stable");
   for(int j=0;j<originals.Count;j++){Check(renderer.Results[j].Sprite==originals[j].Sprite,"support geometry reuses source texture cache");Check(renderer.Results[j].Order==originals[j].Order+211,"support order follows translation only");}
   g.pz+=600;map.libObjs[0]=g;renderer.Prepare(map);Check(!renderer.Place(v,im),"displaced support effect cannot borrow an upper face");v=oldV;g=oldG;map.libObjs[0]=g;renderer.Prepare(map);
   im.nLeft=2;Check(!renderer.Place(v,im),"oversize trimmed upper-face image rejected");im.nLeft=0;
   GameBootstrap.UseIsoY=true;Check(!renderer.Place(v,im),"support repair leaves alternate depth mode unchanged");GameBootstrap.UseIsoY=false;
   Check(!renderer.Place(v,im,ReferenceMapObjectKind.Tile),"support repair never rewrites tile layers");
  }
  r.Portal.path="object/DH/other.gso";renderer.Prepare(map);Check(!renderer.Place(v,im),"portal identity path required");r.Portal.path=pp;r.Portal.clipBy++;renderer.Prepare(map);Check(!renderer.Place(v,im),"actual decoded portal foot required");r.Portal.clipBy=foot;g.xl++;map.libObjs[0]=g;renderer.Prepare(map);Check(!renderer.Place(v,im),"portal logical dimensions required");g.xl=pw;g.id=0x01300c1d;map.libObjs[0]=g;renderer.Prepare(map);Check(!renderer.Place(v,im),"decorative or unrelated model excluded");g.id=pid;map.libObjs[0]=g;renderer.Prepare(map);
  v.Width++;Check(!renderer.Place(v,im),"new door width guarded");v.Width=w;v.Height++;Check(!renderer.Place(v,im),"new door height guarded");v.Height=h;im.clipArr=new[]{1};Check(!renderer.Place(v,im),"new profiles do not override authored clips");im.clipArr=null;map.libObjs.Add(g);renderer.Prepare(map);Check(!renderer.Place(v,im),"new pair ambiguity rejected");map.libObjs.Clear();renderer.Prepare(map);Check(!renderer.Place(v,im),"new unpaired doors unchanged");renderer.Clean();
 }
 static void SupportSurfaces(){
  int[] ids={0x1f2d0835,0x1f2d0836,0x1f2d0642};string[] names={"2101","2102","1602"};
  int[] xs={413,-63,314},ys={158,117,311};
  for(int n=0;n<ids.Length;n++){
   Check(ReferencePortalApertureProfile.TryGet(ids[n],"object/JZ/JZ_HYYSC_"+names[n]+".gso",out var poly,out int w,out int h),"support source identity");
   Check(ReferencePortalApertureProfile.AcceptsAnchor(ids[n],poly,xs[n],ys[n],w,h),"real trigger-supported upper face anchor");
   Check(!ReferencePortalApertureProfile.AcceptsAnchor(ids[n],poly,xs[n],h-1,w,h),"anchor below upper face cannot lower a trunk");
   Check(!ReferencePortalApertureProfile.AcceptsAnchor(ids[n],poly,-110,ys[n],w,h),"unrelated adjoining portal excluded");
   Check(!ReferencePortalApertureProfile.AcceptsAnchor(ids[n],poly,w,ys[n],w,h),"outside right edge excluded");
   Check(!ReferencePortalApertureProfile.CanPair(ids[n],ReferencePortalApertureProfile.PortalId),"other FY family excluded from support surfaces");
   Check(!ReferencePortalApertureProfile.ContinuousForeground(ids[n]),"support surface never promotes original foreground");
  }
  ReferencePortalApertureProfile.TryGet(ids[0],"object/JZ/JZ_HYYSC_2101.gso",out var first);
  Check(!ReferencePortalApertureProfile.AcceptsAnchor(ids[0],first,413,189,477,457),"front lip is not a support-anchor region");
  ReferencePortalApertureProfile.TryGet(ids[1],"object/JZ/JZ_HYYSC_2102.gso",out var adjoining);
  Check(!ReferencePortalApertureProfile.AcceptsAnchor(ids[1],adjoining,-63,158,463,528),"shared-face continuation has bounded height");
  Check(!ReferencePortalApertureProfile.AcceptsAnchor(ids[1],adjoining,-63,-1,463,528),"shared-face continuation has bounded upper edge");
  // The gate/ice and ordinary tree silhouettes remain entirely outside this repair.
  foreach(int id in new[]{0x1b2d04b1,0x1b2d0515,0x0a2d0095,0x0a2d009c})
   Check(!ReferencePortalApertureProfile.IsSupportSurface(id),"solid foreground is never reclassified as a floor");
 }
 static void Backplane(){
  var r=new ResLoader();ResLoader.I=r;r.Portal=new ResLoader.ObjSprite{path="object/DH/DH_FY_0201.gso",tex=new Texture2D(191,176),frames=60,clipBy=176};var renderer=new ReferenceLegacyMapObjectRenderer(r);
  var v=new ReferenceGsmObject{Id=0x142d1a2d,SourceX=4538,SourceY=1634,PivotZ=37,Width=109,Height=169};var im=new ResLoader.ObjSprite{path="object/JZ/JZ_TTD_6701.gso",tex=new Texture2D(108,168),frames=1,clipBy=147,clipWidth=16,clipArr=new[]{-16,-8,0,8,16,24,33},hdScale=1};var gate=new GsmMap.Obj{id=0x013000c9,sx=4527,sy=1593,xl=192,yl=177};var map=new GsmMap();map.libObjs.Add(gate);renderer.Prepare(map);Check(renderer.BackplaneLimit(v,im)==1768,"raised wooden backplane paired with actual blue portal");
  v.SourceX=8607;v.SourceY=1904;v.PivotZ=0;gate.sx=8585;gate.sy=1903;map.libObjs[0]=gate;renderer.Prepare(map);Check(renderer.BackplaneLimit(v,im)==2078,"same asset unraised instance paired without raising any slice");
  im.clipArr[6]=34;Check(renderer.BackplaneLimit(v,im)==int.MaxValue,"different native clipping not reinterpreted");im.clipArr[6]=33;im.path="other.gso";Check(renderer.BackplaneLimit(v,im)==int.MaxValue,"backplane exact path");im.path="object/JZ/JZ_TTD_6701.gso";Check(renderer.BackplaneLimit(v,im,ReferenceMapObjectKind.Tile)==int.MaxValue,"tile cannot pair");GameBootstrap.UseIsoY=true;Check(renderer.BackplaneLimit(v,im)==int.MaxValue,"IsoY unchanged");GameBootstrap.UseIsoY=false;map.libObjs.Add(gate);renderer.Prepare(map);Check(renderer.BackplaneLimit(v,im)==int.MaxValue,"ambiguous backplane portal rejected");map.libObjs.Clear();renderer.Prepare(map);Check(renderer.BackplaneLimit(v,im)==int.MaxValue,"unpaired backplane unchanged");
 }
 static void ArchPair(int id,int ox,int oy,int oz,int px,int py,int pz){
  string path="object/JZ/JZ_DXWG_"+(id==0x182d01f5?"0501":"0502")+".gso";
  Check(ReferencePortalApertureProfile.TryGet(id,path,out var poly,out int width,out int height)&&width==427&&height==452,"arch identity and logical bounds");
  var r=new ResLoader();ResLoader.I=r;r.Portal=new ResLoader.ObjSprite{path="object/DH/DH_FY_1501.gso",tex=new Texture2D(106,193),frames=20,clipBy=179};
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
  NewGeometry(); Backplane(); SupportSurfaces();
  NewPair(0x172d125d,"object/JZ/JZ_SWS_4701.gso",6902,2340,2621,0,401,322,0x0130076d,"object/DH/DH_FY_1901.gso",6931,2450,0,145,156,136);
  NewPair(0x062d0515,"object/JZ/JZ_DFYW_1301.gso",11157,2610,3074,0,695,531,0x013006a5,"object/DH/DH_FY_1701.gso",11393,2748,0,219,282,246);
  NewPair(0x1c2d007c,"object/JZ/JZ_JXMG_0124.gso",12947,3200,3643,0,565,507,0x013006a5,"object/DH/DH_FY_1701.gso",13047,3405,0,219,282,246);
  NewPair(0x1c2d007d,"object/JZ/JZ_JXMG_0125.gso",6484,24,465,0,623,505,0x013006a5,"object/DH/DH_FY_1701.gso",6834,230,0,219,282,246);
  NewPair(0x1c2d007e,"object/JZ/JZ_JXMG_0126.gso",5267,5766,6150,0,549,439,0x013006a5,"object/DH/DH_FY_1701.gso",5556,5910,0,219,282,246);
  NewPair(0x1c2d007f,"object/JZ/JZ_JXMG_0127.gso",8018,5815,6197,0,467,437,0x013006a5,"object/DH/DH_FY_1701.gso",8050,5968,0,219,282,246);
  NewPair(0x0c2d0961,"object/JZ/JZ_HJ_2401.gso",3298,2628,3091,0,615,530,0x013006a5,"object/DH/DH_FY_1701.gso",3494,2740,0,219,282,246);
  NewPair(0x1f2d0835,"object/JZ/JZ_HYYSC_2101.gso",6566,809,1208,0,477,457,0x013006a5,"object/DH/DH_FY_1701.gso",6870,721,0,219,282,246);
  NewPair(0x1f2d0836,"object/JZ/JZ_HYYSC_2102.gso",7042,850,1312,0,463,528,0x013006a5,"object/DH/DH_FY_1701.gso",6870,721,0,219,282,246);
  NewPair(0x1f2d0642,"object/JZ/JZ_HYYSC_1602.gso",12144,3338,3805,-61,777,534,0x013006a5,"object/DH/DH_FY_1701.gso",12349,3464,0,219,282,246);
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
   var r=new ResLoader();ResLoader.I=r;r.Portal=new ResLoader.ObjSprite{path="object/DH/DH_FY_1501.gso",tex=new Texture2D(106,193),frames=20,clipBy=179};var renderer=new ReferenceLegacyMapObjectRenderer(r);
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
