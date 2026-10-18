using System;
namespace UnityEngine {
 public class Object { public static implicit operator bool(Object o)=>o!=null; }
 public struct Vector3 { }
 public static class Time { public static float time, deltaTime; }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 using UnityEngine;
 public class MonsterEntity:Object { public int id=1; public bool DyingForIndependentRuntime; public Vector3 CenterPos()=>default; }
 public partial class RemotePlayer:Object { }
 static class TargetSelect { public static void Select(object o) {} public static void Clear() {} }
 static class UIScale { public static float Factor=1; }
 static class CharData { public static bool riding; }
 class NetClient:Object { public static NetClient I=new NetClient(); public bool ConnectedForIndependentRuntime=true; public int Requests; public void AttackForIndependentRuntime(int id) { Requests++; } }
 public partial class PlayerController:Object {
  public static PlayerController I=new PlayerController(); public bool IsMoving;
  public int Swings, Paths; public void PlayAttackForIndependentRuntime(Vector3 p,float seconds) { Swings++; }
  bool mobileSteering,sitting,moveToTarget; float chantUntil;
  System.Collections.Generic.List<int> path=new System.Collections.Generic.List<int>();
  void SetPathTo(int x,int y,int mode){ Paths++;moveToTarget=true;path.Add(1); }
  void ObserveCombatApproachForPlugins(){}
  public void Steer(bool value){mobileSteering=value;moveToTarget=value;path.Clear();if(value)path.Add(1);}
  public bool HasPath=>path.Count>0;
 }
 public partial class CombatWorld:Object {
  public static CombatWorld I; MonsterEntity target; public bool Range=true, Sight=true;
  // Remote target type comes from the production targeting carrier.
  RemotePlayer pvpTarget;
  long pluginAttackIntentRevision,pluginSkillIntentRevision;
  float queuedRangePx,pendAwaitUntil,pendSkillRangeU,losTipUntil;
  static float AtkInterval=1f;
  Action<string,float> skillTipForIndependentRuntime=(text,scale)=>{};
  static void Ensure(){if(I==null)I=new CombatWorld();}
  void ResumeQueuedSkillForPlugins(int s,float r){} void AdvanceGroundCastForIndependentRuntime(){}
  bool TargetWithin(float r)=>Range; void CastAtTarget(int s){} bool InRange()=>Range;
  bool LosClear()=>Sight;void AdvancePvpAttackForIndependentRuntime(){} void PlayAttackProjectile(){}
  public static bool WantsApproach()=>I.autoAttacking&&!I.Range;
  public static bool TargetWorldPx(out int x,out int y){x=y=100;return true;}
  public static bool Engaged=>I.autoAttacking;
  public void Tick(float dt){Time.deltaTime=dt;Time.time+=dt;Update();}
  public static CombatWorld Reset(){Time.time=0;NetClient.I=new NetClient();PlayerController.I=new PlayerController();I=new CombatWorld();CharData.riding=false;SetTarget(new MonsterEntity());return I;}
  public int State=> (int)atkState; public float Phase=>atkPhaseT;
  public void Pending(){pendSkillId=9;queuedSkillId=8;pendGroundSkill=7;pendAwaitUntil=100;}
  public bool PendingCleared=>pendSkillId==0&&queuedSkillId==0&&pendGroundSkill==0;
  public void Repeat()=>SetTarget(target);
  public void Die()=>target.DyingForIndependentRuntime=true;
  public void CheckPvpRepeat(){var rp=new RemotePlayer();SetPvpTarget(rp);atkPhaseT=.42f;SetPvpTarget(rp);if(atkPhaseT!=.42f)throw new Exception("PvP repeat reset");}

 }
 public partial class RemotePlayer { public bool IsDead,HiddenByInvis; }
 static class Contract {
  static int n; static void Check(bool value,string label){n++;if(!value)throw new Exception(label);}
  static void Swing(CombatWorld c){c.Tick(.01f);c.Tick(.01f);Check(NetClient.I.Requests==1,"first request");}
  public static void Main(){
   var c=CombatWorld.Reset();Swing(c);c.Range=false;c.Tick(.1f);c.Range=true;c.Tick(.1f);c.Tick(.1f);
   Check(NetClient.I.Requests==1&&PlayerController.I.Swings==1,"moving boundary must not restart swing");
   Check(c.State==2,"moving boundary retains recovery");
   c.Sight=false;c.Tick(.1f);c.Sight=true;c.Tick(.1f);Check(NetClient.I.Requests==1,"LOS retains recovery");
   c.Tick(.51f);c.Tick(.01f);c.Tick(.01f);Check(NetClient.I.Requests==2,"recovery expires normally");
   c=CombatWorld.Reset();Swing(c);for(int i=0;i<9;i++){c.Repeat();Check(c.State==2,"same target preserves recovery state");c.Tick(.05f);c.Tick(.05f);}Check(NetClient.I.Requests==1,"repeated taps cannot reset recovery");
   c.Tick(.11f);c.Tick(.01f);c.Tick(.01f);Check(NetClient.I.Requests==2,"repeated input retains normal cadence");
   c=CombatWorld.Reset();Swing(c);c.Tick(1.01f);c.Tick(.01f);c.Tick(.01f);Check(NetClient.I.Requests==2,"stationary cadence unchanged");
   c=CombatWorld.Reset();c.Range=false;for(int i=0;i<30;i++)c.Tick(.1f);Check(NetClient.I.Requests==0,"out of range sends nothing");
   Check(PlayerController.I.ApplyApproachForIndependentRuntime(),"chase out of range");
   PlayerController.I.Steer(true);Check(!PlayerController.I.ApplyApproachForIndependentRuntime(),"joystick owns path during chase");
   c.Range=true;PlayerController.I.StopApproachForIndependentRuntime();Check(PlayerController.I.HasPath,"attack does not clear joystick path");
   PlayerController.I.IsMoving=true;c.Tick(.01f);c.Tick(.01f);Check(NetClient.I.Requests==0,"moving player does not swing");
   PlayerController.I.Steer(false);PlayerController.I.IsMoving=false;c.Tick(.01f);Check(NetClient.I.Requests==1,"release permits attack");
   c=CombatWorld.Reset();NetClient.I.ConnectedForIndependentRuntime=false;c.Tick(.01f);c.Tick(.01f);Check(NetClient.I.Requests==0&&PlayerController.I.Swings==0,"disconnected gates feedback");
   c=CombatWorld.Reset();CharData.riding=true;c.Tick(.01f);c.Tick(.01f);Check(NetClient.I.Requests==0&&PlayerController.I.Swings==0,"riding gates feedback");
   c=CombatWorld.Reset();Swing(c);CombatWorld.NotifyCast(.5f);Check(c.State==0,"skill cancels phase");c.Tick(.1f);Check(NetClient.I.Requests==1,"skill pause prevents swing");
   c.Pending();CombatWorld.StopAttack();Check(c.PendingCleared&&c.State==0,"explicit stop cancels skill intents");
   c=CombatWorld.Reset();Swing(c);c.Die();c.Tick(.1f);Check(c.State==0&&!c.AutoAttackingForIndependentRuntime,"death clears recovery");
   c=CombatWorld.Reset();c.Sight=false;c.Tick(.1f);c.Tick(.1f);Check(NetClient.I.Requests==0,"blocked LOS never requests");
   c=CombatWorld.Reset();c.CheckPvpRepeat();Check(true,"PvP repeat retains elapsed interval");
   Console.WriteLine("PASS "+n+" production attack timing assertions");
  }
 }
}
