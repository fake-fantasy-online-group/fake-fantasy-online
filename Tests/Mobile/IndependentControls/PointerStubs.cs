// Test boundary types only. Pointer routing, unlock, editor dispatch, and targeting are production sources.
// Geometry/session setters and action publication are explicit counters, tested separately.
using System;
using System.Collections.Generic;
namespace UnityEngine {
 public struct Vector2 { internal static Vector2 zero=>new Vector2(); public float magnitude=>(float)Math.Sqrt(sqrMagnitude); public static Vector2 operator /(Vector2 v,float n)=>new Vector2(v.x/n,v.y/n); public static Vector2 operator *(Vector2 v,float n)=>new Vector2(v.x*n,v.y*n); public static implicit operator Vector2(Vector3 v)=>new Vector2(v.x,v.y); public static implicit operator Vector3(Vector2 v)=>new Vector3(v.x,v.y); public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} public float sqrMagnitude=>x*x+y*y; public static Vector2 operator +(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y); public static Vector2 operator -(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y); internal static Vector2 ClampMagnitude(Vector2 v,float max){float m=(float)Math.Sqrt(v.sqrMagnitude);return m>max?new Vector2(v.x*max/m,v.y*max/m):v;} }
 public struct Vector3 { public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public float x,y,z; public Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;} }
 public class GameObject {public bool activeInHierarchy=true;}
 public class Collider2D {public bool enabled=true;}
 internal static class Mathf {internal static float Clamp01(float v)=>Math.Max(0,Math.Min(1,v));internal static float Lerp(float a,float b,float t)=>a+(b-a)*t;internal static float InverseLerp(float a,float b,float t)=>Clamp01((t-a)/(b-a));internal static int Clamp(int v,int min,int max)=>Math.Max(min,Math.Min(max,v));internal static float Atan2(float y,float x)=>(float)Math.Atan2(y,x);public const float Deg2Rad=(float)(Math.PI/180); internal static float Cos(float v)=>(float)Math.Cos(v); internal static float Sin(float v)=>(float)Math.Sin(v); internal static int RoundToInt(float v)=>(int)Math.Round(v); internal static float Abs(float v)=>Math.Abs(v); internal static float Sqrt(float v)=>(float)Math.Sqrt(v); internal static float Max(float a,float b)=>Math.Max(a,b);}
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 using UnityEngine;
 public class MonsterEntity { public GameObject gameObject=new GameObject(); public int id,hp=100; public bool NoAttack; public float worldX,worldY; }
 public class RemotePlayer {public GameObject gameObject=new GameObject();public Collider2D PickBoxForIndependentRuntime=new Collider2D();public int id;public bool IsDead,HostileToLocal,SupportHostileToLocal; public float worldX,worldY;}
 public class PlayerController {public int LayoutStops;public void StopForMobileLayoutEdit(){LayoutStops++;StopMove();}public int FaceOct=2;public int CurDir;public bool Flip;internal static PlayerController I=new PlayerController();public float worldX,worldY,worldW=1000,worldH=1000,ppu=100;public int Stops,MoveRequests,Pickups;public void PickupNearestForIndependentRuntime(){Pickups++;}public bool ManualMoving=true;public void StopMove(){Stops++;ManualMoving=false;} }
 internal static class TargetSelect { internal static object CurrentSourceForIndependentRuntime; }
 internal static class SocialClient {internal static int SelectedMemberId;internal static HashSet<int> Party=new HashSet<int>(); internal static bool InParty(int id)=>Party.Contains(id);}
 internal static class CharData {internal static int hp=100;}
 internal static class Win05 {public class SkInfo {public int canEnemy=1,canFriendly,canSelf,castkind;} internal static SkInfo Info=new SkInfo();internal static SkInfo SkillInfo(int id)=>Info;}
 public class Behavior {public float CastDistance=100; public string InputMode="entity"; public bool TargetSelf,SelfFallback,DeadTargetOnly,AllowsDeadTarget,TargetTeam,TargetEnemy=true;}
 internal static class AuthoritativeProfessionSkill {
  internal static bool Enabled,Learned=true;internal static Behavior Value=new Behavior();internal static bool Uses(int id)=>Enabled;internal static Behavior Current(int id)=>Enabled?Value:null;internal static bool LearnedActive(int id)=>Learned;
  internal static string ValidateTarget(int id,object source) {var v=Value;if(source is MonsterEntity m)return v.TargetEnemy&&!v.DeadTargetOnly&&m.hp>0&&!m.NoAttack?null:"illegal monster";if(source is RemotePlayer p)return v.TargetTeam&&SocialClient.InParty(p.id)&&(!p.IsDead||v.AllowsDeadTarget)&&(!v.DeadTargetOnly||p.IsDead)?null:"illegal player";return "illegal";}
 }
 public class NetClient {internal static NetClient I=new NetClient();internal static int PendingCastSkill=701; internal static Vector3 PendingCastGround=new Vector3(33,44);public int Uses,GroundUses,LastId,LastSkill,X,Y;public void UseSkill(int skill,int id){Uses++;LastId=id;LastSkill=skill;}public void UseSkillAt(int skill,int x,int y,int id){GroundUses++;LastSkill=skill;X=x;Y=y;LastId=id;}}
 public partial class CombatWorld {internal static CombatWorld I=new CombatWorld();public Dictionary<int,MonsterEntity> monsters=new Dictionary<int,MonsterEntity>();public Dictionary<int,RemotePlayer> players=new Dictionary<int,RemotePlayer>();public Dictionary<int,Vector3> trapPos=new Dictionary<int,Vector3>();public Dictionary<int,GameObject> trapObj=new Dictionary<int,GameObject>(); public int pendSkillId=702,pendGroundSkill=703,queuedSkillId=704,SelectionChanges,Approaches;public bool autoAttacking;internal static int TrapPick;internal static bool TryTrapAtPointerForSkill(int x,int y,out int id){id=TrapPick;return id!=0;} }
}
namespace UnityEngine {
 public enum TouchPhase {Began,Moved,Stationary,Ended,Canceled}
 public enum KeyCode {Escape}
 public struct Touch {public int fingerId;public Vector2 position;public TouchPhase phase;}
 internal static class Time {internal static int frameCount;internal static float deltaTime,unscaledTime;}
 internal static class Screen {internal static int width=1000,height=1000;}
 internal static class Application {internal static bool isMobilePlatform=true;}
 internal static class Input {internal static Touch[] touches=new Touch[0];internal static int touchCount=>touches.Length;internal static Touch GetTouch(int i)=>touches[i];internal static Vector2 mousePosition;internal static bool GetMouseButton(int n)=>false;internal static bool GetMouseButtonDown(int n)=>false;internal static bool GetMouseButtonUp(int n)=>false;internal static bool GetKeyDown(KeyCode c)=>false;}
 public struct Rect {public float x,y,width,height;public float xMax=>x+width;public float yMax=>y+height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}public Vector2 center=>new Vector2(x+width/2,y+height/2);public bool Contains(Vector2 p)=>p.x>=x&&p.x<x+width&&p.y>=y&&p.y<y+height;public static bool operator ==(Rect a,Rect b)=>a.x==b.x&&a.y==b.y&&a.width==b.width&&a.height==b.height;public static bool operator !=(Rect a,Rect b)=>!(a==b);public override bool Equals(object o)=>o is Rect r&&this==r;public override int GetHashCode()=>0;}
 public class Camera {internal static Camera main=new Camera();public Vector3 WorldToScreenPoint(Vector3 v)=>new Vector3(v.x*100+500,v.y*100+500);public Vector3 ScreenToWorldPoint(Vector3 v)=>new Vector3((v.x-500)/100,(v.y-500)/100);}
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 using UnityEngine;
 internal static partial class MobileClientPresentation {
  internal static bool Enabled=true,ModalOpen,DiagramOpen,GroupWheelOpen;
  internal static float GroupWheelDeadZone=>24f*Scale;
  internal static Vector2 GroupWheelCenter=>PageRect.center;
  internal static bool GroupWheelContains(Vector2 p)=>(GroupWheelOpen||GameHud.MobileGroupWheelOpening||GameHud.MobileGroupWheelClosing)&&(p-GroupWheelCenter).sqrMagnitude<=96*96;internal static int ActiveSkillButton=-1;internal static float Scale=1;internal static Rect SafeAreaGui=new Rect(0,0,1000,1000);
  internal static Rect JoystickRect=>new Rect(0,800,200,200);internal static Rect SkillRect(int k)=>new Rect(700+k*60,800,50,50);internal static Rect PickupRect=>new Rect(950,960,44,40);internal static Rect AttackRect=>new Rect(900,900,60,60);internal static Rect PageRect=>new Rect(700,900,60,60);internal static Rect MinimapRect=>new Rect(800,0,200,200);internal static Rect MapActionRect(int k)=>new Rect(800+k*40,0,40,40);
  internal static Vector2 GuiPoint(Vector2 p)=>new Vector2(p.x,Screen.height-p.y);internal static bool PanelPointer;internal static bool IsPanelPointer(Vector2 p)=>PanelPointer || (DiagramOpen && new Rect(650,750,200,250).Contains(GuiPoint(p)));internal static bool CancelContains(Vector2 p)=>new Rect(400,400,100,100).Contains(p);
  internal static bool IsJoystickRegion(Vector2 p)=>!GameplayInputBlocked&&JoystickRect.Contains(GuiPoint(p));internal static bool IsPointerOverUi(Vector2 p){if(GameplayInputBlocked)return true;p=GuiPoint(p);if(MountRect.Contains(p)||HealthPotionRect.Contains(p)||ManaPotionRect.Contains(p)||SitRect.Contains(p))return true;if(GroupWheelContains(p)||JoystickRect.Contains(p)||AttackRect.Contains(p)||PickupRect.Contains(p)||PageRect.Contains(p)||MinimapRect.Contains(p))return true;for(int k=0;k<6;k++)if(SkillRect(k).Contains(p))return true;return false;}
 }
 public partial class GameHud {
  internal static GameHud I;internal static int[] hotSkill=new int[30],hotKind=new int[30];public bool showHotbar=true,showMinimap=true,diagramOpen;internal static bool HotSlotUsable(int s)=>true;internal static float SkillCdFrac(int s)=>0;internal static void UseHotSlot(int s){}
  private Map guiMinimap;private Host guiHost;public class Map {public void ToggleDiagram(){}public void ZoomMinimap(bool b){}public void CloseDiagram(){}}public class Host {public void ObserveNativeWorldMapRequestForIndependentRuntime(){}}
  public void Tick()=>Update();public void LoseFocus()=>OnApplicationFocus(false);public void Pause()=>OnApplicationPause(true);public void Disable()=>OnDisable();public int Owner=>mobileFinger;public int Page=>mobilePage;public int WheelHover=>mobileGroupWheel.Hovered;
 }
 internal static class WorldMapDlg {internal static void Toggle(){}}
 internal static class MobileHudArtwork {internal static void ClearRoundTextures(){}internal static UnityEngine.Texture2D RoundTexture(UnityEngine.Texture2D t)=>t;internal static UnityEngine.Rect Inset(UnityEngine.Rect r,float p)=>r;internal static void Image(UnityEngine.Rect r,UnityEngine.Texture2D t,bool pressed=false,bool enabled=true,float opacity=1){}internal static void Label(UnityEngine.Rect r,string text,float scale){}}
 internal static class GameSettings {internal static bool SmartCast=true;internal static void EnsureLoaded(){}}
 public sealed partial class SkillTargeting {
  public enum Aim {Unavailable,Instant,Single,Trap,Circle,Direction}
  internal static SkillTargeting I=new SkillTargeting();internal static bool Active;internal static Aim Mode=Aim.Single;public float rangePx=100;public const float DefaultRange=100;
  internal static float Range=100, Radius=100, Length;
  internal static Aim Resolve(int id,out float range,out float radius,out float angle,out float length,out float width,out bool trap){range=Range;radius=Radius;length=Length;angle=width=0;trap=false;return Mode;}
  private static bool SelfCenteredCircle(Win05.SkInfo s)=>false;
  private static void Begin(int id,Aim m,float range,float radius,float angle,float length,float width,bool trap){Active=true;I.rangePx=range;}
  private void End(){Active=false;}
 }
 public partial class CombatWorld {internal static void CycleNearbyMonster(){}internal static void SetTarget(MonsterEntity m){}internal static void SetPvpTarget(RemotePlayer p){}}
}

namespace UnityEngine {
 public class Texture2D {internal static Texture2D whiteTexture=new Texture2D();}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}}
 internal static class GUI {internal static Color color;internal static void DrawTexture(Rect r,Texture2D t){}}
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 using UnityEngine;
 internal static class ChatUI {internal static int Feedback;internal static void AddLocal(string s){Feedback++;}}
 internal static class ArtworkSkin {internal static void Panel(Rect r,float s){}internal static void ButtonFace(Rect r,string label,float s){}}
 internal static partial class MobileClientPresentation {
  internal static bool MenuExpanded;internal static Rect MenuToggleRect=>new Rect(0,0,44,44);internal static Rect MenuRect=>new Rect(780,230,180,180);
  internal static bool LayoutEditing,LayoutPanelsOpen,SaveFails;internal static int ClosedFrame=-1,Resets,Saves,Cancels;
  internal static bool GameplayInputBlocked=>LayoutEditing||ClosedFrame==Time.frameCount;
  internal static Rect MountRect=>new Rect(300,750,52,52);internal static Rect HealthPotionRect=>new Rect(800,250,52,52);internal static Rect ManaPotionRect=>new Rect(864,250,52,52);internal static Rect SitRect=>new Rect(300,686,52,52);
  internal static Rect FixedActionRect(MobileFixedAction a)=>a==MobileFixedAction.Mount?MountRect:a==MobileFixedAction.HealthPotion?HealthPotionRect:a==MobileFixedAction.ManaPotion?ManaPotionRect:SitRect;
  internal static Dictionary<MobileControlId,Rect> Edits=new Dictionary<MobileControlId,Rect>();
  internal static Rect ResolveControlRect(MobileControlId id){if(Edits.TryGetValue(id,out var r))return r;if(id==MobileControlId.Joystick)return JoystickRect;if(id==MobileControlId.Attack)return AttackRect;if(id==MobileControlId.Pickup)return PickupRect;if(id==MobileControlId.GroupWheel)return PageRect;if(id>=MobileControlId.Skill0&&id<=MobileControlId.Skill5)return SkillRect((int)id-4);return id==MobileControlId.Mount?MountRect:id==MobileControlId.HealthPotion?HealthPotionRect:id==MobileControlId.ManaPotion?ManaPotionRect:SitRect;}
  internal static float ControlScale(MobileControlId id)=>1f;
  internal static void BeginLayoutEdit(){LayoutEditing=true;ClientInput.CancelAll();}
  internal static bool SaveLayoutEdit(out string error){error=SaveFails?"failed":"";if(SaveFails)return false;Saves++;LayoutEditing=false;ClosedFrame=Time.frameCount;return true;}
  internal static void CancelLayoutEdit(){if(LayoutEditing)Cancels++;LayoutEditing=false;ClosedFrame=Time.frameCount;}
  internal static void ResetLayoutEdit(){Resets++;Edits.Clear();}
  internal static void SetControlCenter(MobileControlId id,Vector2 c){var r=ResolveControlRect(id);r.x=c.x-r.width/2;r.y=c.y-r.height/2;Edits[id]=r;}
  internal static void SetControlScale(MobileControlId id,float s){var r=ResolveControlRect(id);r.width=r.height=100*s;Edits[id]=r;}
 }
 public partial class GameHud {
  private static void MobileFixedFeedback(string message){}
  internal static int[] FixedUses=new int[4];internal static MobileFixedActionState MobileFixedState(MobileFixedAction action)=>new MobileFixedActionState("x",null,true,1,0,1,"");
  internal static void UseMobileFixedAction(MobileFixedAction action){FixedUses[(int)action]++;}
  Texture2D[] cooldownFrames;static int CooldownFrameIndexForIndependentRuntime(float f)=>0;
  static void DrawMobileRoundFace(Rect r,float s,bool occupied,bool pressed){}
  public Rect EditButton(int index)=>MobileLayoutButtonRect(index);public Rect EditSlider=>MobileLayoutSliderRect;
  public bool EditAwaitRelease=>mobileLayoutAwaitRelease;
 }
}
