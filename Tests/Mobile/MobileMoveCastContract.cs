using System;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
public static class MobileMoveCastContract {
 static int checks;
 static bool sampleBeforeHud;
 static void Check(bool v,string label){if(!v)throw new Exception(label);checks++;}
 static Touch Stick(TouchPhase p)=>T(10,p,180,100);
 static Touch Skill(TouchPhase p,float x=720,float y=175)=>T(20,p,x,y);
 static Touch T(int id,TouchPhase p,float x,float y)=>new Touch{fingerId=id,phase=p,position=new Vector2(x,y)};
 static void Frame(params Touch[] touches){Time.frameCount++;Input.touches=touches;if(sampleBeforeHud)ClientInput.Refresh();GameHud.I.Tick();ClientInput.Refresh();}
 static void Reset(){ClientInput.CancelAll();GameHud.I=new GameHud();MobileClientPresentation.ModalOpen=MobileClientPresentation.DiagramOpen=false;MobileClientPresentation.SafeAreaGui=new Rect(0,0,1000,1000);GameHud.hotSkill[0]=1;GameSettings.SmartCast=true;SkillTargeting.Mode=SkillTargeting.Aim.Single;PlayerController.I.worldX=PlayerController.I.worldY=500;PlayerController.I.Stops=0;NetClient.I=new NetClient();CombatWorld.I.monsters.Clear();CombatWorld.I.monsters[1]=new MonsterEntity{id=1,worldX=550,worldY=500};TargetSelect.CurrentSourceForIndependentRuntime=CombatWorld.I.monsters[1];Frame();}
 static void Moving(string name){Check(ClientInput.JoystickHeld&&ClientInput.MovementAxis.x>.5f,name);Check(PlayerController.I.Stops==0,"cast never stops movement");}
 public static void Main(){
  foreach(bool sampleFirst in new[]{false,true})foreach(bool skillFirst in new[]{false,true})foreach(bool smart in new[]{false,true})foreach(bool drag in new[]{false,true}){
   sampleBeforeHud=sampleFirst;Reset();GameSettings.SmartCast=smart;
   if(skillFirst){Frame(Skill(TouchPhase.Began));Frame(Skill(TouchPhase.Stationary),Stick(TouchPhase.Began));}
   else {Frame(Stick(TouchPhase.Began));Frame(Stick(TouchPhase.Stationary),Skill(TouchPhase.Began));}
   Check(SkillTargeting.MobileHotSlotActive&&GameHud.I.Owner==20,"both touch orders acquire skill");Moving("move while aiming");Check(GameHud.MobileShortcutPointerOwned,"skill blocks synthesized chat press");
   float x=drag?780:720;Frame(Stick(TouchPhase.Stationary),Skill(TouchPhase.Moved,x));
   Frame(Skill(TouchPhase.Ended,x),Stick(TouchPhase.Stationary));
   Check(NetClient.I.Uses==(smart||drag?1:0),"smart tap/manual drag policy");Moving("move survives release");Check(!SkillTargeting.MobileHotSlotActive&&GameHud.I.Owner==-1,"skill release clears owner");Check(GameHud.MobileShortcutPointerOwned,"release frame still blocks chat");Check(GameHud.I.Page==0,"movement and skills never switch page");
   Frame(Stick(TouchPhase.Stationary));Check(!GameHud.MobileShortcutPointerOwned,"chat guard clears after release");Check(!ClientInput.Pointer.Down&&!ClientInput.Pointer.Up&&!ClientInput.Pointer.Held,"skill does not leak into world");
   Frame(Stick(TouchPhase.Ended));Check(!ClientInput.JoystickHeld&&ClientInput.MovementAxis.x==0,"stick release stops only stick");
  }
  Reset();Frame(Stick(TouchPhase.Began),Skill(TouchPhase.Began));Frame(Stick(TouchPhase.Ended),Skill(TouchPhase.Moved,780));Check(SkillTargeting.MobileHotSlotActive&&!ClientInput.JoystickHeld,"stick release preserves skill");Frame(Skill(TouchPhase.Ended,780));Check(NetClient.I.Uses==1,"skill releases after stick lifted");
  Reset();Frame(Skill(TouchPhase.Began),Stick(TouchPhase.Began));Frame(Skill(TouchPhase.Moved,780),Stick(TouchPhase.Stationary),T(30,TouchPhase.Began,720,75));Check(GameHud.I.Owner==20&&GameHud.I.Page==0,"third page touch cannot steal skill");Frame(T(30,TouchPhase.Moved,660,75),Skill(TouchPhase.Ended,780),Stick(TouchPhase.Stationary));Check(NetClient.I.Uses==1&&GameHud.I.Page==0,"third touch cannot change release or page");Frame(T(30,TouchPhase.Ended,660,75),Stick(TouchPhase.Stationary));Check(GameHud.I.Page==0&&NetClient.I.Uses==1,"ignored third release stays inert");
  Reset();Frame(Stick(TouchPhase.Began),Skill(TouchPhase.Began));Frame(Stick(TouchPhase.Stationary),T(30,TouchPhase.Began,780,175),Skill(TouchPhase.Moved,780));Check(GameHud.I.Owner==20,"third skill finger cannot steal aim");Frame(Stick(TouchPhase.Stationary),T(30,TouchPhase.Canceled,780,175),Skill(TouchPhase.Stationary,780));Check(SkillTargeting.MobileHotSlotActive&&NetClient.I.Uses==0,"canceling non-owner cannot cancel or cast");Frame(Stick(TouchPhase.Canceled),Skill(TouchPhase.Stationary,780));Check(SkillTargeting.MobileHotSlotActive&&!ClientInput.JoystickHeld,"canceling stick preserves skill");Frame(Skill(TouchPhase.Ended,780));Check(NetClient.I.Uses==1,"owner can cast after other fingers cancel");
  foreach(string interruption in new[]{"cancel","cancel-zone","missing","focus","pause","disable","modal","diagram","map","resize"}){
   Reset();Frame(Stick(TouchPhase.Began),Skill(TouchPhase.Began));
   if(interruption=="cancel")Frame(Stick(TouchPhase.Stationary),Skill(TouchPhase.Canceled));
   else if(interruption=="cancel-zone")Frame(Stick(TouchPhase.Stationary),Skill(TouchPhase.Ended,450,550));
   else if(interruption=="missing")Frame(Stick(TouchPhase.Stationary));
   else {
    if(interruption=="focus"){GameHud.I.LoseFocus();ClientInput.CancelAll();}
    if(interruption=="pause"){GameHud.I.Pause();ClientInput.CancelAll();}
    if(interruption=="disable"){GameHud.I.Disable();ClientInput.CancelAll();}
    if(interruption=="modal")MobileClientPresentation.ModalOpen=true;
    if(interruption=="diagram")MobileClientPresentation.DiagramOpen=true;
    if(interruption=="map")ClientInput.CancelAll();
    if(interruption=="resize")MobileClientPresentation.SafeAreaGui=new Rect(0,0,900,1000);
    Frame(Stick(TouchPhase.Stationary),Skill(TouchPhase.Stationary));
   }
   Check(!SkillTargeting.MobileHotSlotActive,"interruption cancels cast: "+interruption);
   MobileClientPresentation.ModalOpen=MobileClientPresentation.DiagramOpen=false;
   Frame(Stick(TouchPhase.Stationary),Skill(TouchPhase.Ended,780));Check(NetClient.I.Uses==0,"no stale cast after "+interruption);
   if(interruption=="cancel"||interruption=="cancel-zone"||interruption=="missing")Moving("skill-only cancel preserves stick");
   Frame(Stick(TouchPhase.Ended));Frame(Stick(TouchPhase.Began),Skill(TouchPhase.Began));Frame(Stick(TouchPhase.Stationary),Skill(TouchPhase.Ended));Check(NetClient.I.Uses==1,"fresh input recovers after "+interruption);
  }
  Reset();SkillTargeting.Mode=SkillTargeting.Aim.Circle;Frame(Stick(TouchPhase.Began),Skill(TouchPhase.Began));Frame(Stick(TouchPhase.Stationary),Skill(TouchPhase.Ended,990));Check(NetClient.I.GroundUses==1&&NetClient.I.X<=600,"drag ground clamp while moving");Moving("ground cast preserves stick");
  Reset();SkillTargeting.Mode=SkillTargeting.Aim.Instant;GameSettings.SmartCast=false;Frame(Stick(TouchPhase.Began),Skill(TouchPhase.Began));Frame(Stick(TouchPhase.Stationary),Skill(TouchPhase.Ended));Check(NetClient.I.Uses==1&&NetClient.I.LastId==0,"self tap while moving in manual mode");Moving("self cast preserves stick");
  Reset();CombatWorld.I.monsters[1].worldX=900;Frame(Stick(TouchPhase.Began),Skill(TouchPhase.Began));Frame(Stick(TouchPhase.Stationary),Skill(TouchPhase.Ended));Check(NetClient.I.Uses==0&&CombatWorld.I.Approaches==0,"out of range never chases");Moving("failed cast preserves stick");
  Console.WriteLine("PASS production mobile HUD/input/cast integration: "+checks+" checks");
 }
}
