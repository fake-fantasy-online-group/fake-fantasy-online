using System;
using System.Collections.Generic;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace UnityEngine {
public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
public struct Rect { public float x,y,width,height; public Rect(float x,float y,float width,float height){this.x=x;this.y=y;this.width=width;this.height=height;} public Vector2 center=>new Vector2(x+width*.5f,y+height*.5f); public float xMax=>x+width; public float yMax=>y+height; public bool Contains(Vector2 p)=>p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax; }
public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;} public static Color white=>new Color(1,1,1,1); public static Color operator *(Color x,Color y)=>new Color(x.r*y.r,x.g*y.g,x.b*y.b,x.a*y.a); }
public class Object { public static int destroyed; public static void Destroy(Object o){destroyed++;} }
public struct Color32 { public byte r,g,b,a; public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;} }
public enum TextureFormat { RGBA32 }
public enum TextureWrapMode { Clamp }
public enum HideFlags { HideAndDontSave }
public enum FilterMode { Point }
public static class Debug { public static int warnings; public static string lastWarning; public static void LogWarning(string text){warnings++;lastWarning=text;} }
public enum RenderTextureFormat { ARGB32 }
public enum RenderTextureReadWrite { sRGB }
public class RenderTexture : Object { public static RenderTexture active; public static int released; public Texture2D source; public static RenderTexture GetTemporary(int w,int h,int d,RenderTextureFormat f,RenderTextureReadWrite rw)=>new RenderTexture(); public static void ReleaseTemporary(RenderTexture t){released++;} }
public static class Graphics { public static int blits; public static void Blit(Texture2D s,RenderTexture t){blits++;t.source=s;} }
public class Texture2D : Object { public bool isReadable=true; public static bool failReadback; public void ReadPixels(Rect r,int x,int y,bool m){if(failReadback)throw new InvalidOperationException("readback failed");pixels=RenderTexture.active.source.CopyPixels();} public Color32[] CopyPixels()=>(Color32[])(pixels??new Color32[width*height]).Clone(); public int width=50,height=50; public string name; public FilterMode filterMode; public TextureWrapMode wrapMode; public HideFlags hideFlags; private Color32[] pixels; public Texture2D(){} public Texture2D(int w,int h,TextureFormat f,bool m){width=w;height=h;} public Color32[] GetPixels32(){if(!isReadable)throw new InvalidOperationException("not readable");return CopyPixels();} public void SetPixels32(Color32[] p){pixels=(Color32[])p.Clone();} public void Apply(bool m,bool r){} }
public enum ScaleMode { ScaleToFit }
public enum TextAnchor { MiddleCenter }
public enum TextClipping { Clip }
public class Font {}
public class RectOffset { public RectOffset(int l,int r,int t,int b){} }
public class GUIContent { public string text; public static GUIContent none=new GUIContent(); }
public class GUIStyleState { public Color textColor; }
public class GUIStyle { public static GUIStyle none=new GUIStyle(); public GUIStyle(){} public GUIStyle(GUIStyle old){} public Font font; public TextAnchor alignment; public TextClipping clipping; public bool richText,wordWrap; public RectOffset padding; public int fontSize; public GUIStyleState normal=new GUIStyleState(); public Vector2 CalcSize(GUIContent c)=>new Vector2(c.text.Length*fontSize,fontSize); }
public class Event { public static Event current=new Event(); public Vector2 mousePosition; }
public static class Input { public static bool down; public static bool GetMouseButton(int i)=>down; }
public static class Mathf { public static float Clamp01(float v)=>Math.Max(0,Math.Min(1,v)); public static float Sqrt(float v)=>(float)Math.Sqrt(v); public static int Min(int a,int b)=>Math.Min(a,b); public static float Min(float a,float b)=>Math.Min(a,b); public static float Max(float a,float b)=>Math.Max(a,b); public static int Max(int a,int b)=>Math.Max(a,b); public static int RoundToInt(float n)=>(int)Math.Round(n); public static int FloorToInt(float n)=>(int)Math.Floor(n); }
public static class GUI {
 public static Color color=Color.white,lastColor; public static bool enabled=true,throwDraw,clicked; public static Texture2D last; public static Rect lastRect,lastUv; public static int draws; public static GUIStyle lastButtonStyle,lastLabelStyle; public static string lastLabel;
 public static void DrawTexture(Rect r,Texture2D t,ScaleMode mode){draws++;last=t;lastRect=r;lastColor=color;if(throwDraw)throw new InvalidOperationException("draw failed");}
 public static void DrawTextureWithTexCoords(Rect r,Texture2D t,Rect uv){lastUv=uv;DrawTexture(r,t,ScaleMode.ScaleToFit);}
 public static bool Button(Rect r,GUIContent c,GUIStyle s){lastButtonStyle=s;return clicked;}
 public static void Label(Rect r,GUIContent c,GUIStyle s){lastRect=r;lastLabel=c.text;lastLabelStyle=s;}
}
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
public static class NpcChatter { public static UnityEngine.Font CnFont()=>null; }
public static class BitmapFont { public static bool Ready=true; public static UnityEngine.Rect lastRect; public static int labels; public static float lastScale; public static string lastText; public static float Measure(string text,float scale)=>text.Length*12f*scale; public static void CenterOutline(UnityEngine.Rect r,string text,UnityEngine.Color color,float scale){labels++;lastRect=r;lastScale=scale;lastText=text;} }
public static class ArtworkSkin { public static bool pressed,selected; public static string text; public static readonly List<string> paths=new List<string>(); public static UnityEngine.Texture2D[] Frames(string path,int count){paths.Add(path);var a=new UnityEngine.Texture2D[count];for(int i=0;i<count;i++)a[i]=new UnityEngine.Texture2D{name=path+"#"+i};return a;} public static void ButtonFace(UnityEngine.Rect r,string value,float scale,bool enabled=true,bool selected=false,float fontScale=1f,bool pressed=false){text=value;ArtworkSkin.pressed=pressed;ArtworkSkin.selected=selected;} }
}

class MobileHudArtworkContract {
static int checks;
static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
static bool Near(float a,float b)=>Math.Abs(a-b)<.001f;
static void Main(string[] args){
 var frames=new UnityEngine.Texture2D[27];
 for(int i=0;i<frames.Length;i++)frames[i]=new UnityEngine.Texture2D{name="entry-"+(i/3)+"-state-"+(i%3)};
 var identities=new HashSet<UnityEngine.Texture2D>();
 for(int i=0;i<9;i++){
  var idle=MobileHudArtwork.ControlFrame(frames,i,false,false);identities.Add(idle);
  Check(idle==frames[i*3+2],"Idle frame for entry "+i);
  Check(MobileHudArtwork.ControlFrame(frames,i,true,false)==frames[i*3+1],"Hover frame for entry "+i);
  Check(MobileHudArtwork.ControlFrame(frames,i,true,true)==frames[i*3],"Pressed frame for entry "+i);
  Check(MobileHudArtwork.ControlFrame(frames,i,false,true)==frames[i*3],"Pressed state wins for entry "+i);
 }
 Check(identities.Count==9,"All nine function entries have different artwork");
 Check(MobileHudArtwork.ControlFrame(frames,-1,false,false)==null,"Negative index is safe");
 Check(MobileHudArtwork.ControlFrame(frames,9,false,false)==null,"Past-last index is safe");
 Check(MobileHudArtwork.ControlFrame(null,0,false,false)==null,"Missing frames are safe");
 frames[3]=null;Check(MobileHudArtwork.ControlFrame(frames,1,true,true)==frames[5],"Missing pressed frame uses same entry idle");
 var r=new UnityEngine.Rect(10,20,48,48);
 MobileHudArtwork.ControlFace(r,frames,null,4,1,true,false);
 Check(UnityEngine.GUI.last.name==frames[13].name+" (mobile menu)","Complete bottom icon is preferred");
 Check(Near(UnityEngine.GUI.lastRect.width,44)&&Near(UnityEngine.GUI.lastRect.height,44),"Bottom artwork preserves inner padding");
 var legacy=new UnityEngine.Texture2D[27];for(int i=0;i<27;i++)legacy[i]=new UnityEngine.Texture2D{name="legacy-"+i,width=42,height=55};
 MobileHudArtwork.ControlFace(r,null,legacy,8,1,false,true);
 Check(UnityEngine.GUI.last.name==legacy[24].name+" (mobile menu)","Legacy fallback uses corresponding entry and state");
 Check(Near(UnityEngine.GUI.lastRect.height,44),"Full legacy artwork fits the available height");
 Check(UnityEngine.GUI.last.width==1 && UnityEngine.GUI.last.height==1,"Transparent menu source has no fabricated label or frame");
 MobileHudArtwork.ControlFace(r,frames,legacy,2,1,false,false);
 Check(UnityEngine.GUI.last.name==legacy[8].name+" (mobile menu)","Top menu prefers the original complete top artwork");
 var prior=new UnityEngine.Color(.4f,.5f,.6f,.7f);UnityEngine.GUI.color=prior;
 MobileHudArtwork.Image(r,frames[0],true);
 Check(UnityEngine.GUI.lastColor.r<prior.r&&UnityEngine.GUI.lastColor.a==prior.a,"Pressed tint retains alpha");
 Check(UnityEngine.GUI.color.r==prior.r&&UnityEngine.GUI.color.a==prior.a,"Image restores GUI color");
 UnityEngine.GUI.enabled=false;MobileHudArtwork.ControlFace(r,frames,null,3,1,true,true);
 Check(UnityEngine.GUI.lastColor.r<prior.r,"Disabled complete icons dim");
 Check(UnityEngine.GUI.color.r==prior.r&&UnityEngine.GUI.color.a==prior.a,"Complete icons restore GUI color");
 MobileHudArtwork.ControlFace(r,null,legacy,3,1,true,true);
 Check(UnityEngine.GUI.lastColor.r<prior.r,"Disabled legacy icons dim");
 Check(UnityEngine.GUI.color.r==prior.r&&UnityEngine.GUI.color.a==prior.a,"Legacy icons restore GUI color");
 UnityEngine.GUI.enabled=true;UnityEngine.GUI.throwDraw=true;
 try{MobileHudArtwork.Image(r,frames[0],true);}catch(InvalidOperationException){}
 Check(UnityEngine.GUI.color.r==prior.r&&UnityEngine.GUI.color.a==prior.a,"Image restores GUI color on draw exception");
 try{MobileHudArtwork.ControlFace(r,null,legacy,0,1,true,true);}catch(InvalidOperationException){}
 Check(UnityEngine.GUI.color.r==prior.r&&UnityEngine.GUI.color.a==prior.a,"Legacy image restores GUI color on draw exception");
 UnityEngine.GUI.throwDraw=false;int draws=UnityEngine.GUI.draws;MobileHudArtwork.Image(r,null);
 Check(UnityEngine.GUI.draws==draws,"Missing artwork does not draw a default skin box");
 MobileHudArtwork.Label(r,"普攻",1);Check(BitmapFont.lastText=="普攻"&&Near(BitmapFont.lastScale,1),"Mobile labels use native bitmap font");
 MobileHudArtwork.Label(r,"移到这里取消施法",1);Check(BitmapFont.lastScale*12*8<=r.width-10,"Long labels fit their reserved width");
 BitmapFont.Ready=false;MobileHudArtwork.Label(r,"功能",1);
 Check(UnityEngine.GUI.lastLabel=="功能"&&UnityEngine.GUI.lastLabelStyle.alignment==UnityEngine.TextAnchor.MiddleCenter,"Fallback Chinese text is centered");
 Check(UnityEngine.GUI.lastLabelStyle.fontSize==12,"Fallback size follows HUD scale");
 UnityEngine.GUI.clicked=true;UnityEngine.Input.down=true;UnityEngine.Event.current.mousePosition=new UnityEngine.Vector2(25,35);
 Check(MobileHudArtwork.TextButton(r,"功能",1),"Text button preserves IMGUI activation");
 Check(UnityEngine.GUI.lastButtonStyle==UnityEngine.GUIStyle.none,"Text button never overlays default GUI skin");
 Check(ArtworkSkin.pressed&&ArtworkSkin.selected,"Text button face receives pressed feedback");
 foreach(float scale in new[]{.5f,1f,2f}){var tiny=new UnityEngine.Rect(0,0,3*scale,2*scale);var inner=MobileHudArtwork.Inset(tiny,6*scale);Check(inner.width>=0&&inner.height>=0&&inner.xMax<=tiny.xMax&&inner.yMax<=tiny.yMax,"Inset remains bounded at "+scale);}
 MobileHudArtwork.Joystick(new UnityEngine.Rect(0,0,160,160),new UnityEngine.Vector2(1,0),1);
 Check(UnityEngine.GUI.last.name==MobileHudArtwork.RoundControlPath+"#0","Joystick uses a complete original bronze face, never a partial arc");
 Check(Near(UnityEngine.GUI.lastRect.width,52)&&Near(UnityEngine.GUI.lastRect.center.x,120),"Thumb visual size and travel stay inside original touch region");
 Check(Near(UnityEngine.GUI.lastColor.a,prior.a*.92f),"Thumb face has deliberate opacity");
 Check(UnityEngine.GUI.color.a==prior.a,"Joystick opacity restores GUI color");
 MobileHudArtwork.AttackFace(new UnityEngine.Rect(0,0,96,96),1,false);
 Check(UnityEngine.GUI.last.name==MobileHudArtwork.AttackIconPath+"#0"&&Near(UnityEngine.GUI.lastRect.width,64),"Attack uses native sword canvas at visible icon size");
 MobileHudArtwork.SkillFace(new UnityEngine.Rect(0,0,64,64),1,false,false);
 Check(UnityEngine.GUI.last.name==MobileHudArtwork.SkillWellPath+"#0"&&Near(UnityEngine.GUI.lastRect.width,60),"Skill uses thin original square well");
 Check(Near(UnityEngine.GUI.lastColor.a,prior.a*.48f),"Empty skill slots recede");
 MobileHudArtwork.SkillFace(new UnityEngine.Rect(0,0,64,64),1,true,false);
 Check(Near(UnityEngine.GUI.lastColor.a,prior.a*.92f),"Assigned skill slots have stronger contrast");
 Check(UnityEngine.GUI.color.a==prior.a,"Skill opacity restores GUI color");
 UnityEngine.Input.down=false;UnityEngine.Event.current.mousePosition=new UnityEngine.Vector2(-1,-1);
 Check(MobileHudArtwork.MenuButton(r,1,false),"Icon menu retains IMGUI release activation");
 Check(UnityEngine.GUI.last.name==MobileHudArtwork.MenuTogglePath+"#4","Collapsed menu selects down glyph rotated left");
 MobileHudArtwork.MenuButton(r,1,true);Check(UnityEngine.GUI.last.name==MobileHudArtwork.MenuTogglePath+"#0","Expanded menu selects up glyph rotated right");
 // All interaction states retain authored direction and original hit behavior.
 for(int state=0;state<4;state++)for(int expanded=0;expanded<2;expanded++){
  UnityEngine.GUI.enabled=state!=3;UnityEngine.Input.down=state==2;
  UnityEngine.Event.current.mousePosition=state==1||state==2?new UnityEngine.Vector2(25,35):new UnityEngine.Vector2(-1,-1);
  MobileHudArtwork.MenuButton(r,1,expanded==1);
  Check(UnityEngine.GUI.last.name==MobileHudArtwork.MenuTogglePath+"#"+((expanded==1?0:4)+state),"Arrow direction for interaction state "+state);
 }
 UnityEngine.GUI.enabled=true;UnityEngine.Input.down=false;
 BitmapFont.Ready=true;
 foreach(float scale in new[]{.5f,.73f,1f,1.5f,2f,3f})for(int state=0;state<4;state++){
  var target=new UnityEngine.Rect(13,17,48*scale,48*scale);
  UnityEngine.GUI.enabled=state!=3;int labels=BitmapFont.labels;
  MobileHudArtwork.ControlFace(target,frames,legacy,7,scale,state==1,state==2);
  var icon=UnityEngine.GUI.lastRect;var label=BitmapFont.lastRect;
  Check(BitmapFont.labels==labels+1&&BitmapFont.lastText=="货架","Rack alone draws the standalone caption");
  Check(Near(BitmapFont.lastScale,scale)&&Near(label.center.x,target.center.x),"Rack text uses centered native size");
  Check(icon.yMax<=label.y&&label.yMax<=target.yMax&&icon.x>=target.x&&icon.xMax<=target.xMax,"Rack icon and label stay separate inside the hit target");
  for(int index=0;index<9;index++)if(index!=7){
   MobileHudArtwork.ControlFace(target,frames,legacy,index,scale,state==1,state==2);
   Check(BitmapFont.labels==labels+1&&Near(UnityEngine.GUI.lastRect.height,44*scale),"Other menu faces stay unchanged");
  }
 }
 UnityEngine.GUI.enabled=true;
 BitmapFont.Ready=false;MobileHudArtwork.ControlFace(r,null,null,7,1,false,false);
 Check(UnityEngine.GUI.lastLabel=="货架"&&UnityEngine.GUI.lastLabelStyle.fontSize==12,"Missing rack texture/font retains readable fallback caption");
 BitmapFont.Ready=true;
 // Regression fixtures use Unity's bottom-origin pixels, including odd native height.
 for(int state=0;state<8;state++){
  var native=new UnityEngine.Texture2D(20,19,UnityEngine.TextureFormat.RGBA32,false){name="toggle-fixture"};
  var raw=new UnityEngine.Color32[380];for(int i=0;i<raw.Length;i++)raw[i]=new UnityEngine.Color32(90,30,10,255);
  int gx=state%4==0?8:7,gy=state%4==0?7:6;
  for(int y=0;y<8;y++)for(int x=0;x<7;x++)raw[(18-gy-y)*20+gx+x]=new UnityEngine.Color32((byte)(220+x),200,(byte)(100+y),255);
  native.SetPixels32(raw);var fixedToggle=MobileHudArtwork.MenuToggleTexture(native,state);var output=fixedToggle.GetPixels32();
  Check(fixedToggle.width==40&&fixedToggle.height==38,"Toggle permits half-native-pixel alignment for state "+state);
  int minX=40,maxX=0,minY=38,maxY=0;
  for(int y=0;y<38;y++)for(int x=0;x<40;x++)if(output[y*40+x].g==200){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
  Check(minX==12&&maxX==27&&minY==12&&maxY==25,"Real glyph bounds centered in state "+state);
  foreach(float scale in new[]{.5f,.73f,1f,1.25f,1.5f,2f,3f}){
   var target=new UnityEngine.Rect(13.25f,17.75f,48*scale,48*scale);var fit=MobileHudArtwork.Fit(MobileHudArtwork.Inset(target,6*scale),40,38);
   Check(Near(fit.x+(minX+maxX+1)*.5f/40*fit.width,target.center.x)&&Near(fit.y+(minY+maxY+1)*.5f/38*fit.height,target.center.y),"Visible glyph alignment at fractional scale "+scale);
  }
  Check(native.GetPixels32()[(18-gy)*20+gx].g==200&&output[0].g==30,"Native source and original button border preserved");
  for(int y=0;y<8;y++)for(int x=0;x<7;x++)for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++){
   var rotated=output[(37-(12+x*2+dy))*40+12+(7-y)*2+dx];
   Check(rotated.r==220+x&&rotated.b==100+y,"Only glyph pixels rotate clockwise without mirroring");
  }
  for(int y=0;y<38;y++)for(int x=0;x<40;x++){
   int sx=x/2,sy=y/2,top=18-sy;
   bool oldGlyph=sx>=gx&&sx<gx+7&&top>=gy&&top<gy+8;
   bool newGlyph=x>=12&&x<28&&y>=12&&y<26;
   if(!oldGlyph&&!newGlyph)Check(output[y*40+x].r==raw[sy*20+sx].r&&output[y*40+x].g==raw[sy*20+sx].g,"Background stays upright and unchanged");
  }
 }
 var labeled=new UnityEngine.Texture2D(42,55,UnityEngine.TextureFormat.RGBA32,false){name="label-fixture"};
 var labeledPixels=new UnityEngine.Color32[42*55];
 for(int y=0;y<55;y++)for(int x=0;x<42;x++)labeledPixels[y*42+x]=new UnityEngine.Color32((byte)(y<19?255:0),(byte)(y>=19?200:0),0,255);
 labeled.SetPixels32(labeledPixels);var onlyIcon=MobileHudArtwork.MenuIconTexture(labeled);
 Check(onlyIcon.width==42&&onlyIcon.height==36,"Icon copy excludes all 19 bottom label/box rows");
 foreach(var pixel in onlyIcon.GetPixels32())Check(pixel.r==0,"No label or label-box pixels survive");
 Check(labeled.GetPixels32()[0].r==255,"Original desktop label pixels unchanged");
 MobileHudArtwork.ClearRoundTextures();
 var source=new UnityEngine.Texture2D(16,16,UnityEngine.TextureFormat.RGBA32,false);
 var pixels=new UnityEngine.Color32[256];for(int i=0;i<pixels.Length;i++)pixels[i]=new UnityEngine.Color32(80,120,160,200);source.SetPixels32(pixels);
 var round=MobileHudArtwork.RoundTexture(source);var masked=round.GetPixels32();
 Check(masked[0].a==0&&masked[15].a==0&&masked[240].a==0&&masked[255].a==0,"All square icon corners are transparent");
 Check(masked[8*16+8].a==200,"Center retains original alpha");
 Check(masked[7].a>0&&masked[7].a<200,"Circle edge is feathered");
 Check(masked[8*16+8].r==80&&source.GetPixels32()[0].a==200,"Original RGB and borrowed source are preserved");
 Check(object.ReferenceEquals(round,MobileHudArtwork.RoundTexture(source)),"Repeated GUI events reuse mask texture");
 Check(MobileHudArtwork.RoundTexture(null)==null,"Empty shortcut is safe");
 int destroyed=UnityEngine.Object.destroyed;MobileHudArtwork.ClearRoundTextures();Check(UnityEngine.Object.destroyed>=destroyed+1,"HUD cleanup destroys only owned copy");
 Check(!object.ReferenceEquals(round,MobileHudArtwork.RoundTexture(source)),"Cleared cache recreates mask");
 var wide=new UnityEngine.Texture2D(24,16,UnityEngine.TextureFormat.RGBA32,false);var circle=MobileHudArtwork.RoundTexture(wide);Check(circle.width==16&&circle.height==16,"Non-square art is center-cropped to a true circle, never an ellipse");
 var gpu=new UnityEngine.Texture2D(16,16,UnityEngine.TextureFormat.RGBA32,false){isReadable=false};gpu.SetPixels32(pixels);
 var oldTarget=new UnityEngine.RenderTexture();UnityEngine.RenderTexture.active=oldTarget;
 int releases=UnityEngine.RenderTexture.released;var gpuCircle=MobileHudArtwork.RoundTexture(gpu);
 Check(gpuCircle.GetPixels32()[0].a==0&&gpuCircle.GetPixels32()[136].a==200,"GPU-only source uses readback then same circular mask");
 Check(UnityEngine.Graphics.blits==1&&UnityEngine.RenderTexture.released==releases+1&&UnityEngine.RenderTexture.active==oldTarget,"Readback restores active target and releases temporary");
 var failed=new UnityEngine.Texture2D(16,16,UnityEngine.TextureFormat.RGBA32,false){isReadable=false,name="failed-icon"};UnityEngine.Texture2D.failReadback=true;
 int warnings=UnityEngine.Debug.warnings;
 Check(MobileHudArtwork.RoundTexture(failed)==null&&UnityEngine.RenderTexture.active==oldTarget&&UnityEngine.RenderTexture.released==releases+2,"Readback failure cleans up and returns no square without interrupting HUD");
 Check(UnityEngine.Debug.warnings==warnings+1&&UnityEngine.Debug.lastWarning.Contains("failed-icon"),"Failed icon records one explicit diagnostic");
 int blits=UnityEngine.Graphics.blits;
 Check(MobileHudArtwork.RoundTexture(failed)==null&&UnityEngine.Graphics.blits==blits&&UnityEngine.Debug.warnings==warnings+1,"Cached failure does not retry or spam each GUI event");
 UnityEngine.Texture2D.failReadback=false;MobileHudArtwork.ClearRoundTextures();
 Check(MobileHudArtwork.RoundTexture(failed)!=null&&UnityEngine.Graphics.blits==blits+1,"Cache reset allows failed icon to recover");
 var failedMenu=new UnityEngine.Texture2D(20,19,UnityEngine.TextureFormat.RGBA32,false){isReadable=false,name="failed-menu"};
 UnityEngine.Texture2D.failReadback=true;int menuWarnings=UnityEngine.Debug.warnings;
 Check(MobileHudArtwork.MenuToggleTexture(failedMenu,0)==null,"Menu readback failure does not abort the HUD");
 int menuBlits=UnityEngine.Graphics.blits;
 Check(MobileHudArtwork.MenuToggleTexture(failedMenu,0)==null&&UnityEngine.Graphics.blits==menuBlits&&UnityEngine.Debug.warnings==menuWarnings+1,"Menu failure is cached without readback/log spam");
 UnityEngine.Texture2D.failReadback=false;MobileHudArtwork.ClearRoundTextures();
 Check(MobileHudArtwork.MenuToggleTexture(failedMenu,0)!=null,"Menu cache reset retries recovered GPU source");
 if(args.Length>=2){
  using(var input=new System.IO.BinaryReader(System.IO.File.OpenRead(args[0]))){
   int w=input.ReadInt32(),h=input.ReadInt32();var fixture=new UnityEngine.Texture2D(w,h,UnityEngine.TextureFormat.RGBA32,false);var data=new UnityEngine.Color32[w*h];
   for(int i=0;i<data.Length;i++)data[i]=new UnityEngine.Color32(input.ReadByte(),input.ReadByte(),input.ReadByte(),input.ReadByte());fixture.SetPixels32(data);
   var transformed=args.Length==3 ? (args[2]=="icon" ? MobileHudArtwork.MenuIconTexture(fixture) : MobileHudArtwork.MenuToggleTexture(fixture,int.Parse(args[2]))) : MobileHudArtwork.RoundTexture(fixture);
   var output=transformed.GetPixels32();using(var writer=new System.IO.BinaryWriter(System.IO.File.Create(args[1])))foreach(var pixel in output){writer.Write(pixel.r);writer.Write(pixel.g);writer.Write(pixel.b);writer.Write(pixel.a);}
  }
 }
 Console.WriteLine("PASS mobile HUD artwork: "+checks+" deterministic assertions; real production helper with Unity API stubs, not rendering.");
}
}
