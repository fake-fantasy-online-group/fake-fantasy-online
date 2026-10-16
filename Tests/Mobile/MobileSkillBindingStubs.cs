using System;
using System.Collections.Generic;
namespace UnityEngine
{
 public class Texture2D {}
 public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
 public struct Rect { public float x,y,width,height; public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;} public Vector2 center=>new Vector2(x+width/2,y+height/2); public float xMax=>x+width; public float yMax=>y+height; public bool Contains(Vector2 p)=>p.x>=x&&p.x<x+width&&p.y>=y&&p.y<y+height; public static bool operator ==(Rect a,Rect b)=>a.x==b.x&&a.y==b.y&&a.width==b.width&&a.height==b.height; public static bool operator !=(Rect a,Rect b)=>!(a==b); public override bool Equals(object o)=>o is Rect r&&this==r; public override int GetHashCode()=>0; }
 public struct Color { public Color(float a,float b,float c){} }
 public enum EventModifiers {Shift,Control}
 public enum EventType { MouseDown,MouseDrag,MouseUp,Repaint,Layout,Used }
 public class Event { public static Event current=new Event {type=EventType.Repaint}; public EventType type; public int button,clickCount; public EventModifiers modifiers; public Vector2 mousePosition; public void Use(){type=EventType.Used;} }
 public enum TouchPhase { Began,Canceled,Ended }
 public struct Touch { public TouchPhase phase; public int fingerId; public Vector2 position; }
 public static class Application { public static bool isMobilePlatform; }
 public static class Time { public static float unscaledTime; public static int frameCount; }
 public static class Screen { public static int width=960,height=540; }
 public static class Input { public static int touchCount; public static Touch touch; public static Touch GetTouch(int n)=>touch; }
 public static class Mathf { public static float Min(float a,float b)=>Math.Min(a,b); public static float Max(float a,float b)=>Math.Max(a,b); public static float Clamp(float v,float lo,float hi)=>Math.Max(lo,Math.Min(v,hi)); }
 public class GUIContent { public static GUIContent none=new GUIContent(); }
 public class GUIStyle { public static GUIStyle none=new GUIStyle(); }
 public static class GUI { public static int depth; public static string faceLabel; public static bool Button(Rect r,GUIContent c,GUIStyle s)=>Button(r,faceLabel); public static bool enabled=true; public static string click; public static bool Button(Rect r,string label){if(enabled && label==click){click=null;return true;}return false;} public static void Box(Rect r,string s){} }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
 using UnityEngine;
 public partial class GameHud { internal static int[] hotSkill=new int[30],hotKind=new int[30],hotServerIcon=new int[30]; internal static Texture2D[] hotIcon=new Texture2D[30]; private static bool hotbarExpanded; private static int hotbarRow; }
 public class NetClient { public static NetClient I=new NetClient(); public int writes,upgrades; public int[] savedIds,savedKinds; public void SetHotbarForIndependentRuntime(int[] a,int[] b,bool c){writes++;savedIds=(int[])a.Clone();savedKinds=(int[])b.Clone();} public void UpgradeSkillForIndependentRuntime(int id){upgrades++;} public static Texture2D LoadItemIcon(int id)=>null; }
 public static class CharData { public struct PetSkill { public int id,icon,level,max; public bool active; public string name,desc; } public static PetSkill[] petSkills; public static Dictionary<int,int> petSkillIcons; public static Dictionary<int,int> skills=new Dictionary<int,int>(); }
 public class ResLoader { public static ResLoader I; public byte[] ReadRes(string s)=>null; }
 public static class DimgDecoder { public struct Result { public bool ok; public Texture2D tex; public float scale; } public static Result Decode(byte[] b,int i)=>default; }
 public static class TexScale { public static void Register(Texture2D t,float s){} }
 public static class MobileClientPresentation { public static bool Enabled=true; public static Rect SafeAreaGui; public static float Scale=1; public static void RemovePanel(object o){} public static void RegisterPanel(object o,Rect r){} }
 public static class PlayerController { public static void SuppressWorldClick(){} }
 public static class ArtworkSkin { public static void ButtonFace(Rect r,string text,float s,bool enabled,bool hover,bool pressed){GUI.faceLabel=text;} }
 public class Rank { public bool CanCast; }
 public class Growth { public Rank SkillRank(int i)=>new Rank(); }
 public static class EndgamePanel { public static bool CubeContainerOpen; public static Growth GrowthState=new Growth(); }
 public static class UITip { public static bool MobileContainsPointer(Vector2 p)=>false; public static void DrawMobilePage(Rect r,string text,float scale,ref int firstRow){} public static void ShowSkill(string s,float f){} }
 public partial class Win05
 {
  public class SkInfo { public int id,icon,tgt,type; }
  public bool IsOpen=>cur!=null;
  public class Def { public float w=283,h=397; }
  private bool petSkillPanelOpen;
  private Texture2D PetSkillIcon(int id,int icon)=>null;
  private void DrawTooltip(string name,string text,Vector2 point){}
  internal void PetActions(int id){cur="petdlg";petSkillPanelOpen=true;SelectForActions(id);}
  internal void ReadPet(Event ev)=>ReadMobilePetSkills(ev,p=>10);
  internal void Cover(bool value){mobileSkillInputBlocked=value;} private float PetSkillExtraH()=>0; private float s=1,winX,winY; private bool positioned,dragging; private string cur="skilldlg"; internal static float skillScroll; private static int skillTab;
  public static SkInfo info=new SkInfo {id=10,icon=2};
  public static int SkillIconId(int id)=>0;
  private static bool SkillUsableForHotbar(SkInfo i)=>i!=null&&i.tgt!=18&&i.type!=19&&i.type!=20;
  private bool UsesGrowthSkill(int id)=>false;
  private SkInfo SkillInfo(int id)=>id==info.id?info:null;
  private Texture2D LoadSkillIcon(int id)=>null;
  private bool CanShowUpgrade(int id)=>true;
  private string BuildSkillTip(SkInfo i,int level)=>"original tip";
  private void RequestSkillTipForIndependentRuntime(string t,float s){}
  internal MobileSkillSelection Selection=>mobileSkills;
  internal void SelectForActions(int id){mobileSkills.Select(id);mobileSkillActions=true;mobileSkillAnchor=new Vector2(100,100);}
  internal bool Holding=>mobileSkillHeld;
  internal void DrawActions()=>DrawMobileSkillActions((x,y,w,h)=>new Rect(x,y,w,h),new Def());
  internal bool Pointer(EventType type,float x,float y)=>MobileSkillTreeInput(new Rect(0,0,230,299),new Event{type=type,mousePosition=new Vector2(x,y)},500);
  internal void LoseFocus()=>OnApplicationFocus(false);
  internal Rect Prepare(Rect safe){MobileClientPresentation.SafeAreaGui=safe;s=1;PrepareMobileSkillWindow(new Def());return new Rect(winX,winY,283*s,397*s);}
  internal Rect BottomButtons=>new Rect(winX+14*s,winY+366*s,254*s,28*s);
 }
}
