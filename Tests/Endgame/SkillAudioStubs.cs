// Deterministic Unity, asset IO and unrelated actor/state substitutes only.
// Audio decisions, skill receipts, cast dispatch, attack entry and projectiles
// are compiled from their production source files by run-skill-audio-contract.sh.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object value) => value != null; }
    public class MonoBehaviour : Object { public Transform transform = new Transform(); }
    public class Transform : Object { public Vector3 position; }
    public struct Vector2
    {
        public float x,y;
        public Vector2(float x,float y) { this.x=x;this.y=y; }
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x,v.y);
        public static float Distance(Vector2 a,Vector2 b) => (float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y));
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public static Vector3 up => new Vector3(0,1,0);
        public static Vector3 operator +(Vector3 a,Vector3 b) => new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator *(Vector3 v,float n) => new Vector3(v.x*n,v.y*n,v.z*n);
    }
    public static class Time { public static float time,deltaTime; }
    public static class Mathf
    {
        public const float Rad2Deg = 57.29578f;
        public static float Abs(float v) => Math.Abs(v);
        public static float Atan2(float y,float x) => (float)Math.Atan2(y,x);
        public static int RoundToInt(float v) => (int)Math.Round(v);
        public static int Max(int a,int b) => Math.Max(a,b);
        public static float Max(float a,float b) => Math.Max(a,b);
        public static int Clamp(int v,int lo,int hi) => Math.Min(hi,Math.Max(lo,v));
    }
    public static class Debug { public static void LogWarning(string text) { } }
    public static class JsonUtility
    {
        public static T FromJson<T>(string text) => (T)Read(typeof(T),FakeFantasyOnline.Foundation.BoundedJson.Parse(text));
        private static object Read(Type type,object value)
        {
            if(value==null) return null;
            if(type==typeof(int)) return Convert.ToInt32(value);
            if(type.IsArray)
            {
                var list=(IReadOnlyList<object>)value;
                var array=Array.CreateInstance(type.GetElementType(),list.Count);
                for(int i=0;i<list.Count;i++) array.SetValue(Read(type.GetElementType(),list[i]),i);
                return array;
            }
            var result=Activator.CreateInstance(type,true);
            var map=(IReadOnlyDictionary<string,object>)value;
            foreach(var field in type.GetFields())
                if(map.TryGetValue(field.Name,out var v)) field.SetValue(result,Read(field.FieldType,v));
            return result;
        }
    }
}
namespace FakeFantasyOnline.Application
{
    public interface ISkillCastReceiveOwner { }
    public interface ISkillCastReceiveOwnerProvider { }
    public interface IRemotePlayerTailReceiveOwner { }
    public interface IPlayerHairReceiveContext { int SelfEntityId { get; } void ReceiveLocalPlayerHair(int head,int hair); }
    public class CharacterAppearanceRecord { public int AttackVariant,WeaponCategory,AttackDistance,Body,Cap,Backpack,RightWeapon,LeftWeapon,Face; }
    public class WorldPlayerAppearanceState
    {
        public CharacterAppearanceRecord Appearance; public int EntityId,AvatarAura,TitleEffect; public object SoulEffects; public int? GlowMode,GlowUnlock;
    }
    public class WorldRemoteRidingState { public int EntityId,MountModel,MountTianmo,RiderSeat,RideAnchorId,MountSeats;public bool Riding; }
    public class WorldSkillCastState
    {
        public int SkillId,Phase,CasterEntityId,ChantHoldMilliseconds,CooldownMilliseconds,AimX,AimY;
        public int? TargetEntityId;public byte? ScalePercent;public bool HasAim;
        public WorldSkillCastReceipt Receipt { get; } = new WorldSkillCastReceipt();
    }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    using UnityEngine;
    public static class SoundManager
    {
        public static readonly List<(int id,int delay)> Calls=new List<(int,int)>();
        public static void Play(int id,int delay=0) { Calls.Add((id,delay)); }
    }
    public class ReferenceWorldCombatSound
    {
        internal static int SwingSoundId(int category) => CombatWorld.SwingSndForIndependentRuntime(category);
    }
    public static class GameTables { public static string Text;public static string LoadText(string name) => Text; }
    public static class CharData
    {
        public static int gender=1,weaponCType=1,atkDist=75,atkVariant=1,titleEffect,glowMode,glowUnlock;
        public static bool riding;
        public static void SetHair(int head,int hair) { }
        public static void SetAppearance(int body,int cap,int backpack,int right,int left,int face) { }
        public static int GetPref(string key,int fallback) => fallback;
        public static void SetPref(string key,int value) { }
    }
    public class NetClient { public static int SelfEntityId=1,PendingCastSkill;public static Vector3? PendingCastGround; }
    public class PlayerAvatar
    {
        public class Pose { }
        public Pose[] stand=new Pose[5],walk=new Pose[5],sit=new Pose[5],ride=new Pose[5];
        public readonly Pose[,] Attack=new Pose[5,2],Magic=new Pose[5,3];
        public Pose[,] AttackPose(int variant,out int fpd) { fpd=2;return Attack; }
        public Pose[,] MagicPose(out int fpd) { fpd=3;return Magic; }
        public Pose[] PoseAt(int action) => new Pose[5];
        public Pose[,] PoseFrames(int action,out int fpd) { fpd=1;return new Pose[5,1]; }
    }
    public struct ReferenceWorldPlayerFacing
    {
        public int Octant,StoredDirection;public bool FlipX;
        public ReferenceWorldPlayerFacing(int oct,int dir,bool flip,string name) { Octant=oct;StoredDirection=dir;FlipX=flip; }
    }
    public partial class PlayerController : MonoBehaviour
    {
        public static PlayerController I;
        private bool sitting,flip,actIsAtkSwing,actLoop;
        private int DIRS=5,FPD,actAction,actFpd,curDir;
        private float chantUntil,lastHurtT,actElapsed,actDur,actFrameSec;
        private PlayerAvatar.Pose[,] actFrames,chantFrames;
        private PlayerAvatar.Pose[] standFrames,walkFrames,sitFrames,rideFrames;
        private readonly int[] dirByOct={0,1,2,3,4,3,2,1};
        private readonly bool[] flipByOct=new bool[8];
        private readonly string[] OctName=new string[8];
        private void StopChant() { }
        public void SetChargeFx(EffectPlayer fx) { }
        public void SetSoulFx(object fx) { }
    }
    public class RemotePlayer : MonoBehaviour
    {
        public int id,Hp,WeaponCType=16,AtkDistPx=400,Attacks,Casts;
        public void PlayAttack(Vector3? face=null) { Attacks++; }
        public void PlayCast() { Casts++; }
        public void SetHp(int hp) { Hp=hp; }
        public void SetWeapon(int category,int distance) { WeaponCType=category;AtkDistPx=distance; }
        public void SetRiding(bool ride,int model,int tianmo,int seat,int anchor,int seats) { }
        public void SetInvis(bool invisible) { }
    }
    public class MonsterEntity : MonoBehaviour
    {
        public int id,Hp,Attacks,Prepares; public bool NoAttack,IsDying;
        public void PlayAttack() { Attacks++; }
        public void PlayPrepare() { Prepares++; }
        public void SetHp(int hp) { Hp=hp; }
        public void BeginDie() { IsDying=true; }
        public Vector3 CenterPos() => transform.position;
    }
    public partial class CombatWorld : MonoBehaviour
    {
        public static CombatWorld I;
        internal readonly Dictionary<int,RemotePlayer> players=new Dictionary<int,RemotePlayer>();
        internal readonly Dictionary<int,MonsterEntity> monsters=new Dictionary<int,MonsterEntity>();
        private float ppu=100,worldW=1000,worldH=1000;
        private MonsterEntity target;
        public static bool HasTarget => I.target!=null;
        public static Vector3? TargetCenterPos => I.target?.CenterPos();
        public static RemotePlayer FindPlayer(int id) => I.players.TryGetValue(id,out var player)?player:null;
        public static void UpdatePlayerHair(int id,int head,int hair) { }
        public static void UpdatePlayerAppearance(int id,int body,int cap,int backpack,int right,int left,int face,int variant,int aura,object soul,int title,int glow) { }
    }
    public static class CombatNumber
    {
        public static Transform PlayerTransform;public static float Ppu=100;
        public static int Count;
        public static void Spawn(Vector3 at,int damage,int type,float ppu) { Count++; }
    }
    public static class MissFx { public static int Count;public static void Spawn(Vector3 at,float ppu) { Count++; } }
    public class EffectPlayer : Object
    {
        public static float NextScale;
        public static int Impacts,Projectiles;
        public static EffectPlayer Play(Vector3 at,string name,float ppu,int a,int b,bool c,bool d) { Impacts++;return new EffectPlayer(); }
        public static EffectPlayer PlayNamed(Vector3 at,string name,float ppu,int a,int b,bool c,bool d) { Projectiles++;return new EffectPlayer(); }
        public static EffectPlayer PlaySustained(Vector3 at,string name,float ppu,Transform anchor,float duration) => new EffectPlayer();
        public void MoveTo(Transform target,float milliseconds) { }
    }
    public partial class Win05
    {
        public class SkInfo { public int canFriendly,canEnemy=1,shape,dist=400,tgt=1,castkind,radius=100; }
        public static SkInfo SkillInfo(int id) => new SkInfo();
    }
    public partial class SkillEffectDB { public static bool IsPhysical(int id) => id==10201; }
    public static partial class SkillElementDB { public static string ChargeEff(int id) => null; }
    public static class SkillCaster { public static int Casts; public static Vector3? Ground; public static void Cast(int skill,Transform caster,Transform target,float ppu,Vector3? ground,bool support) { Casts++; Ground = ground; } }
    public static class EndgamePanel { public static int RiftMonsterID; public static bool IsRiftMonsterCast(int id) => id == RiftMonsterID; }
    public static class GameBootstrap { public static float CurWorldW = 1000, CurWorldH = 1000; }
    public static class SkillTargeting { public static bool SelfCenteredCircle(Win05.SkInfo info) => false; }
    public static class SkillRangeRing { public static void Show(Vector3 at,int radius,float ppu) { } }
    public static class AuthoritativeProfessionSkill { public static bool Uses(int id) => false;public static bool SelfCentered(int id) => false; }
    public static class GameHud { public static void StartSkillCooldown(int skill,float seconds) { } }
    public sealed partial class ReferenceWorldReceiveOwner : FakeFantasyOnline.Application.IPlayerHairReceiveContext { }
}
