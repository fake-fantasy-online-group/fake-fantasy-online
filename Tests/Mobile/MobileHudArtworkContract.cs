using System;
using System.Collections.Generic;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace UnityEngine {
public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
public struct Rect { public float x,y,width,height; public Rect(float x,float y,float width,float height){this.x=x;this.y=y;this.width=width;this.height=height;} public Vector2 center=>new Vector2(x+width*.5f,y+height*.5f); public float xMax=>x+width; public float yMax=>y+height; public bool Contains(Vector2 p)=>p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax; }
public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;} public static Color white=>new Color(1,1,1,1); public static Color operator *(Color x,Color y)=>new Color(x.r*y.r,x.g*y.g,x.b*y.b,x.a*y.a); }
public class Texture2D { public int width=50,height=50; public string name; }
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
public static class Mathf { public static float Min(float a,float b)=>Math.Min(a,b); public static float Max(float a,float b)=>Math.Max(a,b); public static int Max(int a,int b)=>Math.Max(a,b); public static int RoundToInt(float n)=>(int)Math.Round(n); public static int FloorToInt(float n)=>(int)Math.Floor(n); }
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
public static class BitmapFont { public static bool Ready=true; public static float lastScale; public static string lastText; public static float Measure(string text,float scale)=>text.Length*12f*scale; public static void CenterOutline(UnityEngine.Rect r,string text,UnityEngine.Color color,float scale){lastScale=scale;lastText=text;} }
public static class ArtworkSkin { public static bool pressed,selected; public static string text; public static readonly List<string> paths=new List<string>(); public static UnityEngine.Texture2D[] Frames(string path,int count){paths.Add(path);return new[]{new UnityEngine.Texture2D{name=path}};} public static void ButtonFace(UnityEngine.Rect r,string value,float scale,bool enabled=true,bool selected=false,float fontScale=1f,bool pressed=false){text=value;ArtworkSkin.pressed=pressed;ArtworkSkin.selected=selected;} }
}

class MobileHudArtworkContract {
static int checks;
static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
static bool Near(float a,float b)=>Math.Abs(a-b)<.001f;
static void Main(){
 var frames=new UnityEngine.Texture2D[27];
 for(int i=0;i<frames.Length;i++)frames[i]=new UnityEngine.Texture2D{name="entry-"+(i/3)+"-state-"+(i%3)};
 var identities=new HashSet<UnityEngine.Texture2D>();
 for(int i=0;i<9;i++){
  var idle=MobileHudArtwork.ControlFrame(frames,i,false,false);identities.Add(idle);
  Check(idle==frames[i*3],"Idle frame for entry "+i);
  Check(MobileHudArtwork.ControlFrame(frames,i,true,false)==frames[i*3+1],"Hover frame for entry "+i);
  Check(MobileHudArtwork.ControlFrame(frames,i,true,true)==frames[i*3+2],"Pressed frame for entry "+i);
  Check(MobileHudArtwork.ControlFrame(frames,i,false,true)==frames[i*3+2],"Pressed state wins for entry "+i);
 }
 Check(identities.Count==9,"All nine function entries have different artwork");
 Check(MobileHudArtwork.ControlFrame(frames,-1,false,false)==null,"Negative index is safe");
 Check(MobileHudArtwork.ControlFrame(frames,9,false,false)==null,"Past-last index is safe");
 Check(MobileHudArtwork.ControlFrame(null,0,false,false)==null,"Missing frames are safe");
 frames[5]=null;Check(MobileHudArtwork.ControlFrame(frames,1,true,true)==frames[3],"Missing pressed frame uses same entry idle");
 var r=new UnityEngine.Rect(10,20,48,48);
 MobileHudArtwork.ControlFace(r,frames,null,4,1,true,false);
 Check(UnityEngine.GUI.last==frames[13],"Complete bottom icon is preferred");
 Check(Near(UnityEngine.GUI.lastRect.width,44)&&Near(UnityEngine.GUI.lastRect.height,44),"Bottom artwork preserves inner padding");
 var legacy=new UnityEngine.Texture2D[27];for(int i=0;i<27;i++)legacy[i]=new UnityEngine.Texture2D{name="legacy-"+i,width=42,height=55};
 MobileHudArtwork.ControlFace(r,null,legacy,8,1,false,true);
 Check(UnityEngine.GUI.last==legacy[26],"Legacy fallback uses corresponding entry and state");
 Check(Near(UnityEngine.GUI.lastRect.height,44),"Full legacy artwork fits the available height");
 Check(Near(UnityEngine.GUI.lastRect.width/UnityEngine.GUI.lastRect.height,42f/55f),"Full legacy frame retains aspect ratio including decorative bottom");
 MobileHudArtwork.ControlFace(r,frames,legacy,2,1,false,false);
 Check(UnityEngine.GUI.last==legacy[6],"Top menu prefers the original complete top artwork");
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
 Check(UnityEngine.GUI.last.name==MobileHudArtwork.RoundControlPath,"Joystick uses a complete original bronze face, never a partial arc");
 Check(Near(UnityEngine.GUI.lastRect.width,52)&&Near(UnityEngine.GUI.lastRect.center.x,120),"Thumb visual size and travel stay inside original touch region");
 Check(Near(UnityEngine.GUI.lastColor.a,prior.a*.92f),"Thumb face has deliberate opacity");
 Check(UnityEngine.GUI.color.a==prior.a,"Joystick opacity restores GUI color");
 MobileHudArtwork.AttackFace(new UnityEngine.Rect(0,0,96,96),1,false);
 Check(UnityEngine.GUI.last.name==MobileHudArtwork.AttackIconPath&&Near(UnityEngine.GUI.lastRect.width,64),"Attack uses native sword canvas at visible icon size");
 MobileHudArtwork.SkillFace(new UnityEngine.Rect(0,0,64,64),1,false,false);
 Check(UnityEngine.GUI.last.name==MobileHudArtwork.SkillWellPath&&Near(UnityEngine.GUI.lastRect.width,60),"Skill uses thin original square well");
 Check(Near(UnityEngine.GUI.lastColor.a,prior.a*.48f),"Empty skill slots recede");
 MobileHudArtwork.SkillFace(new UnityEngine.Rect(0,0,64,64),1,true,false);
 Check(Near(UnityEngine.GUI.lastColor.a,prior.a*.92f),"Assigned skill slots have stronger contrast");
 Check(UnityEngine.GUI.color.a==prior.a,"Skill opacity restores GUI color");
 Console.WriteLine("PASS mobile HUD artwork: "+checks+" deterministic assertions; real production helper with Unity API stubs, not rendering.");
}
}
