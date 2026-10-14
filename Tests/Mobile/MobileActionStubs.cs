using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine
{
    public static class Time { public static float unscaledTime; }
    public static class Mathf
    {
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Clamp(float x,float min,float max)=>Math.Max(min,Math.Min(max,x));
    }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public static class MobileClientPresentation { public static bool Enabled=true,ModalOpen,DiagramOpen; }
    public static class ClientInput
    {
        public static bool JoystickHeld,MobileSkillGestureActive,Confirmed;
        public static Vector2 MovementAxis,AimPosition;
        public static void Refresh() { }
        public static void CancelAll() { JoystickHeld=false; MovementAxis=Vector2.zero; SkillTargeting.CancelMobileHotSlot(); }
        public static void SetSkillAim(Vector2 p,bool confirm,bool cancel) { AimPosition=p; MobileSkillGestureActive=true;Confirmed=confirm; }
        public static void ClearSkillAim() { MobileSkillGestureActive=Confirmed=false; }
    }
    public static class CombatWorld { public static int Stops; public static void StopAttack() { Stops++; } }
    public partial class PlayerController
    {
        bool followMode,sitting,moveToTarget;
        int followId,pathIdx,pendHops;
        List<Vector2> path=new List<Vector2>();
        public float worldX=500,worldY=500,worldW=1000,worldH=1000;
        public Vector2 Requested; public int Requests;
        float MoveStepPx()=>400;
        void SetPathTo(float x,float y) { Requested=new Vector2(x,y);Requests++;moveToTarget=true; }
        void ResetMobilePlayerGesture() { }
        public void Tick()=>UpdateMobileMovement();
        public bool Moving=>moveToTarget;
        public void Pause()=>OnApplicationPause(true);
    }
    public sealed partial class SkillTargeting
    {
        public enum Aim { Instant,Single,Circle,Direction,Trap,Unavailable }
        static SkillTargeting I=new SkillTargeting();
        public static bool Active;
        public static Aim NextAim=Aim.Circle;
        public static Aim Resolve(int id,out float r,out float radius,out float angle,out float l,out float w,out bool trap)
        { r=radius=angle=l=w=0;trap=false;return NextAim; }
        void End() { Active=false;ClientInput.ClearSkillAim(); }
    }
    public static class GameHud
    {
        public static int[] hotSkill=new int[30],hotKind=new int[30];
        public static int Uses; public static bool Reject;
        public static void UseHotSlot(int slot)
        {
            Uses++;
            if(!Reject && ClientInput.MobileSkillGestureActive) SkillTargeting.Active=true;
        }
    }
}
