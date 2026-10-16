using System;
using System.Collections.Generic;
namespace UnityEngine {
 public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} public float sqrMagnitude=>x*x+y*y; public static Vector2 operator +(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y); public static Vector2 operator -(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y); public static Vector2 ClampMagnitude(Vector2 v,float max){float m=(float)Math.Sqrt(v.sqrMagnitude);return m>max?new Vector2(v.x*max/m,v.y*max/m):v;} }
 public struct Vector3 {public float x,y,z; public Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;} }
 public class GameObject {public bool activeInHierarchy=true;}
 public class Collider2D {public bool enabled=true;}
 public static class Mathf {public static int RoundToInt(float v)=>(int)Math.Round(v); public static float Abs(float v)=>Math.Abs(v); public static float Sqrt(float v)=>(float)Math.Sqrt(v); public static float Max(float a,float b)=>Math.Max(a,b);}
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 using UnityEngine;
 public class MonsterEntity { public GameObject gameObject=new GameObject(); public int id,hp=100; public bool NoAttack; public float worldX,worldY; }
 public class RemotePlayer {public GameObject gameObject=new GameObject();public Collider2D PickBoxForIndependentRuntime=new Collider2D();public int id;public bool IsDead,HostileToLocal,SupportHostileToLocal; public float worldX,worldY;}
 public class PlayerController {public static PlayerController I=new PlayerController();public float worldX,worldY,worldW=1000,worldH=1000,ppu=100;public int Stops,MoveRequests;public bool ManualMoving=true;public void StopMove(){Stops++;ManualMoving=false;} }
 public static class TargetSelect { public static object CurrentSourceForIndependentRuntime; }
 public static class SocialClient {public static int SelectedMemberId;public static HashSet<int> Party=new HashSet<int>(); public static bool InParty(int id)=>Party.Contains(id);}
 public static class CharData {public static int hp=100;}
 public static class Win05 {public class SkInfo {public int canEnemy=1,canFriendly,canSelf;} public static SkInfo Info=new SkInfo();public static SkInfo SkillInfo(int id)=>Info;}
 public class Area {public float Radius=500,Length; }
 public static class SkillTargeting { public enum Aim {Single,Circle,Direction} public static Aim Resolve(int id,out float range,out float radius,out float angle,out float length,out float width,out bool trap) {var b=AuthoritativeProfessionSkill.Value;range=b.CastDistance;radius=b.Area.Radius;length=b.Area.Length;angle=120;width=0;trap=false;return b.InputMode=="direction"?Aim.Direction:b.InputMode=="point"?Aim.Circle:Aim.Single;} }
 public class Behavior {public Area Area=new Area();public float CastDistance=100; public string InputMode="entity"; public bool TargetSelf,SelfFallback,DeadTargetOnly,AllowsDeadTarget,TargetTeam,TargetEnemy=true;}
 public static class AuthoritativeProfessionSkill {
  public static bool Enabled,Learned=true;public static Behavior Value=new Behavior();public static bool Uses(int id)=>Enabled;public static Behavior Current(int id)=>Enabled?Value:null;public static bool LearnedActive(int id)=>Learned;
  public static string ValidateTarget(int id,object source) {var v=Value;if(source is MonsterEntity m)return v.TargetEnemy&&!v.DeadTargetOnly&&m.hp>0&&!m.NoAttack?null:"illegal monster";if(source is RemotePlayer p)return v.TargetTeam&&SocialClient.InParty(p.id)&&(!p.IsDead||v.AllowsDeadTarget)&&(!v.DeadTargetOnly||p.IsDead)?null:"illegal player";return "illegal";}
 }
 public class NetClient {public static NetClient I=new NetClient();public static int PendingCastSkill=701; public static Vector3 PendingCastGround=new Vector3(33,44);public int Uses,GroundUses,LastId,LastSkill,X,Y;public void UseSkill(int skill,int id){Uses++;LastId=id;LastSkill=skill;}public void UseSkillAt(int skill,int x,int y,int id){GroundUses++;LastSkill=skill;X=x;Y=y;LastId=id;}}
 public partial class CombatWorld {public static CombatWorld I=new CombatWorld();public Dictionary<int,MonsterEntity> monsters=new Dictionary<int,MonsterEntity>();public Dictionary<int,RemotePlayer> players=new Dictionary<int,RemotePlayer>();public Dictionary<int,Vector3> trapPos=new Dictionary<int,Vector3>();public Dictionary<int,GameObject> trapObj=new Dictionary<int,GameObject>(); public int pendSkillId=702,pendGroundSkill=703,queuedSkillId=704,SelectionChanges,Approaches;public bool autoAttacking;public static int TrapPick;public static bool TryTrapAtPointerForSkill(int x,int y,out int id){id=TrapPick;return id!=0;} }
}
