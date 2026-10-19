using System;
using System.Collections.Generic;
namespace UnityEngine {
 public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} public static Vector2 operator -(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y); }
 public struct Rect { public float x,y,width,height; public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;} public float xMax=>x+width; public float yMax=>y+height; public Vector2 center=>new Vector2(x+width/2,y+height/2); public bool Contains(Vector2 p)=>p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax; }
 public struct Color {public Color(float r,float g,float b,float a=1){} public static Color white=>new Color(1,1,1);}
 public struct Color32 {public Color32(byte r,byte g,byte b,byte a){} public static implicit operator Color(Color32 c)=>Color.white;}
 public class MonoBehaviour {public static void DontDestroyOnLoad(object o){}}
 public class GameObject {public GameObject(string n){} public T AddComponent<T>() where T:new()=>new T();}
 public class Texture2D {public static Texture2D whiteTexture=new Texture2D();}
 public static class GUI {public static Color color; public static int depth; public static void DrawTexture(Rect r,Texture2D t){} public static void DrawTextureWithTexCoords(Rect r,Texture2D t,Rect uv){} }
 public enum EventType {Repaint,MouseDown,MouseDrag,MouseUp,Used}
 public class Event {public static Event current=new Event();public EventType type;public int button;public Vector2 mousePosition;public void Use(){type=EventType.Used;}}
 public static class Mathf {public static float Min(float a,float b)=>Math.Min(a,b);public static float Clamp(float v,float a,float b)=>Math.Min(Math.Max(v,a),b);public static float Clamp01(float v)=>Clamp(v,0,1);public static float Round(float v)=>(float)Math.Round(v);}
 public static class Screen {public static int width=960,height=540;public static float dpi=320;public static Rect safeArea=new Rect(0,0,960,540);}
 public static class Input {public static Vector2 mousePosition;}
 public static class PlayerPrefs {static Dictionary<string,float> values=new Dictionary<string,float>();public static float GetFloat(string k,float d)=>values.ContainsKey(k)?values[k]:d;public static void SetFloat(string k,float v){values[k]=v;}public static void Save(){}}
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 using UnityEngine;
 public static class MobileClientPresentation {public static bool Enabled=true;public static Rect SafeAreaGui=>new Rect(Screen.safeArea.x,Screen.height-Screen.safeArea.yMax,Screen.safeArea.width,Screen.safeArea.height);}
 public class ResLoader {public static ResLoader I; public Texture2D LoadGsa(string s)=>null;public Texture2D LoadGsaFrame(string s,int i)=>null;}
 public static class CharData {public class PetSkill {public int id,level;}public static PetSkill[] petSkills;public static int pkMode,petPickQuality;}
 public static class GameSettings {public static bool HotbarBottom,ColorWeak,ShowPlayerNames,ShowMonsterNames,Shadow,Weather,ShowTeamNames,SmartCast,HeadHpBarOn;public static (int,int)[] ResOpts={(960,540),(1280,720),(1920,1080)};public static int ResIndex()=>0;public static void ApplyRes(int i){}public static void Save(){}public static void EnsureLoaded(){}}
 public static class BitmapFont {public static bool Ready=>false;public static void Draw(float x,float y,string text,Color color,float s){}}
 public static class WinZ {public static (int,bool) Frame(object o,Rect r,int i)=>(i,false);}
 public static class SysMsgBox {public static bool Visible;}
 public static class SysOptionDlg {public static void Hide(){}}
 public static class ArtworkSkin {public static void Panel(Rect r,float s){}public static void Row(Rect r,float s,bool hi){}}
 public class MusicManager {public static MusicManager I;public void SetVolume(float f){}}
 public static class SoundManager {public static void Volume(float f){}}
 public static class HeadHpBar {public static bool Show;}
 public static class WeatherFX {public static void Refresh(){}}
 public class NetClient {public static NetClient I;public void SetSmartCastForIndependentRuntime(bool b){}public void SetPkModeForIndependentRuntime(int i){}public void PetPickQualityForIndependentRuntime(byte b){}}
}
