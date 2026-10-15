using System;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
public static class MobileGroupWheelContract {
 static int checks;static bool sampleBeforeHud;
 static void Check(bool v,string label){if(!v)throw new Exception(label);checks++;}
 static Touch T(int id,TouchPhase p,float x,float y)=>new Touch{fingerId=id,phase=p,position=new Vector2(x,y)};
 static Touch Stick(TouchPhase p)=>T(10,p,180,100);
 static Touch Wheel(TouchPhase p,float dx=0,float dy=0)=>T(20,p,730+dx,70-dy);
 static void Frame(params Touch[] touches){Time.frameCount++;Input.touches=touches;if(sampleBeforeHud)ClientInput.Refresh();GameHud.I.Tick();ClientInput.Refresh();}
 static void Reset(){ClientInput.CancelAll();GameHud.I=new GameHud();MobileClientPresentation.ModalOpen=MobileClientPresentation.DiagramOpen=MobileClientPresentation.GroupWheelOpen=false;MobileClientPresentation.SafeAreaGui=new Rect(0,0,1000,1000);Frame();}
 public static void Main(){
  for(int group=0;group<4;group++)for(int button=0;button<6;button++)
   Check(MobileGroupWheel.HotbarSlot(group,button)==group*6+button,"24 exact mobile mappings");
  foreach(int invalid in new[]{-1,4,8,int.MaxValue})Check(MobileGroupWheel.HotbarSlot(invalid,0)==-1,"invalid group rejected");
  foreach(int invalid in new[]{-1,6,30,int.MaxValue})Check(MobileGroupWheel.HotbarSlot(0,invalid)==-1,"invalid button rejected");
  for(int group=0;group<4;group++)for(int button=0;button<6;button++){
   Reset();MobileGroupWheel.Direction(group,out float dx,out float dy);
   Frame(Stick(TouchPhase.Began),Wheel(TouchPhase.Began));Frame(Stick(TouchPhase.Stationary),Wheel(TouchPhase.Ended,dx*72,dy*72));Frame(Stick(TouchPhase.Stationary));
   int slot=group*6+button;GameHud.hotSkill[slot]=100+slot;GameHud.hotKind[slot]=0;SkillTargeting.Mode=SkillTargeting.Aim.Instant;NetClient.I=new NetClient();
   Vector2 center=MobileClientPresentation.SkillRect(button).center;
   Frame(Stick(TouchPhase.Stationary),T(21,TouchPhase.Began,center.x,1000-center.y));
   Check(GameHud.I.Owner==21&&GameHud.MobileShortcutPointerOwned,"each of six buttons owns touch");
   Frame(Stick(TouchPhase.Stationary),T(21,TouchPhase.Ended,center.x,1000-center.y));
   Check(NetClient.I.LastSkill==100+slot&&NetClient.I.Uses==1&&ClientInput.JoystickHeld,"all 24 buttons release exact binding while moving");
  }
  foreach(int slot in new[]{-1,24,29,30,int.MaxValue})Check(!GameHud.MobileHotSlotUsable(slot),"mobile rejects non-mobile slots without changing desktop data");
  // Same frame opening plus a third touch in otherwise-world space or an
  // underlying skill must not depend on HUD update or touch-array ordering.
  foreach(bool inputFirst in new[]{true,false})foreach(bool thirdFirst in new[]{true,false})foreach(float thirdY in new[]{175f,70f}){
   sampleBeforeHud=inputFirst;Reset();Frame(Stick(TouchPhase.Began));
   Touch third=T(30,TouchPhase.Began,thirdY==70?650:720,thirdY);
   if(thirdFirst)Frame(third,Stick(TouchPhase.Stationary),Wheel(TouchPhase.Began));
   else Frame(Wheel(TouchPhase.Began),Stick(TouchPhase.Stationary),third);
   Check(GameHud.I.Owner==20&&MobileClientPresentation.GroupWheelOpen,"same frame wheel wins third touch ordering");
   Check(!ClientInput.Pointer.Down&&!ClientInput.Pointer.Held&&!SkillTargeting.MobileHotSlotActive,"opening wheel consumes outer/skill third touch before world");
   Check(ClientInput.JoystickHeld,"opening never cancels existing joystick");
   Frame(Wheel(TouchPhase.Ended,72,0),Stick(TouchPhase.Stationary),T(30,TouchPhase.Ended,650,70));
   Check(!ClientInput.Pointer.Up&&GameHud.I.Page==1,"third release cannot leak or change selected group");
  }
  foreach(bool inputFirst in new[]{true,false})foreach(bool thirdFirst in new[]{true,false}){
   sampleBeforeHud=inputFirst;Reset();Frame(Stick(TouchPhase.Began),Wheel(TouchPhase.Began));
   if(thirdFirst)Frame(T(30,TouchPhase.Began,650,70),Stick(TouchPhase.Stationary),Wheel(TouchPhase.Ended,72,0));
   else Frame(Wheel(TouchPhase.Ended,72,0),Stick(TouchPhase.Stationary),T(30,TouchPhase.Began,650,70));
   Check(!ClientInput.Pointer.Down&&!ClientInput.Pointer.Held&&ClientInput.JoystickHeld,"closing frame outer touch remains UI regardless of sampling order");
   Frame(T(30,TouchPhase.Ended,650,70),Stick(TouchPhase.Stationary));Check(!ClientInput.Pointer.Up&&GameHud.I.Page==1,"closing frame third finger never leaks later release");
  }
  sampleBeforeHud=false;
  int[] order={0,1,2,3};
  for(int sector=0;sector<4;sector++){
   double angle=sector*Math.PI/2;float dx=(float)Math.Sin(angle)*72,dy=-(float)Math.Cos(angle)*72;
   Check(MobileGroupWheel.Hit(dx,dy,24)==order[sector],"clockwise mapping "+sector);
   MobileGroupWheel.Direction(order[sector],out float x,out float y);Check(Math.Abs(x*72-dx)<.01&&Math.Abs(y*72-dy)<.01,"render direction matches hit");
   foreach(bool wheelFirst in new[]{false,true}){
    Reset();if(wheelFirst){Frame(Wheel(TouchPhase.Began));Frame(Wheel(TouchPhase.Stationary),Stick(TouchPhase.Began));}else{Frame(Stick(TouchPhase.Began));Frame(Stick(TouchPhase.Stationary),Wheel(TouchPhase.Began));}
    Check(MobileClientPresentation.GroupWheelOpen&&GameHud.I.WheelHover==-1,"opens immediately at center");
    Frame(Stick(TouchPhase.Stationary),Wheel(TouchPhase.Moved,dx,dy),T(30,TouchPhase.Began,720,175));
    Check(GameHud.I.WheelHover==order[sector]&&GameHud.I.Page==0,"hover previews without commit, third finger inert");
    Check(ClientInput.JoystickHeld&&ClientInput.MovementAxis.x>.5f,"joystick continues during wheel");
    Frame(T(30,TouchPhase.Ended,720,175),Stick(TouchPhase.Stationary),Wheel(TouchPhase.Ended,dx,dy));
    Check(GameHud.I.Page==order[sector]&&!MobileClientPresentation.GroupWheelOpen&&GameHud.I.Owner==-1,"release commits and closes");
    Check(ClientInput.JoystickHeld&&GameHud.MobileShortcutPointerOwned,"joystick survives release and chat remains guarded");
    Frame(Stick(TouchPhase.Stationary));Check(!GameHud.MobileShortcutPointerOwned,"release guard clears next frame");
    Frame(Wheel(TouchPhase.Began),Stick(TouchPhase.Stationary));Frame(Wheel(TouchPhase.Moved,72,0),Stick(TouchPhase.Stationary));Frame(Wheel(TouchPhase.Ended),Stick(TouchPhase.Stationary));
    Check(GameHud.I.Page==order[sector],"return to center cancels without changing selected group");
   }
  }
  Check(MobileGroupWheel.Hit(24,0,24)==-1&&MobileGroupWheel.Hit(0,0,24)==-1,"inclusive dead zone");
  Check(MobileGroupWheel.ClampCenter(1,0,200,96)==96&&MobileGroupWheel.ClampCenter(199,0,200,96)==104,"safe edge clamping");
  Check(MobileGroupWheel.ClampCenter(1,10,100,96)==60,"tiny viewport centers safely");
  foreach(string interruption in new[]{"cancel","missing","focus","pause","disable","modal","diagram","resize","hide"}){
   Reset();Frame(Stick(TouchPhase.Began),Wheel(TouchPhase.Began));Frame(Stick(TouchPhase.Stationary),Wheel(TouchPhase.Moved,72,0));
   if(interruption=="cancel")Frame(Stick(TouchPhase.Stationary),Wheel(TouchPhase.Canceled,72,0));
   else if(interruption=="missing")Frame(Stick(TouchPhase.Stationary));
   else {
    if(interruption=="focus")GameHud.I.LoseFocus();if(interruption=="pause")GameHud.I.Pause();if(interruption=="disable")GameHud.I.Disable();
    if(interruption=="modal")MobileClientPresentation.ModalOpen=true;if(interruption=="diagram")MobileClientPresentation.DiagramOpen=true;
    if(interruption=="resize")MobileClientPresentation.SafeAreaGui=new Rect(0,0,900,1000);if(interruption=="hide")GameHud.I.showHotbar=false;
    Frame(Stick(TouchPhase.Stationary),Wheel(TouchPhase.Stationary,72,0));
   }
   Check(!MobileClientPresentation.GroupWheelOpen&&GameHud.I.WheelHover==-1&&GameHud.I.Page==0,"interruption resets wheel "+interruption);
   MobileClientPresentation.ModalOpen=MobileClientPresentation.DiagramOpen=false;GameHud.I.showHotbar=true;
   Frame(Stick(TouchPhase.Stationary),Wheel(TouchPhase.Ended,72,0));Check(GameHud.I.Page==0,"stale release inert "+interruption);
   Frame(Wheel(TouchPhase.Began),Stick(TouchPhase.Stationary));Frame(Wheel(TouchPhase.Ended,0,72),Stick(TouchPhase.Stationary));Check(GameHud.I.Page==2,"fresh wheel recovers "+interruption);
  }
  Reset();Frame(Wheel(TouchPhase.Began),Stick(TouchPhase.Began));Frame(Stick(TouchPhase.Ended),Wheel(TouchPhase.Moved,-72,0));Check(MobileClientPresentation.GroupWheelOpen&&!ClientInput.JoystickHeld,"stick release does not close wheel");Frame(Wheel(TouchPhase.Ended,-72,0));Check(GameHud.I.Page==3,"left selects group4 directly, never cycles");
  Console.WriteLine("PASS production group wheel/input contract: "+checks+" checks");
 }
}
