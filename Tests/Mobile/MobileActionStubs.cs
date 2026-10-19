using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine
{
    public struct Vector3 {public float x,y,z; public Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;}public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y); public static implicit operator Vector2(Vector3 v)=>new Vector2(v.x,v.y); public static implicit operator Vector3(Vector2 v)=>new Vector3(v.x,v.y);}
    public class Camera {public static Camera main=new Camera(); public Vector3 WorldToScreenPoint(Vector3 v)=>v;public Vector3 ScreenToWorldPoint(Vector3 v)=>v;}
    public static class Screen {public static int width=960,height=540;}
    public static class Time { public static float unscaledTime; }
    public static class Mathf
    {
        public const float Deg2Rad=(float)(Math.PI/180); public static float Cos(float v)=>(float)Math.Cos(v); public static float Sin(float v)=>(float)Math.Sin(v); public static int RoundToInt(float f)=>(int)Math.Round(f);
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Clamp(float x,float min,float max)=>Math.Max(min,Math.Min(max,x));
    }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public static class MobileClientPresentation { public static bool Enabled=true,ModalOpen,DiagramOpen,GameplayInputBlocked;public static float Scale=1; }
    public static class ClientInput
    {
        public static bool JoystickHeld,MobileSkillGestureActive,Confirmed;
        public static Vector2 MovementAxis,AimPosition;
        public static void Refresh() { }
        public static void CancelAll() { JoystickHeld=false; MovementAxis=Vector2.zero; SkillTargeting.CancelMobileHotSlot(); }
        public static void SetSkillAim(Vector2 p,bool confirm,bool cancel) { AimPosition=p; MobileSkillGestureActive=true;Confirmed=confirm; }
        public static void ClearSkillAim() { MobileSkillGestureActive=Confirmed=false; }
    }
    public static class CombatWorld {
 public static int Stops,Units,Grounds,Traps,NearestTraps;
 public static bool TryMobileNearestTrapCast(int id,float range){NearestTraps++;return true;}public static bool HasTarget=true;public static bool LastNearest,LastDirection; public static int RequiredTarget;
 public static void StopAttack(){Stops++;}
 public static bool TryMobileUnitCast(int id,float range,bool nearest){Units++;LastNearest=nearest;return true;}
 public static bool TryMobileUnitCastAt(int id,int x,int y,float range){Units++;LastNearest=false;return true;}
 public static bool TryMobileTrapCast(int id,int x,int y,float range){Traps++;return true;}
 public static bool TryMobileSkillTarget(int id,float range,bool nearest,out int target,out Vector2 point){target=5;point=new Vector2(10,20);return HasTarget;}
 public static bool TryMobileGroundCast(int id,int x,int y,float range,bool direction,int requiredTargetId=0){Grounds++;LastDirection=direction;RequiredTarget=requiredTargetId;return true;}
 }
 public class NetClient {public static NetClient I=new NetClient();public int Uses,LastTarget;public void UseSkill(int skill,int target){Uses++;LastTarget=target;}}
 public static class Win05 {public class SkInfo{public int canFriendly,canEnemy=1,castkind=1,dist=100;}public static SkInfo Info=new SkInfo();public static SkInfo SkillInfo(int id)=>Info;}
 public static class GameSettings {public static bool SmartCast=true;public static void EnsureLoaded(){}}
 public static class AuthoritativeProfessionSkill {public static bool Uses(int id)=>false;public static bool LearnedActive(int id)=>true;}

    public partial class PlayerController
    {
        public static PlayerController I;public float ppu=100;
        bool followMode,sitting,moveToTarget;
        int followId,pathIdx,pendHops;
        List<Vector2> path=new List<Vector2>();
        public int FaceOct=2;public float worldX=500,worldY=500,worldW=1000,worldH=1000;
        public Vector2 Requested; public int Requests;
        float MoveStepPx()=>400;
        void SetPathTo(float x,float y) { Requested=new Vector2(x,y);Requests++;moveToTarget=true; }
        void ResetMobilePlayerGesture() { }
        public void CancelPickup(){}
        public void ClearFollow(){followId=0;}
        public void StopMove(){path.Clear();pathIdx=pendHops=0;moveToTarget=false;}
        public void Tick()=>UpdateMobileMovement();
        public bool Moving=>moveToTarget;
        public void Pause()=>OnApplicationPause(true);
    }
    public sealed partial class SkillTargeting
    {
        public enum Aim { Instant,Single,Circle,Direction,Trap,Unavailable }
        const float DefaultRange=400;float rangePx=100;
        static SkillTargeting I=new SkillTargeting();
        public static bool Active;
        public static Aim NextAim=Aim.Circle;
        public static bool SelfCenteredCircle(Win05.SkInfo info)=>false;
        public static Aim Resolve(int id,out float r,out float radius,out float angle,out float l,out float w,out bool trap)
        { r=radius=angle=l=w=0;trap=false;return NextAim; }
        public static void Begin(int id,Aim aim,float range,float radius,float angle,float l,float w,bool trap){Active=true;}
        void End() { Active=false;ClientInput.ClearSkillAim(); }
    }
    public static class GameHud
    {
        public static int[] hotSkill=new int[30],hotKind=new int[30];
        public static int Uses; public static bool Reject;
        public static bool MobileHotSlotUsable(int slot)=>(uint)slot<30 && hotSkill[slot]!=0 && !Reject;
        public static void UseHotSlot(int slot)
        {
            Uses++;
            if(!Reject && ClientInput.MobileSkillGestureActive) SkillTargeting.Active=true;
        }
    }
}
