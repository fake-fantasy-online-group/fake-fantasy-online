using System;
using System.Collections.Generic;
namespace UnityEngine {
 public struct Vector2 { public static Vector2 zero=>new Vector2(); public float magnitude=>(float)Math.Sqrt(sqrMagnitude); public static Vector2 operator /(Vector2 v,float n)=>new Vector2(v.x/n,v.y/n); public static Vector2 operator *(Vector2 v,float n)=>new Vector2(v.x*n,v.y*n); public static implicit operator Vector2(Vector3 v)=>new Vector2(v.x,v.y); public static implicit operator Vector3(Vector2 v)=>new Vector3(v.x,v.y); public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} public float sqrMagnitude=>x*x+y*y; public static Vector2 operator +(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y); public static Vector2 operator -(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y); public static Vector2 ClampMagnitude(Vector2 v,float max){float m=(float)Math.Sqrt(v.sqrMagnitude);return m>max?new Vector2(v.x*max/m,v.y*max/m):v;} }
 public struct Vector3 { public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public float x,y,z; public Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;} }
 public class GameObject {public bool activeInHierarchy=true;}
 public class Collider2D {public bool enabled=true;}
 public static class Mathf {public static int Clamp(int v,int min,int max)=>Math.Max(min,Math.Min(max,v));public static float Atan2(float y,float x)=>(float)Math.Atan2(y,x);public const float Deg2Rad=(float)(Math.PI/180); public static float Cos(float v)=>(float)Math.Cos(v); public static float Sin(float v)=>(float)Math.Sin(v); public static int RoundToInt(float v)=>(int)Math.Round(v); public static float Abs(float v)=>Math.Abs(v); public static float Sqrt(float v)=>(float)Math.Sqrt(v); public static float Max(float a,float b)=>Math.Max(a,b);}
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 using UnityEngine;
 public class MonsterEntity { public GameObject gameObject=new GameObject(); public int id,hp=100; public bool NoAttack; public float worldX,worldY; }
 public class RemotePlayer {public GameObject gameObject=new GameObject();public Collider2D PickBoxForIndependentRuntime=new Collider2D();public int id;public bool IsDead,HostileToLocal,SupportHostileToLocal; public float worldX,worldY;}
 public class PlayerController {public int FaceOct=2;public int CurDir;public bool Flip;public static PlayerController I=new PlayerController();public float worldX,worldY,worldW=1000,worldH=1000,ppu=100;public int Stops,MoveRequests;public bool ManualMoving=true;public void StopMove(){Stops++;ManualMoving=false;} }
 public static class TargetSelect { public static object CurrentSourceForIndependentRuntime; }
 public static class SocialClient {public static int SelectedMemberId;public static HashSet<int> Party=new HashSet<int>(); public static bool InParty(int id)=>Party.Contains(id);}
 public static class CharData {public static int hp=100;}
 public static class Win05 {public class SkInfo {public int canEnemy=1,canFriendly,canSelf,castkind;} public static SkInfo Info=new SkInfo();public static SkInfo SkillInfo(int id)=>Info;}
 public class Behavior {public float CastDistance=100; public string InputMode="entity"; public bool TargetSelf,SelfFallback,DeadTargetOnly,AllowsDeadTarget,TargetTeam,TargetEnemy=true;}
 public static class AuthoritativeProfessionSkill {
  public static bool Enabled,Learned=true;public static Behavior Value=new Behavior();public static bool Uses(int id)=>Enabled;public static Behavior Current(int id)=>Enabled?Value:null;public static bool LearnedActive(int id)=>Learned;
  public static string ValidateTarget(int id,object source) {var v=Value;if(source is MonsterEntity m)return v.TargetEnemy&&!v.DeadTargetOnly&&m.hp>0&&!m.NoAttack?null:"illegal monster";if(source is RemotePlayer p)return v.TargetTeam&&SocialClient.InParty(p.id)&&(!p.IsDead||v.AllowsDeadTarget)&&(!v.DeadTargetOnly||p.IsDead)?null:"illegal player";return "illegal";}
 }
 public class NetClient {public static NetClient I=new NetClient();public static int PendingCastSkill=701; public static Vector3 PendingCastGround=new Vector3(33,44);public int Uses,GroundUses,LastId,LastSkill,X,Y;public void UseSkill(int skill,int id){Uses++;LastId=id;LastSkill=skill;}public void UseSkillAt(int skill,int x,int y,int id){GroundUses++;LastSkill=skill;X=x;Y=y;LastId=id;}}
 public partial class CombatWorld {public static CombatWorld I=new CombatWorld();public Dictionary<int,MonsterEntity> monsters=new Dictionary<int,MonsterEntity>();public Dictionary<int,RemotePlayer> players=new Dictionary<int,RemotePlayer>();public Dictionary<int,Vector3> trapPos=new Dictionary<int,Vector3>();public Dictionary<int,GameObject> trapObj=new Dictionary<int,GameObject>(); public int pendSkillId=702,pendGroundSkill=703,queuedSkillId=704,SelectionChanges,Approaches;public bool autoAttacking;public static int TrapPick;public static bool TryTrapAtPointerForSkill(int x,int y,out int id){id=TrapPick;return id!=0;} }
}
namespace UnityEngine {
 public enum TouchPhase {Began,Moved,Stationary,Ended,Canceled}
 public enum KeyCode {Escape}
 public struct Touch {public int fingerId;public Vector2 position;public TouchPhase phase;}
 public static class Time {public static int frameCount;public static float deltaTime;}
 public static class Screen {public static int width=1000,height=1000;}
 public static class Application {public static bool isMobilePlatform=true;}
 public static class Input {public static Touch[] touches=new Touch[0];public static int touchCount=>touches.Length;public static Touch GetTouch(int i)=>touches[i];public static Vector2 mousePosition;public static bool GetMouseButton(int n)=>false;public static bool GetMouseButtonDown(int n)=>false;public static bool GetMouseButtonUp(int n)=>false;public static bool GetKeyDown(KeyCode c)=>false;}
 public struct Rect {public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}public Vector2 center=>new Vector2(x+width/2,y+height/2);public bool Contains(Vector2 p)=>p.x>=x&&p.x<x+width&&p.y>=y&&p.y<y+height;public static bool operator ==(Rect a,Rect b)=>a.x==b.x&&a.y==b.y&&a.width==b.width&&a.height==b.height;public static bool operator !=(Rect a,Rect b)=>!(a==b);public override bool Equals(object o)=>o is Rect r&&this==r;public override int GetHashCode()=>0;}
 public class Camera {public static Camera main=new Camera();public Vector3 WorldToScreenPoint(Vector3 v)=>new Vector3(v.x*100+500,v.y*100+500);public Vector3 ScreenToWorldPoint(Vector3 v)=>new Vector3((v.x-500)/100,(v.y-500)/100);}
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 using UnityEngine;
 public static class MobileClientPresentation {
  public static bool Enabled=true,ModalOpen,DiagramOpen,GroupWheelOpen;
  public static float GroupWheelDeadZone=>24f*Scale;
  public static Vector2 GroupWheelCenter=>PageRect.center;
  public static bool GroupWheelContains(Vector2 p)=>(GroupWheelOpen||GameHud.MobileGroupWheelOpening||GameHud.MobileGroupWheelClosing)&&(p-GroupWheelCenter).sqrMagnitude<=96*96;public static int ActiveSkillButton=-1;public static float Scale=1;public static Rect SafeAreaGui=new Rect(0,0,1000,1000);
  public static Rect JoystickRect=>new Rect(0,800,200,200);public static Rect SkillRect(int k)=>new Rect(700+k*60,800,50,50);public static Rect AttackRect=>new Rect(900,900,60,60);public static Rect PageRect=>new Rect(700,900,60,60);public static Rect MinimapRect=>new Rect(800,0,200,200);public static Rect MapActionRect(int k)=>new Rect(800+k*40,0,40,40);
  public static Vector2 GuiPoint(Vector2 p)=>new Vector2(p.x,Screen.height-p.y);public static bool IsPanelPointer(Vector2 p)=>false;public static bool CancelContains(Vector2 p)=>new Rect(400,400,100,100).Contains(p);
  public static bool IsJoystickRegion(Vector2 p)=>JoystickRect.Contains(GuiPoint(p));public static bool IsPointerOverUi(Vector2 p){p=GuiPoint(p);if(GroupWheelContains(p)||JoystickRect.Contains(p)||AttackRect.Contains(p)||PageRect.Contains(p)||MinimapRect.Contains(p))return true;for(int k=0;k<6;k++)if(SkillRect(k).Contains(p))return true;return false;}
 }
 public partial class GameHud {
  public static GameHud I;public static int[] hotSkill=new int[30],hotKind=new int[30];public bool showHotbar=true,showMinimap=true,diagramOpen;public static bool HotSlotUsable(int s)=>true;public static float SkillCdFrac(int s)=>0;public static void UseHotSlot(int s){}
  private Map guiMinimap;private Host guiHost;public class Map {public void ToggleDiagram(){}public void ZoomMinimap(bool b){}public void CloseDiagram(){}}public class Host {public void ObserveNativeWorldMapRequestForIndependentRuntime(){}}
  public void Tick()=>Update();public void LoseFocus()=>OnApplicationFocus(false);public void Pause()=>OnApplicationPause(true);public void Disable()=>OnDisable();public int Owner=>mobileFinger;public int Page=>mobilePage;public int WheelHover=>mobileGroupWheel.Hovered;
 }
 public static class WorldMapDlg {public static void Toggle(){}}
 public static class MobileHudArtwork {public static void ClearRoundTextures(){}}
 public static class GameSettings {public static bool SmartCast=true;public static void EnsureLoaded(){}}
 public sealed partial class SkillTargeting {
  public enum Aim {Unavailable,Instant,Single,Trap,Circle,Direction}
  public static SkillTargeting I=new SkillTargeting();public static bool Active;public static Aim Mode=Aim.Single;public float rangePx=100;public const float DefaultRange=100;
  private static Aim Resolve(int id,out float range,out float radius,out float angle,out float length,out float width,out bool trap){range=100;radius=angle=length=width=0;trap=false;return Mode;}
  private static bool SelfCenteredCircle(Win05.SkInfo s)=>false;
  private static void Begin(int id,Aim m,float range,float radius,float angle,float length,float width,bool trap){Active=true;I.rangePx=range;}
  private void End(){Active=false;}
 }
 public partial class CombatWorld {public static void CycleNearbyMonster(){}public static void SetTarget(MonsterEntity m){}public static void SetPvpTarget(RemotePlayer p){}}
}
